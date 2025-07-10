using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using DBOptimizationStudy.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBOptimizationStudy.Services
{
    /// <summary>
    /// 性能测试服务 - 用于测试各种查询的性能
    /// </summary>
    public class PerformanceTestService
    {
        private readonly DatabaseService _databaseService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PerformanceTestService> _logger;
        private readonly int _testIterations;

        public PerformanceTestService(
            DatabaseService databaseService,
            IConfiguration configuration,
            ILogger<PerformanceTestService> logger)
        {
            _databaseService = databaseService;
            _configuration = configuration;
            _logger = logger;
            _testIterations = _configuration.GetValue<int>("DatabaseSettings:TestIterations", 5);
        }

        /// <summary>
        /// 运行所有性能测试
        /// </summary>
        public async Task RunAllPerformanceTestsAsync()
        {
            _logger.LogInformation("开始运行所有性能测试...");

            var results = new List<PerformanceTestResult>();

            // 1. 基础查询测试
            results.AddRange(await RunBasicQueriesTestAsync());

            // 2. JOIN查询测试
            results.AddRange(await RunJoinQueriesTestAsync());

            // 3. 聚合查询测试
            results.AddRange(await RunAggregateQueriesTestAsync());

            // 4. 排序查询测试
            results.AddRange(await RunSortingQueriesTestAsync());

            // 5. 分页查询测试
            results.AddRange(await RunPagingQueriesTestAsync());

            // 6. 范围查询测试
            results.AddRange(await RunRangeQueriesTestAsync());

            // 输出测试结果摘要
            PrintTestResultsSummary(results);

            _logger.LogInformation("所有性能测试完成！");
        }

        /// <summary>
        /// 基础查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunBasicQueriesTestAsync()
        {
            _logger.LogInformation("开始基础查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: 简单SELECT查询
            var selectAllQuery = "SELECT TOP 1000 * FROM Users";
            results.Add(await ExecuteQueryTestAsync("基础查询 - 前1000条记录", "SELECT", selectAllQuery));

            // 测试2: WHERE条件查询
            var whereQuery = "SELECT * FROM Users WHERE IsActive = 1";
            results.Add(await ExecuteQueryTestAsync("基础查询 - WHERE条件", "SELECT", whereQuery));

            // 测试3: 特定字段查询
            var specificFieldsQuery = "SELECT Id, FirstName, LastName, Email FROM Users WHERE DepartmentId = 1";
            results.Add(await ExecuteQueryTestAsync("基础查询 - 特定字段", "SELECT", specificFieldsQuery));

            return results;
        }

        /// <summary>
        /// JOIN查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunJoinQueriesTestAsync()
        {
            _logger.LogInformation("开始JOIN查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: INNER JOIN
            var innerJoinQuery = @"
                SELECT u.FirstName, u.LastName, d.Name as DepartmentName
                FROM Users u
                INNER JOIN Departments d ON u.DepartmentId = d.Id
                WHERE u.IsActive = 1";
            results.Add(await ExecuteQueryTestAsync("JOIN查询 - INNER JOIN", "SELECT", innerJoinQuery));

            // 测试2: LEFT JOIN
            var leftJoinQuery = @"
                SELECT u.FirstName, u.LastName, COUNT(o.Id) as OrderCount
                FROM Users u
                LEFT JOIN Orders o ON u.Id = o.UserId
                WHERE u.IsActive = 1
                GROUP BY u.Id, u.FirstName, u.LastName";
            results.Add(await ExecuteQueryTestAsync("JOIN查询 - LEFT JOIN with GROUP BY", "SELECT", leftJoinQuery));

            // 测试3: 多表JOIN
            var multiJoinQuery = @"
                SELECT u.FirstName, u.LastName, d.Name as DepartmentName, 
                       COUNT(o.Id) as OrderCount, SUM(o.TotalAmount) as TotalSpent
                FROM Users u
                INNER JOIN Departments d ON u.DepartmentId = d.Id
                LEFT JOIN Orders o ON u.Id = o.UserId
                WHERE u.IsActive = 1
                GROUP BY u.Id, u.FirstName, u.LastName, d.Name
                HAVING COUNT(o.Id) > 0";
            results.Add(await ExecuteQueryTestAsync("JOIN查询 - 多表JOIN", "SELECT", multiJoinQuery));

            return results;
        }

        /// <summary>
        /// 聚合查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunAggregateQueriesTestAsync()
        {
            _logger.LogInformation("开始聚合查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: COUNT查询
            var countQuery = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";
            results.Add(await ExecuteQueryTestAsync("聚合查询 - COUNT", "SELECT", countQuery));

            // 测试2: SUM和AVG查询
            var sumAvgQuery = @"
                SELECT DepartmentId, COUNT(*) as UserCount, 
                       AVG(Salary) as AvgSalary, SUM(Salary) as TotalSalary
                FROM Users 
                WHERE IsActive = 1 
                GROUP BY DepartmentId";
            results.Add(await ExecuteQueryTestAsync("聚合查询 - SUM/AVG/GROUP BY", "SELECT", sumAvgQuery));

            // 测试3: 复杂聚合查询
            var complexAggQuery = @"
                SELECT YEAR(OrderDate) as OrderYear, MONTH(OrderDate) as OrderMonth,
                       COUNT(*) as OrderCount, SUM(TotalAmount) as MonthlyRevenue,
                       AVG(TotalAmount) as AvgOrderValue
                FROM Orders
                GROUP BY YEAR(OrderDate), MONTH(OrderDate)
                ORDER BY OrderYear DESC, OrderMonth DESC";
            results.Add(await ExecuteQueryTestAsync("聚合查询 - 复杂聚合", "SELECT", complexAggQuery));

            return results;
        }

        /// <summary>
        /// 排序查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunSortingQueriesTestAsync()
        {
            _logger.LogInformation("开始排序查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: 单字段排序
            var singleSortQuery = "SELECT TOP 10000 * FROM Users ORDER BY LastName";
            results.Add(await ExecuteQueryTestAsync("排序查询 - 单字段排序", "SELECT", singleSortQuery));

            // 测试2: 多字段排序
            var multiSortQuery = "SELECT TOP 10000 * FROM Users ORDER BY DepartmentId, Salary DESC, LastName";
            results.Add(await ExecuteQueryTestAsync("排序查询 - 多字段排序", "SELECT", multiSortQuery));

            // 测试3: 日期排序
            var dateSortQuery = "SELECT TOP 10000 * FROM Orders ORDER BY OrderDate DESC";
            results.Add(await ExecuteQueryTestAsync("排序查询 - 日期排序", "SELECT", dateSortQuery));

            return results;
        }

        /// <summary>
        /// 分页查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunPagingQueriesTestAsync()
        {
            _logger.LogInformation("开始分页查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: OFFSET/FETCH分页
            var offsetFetchQuery = @"
                SELECT * FROM Users 
                WHERE IsActive = 1
                ORDER BY Id
                OFFSET 50000 ROWS FETCH NEXT 100 ROWS ONLY";
            results.Add(await ExecuteQueryTestAsync("分页查询 - OFFSET/FETCH", "SELECT", offsetFetchQuery));

            // 测试2: ROW_NUMBER分页
            var rowNumberQuery = @"
                WITH PagedUsers AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
                    FROM Users WHERE IsActive = 1
                )
                SELECT * FROM PagedUsers WHERE RowNum BETWEEN 50001 AND 50100";
            results.Add(await ExecuteQueryTestAsync("分页查询 - ROW_NUMBER", "SELECT", rowNumberQuery));

            return results;
        }

        /// <summary>
        /// 范围查询测试
        /// </summary>
        public async Task<List<PerformanceTestResult>> RunRangeQueriesTestAsync()
        {
            _logger.LogInformation("开始范围查询测试...");
            var results = new List<PerformanceTestResult>();

            // 测试1: 数值范围查询
            var numericRangeQuery = "SELECT * FROM Users WHERE Salary BETWEEN 50000 AND 100000";
            results.Add(await ExecuteQueryTestAsync("范围查询 - 数值范围", "SELECT", numericRangeQuery));

            // 测试2: 日期范围查询
            var dateRangeQuery = @"
                SELECT * FROM Orders 
                WHERE OrderDate BETWEEN '2024-01-01' AND '2024-12-31'";
            results.Add(await ExecuteQueryTestAsync("范围查询 - 日期范围", "SELECT", dateRangeQuery));

            // 测试3: IN查询
            var inQuery = "SELECT * FROM Users WHERE DepartmentId IN (1, 2, 3, 4, 5)";
            results.Add(await ExecuteQueryTestAsync("范围查询 - IN条件", "SELECT", inQuery));

            return results;
        }

        /// <summary>
        /// 执行单个查询测试
        /// </summary>
        private async Task<PerformanceTestResult> ExecuteQueryTestAsync(string testName, string queryType, string query)
        {
            var times = new List<long>();

            using var connection = new SqlConnection(_databaseService.ConnectionString);
            await connection.OpenAsync();

            // 执行多次测试取平均值
            for (int i = 0; i < _testIterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = 300; // 5分钟超时

                var recordCount = 0;
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    recordCount++;
                }

                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);

                if (i == 0) // 记录第一次的结果数量
                {
                    // 这里可以记录影响的记录数
                }
            }

            // 计算平均执行时间（排除最高和最低值）
            times.Sort();
            var avgTime = times.Count > 2 
                ? times.GetRange(1, times.Count - 2).Sum() / (times.Count - 2)
                : times.Sum() / times.Count;

            var result = new PerformanceTestResult
            {
                TestName = testName,
                QueryType = queryType,
                ExecutionTimeMs = avgTime,
                TestDate = DateTime.Now,
                AdditionalInfo = $"执行次数: {_testIterations}, 最小: {times[0]}ms, 最大: {times[^1]}ms"
            };

            _logger.LogInformation($"{testName}: 平均执行时间 {avgTime}ms");
            return result;
        }

        /// <summary>
        /// 打印测试结果摘要
        /// </summary>
        private void PrintTestResultsSummary(List<PerformanceTestResult> results)
        {
            _logger.LogInformation("\n========== 性能测试结果摘要 ==========");
            
            foreach (var result in results)
            {
                _logger.LogInformation($"{result.TestName}: {result.ExecutionTimeMs}ms");
            }

            // 按执行时间排序，找出最慢的查询
            results.Sort((x, y) => y.ExecutionTimeMs.CompareTo(x.ExecutionTimeMs));
            
            _logger.LogInformation("\n========== 最慢的5个查询 ==========");
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                var result = results[i];
                _logger.LogInformation($"{i + 1}. {result.TestName}: {result.ExecutionTimeMs}ms");
            }
        }
    }
}
