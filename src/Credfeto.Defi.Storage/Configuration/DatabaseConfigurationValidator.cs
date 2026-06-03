using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Storage.Configuration;

public sealed class DatabaseConfigurationValidator : IValidateOptions<DatabaseConfiguration>
{
    public ValidateOptionsResult Validate(string? name, DatabaseConfiguration options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Database ConnectionString must be configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
