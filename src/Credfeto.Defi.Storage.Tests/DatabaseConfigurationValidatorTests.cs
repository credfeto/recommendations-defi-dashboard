using Credfeto.Defi.Storage.Configuration;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class DatabaseConfigurationValidatorTests : TestBase
{
    private readonly DatabaseConfigurationValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceConnectionString_ReturnsFailure(string connectionString)
    {
        DatabaseConfiguration config = new() { ConnectionString = connectionString };

        ValidateOptionsResult result = this._validator.Validate(name: null, options: config);

        Assert.True(result.Failed, userMessage: "Validation should fail when connection string is empty or whitespace");
    }

    [Fact]
    public void Validate_WithValidConnectionString_ReturnsSuccess()
    {
        DatabaseConfiguration config = new()
        {
            ConnectionString = "Server=(local);Database=test;Integrated Security=true;",
        };

        ValidateOptionsResult result = this._validator.Validate(name: null, options: config);

        Assert.True(result.Succeeded, userMessage: "Validation should succeed when connection string is set");
    }
}
