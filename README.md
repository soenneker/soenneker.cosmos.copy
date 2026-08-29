[![](https://img.shields.io/nuget/v/soenneker.cosmos.copy.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.cosmos.copy/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.copy/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.copy/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.cosmos.copy.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.cosmos.copy/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.copy/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.copy/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Copy

A utility to copy to and from Cosmos databases and containers.

## Install

```bash
dotnet add package Soenneker.Cosmos.Copy
```

## Quick start

```csharp
using Soenneker.Cosmos.Copy.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddCosmosCopyUtilAsSingleton();
```

Adds `ICosmosCopyUtil` as a singleton service.

## What you get

- `ICosmosCopyUtil` — A utility to copy to and from Cosmos databases and containers.
- `CosmosCopyUtilRegistrar` — A utility to copy to and from Cosmos databases and containers.
- `ContainerCopyConfig` — Configuration for copying a specific container, including optional cutoff time and exclusion flag.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `ICosmosCopyUtil.CopyDatabase(sourceEndpoint, sourceAccountKey, sourceDatabaseName, destinationEndpoint, destinationAccountKey, destinationDatabaseName, cutoffUtc, numTasks, containerConfigs, cancellationToken)` | Copies all containers and their items from a source database to a destination database. Prior to copying, all existing containers in the destination database are deleted, then recreated to match the source. Optionally filters items by createdAt >= cutoffUtc (global default, can be overridden per container). Optionally configures per-container cutoff times and exclusion via containerConfigs. | A task that completes when the copy database operation is complete. |
| `ICosmosCopyUtil.CopyContainer(sourceEndpoint, sourceAccountKey, sourceDatabaseName, sourceContainerName, destinationEndpoint, destinationAccountKey, destinationDatabaseName, destinationContainerName, cutoffUtc, numTasks, cancellationToken)` | Copies items from a source container to a destination container. Optionally filters items by createdAt >= cutoffUtc. Containers are created in the destination if they do not exist. | A task that completes when the copy container operation is complete. |
| `CosmosCopyUtilRegistrar.AddCosmosCopyUtilAsSingleton(services)` | Adds `ICosmosCopyUtil` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `CosmosCopyUtilRegistrar.AddCosmosCopyUtilAsScoped(services)` | Adds `ICosmosCopyUtil` as a scoped service. | The same service collection, so additional registrations can be chained. |
| `ContainerCopyConfig.ContainerName` | The name of the container to configure. | The name of the container to configure. |
| `ContainerCopyConfig.CutoffUtc` | Optional cutoff time for filtering items by createdAt. If null, uses the global cutoff time or no filter. | Optional cutoff time for filtering items by createdAt. If null, uses the global cutoff time or no filter. |
| `ContainerCopyConfig.Exclude` | If true, this container will be excluded from the copy operation. | If true, this container will be excluded from the copy operation. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
