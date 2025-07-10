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
                .RuleFor(d => d.Name, f => f.Commerce.Department())
                .RuleFor(d => d.Description, f => f.Lorem.Sentence())
                .RuleFor(d => d.CreatedDate, f => f.Date.Past(2))
                .RuleFor(d => d.IsActive, f => f.Random.Bool(0.9f));

            var departments = departmentFaker.Generate(count);

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            foreach (var department in departments)
            {
                var query = @"
                    INSERT INTO Departments (Name, Description, CreatedDate, IsActive)
                    VALUES (@Name, @Description, @CreatedDate, @IsActive)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", department.Name);
                command.Parameters.AddWithValue("@Description", department.Description);
                command.Parameters.AddWithValue("@CreatedDate", department.CreatedDate);
                command.Parameters.AddWithValue("@IsActive", department.IsActive);

                await command.ExecuteNonQueryAsync();
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
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.DateOfBirth, f => f.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18)))
                .RuleFor(u => u.CreatedDate, f => f.Date.Past(2))
                .RuleFor(u => u.LastLoginDate, f => f.Random.Bool(0.7f) ? f.Date.Recent(30) : null)
                .RuleFor(u => u.City, f => f.Address.City())
                .RuleFor(u => u.State, f => f.Address.State())
                .RuleFor(u => u.Country, f => f.Address.Country())
                .RuleFor(u => u.ZipCode, f => f.Address.ZipCode())
                .RuleFor(u => u.Salary, f => f.Random.Decimal(30000, 200000))
                .RuleFor(u => u.DepartmentId, f => f.PickRandom(departmentIds))
                .RuleFor(u => u.IsActive, f => f.Random.Bool(0.85f))
                .RuleFor(u => u.Notes, f => f.Lorem.Paragraph());

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            for (int i = 0; i < count; i += _batchSize)
            {
                var batchCount = Math.Min(_batchSize, count - i);
                var users = userFaker.Generate(batchCount);

                await InsertUsersBatchAsync(connection, users);

                if ((i + batchCount) % 50000 == 0)
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
                .RuleFor(o => o.TotalAmount, f => f.Random.Decimal(10, 5000))
                .RuleFor(o => o.Status, f => f.PickRandom(orderStatuses))
                .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
                .RuleFor(o => o.ShippedDate, (f, o) => o.Status == "Shipped" || o.Status == "Delivered" ? f.Date.Between(o.OrderDate, DateTime.Now) : null)
                .RuleFor(o => o.DeliveredDate, (f, o) => o.Status == "Delivered" && o.ShippedDate.HasValue ? f.Date.Between(o.ShippedDate.Value, DateTime.Now) : null)
                .RuleFor(o => o.Notes, f => f.Lorem.Sentence());

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            for (int i = 0; i < count; i += _batchSize)
            {
                var batchCount = Math.Min(_batchSize, count - i);
                var orders = orderFaker.Generate(batchCount);

                await InsertOrdersBatchAsync(connection, orders);

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
                ids.Add(reader.GetInt32("Id"));
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
                ids.Add(reader.GetInt32("Id"));
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
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
                command.Parameters.AddWithValue("@CreatedDate", user.CreatedDate);
                command.Parameters.AddWithValue("@LastLoginDate", (object?)user.LastLoginDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@City", user.City);
                command.Parameters.AddWithValue("@State", user.State);
                command.Parameters.AddWithValue("@Country", user.Country);
                command.Parameters.AddWithValue("@ZipCode", user.ZipCode);
                command.Parameters.AddWithValue("@Salary", user.Salary);
                command.Parameters.AddWithValue("@DepartmentId", user.DepartmentId);
                command.Parameters.AddWithValue("@IsActive", user.IsActive);
                command.Parameters.AddWithValue("@Notes", user.Notes);

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertOrdersBatchAsync(SqlConnection connection, List<Order> orders)
        {
            var query = @"
                INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status, ShippingAddress, ShippedDate, DeliveredDate, Notes)
                VALUES (@UserId, @OrderDate, @TotalAmount, @Status, @ShippingAddress, @ShippedDate, @DeliveredDate, @Notes)";

            foreach (var order in orders)
            {
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", order.UserId);
                command.Parameters.AddWithValue("@OrderDate", order.OrderDate);
                command.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                command.Parameters.AddWithValue("@Status", order.Status);
                command.Parameters.AddWithValue("@ShippingAddress", order.ShippingAddress);
                command.Parameters.AddWithValue("@ShippedDate", (object?)order.ShippedDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@DeliveredDate", (object?)order.DeliveredDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@Notes", order.Notes);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
