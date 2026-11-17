using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;
using VisitorCounterApi.Configuration;

namespace VisitorCounterApi.Data
{
    // The only class that knows about SQL.
    public sealed class SqlVisitorCounterRepository : IVisitorCounterRepository
    {
        // One atomic statement: add 1 and return the new value.
        // No user input is used, so SQL injection is not possible here.
        private const string IncrementSql = @"
        UPDATE dbo.VisitorCount
        SET VisitCount = VisitCount + 1
        OUTPUT INSERTED.VisitCount
        WHERE Id = 1;";

        private readonly DatabaseSettings _settings;

        public SqlVisitorCounterRepository(DatabaseSettings settings)
        {
            _settings = settings;
        }

        public async Task<int> IncrementAndGetAsync(CancellationToken cancellationToken = default)
        {
            SqlConnection connection = new SqlConnection(_settings.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            SqlCommand command = new SqlCommand(IncrementSql, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null)
            {
                throw new InvalidOperationException("Counter row (Id = 1) was not found in dbo.VisitorCount.");
            }

            return Convert.ToInt32(result);
        }
    }
}