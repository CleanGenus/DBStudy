using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBOptimizationStudy.Services
{
    /// <summary>
    /// 数据库连接和基础操作服务
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        public string ConnectionString => _connectionString;

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("数据库连接成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接失败");
                return false;
            }
        }

        /// <summary>
        /// 创建数据库（如果不存在）
        /// </summary>
        public async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                var databaseName = builder.InitialCatalog;
                builder.InitialCatalog = "master";

                using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();

                var checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";
                using var checkCommand = new SqlCommand(checkDbQuery, connection);
                var dbExists = (int)await checkCommand.ExecuteScalarAsync() > 0;

                if (!dbExists)
                {
                    var createDbQuery = $"CREATE DATABASE [{databaseName}]";
                    using var createCommand = new SqlCommand(createDbQuery, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation($"数据库 {databaseName} 创建成功");
                }
                else
                {
                    _logger.LogInformation($"数据库 {databaseName} 已存在");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建数据库失败");
                throw;
            }
        }

        /// <summary>
        /// 执行SQL脚本
        /// </summary>
        public async Task ExecuteScriptAsync(string script)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 分割脚本以处理GO语句
                var batches = script.Split(new[] { "GO\r\n", "GO\n", "GO" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var batch in batches)
                {
                    if (!string.IsNullOrWhiteSpace(batch))
                    {
                        using var command = new SqlCommand(batch.Trim(), connection);
                        command.CommandTimeout = 300; // 5分钟超时
                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("SQL脚本执行成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL脚本失败");
                throw;
            }
        }

        /// <summary>
        /// 获取表记录数
        /// </summary>
        public async Task<long> GetTableRowCountAsync(string tableName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = $"SELECT COUNT(*) FROM {tableName}";
                using var command = new SqlCommand(query, connection);
                return (long)await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取表 {tableName} 记录数失败");
                return 0;
            }
        }
    }
}
