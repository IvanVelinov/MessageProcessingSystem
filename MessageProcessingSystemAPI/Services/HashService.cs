using Dapper;
using MySqlConnector;
using Shared.Models;

namespace MessageProcessingSystemAPI.Services
{
    public class HashService
    {
        private readonly string _connectionString;

        public HashService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MariaDb");
        }

        /// <summary>
        /// Retrieves the number of inserted hashes grouped by date.
        /// </summary>
        /// <returns>List of hash counts per day.</returns>
        public async Task<List<HashCountDto>> GetHashCountsAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = @"
            SELECT DATE(date) AS Date, COUNT(*) AS Count
            FROM hornethashes
            GROUP BY DATE(date)
            ORDER BY DATE(date);
        ";

            var results = await connection.QueryAsync<HashCountDto>(sql);
            return results.ToList();
        }
    }
}
