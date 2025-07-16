using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using DBOptimizationStudy.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBOptimizationStudy.Services
{
    /// <summary>
    /// 数据生成服务 - 用于生成百万级测试数据
    /// </summary>
    public class DataGenerationService
    {
        private readonly DatabaseService _databaseService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataGenerationService> _logger;
        private readonly int _batchSize;

        public DataGenerationService(
            DatabaseService databaseService,
            IConfiguration configuration,
            ILogger<DataGenerationService> logger)
        {
            _databaseService = databaseService;
            _configuration = configuration;
            _logger = logger;
            _batchSize = _configuration.GetValue<int>("DatabaseSettings:BatchSize", 10000);
        }

        /// <summary>
        /// 生成部门数据
        /// </summary>
        public async Task GenerateDepartmentsAsync(int count = 100)
        {
            _logger.LogInformation($"开始生成 {count} 条部门数据...");
            var stopwatch = Stopwatch.StartNew();

            var departmentFaker = new Faker<Department>()
                .RuleFor(d => d.Name, f => TruncateString(f.Commerce.Department(), 90))
                .RuleFor(d => d.Description, f => TruncateString(f.Lorem.Sentence(), 450))
                .RuleFor(d => d.CreatedDate, f => f.Date.Past(2))
                .RuleFor(d => d.IsActive, f => f.Random.Bool(0.9f));

            var departments = departmentFaker.Generate(count);

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            foreach (var department in departments)
            {
                try
                {
                    var query = @"
                        INSERT INTO Departments (Name, Description, CreatedDate, IsActive)
                        VALUES (@Name, @Description, @CreatedDate, @IsActive)";

                    using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", TruncateString(department.Name, 100));
                    command.Parameters.AddWithValue("@Description", TruncateString(department.Description, 500));
                    command.Parameters.AddWithValue("@CreatedDate", department.CreatedDate);
                    command.Parameters.AddWithValue("@IsActive", department.IsActive);

                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, $"插入部门数据失败: Name={department.Name}");

                    if (ex.Number == 8152)
                    {
                        _logger.LogError("部门数据截断错误");
                        _logger.LogError($"部门数据: Name长度={department.Name?.Length}, Description长度={department.Description?.Length}");
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"插入部门数据时发生未知错误: {department.Name}");
                    continue;
                }
            }

            stopwatch.Stop();
            _logger.LogInformation($"部门数据生成完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 生成用户数据
        /// </summary>
        public async Task GenerateUsersAsync(int count = 1000000)
        {
            _logger.LogInformation($"开始生成 {count} 条用户数据...");
            var stopwatch = Stopwatch.StartNew();

            // 获取部门ID列表
            var departmentIds = await GetDepartmentIdsAsync();
            if (!departmentIds.Any())
            {
                throw new InvalidOperationException("请先生成部门数据");
            }

            var userFaker = new Faker<User>()
                .RuleFor(u => u.FirstName, f => TruncateString(f.Name.FirstName(), 90)) // 留有缓冲
                .RuleFor(u => u.LastName, f => TruncateString(f.Name.LastName(), 90))
                .RuleFor(u => u.Email, f =>
                {
                    // 生成更短的邮箱地址，避免过长
                    var firstName = TruncateString(f.Name.FirstName(), 15);
                    var lastName = TruncateString(f.Name.LastName(), 15);
                    var domain = f.PickRandom(new[] { "gmail.com", "outlook.com", "yahoo.com", "example.com" });
                    return $"{firstName.ToLower()}.{lastName.ToLower()}@{domain}";
                })
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
                .RuleFor(u => u.DateOfBirth, f => f.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18)))
                .RuleFor(u => u.CreatedDate, f => f.Date.Past(2))
                .RuleFor(u => u.LastLoginDate, f => f.Random.Bool(0.7f) ? f.Date.Recent(30) : null)
                .RuleFor(u => u.City, f => TruncateString(f.Address.City(), 90))
                .RuleFor(u => u.State, f => TruncateString(f.Address.State(), 90))
                .RuleFor(u => u.Country, f => TruncateString(f.Address.Country(), 90))
                .RuleFor(u => u.ZipCode, f => TruncateString(f.Address.ZipCode(), 15))
                .RuleFor(u => u.Salary, f => Math.Round(f.Random.Decimal(30000, 200000), 2))
                .RuleFor(u => u.DepartmentId, f => f.PickRandom(departmentIds))
                .RuleFor(u => u.IsActive, f => f.Random.Bool(0.85f))
                .RuleFor(u => u.Notes, f => TruncateString(f.Lorem.Sentences(2), 3000));

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            for (int i = 0; i < count; i += _batchSize)
            {
                var batchCount = Math.Min(_batchSize, count - i);
                var users = userFaker.Generate(batchCount);

                await InsertUsersBulkCopyAsync(connection, users);

                if ((i + batchCount) % 100000 == 0)
                {
                    _logger.LogInformation($"已生成 {i + batchCount} 条用户数据...");
                }
            }

            stopwatch.Stop();
            _logger.LogInformation($"用户数据生成完成，总计: {count} 条，耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 生成订单数据
        /// </summary>
        public async Task GenerateOrdersAsync(int count = 2000000)
        {
            _logger.LogInformation($"开始生成 {count} 条订单数据...");
            var stopwatch = Stopwatch.StartNew();

            // 获取用户ID列表
            var userIds = await GetUserIdsAsync();
            if (!userIds.Any())
            {
                throw new InvalidOperationException("请先生成用户数据");
            }

            var orderStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

            var orderFaker = new Faker<Order>()
                .RuleFor(o => o.UserId, f => f.PickRandom(userIds))
                .RuleFor(o => o.OrderDate, f => f.Date.Past(1))
                .RuleFor(o => o.TotalAmount, f => Math.Round(f.Random.Decimal(10, 5000), 2))
                .RuleFor(o => o.Status, f => f.PickRandom(orderStatuses))
                .RuleFor(o => o.ShippingAddress, f => TruncateString(f.Address.FullAddress(), 950)) // 留有缓冲
                .RuleFor(o => o.ShippedDate, (f, o) => o.Status == "Shipped" || o.Status == "Delivered" ? f.Date.Between(o.OrderDate, DateTime.Now) : null)
                .RuleFor(o => o.DeliveredDate, (f, o) => o.Status == "Delivered" && o.ShippedDate.HasValue ? f.Date.Between(o.ShippedDate.Value, DateTime.Now) : null)
                .RuleFor(o => o.Notes, f => TruncateString(f.Lorem.Sentences(1), 3000));

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            for (int i = 0; i < count; i += _batchSize)
            {
                var batchCount = Math.Min(_batchSize, count - i);
                var orders = orderFaker.Generate(batchCount);

                await InsertOrdersBulkCopyAsync(connection, orders);

                if ((i + batchCount) % 100000 == 0)
                {
                    _logger.LogInformation($"已生成 {i + batchCount} 条订单数据...");
                }
            }

            stopwatch.Stop();
            _logger.LogInformation($"订单数据生成完成，总计: {count} 条，耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        private async Task<List<int>> GetDepartmentIdsAsync()
        {
            var ids = new List<int>();
            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT Id FROM Departments WHERE IsActive = 1";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        private async Task<List<int>> GetUserIdsAsync()
        {
            var ids = new List<int>();
            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT Id FROM Users WHERE IsActive = 1";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        private async Task InsertUsersBatchAsync(SqlConnection connection, List<User> users)
        {
            var query = @"
                INSERT INTO Users (FirstName, LastName, Email, PhoneNumber, DateOfBirth, CreatedDate, 
                                 LastLoginDate, City, State, Country, ZipCode, Salary, DepartmentId, IsActive, Notes)
                VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @DateOfBirth, @CreatedDate, 
                       @LastLoginDate, @City, @State, @Country, @ZipCode, @Salary, @DepartmentId, @IsActive, @Notes)";

            foreach (var user in users)
            {
                try
                {
                    using var command = new SqlCommand(query, connection);

                    // 数据验证和截断处理
                    command.Parameters.AddWithValue("@FirstName", TruncateString(user.FirstName, 100));
                    command.Parameters.AddWithValue("@LastName", TruncateString(user.LastName, 100));
                    command.Parameters.AddWithValue("@Email", TruncateString(user.Email, 255));
                    command.Parameters.AddWithValue("@PhoneNumber", TruncateString(user.PhoneNumber, 50));
                    command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
                    command.Parameters.AddWithValue("@CreatedDate", user.CreatedDate);
                    command.Parameters.AddWithValue("@LastLoginDate", (object?)user.LastLoginDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@City", TruncateString(user.City, 100));
                    command.Parameters.AddWithValue("@State", TruncateString(user.State, 100));
                    command.Parameters.AddWithValue("@Country", TruncateString(user.Country, 100));
                    command.Parameters.AddWithValue("@ZipCode", TruncateString(user.ZipCode, 20));
                    command.Parameters.AddWithValue("@Salary", user.Salary);
                    command.Parameters.AddWithValue("@DepartmentId", user.DepartmentId);
                    command.Parameters.AddWithValue("@IsActive", user.IsActive);
                    command.Parameters.AddWithValue("@Notes", TruncateString(user.Notes, 4000)); // 限制Notes长度

                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, $"插入用户数据失败: FirstName={user.FirstName}, LastName={user.LastName}, Email={user.Email}");

                    // 记录详细的错误信息
                    if (ex.Number == 8152) // String or binary data would be truncated
                    {
                        _logger.LogError("数据截断错误 - 字段长度超出限制");
                        _logger.LogError($"用户数据: FirstName长度={user.FirstName?.Length}, LastName长度={user.LastName?.Length}, Email长度={user.Email?.Length}");
                    }

                    // 继续处理其他记录，不中断整个批次
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"插入用户数据时发生未知错误: {user.Email}");
                    continue;
                }
            }
        }

        private async Task InsertOrdersBatchAsync(SqlConnection connection, List<Order> orders)
        {
            var query = @"
                INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status, ShippingAddress, ShippedDate, DeliveredDate, Notes)
                VALUES (@UserId, @OrderDate, @TotalAmount, @Status, @ShippingAddress, @ShippedDate, @DeliveredDate, @Notes)";

            foreach (var order in orders)
            {
                try
                {
                    using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", order.UserId);
                    command.Parameters.AddWithValue("@OrderDate", order.OrderDate);
                    command.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                    command.Parameters.AddWithValue("@Status", TruncateString(order.Status, 50));
                    command.Parameters.AddWithValue("@ShippingAddress", TruncateString(order.ShippingAddress, 1000));
                    command.Parameters.AddWithValue("@ShippedDate", (object?)order.ShippedDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DeliveredDate", (object?)order.DeliveredDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Notes", TruncateString(order.Notes, 4000));

                    await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, $"插入订单数据失败: OrderId={order.Id}, UserId={order.UserId}");

                    if (ex.Number == 8152)
                    {
                        _logger.LogError("订单数据截断错误");
                        _logger.LogError($"订单数据: Status长度={order.Status?.Length}, Address长度={order.ShippingAddress?.Length}");
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"插入订单数据时发生未知错误: OrderId={order.Id}");
                    continue;
                }
            }
        }

        /// <summary>
        /// 截断字符串以适应数据库字段长度限制
        /// </summary>
        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        /// <summary>
        /// 使用 SqlBulkCopy 进行批量插入（最优性能）
        /// </summary>
        private async Task InsertUsersBulkCopyAsync(SqlConnection connection, List<User> users)
        {
            try
            {
                // 创建 DataTable
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("FirstName", typeof(string));
                dataTable.Columns.Add("LastName", typeof(string));
                dataTable.Columns.Add("Email", typeof(string));
                dataTable.Columns.Add("PhoneNumber", typeof(string));
                dataTable.Columns.Add("DateOfBirth", typeof(DateTime));
                dataTable.Columns.Add("CreatedDate", typeof(DateTime));
                dataTable.Columns.Add("LastLoginDate", typeof(DateTime));
                dataTable.Columns.Add("City", typeof(string));
                dataTable.Columns.Add("State", typeof(string));
                dataTable.Columns.Add("Country", typeof(string));
                dataTable.Columns.Add("ZipCode", typeof(string));
                dataTable.Columns.Add("Salary", typeof(decimal));
                dataTable.Columns.Add("DepartmentId", typeof(int));
                dataTable.Columns.Add("IsActive", typeof(bool));
                dataTable.Columns.Add("Notes", typeof(string));

                // 填充数据
                foreach (var user in users)
                {
                    var row = dataTable.NewRow();
                    row["FirstName"] = TruncateString(user.FirstName, 100);
                    row["LastName"] = TruncateString(user.LastName, 100);
                    row["Email"] = TruncateString(user.Email, 255);
                    row["PhoneNumber"] = TruncateString(user.PhoneNumber, 50);
                    row["DateOfBirth"] = user.DateOfBirth;
                    row["CreatedDate"] = user.CreatedDate;
                    row["LastLoginDate"] = user.LastLoginDate ?? (object)DBNull.Value;
                    row["City"] = TruncateString(user.City, 100);
                    row["State"] = TruncateString(user.State, 100);
                    row["Country"] = TruncateString(user.Country, 100);
                    row["ZipCode"] = TruncateString(user.ZipCode, 20);
                    row["Salary"] = user.Salary;
                    row["DepartmentId"] = user.DepartmentId;
                    row["IsActive"] = user.IsActive;
                    row["Notes"] = TruncateString(user.Notes, 4000);

                    dataTable.Rows.Add(row);
                }

                // 使用 SqlBulkCopy
                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName = "Users",
                    BatchSize = _batchSize,
                    BulkCopyTimeout = 300 // 5分钟超时
                };

                // 映射列
                bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                bulkCopy.ColumnMappings.Add("LastName", "LastName");
                bulkCopy.ColumnMappings.Add("Email", "Email");
                bulkCopy.ColumnMappings.Add("PhoneNumber", "PhoneNumber");
                bulkCopy.ColumnMappings.Add("DateOfBirth", "DateOfBirth");
                bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                bulkCopy.ColumnMappings.Add("LastLoginDate", "LastLoginDate");
                bulkCopy.ColumnMappings.Add("City", "City");
                bulkCopy.ColumnMappings.Add("State", "State");
                bulkCopy.ColumnMappings.Add("Country", "Country");
                bulkCopy.ColumnMappings.Add("ZipCode", "ZipCode");
                bulkCopy.ColumnMappings.Add("Salary", "Salary");
                bulkCopy.ColumnMappings.Add("DepartmentId", "DepartmentId");
                bulkCopy.ColumnMappings.Add("IsActive", "IsActive");
                bulkCopy.ColumnMappings.Add("Notes", "Notes");

                await bulkCopy.WriteToServerAsync(dataTable);

                _logger.LogInformation($"成功批量插入 {users.Count} 条用户数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量插入用户数据失败，记录数: {users.Count}");
                throw;
            }
        }
        /// <summary>
        /// 使用 SqlBulkCopy 进行订单批量插入（最优性能）
        /// </summary>
        private async Task InsertOrdersBulkCopyAsync(SqlConnection connection, List<Order> orders)
        {
            try
            {
                // 创建 DataTable
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("UserId", typeof(int));
                dataTable.Columns.Add("OrderDate", typeof(DateTime));
                dataTable.Columns.Add("TotalAmount", typeof(decimal));
                dataTable.Columns.Add("Status", typeof(string));
                dataTable.Columns.Add("ShippingAddress", typeof(string));
                dataTable.Columns.Add("ShippedDate", typeof(DateTime));
                dataTable.Columns.Add("DeliveredDate", typeof(DateTime));
                dataTable.Columns.Add("Notes", typeof(string));

                // 填充数据
                foreach (var order in orders)
                {
                    var row = dataTable.NewRow();
                    row["UserId"] = order.UserId;
                    row["OrderDate"] = order.OrderDate;
                    row["TotalAmount"] = order.TotalAmount;
                    row["Status"] = TruncateString(order.Status, 50);
                    row["ShippingAddress"] = TruncateString(order.ShippingAddress, 1000);
                    row["ShippedDate"] = order.ShippedDate ?? (object)DBNull.Value;
                    row["DeliveredDate"] = order.DeliveredDate ?? (object)DBNull.Value;
                    row["Notes"] = TruncateString(order.Notes, 4000);

                    dataTable.Rows.Add(row);
                }

                // 使用 SqlBulkCopy
                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName = "Orders",
                    BatchSize = _batchSize,
                    BulkCopyTimeout = 300 // 5分钟超时
                };

                // 映射列
                bulkCopy.ColumnMappings.Add("UserId", "UserId");
                bulkCopy.ColumnMappings.Add("OrderDate", "OrderDate");
                bulkCopy.ColumnMappings.Add("TotalAmount", "TotalAmount");
                bulkCopy.ColumnMappings.Add("Status", "Status");
                bulkCopy.ColumnMappings.Add("ShippingAddress", "ShippingAddress");
                bulkCopy.ColumnMappings.Add("ShippedDate", "ShippedDate");
                bulkCopy.ColumnMappings.Add("DeliveredDate", "DeliveredDate");
                bulkCopy.ColumnMappings.Add("Notes", "Notes");

                await bulkCopy.WriteToServerAsync(dataTable);

                _logger.LogInformation($"成功批量插入 {orders.Count} 条订单数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量插入订单数据失败，记录数: {orders.Count}");
                throw;
            }
        }
    }

}
