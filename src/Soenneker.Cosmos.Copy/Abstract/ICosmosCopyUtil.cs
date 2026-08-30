using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Cosmos.Copy.Dtos;

namespace Soenneker.Cosmos.Copy.Abstract;

/// <summary>
/// A utility to copy to and from Cosmos databases and containers
/// </summary>
public interface ICosmosCopyUtil
{
    /// <summary>
    /// Replaces the destination database's containers with copies of the selected source containers and their items.
    /// Destination containers use the copy utility's standard container configuration; source indexing, throughput, and other container settings are not cloned.
    /// </summary>
    /// <param name="sourceEndpoint">source Endpoint to read or transform.</param>
    /// <param name="sourceAccountKey">source Account Key to read or transform.</param>
    /// <param name="sourceDatabaseName">source Database Name to read or transform.</param>
    /// <param name="destinationEndpoint">destination Endpoint that receives the result.</param>
    /// <param name="destinationAccountKey">destination Account Key that receives the result.</param>
    /// <param name="destinationDatabaseName">destination Database Name that receives the result.</param>
    /// <param name="cutoffUtc">When specified, copies only documents whose <c>createdAt</c> value is at or after this time.</param>
    /// <param name="numTasks">Maximum number of pending destination upserts in each batch.</param>
    /// <param name="containerConfigs">Optional exclusions and per-container cutoff overrides, matched by container name without regard to case.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the copy database operation is complete.</returns>
    ValueTask CopyDatabase(string sourceEndpoint, string sourceAccountKey, string sourceDatabaseName, string destinationEndpoint, string destinationAccountKey,
        string destinationDatabaseName, DateTimeOffset? cutoffUtc = null, int numTasks = 50, IEnumerable<ContainerCopyConfig>? containerConfigs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts items from a source container into a destination container, creating the destination container when needed.
    /// </summary>
    /// <param name="sourceEndpoint">source Endpoint to read or transform.</param>
    /// <param name="sourceAccountKey">source Account Key to read or transform.</param>
    /// <param name="sourceDatabaseName">source Database Name to read or transform.</param>
    /// <param name="sourceContainerName">source Container Name to read or transform.</param>
    /// <param name="destinationEndpoint">destination Endpoint that receives the result.</param>
    /// <param name="destinationAccountKey">destination Account Key that receives the result.</param>
    /// <param name="destinationDatabaseName">destination Database Name that receives the result.</param>
    /// <param name="destinationContainerName">destination Container Name that receives the result.</param>
    /// <param name="cutoffUtc">When specified, copies only documents whose <c>createdAt</c> value is at or after this time.</param>
    /// <param name="numTasks">Maximum number of pending destination upserts in each batch.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the copy container operation is complete.</returns>
    ValueTask CopyContainer(string sourceEndpoint, string sourceAccountKey, string sourceDatabaseName, string sourceContainerName, string destinationEndpoint,
        string destinationAccountKey, string destinationDatabaseName, string destinationContainerName, DateTimeOffset? cutoffUtc = null, int numTasks = 50,
        CancellationToken cancellationToken = default);
}
