using Dapper;
using MySqlConnector;

namespace MessageProcessingSystemProcessor.Services
{
    public class HashInsertService
    {
        private readonly string _connectionString;

        public HashInsertService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MariaDb");
        }

        public async Task SaveHashAsync(string sha1)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = "INSERT INTO hornethashes (date, sha1) VALUES (@Date, @Sha1)";
            await connection.ExecuteAsync(sql, new { Date = DateTime.UtcNow.Date, Sha1 = sha1 });
        }

        public async Task SaveHashBatchAsync(List<string> hashes)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            var sql = "INSERT INTO hornethashes (date, sha1) VALUES ";

            var values = hashes.Select(h => $"('{DateTime.UtcNow:yyyy-MM-dd}', '{h}')");
            sql += string.Join(",", values);

            using var command = new MySqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
    }
}
