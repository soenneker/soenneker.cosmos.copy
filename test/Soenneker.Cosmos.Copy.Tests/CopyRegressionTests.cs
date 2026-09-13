using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Soenneker.Cosmos.Container.Abstract;
using Soenneker.Cosmos.Container.Setup.Abstract;

namespace Soenneker.Cosmos.Copy.Tests;

public class CopyRegressionTests
{
    [Test]
    public async Task CopyUsesFreedWorkerBeforeSlowWriteFinishes()
    {
        var slow = new TaskCompletionSource<ItemResponse<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var destination = new Mock<Microsoft.Azure.Cosmos.Container>();
        var response = Mock.Of<ItemResponse<JsonElement>>();
        int active = 0, peak = 0, completed = 0;
        destination.Setup(c => c.UpsertItemAsync(It.IsAny<JsonElement>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(async (JsonElement item, PartitionKey? pk, ItemRequestOptions options, CancellationToken ct) =>
            {
                options.EnableContentResponseOnWrite.Should().BeFalse();
                int current = Interlocked.Increment(ref active);
                int previous;
                do { previous = Volatile.Read(ref peak); } while (current > previous && Interlocked.CompareExchange(ref peak, current, previous) != previous);
                if (item.GetInt32() == 0) await slow.Task.WaitAsync(ct);
                if (item.GetInt32() == 2) thirdStarted.TrySetResult();
                Interlocked.Decrement(ref active);
                Interlocked.Increment(ref completed);
                return response;
            });
        var util = Create(destination.Object, [0, 1, 2, 3]);
        Task copying = util.CopyContainer("source", "key", "db", "container", "dest", "key", "db", "container", numTasks: 2).AsTask();
        try
        {
            await thirdStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            copying.IsCompleted.Should().BeFalse();
        }
        finally { slow.TrySetResult(response); await copying; }
        completed.Should().Be(4);
        peak.Should().Be(2);
        active.Should().Be(0);
    }

    [Test]
    public async Task InvalidConcurrencyDoesNotDeleteDestination()
    {
        var containers = new Mock<ICosmosContainerUtil>(MockBehavior.Strict);
        var util = new CosmosCopyUtil(NullLogger<CosmosCopyUtil>.Instance, containers.Object, Mock.Of<ICosmosContainerSetupUtil>());
        Func<Task> run = async () => await util.CopyDatabase("https://source", "key", "db", "https://dest", "key", "db", numTasks: 0);
        await run.Should().ThrowAsync<ArgumentOutOfRangeException>();
        containers.VerifyNoOtherCalls();
    }

    [Test]
    public async Task FailedWriteSettlesOtherWorkersBeforeReturning()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool settled = false;
        var destination = new Mock<Microsoft.Azure.Cosmos.Container>();
        destination.Setup(c => c.UpsertItemAsync(It.IsAny<JsonElement>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(async (JsonElement item, PartitionKey? pk, ItemRequestOptions options, CancellationToken ct) =>
            {
                if (item.GetInt32() == 0)
                {
                    started.TrySetResult();
                    try { await release.Task; }
                    finally { settled = true; }
                    return Mock.Of<ItemResponse<JsonElement>>();
                }
                await started.Task;
                throw new InvalidOperationException("write failed");
            });
        var util = Create(destination.Object, [0, 1]);
        Task copying = util.CopyContainer("source", "key", "db", "container", "dest", "key", "db", "container", numTasks: 2).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        copying.IsCompleted.Should().BeFalse();
        release.TrySetResult();
        Func<Task> run = () => copying;
        await run.Should().ThrowAsync<InvalidOperationException>();
        settled.Should().BeTrue();
    }

    private static CosmosCopyUtil Create(Microsoft.Azure.Cosmos.Container destination, int[] values)
    {
        var source = new Mock<Microsoft.Azure.Cosmos.Container>();
        source.Setup(c => c.GetItemQueryIterator<JsonElement>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
            .Returns(new Iterator(values.Select(v => JsonSerializer.SerializeToElement(v)).ToArray()));
        var containers = new Mock<ICosmosContainerUtil>();
        containers.Setup(c => c.Get("source", "key", "db", "container", It.IsAny<CancellationToken>())).Returns(new ValueTask<Microsoft.Azure.Cosmos.Container>(source.Object));
        var response = new Mock<ContainerResponse>();
        response.SetupGet(r => r.Container).Returns(destination);
        var setup = new Mock<ICosmosContainerSetupUtil>();
        setup.Setup(s => s.Ensure("dest", "key", "db", "container", It.IsAny<CancellationToken>())).Returns(new ValueTask<ContainerResponse?>(response.Object));
        return new CosmosCopyUtil(NullLogger<CosmosCopyUtil>.Instance, containers.Object, setup.Object);
    }

    private sealed class Iterator(JsonElement[] items) : FeedIterator<JsonElement>
    {
        private bool _read;
        public override bool HasMoreResults => !_read;
        public override Task<FeedResponse<JsonElement>> ReadNextAsync(CancellationToken cancellationToken = default)
        { _read = true; return Task.FromResult<FeedResponse<JsonElement>>(new Response(items)); }
    }
    private sealed class Response(JsonElement[] items) : FeedResponse<JsonElement>
    {
        public override string ContinuationToken => null!;
        public override int Count => items.Length;
        public override string IndexMetrics => null!;
        public override Headers Headers => null!;
        public override IEnumerable<JsonElement> Resource => items;
        public override HttpStatusCode StatusCode => HttpStatusCode.OK;
        public override CosmosDiagnostics Diagnostics => null!;
        public override IEnumerator<JsonElement> GetEnumerator() => ((IEnumerable<JsonElement>)items).GetEnumerator();
    }
}
