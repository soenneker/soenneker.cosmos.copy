[![](https://img.shields.io/nuget/v/soenneker.cosmos.copy.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.cosmos.copy/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.copy/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.copy/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.cosmos.copy.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.cosmos.copy/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.copy/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.copy/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Copy

Copies documents between Azure Cosmos DB containers or replaces the contents of one database from another.

## Installation

```bash
dotnet add package Soenneker.Cosmos.Copy
```

## Registration

```csharp
using Soenneker.Cosmos.Copy.Abstract;
using Soenneker.Cosmos.Copy.Registrars;

services.AddCosmosCopyUtilAsSingleton();

ICosmosCopyUtil copy = serviceProvider.GetRequiredService<ICosmosCopyUtil>();
```

`AddCosmosCopyUtilAsScoped()` is also available. Both registrations add the Cosmos suite dependencies as singletons.

## Copy a container

```csharp
await copy.CopyContainer(
    sourceEndpoint: sourceEndpoint,
    sourceAccountKey: sourceKey,
    sourceDatabaseName: "production",
    sourceContainerName: "orders",
    destinationEndpoint: destinationEndpoint,
    destinationAccountKey: destinationKey,
    destinationDatabaseName: "staging",
    destinationContainerName: "orders",
    cutoffUtc: DateTimeOffset.UtcNow.AddDays(-30),
    numTasks: 25,
    cancellationToken: cancellationToken);
```

Documents are upserted, so documents already present in the destination with the same `id` and partition key are replaced. Existing destination documents that are not returned by the source query remain in place.

When `cutoffUtc` is supplied, the source query requires a `createdAt` property and copies documents where `createdAt >= cutoffUtc`. Omit it to copy every document.

## Replace a database

```csharp
var containerOptions = new[]
{
    new ContainerCopyConfig { ContainerName = "audit", Exclude = true },
    new ContainerCopyConfig
    {
        ContainerName = "orders",
        CutoffUtc = DateTimeOffset.UtcNow.AddDays(-7)
    }
};

await copy.CopyDatabase(
    sourceEndpoint,
    sourceKey,
    "production",
    destinationEndpoint,
    destinationKey,
    "staging",
    cutoffUtc: DateTimeOffset.UtcNow.AddDays(-30),
    containerConfigs: containerOptions,
    cancellationToken: cancellationToken);
```

`CopyDatabase` is destructive: it deletes every container in the destination database before copying. An excluded source container is not recreated. The source and destination database cannot be the same.

A per-container cutoff overrides the global cutoff. Container names in `containerConfigs` are matched case-insensitively and must be unique.

## Container compatibility

Destination containers are created with `/partitionKey` as the partition-key path and without dedicated throughput. The utility copies documents, not the source container's partition-key definition, indexing policy, unique keys, TTL, throughput, or other settings. Use it only when `/partitionKey` is valid for the documents being copied, and configure any additional destination settings separately.

The `numTasks` argument controls the number of upserts awaited together; it must be at least `1`. Copy failures and cancellation propagate to the caller. Completed deletes and upserts are not rolled back.
