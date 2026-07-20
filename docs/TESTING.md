# Testing Guide

## Overview

Tests use **xunit v3** with **FunFair.Test.Common** and **NSubstitute** for mocking. The test projects are
`src/Credfeto.Defi.Server.Tests` and `src/Credfeto.Defi.Server.Composition.Tests`.

`Credfeto.Defi.Server.Composition` holds the host-agnostic DI registration (`AddDefiBusinessServices`) and
endpoint handler bodies (`HealthEndpointHandlers`, `PoolsEndpointHandlers`) split out of `Credfeto.Defi.Server`
so they can be exercised directly in unit tests without a real Kestrel host or `WebApplicationFactory`.

## Running Tests

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/

dotnet test Credfeto.Defi.Server.Composition.Tests/Credfeto.Defi.Server.Composition.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/
```

## Coverage

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/ \
  -- --coverage --coverage-output-format cobertura \
     --coverage-output /tmp/coverage/Credfeto.Defi.Server.coverage.cobertura.xml

dotnet test Credfeto.Defi.Server.Composition.Tests/Credfeto.Defi.Server.Composition.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/ \
  -- --coverage --coverage-output-format cobertura \
     --coverage-output /tmp/coverage/Credfeto.Defi.Server.Composition.coverage.cobertura.xml
```

Generate an HTML report per assembly (`Credfeto.Defi.Server` references `Credfeto.Defi.Server.Composition`, so
their cobertura files must never be passed to the same `reportgenerator` run - doing so would attribute
`Composition`'s covered lines to `Server`'s report and understate `Server`'s real coverage):

```sh
dotnet reportgenerator \
  -reports:/tmp/coverage/Credfeto.Defi.Server.coverage.cobertura.xml \
  -targetdir:/tmp/coverage/Credfeto.Defi.Server \
  -reporttypes:Html

dotnet reportgenerator \
  -reports:/tmp/coverage/Credfeto.Defi.Server.Composition.coverage.cobertura.xml \
  -targetdir:/tmp/coverage/Credfeto.Defi.Server.Composition \
  -reporttypes:Html
```

If a combined view is needed (e.g. for a summary dashboard), generate it as a separate step after the
per-assembly reports above, not instead of them:

```sh
dotnet reportgenerator \
  -reports:"/tmp/coverage/*.coverage.cobertura.xml" \
  -targetdir:/tmp/coverage/combined \
  -reporttypes:Html
```

## Test Patterns

All test classes derive from `FunFair.Test.Common.TestBase`. Use `GetSubstitute<T>()` (not `Substitute.For<T>()`) for mocks:

```csharp
public sealed class MyServiceTests : TestBase
{
    [Fact]
    public void MyTest()
    {
        IMyDependency dep = GetSubstitute<IMyDependency>();
        // ...
    }
}
```

Logger mocks use `GetTypedLogger<T>()` (instance method):

```csharp
ILogger<MyService> logger = this.GetTypedLogger<MyService>();
```

## Test File Naming

| Test type | Pattern |
| --- | --- |
| Unit | `<Subject>Tests.cs` |
| Extended / edge-case | `<Subject>ExtendedTests.cs` |

## Tools

- **xunit v3** via `xunit.v3.mtp-v2`
- **FunFair.Test.Common** for `TestBase`, logger helpers, and DI test patterns
- **FunFair.Test.Source.Generator** for source-generated test boilerplate
- **Microsoft.Extensions.TimeProvider.Testing** for `FakeTimeProvider`
- **Microsoft.Testing.Extensions.CodeCoverage** for coverage collection
