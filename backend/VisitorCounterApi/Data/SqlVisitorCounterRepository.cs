using Microsoft.Data.SqlClient;
using VisitorCounterApi.Configuration;

namespace VisitorCounterApi.Data;

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

    // A paused serverless database needs time to wake up and rejects the first logins
    // with a temporary ("transient") error, so we try again a few times.
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly DatabaseSettings _settings;

    public SqlVisitorCounterRepository(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<int> IncrementAndGetAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await TryIncrementAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.IsTransient && attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private async Task<int> TryIncrementAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(IncrementSql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Counter row (Id = 1) was not found in dbo.VisitorCount.");
        }

        return Convert.ToInt32(result);
    }
}