# Testing Guide

## Overview

Tests use **xunit v3** with **FunFair.Test.Common** and **NSubstitute** for mocking. The test project is `src/Credfeto.Defi.Server.Tests`.

## Running Tests

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/
```

**268 tests, all passing.**

## Coverage

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/ \
  -- --coverage --coverage-output-format cobertura \
     --coverage-output /tmp/coverage/Credfeto.Defi.Server.coverage.cobertura.xml
```

Generate an HTML report:

```sh
dotnet reportgenerator \
  -reports:/tmp/coverage/Credfeto.Defi.Server.coverage.cobertura.xml \
  -targetdir:/tmp/coverage/report \
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
