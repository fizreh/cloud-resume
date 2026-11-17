using Microsoft.Extensions.Configuration;
using System;

namespace VisitorCounterApi.Configuration
{
    // A class (not a record), because a record's automatic ToString() could print the secret in logs.
    public sealed class DatabaseSettings
    {
        public string ConnectionString { get; }

        private DatabaseSettings(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Fail fast: if the setting is missing, the app tells you immediately and clearly.
        public static DatabaseSettings FromConfiguration(IConfiguration configuration)
        {
            var value = configuration[SettingNames.SqlConnectionString];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Missing setting '{SettingNames.SqlConnectionString}'. " +
                    "Locally: add it to the .env file. In Azure: add it to the Function App configuration.");
            }

            return new DatabaseSettings(value);
        }
    }
}