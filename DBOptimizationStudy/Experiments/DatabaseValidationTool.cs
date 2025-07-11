using DBOptimizationStudy.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DBOptimizationStudy.Experiments
{
    /// <summary>
    /// 数据库字段验证工具
    /// </summary>
    public class DatabaseValidationTool
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<DatabaseValidationTool> _logger;

        public DatabaseValidationTool(DatabaseService databaseService, ILogger<DatabaseValidationTool> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        /// <summary>
        /// 检查数据库表结构和字段长度
        /// </summary>
        public async Task ValidateDatabaseStructureAsync()
        {
            Console.WriteLine("🔍 数据库结构验证工具");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("检查表结构、字段长度和潜在的数据截断问题");
            Console.WriteLine("");

            await CheckTableStructure();
            await CheckDataIntegrity();
            await CheckConstraints();
            await ProvideOptimizationSuggestions();
        }

        private async Task CheckTableStructure()
        {
            Console.WriteLine("📋 表结构检查");
            Console.WriteLine("═══════════════════════════════════════");

            var tableInfo = await _databaseService.ExecuteQueryAsync(@"
                SELECT 
                    t.name AS TableName,
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    c.max_length,
                    c.precision,
                    c.scale,
                    c.is_nullable,
                    c.is_identity,
                    CASE 
                        WHEN c.default_object_id != 0 THEN 1
                        ELSE 0
                    END AS HasDefault
                FROM sys.tables t
                INNER JOIN sys.columns c ON t.object_id = c.object_id
                INNER JOIN sys.types ty ON c.system_type_id = ty.system_type_id
                WHERE t.name IN ('Users', 'Orders', 'Departments', 'PerformanceTestResults')
                  AND ty.name != 'sysname'
                ORDER BY t.name, c.column_id
            ");

            Console.WriteLine("表名        | 列名              | 数据类型     | 最大长度 | 精度 | 刻度 | 可空 | 自增");
            Console.WriteLine("─────────────────────────────────────────────────────────────────────────────────");

            foreach (var row in tableInfo)
            {
                var maxLength = row["max_length"]?.ToString() == "-1" ? "MAX" : row["max_length"]?.ToString();
                var precision = row["precision"]?.ToString();
                var scale = row["scale"]?.ToString();
                var nullable = (bool)row["is_nullable"] ? "是" : "否";
                var identity = (bool)row["is_identity"] ? "是" : "否";

                Console.WriteLine($"{row["TableName"],-10} | {row["ColumnName"],-16} | {row["DataType"],-11} | {maxLength,-8} | {precision,-4} | {scale,-4} | {nullable,-4} | {identity}");
            }
            Console.WriteLine("");
        }

        private async Task CheckDataIntegrity()
        {
            Console.WriteLine("🔍 数据完整性检查");
            Console.WriteLine("═══════════════════════════════════════");

            // 检查用户表的数据长度
            var userDataCheck = await _databaseService.ExecuteQueryAsync(@"
                SELECT 
                    'Users' AS TableName,
                    'FirstName' AS ColumnName,
                    MAX(LEN(FirstName)) AS MaxLength,
                    AVG(CAST(LEN(FirstName) AS FLOAT)) AS AvgLength,
                    COUNT(*) AS RecordCount
                FROM Users
                WHERE FirstName IS NOT NULL
                UNION ALL
                SELECT 
                    'Users', 'LastName', MAX(LEN(LastName)), AVG(CAST(LEN(LastName) AS FLOAT)), COUNT(*)
                FROM Users
                WHERE LastName IS NOT NULL
                UNION ALL
                SELECT 
                    'Users', 'Email', MAX(LEN(Email)), AVG(CAST(LEN(Email) AS FLOAT)), COUNT(*)
                FROM Users
                WHERE Email IS NOT NULL
                UNION ALL
                SELECT 
                    'Users', 'PhoneNumber', MAX(LEN(PhoneNumber)), AVG(CAST(LEN(PhoneNumber) AS FLOAT)), COUNT(*)
                FROM Users
                WHERE PhoneNumber IS NOT NULL
                UNION ALL
                SELECT 
                    'Users', 'City', MAX(LEN(City)), AVG(CAST(LEN(City) AS FLOAT)), COUNT(*)
                FROM Users
                WHERE City IS NOT NULL
            ");

            Console.WriteLine("表名   | 字段名       | 最大长度 | 平均长度 | 记录数");
            Console.WriteLine("──────────────────────────────────────────────────────");

            foreach (var row in userDataCheck)
            {
                Console.WriteLine($"{row["TableName"],-6} | {row["ColumnName"],-12} | {row["MaxLength"],-8} | {Math.Round((double)row["AvgLength"], 1),-8} | {row["RecordCount"]}");
            }
            Console.WriteLine("");

            // 检查可能的数据截断问题
            await CheckPotentialTruncationIssues();
        }

        private async Task CheckPotentialTruncationIssues()
        {
            Console.WriteLine("⚠️  潜在截断问题检查");
            Console.WriteLine("─────────────────────────────────────");

            // 检查接近字段长度限制的数据
            var truncationCheck = await _databaseService.ExecuteQueryAsync(@"
                WITH FieldLengthCheck AS (
                    SELECT 'Users' AS TableName, 'FirstName' AS FieldName, 100 AS MaxAllowed, MAX(LEN(FirstName)) AS CurrentMax FROM Users
                    UNION ALL
                    SELECT 'Users', 'LastName', 100, MAX(LEN(LastName)) FROM Users
                    UNION ALL
                    SELECT 'Users', 'Email', 255, MAX(LEN(Email)) FROM Users
                    UNION ALL
                    SELECT 'Users', 'PhoneNumber', 50, MAX(LEN(PhoneNumber)) FROM Users
                    UNION ALL
                    SELECT 'Orders', 'Status', 50, MAX(LEN(Status)) FROM Orders
                    UNION ALL
                    SELECT 'Orders', 'ShippingAddress', 1000, MAX(LEN(ShippingAddress)) FROM Orders
                )
                SELECT 
                    TableName,
                    FieldName,
                    MaxAllowed,
                    CurrentMax,
                    CAST((CurrentMax * 100.0 / MaxAllowed) AS DECIMAL(5,1)) AS UsagePercentage,
                    CASE 
                        WHEN CurrentMax > MaxAllowed THEN '🔴 超出限制'
                        WHEN CurrentMax > MaxAllowed * 0.9 THEN '🟡 接近限制'
                        WHEN CurrentMax > MaxAllowed * 0.7 THEN '🟢 使用较多'
                        ELSE '✅ 正常'
                    END AS Status
                FROM FieldLengthCheck
                ORDER BY UsagePercentage DESC
            ");

            Console.WriteLine("表名   | 字段名         | 限制长度 | 当前最大 | 使用率% | 状态");
            Console.WriteLine("─────────────────────────────────────────────────────────────────");

            foreach (var row in truncationCheck)
            {
                Console.WriteLine($"{row["TableName"],-6} | {row["FieldName"],-14} | {row["MaxAllowed"],-8} | {row["CurrentMax"],-8} | {row["UsagePercentage"],-7} | {row["Status"]}");
            }
            Console.WriteLine("");
        }

        private async Task CheckConstraints()
        {
            Console.WriteLine("🔗 约束检查");
            Console.WriteLine("═══════════════════════════════════════");

            var constraintInfo = await _databaseService.ExecuteQueryAsync(@"
                SELECT 
                    t.name AS TableName,
                    c.name AS ConstraintName,
                    c.type_desc AS ConstraintType,
                    CASE 
                        WHEN c.type = 'F' THEN 
                            (SELECT OBJECT_NAME(fk.referenced_object_id) 
                             FROM sys.foreign_keys fk 
                             WHERE fk.object_id = c.object_id)
                        ELSE NULL
                    END AS ReferencedTable
                FROM sys.tables t
                INNER JOIN sys.objects c ON t.object_id = c.parent_object_id
                WHERE t.name IN ('Users', 'Orders', 'Departments', 'PerformanceTestResults')
                  AND c.type IN ('PK', 'F', 'C', 'UQ')
                ORDER BY t.name, c.type_desc
            ");

            Console.WriteLine("表名             | 约束名称                    | 约束类型        | 引用表");
            Console.WriteLine("────────────────────────────────────────────────────────────────────");

            foreach (var row in constraintInfo)
            {
                Console.WriteLine($"{row["TableName"],-15} | {row["ConstraintName"],-26} | {row["ConstraintType"],-14} | {row["ReferencedTable"] ?? ""}");
            }
            Console.WriteLine("");
        }

        private async Task ProvideOptimizationSuggestions()
        {
            Console.WriteLine("💡 优化建议");
            Console.WriteLine("═══════════════════════════════════════");

            var suggestions = new List<string>();

            // 检查是否有未使用的大字段
            var largeFieldCheck = await _databaseService.ExecuteQueryAsync(@"
                SELECT 
                    COUNT(*) AS EmptyNotesCount,
                    (SELECT COUNT(*) FROM Users) AS TotalUsers
                FROM Users 
                WHERE Notes IS NULL OR Notes = ''
            ");

            foreach (var row in largeFieldCheck)
            {
                var emptyCount = (int)row["EmptyNotesCount"];
                var totalCount = (int)row["TotalUsers"];
                var emptyPercentage = (double)emptyCount / totalCount * 100;

                if (emptyPercentage > 80)
                {
                    suggestions.Add($"🔍 Users表的Notes字段有{emptyPercentage:F1}%为空，考虑将其移到单独的表中");
                }
            }

            // 检查数据类型优化建议
            suggestions.Add("📊 数据类型优化建议:");
            suggestions.Add("   • 如果Email地址都很短，可以将NVARCHAR(255)改为VARCHAR(200)节省空间");
            suggestions.Add("   • 如果不需要Unicode支持，NVARCHAR可以改为VARCHAR节省50%空间");
            suggestions.Add("   • PhoneNumber如果格式固定，可以使用更短的字段");
            suggestions.Add("   • 考虑将经常为空的大字段(Notes)移到单独的表中");

            suggestions.Add("");
            suggestions.Add("🚀 性能优化建议:");
            suggestions.Add("   • 在经常查询的字段上添加索引: Email, DepartmentId, Status");
            suggestions.Add("   • 考虑在复合查询字段上创建复合索引");
            suggestions.Add("   • 对于大表，考虑按时间或业务维度进行分区");
            suggestions.Add("   • 定期维护统计信息和索引碎片");

            foreach (var suggestion in suggestions)
            {
                Console.WriteLine(suggestion);
            }
            Console.WriteLine("");
        }

        /// <summary>
        /// 创建数据长度监控视图
        /// </summary>
        public async Task CreateDataLengthMonitoringViewAsync()
        {
            Console.WriteLine("📊 创建数据长度监控视图");
            Console.WriteLine("═══════════════════════════════════════");

            var createViewSql = @"
                -- 创建数据长度监控视图
                IF OBJECT_ID('vw_DataLengthMonitoring', 'V') IS NOT NULL
                    DROP VIEW vw_DataLengthMonitoring;
                GO

                CREATE VIEW vw_DataLengthMonitoring AS
                SELECT 
                    'Users' AS TableName,
                    'FirstName' AS FieldName,
                    100 AS MaxAllowed,
                    MAX(LEN(FirstName)) AS CurrentMax,
                    AVG(CAST(LEN(FirstName) AS FLOAT)) AS AvgLength,
                    COUNT(*) AS RecordCount
                FROM Users
                WHERE FirstName IS NOT NULL
                
                UNION ALL
                
                SELECT 
                    'Users', 'LastName', 100, 
                    MAX(LEN(LastName)), 
                    AVG(CAST(LEN(LastName) AS FLOAT)), 
                    COUNT(*)
                FROM Users
                WHERE LastName IS NOT NULL
                
                UNION ALL
                
                SELECT 
                    'Users', 'Email', 255, 
                    MAX(LEN(Email)), 
                    AVG(CAST(LEN(Email) AS FLOAT)), 
                    COUNT(*)
                FROM Users
                WHERE Email IS NOT NULL;
            ";

            await _databaseService.ExecuteScriptAsync(createViewSql);
            Console.WriteLine("✅ 数据长度监控视图创建完成");
            Console.WriteLine("   使用方法: SELECT * FROM vw_DataLengthMonitoring");
            Console.WriteLine("");
        }

        /// <summary>
        /// 生成数据库优化报告
        /// </summary>
        public async Task GenerateOptimizationReportAsync()
        {
            Console.WriteLine("📄 生成数据库优化报告");
            Console.WriteLine("═══════════════════════════════════════");

            var report = new StringBuilder();
            report.AppendLine("# 数据库优化报告");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("");

            // 表统计信息
            var tableStats = await _databaseService.ExecuteQueryAsync(@"
                SELECT 
                    t.name AS TableName,
                    p.rows AS RowCount,
                    (SUM(a.total_pages) * 8) AS TotalSpaceKB,
                    (SUM(a.used_pages) * 8) AS UsedSpaceKB
                FROM sys.tables t
                INNER JOIN sys.indexes i ON t.object_id = i.object_id
                INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                WHERE t.name IN ('Users', 'Orders', 'Departments', 'PerformanceTestResults')
                  AND i.index_id <= 1
                GROUP BY t.name, p.rows
                ORDER BY TotalSpaceKB DESC
            ");

            report.AppendLine("## 表存储统计");
            report.AppendLine("| 表名 | 行数 | 总空间(KB) | 已用空间(KB) |");
            report.AppendLine("|------|------|------------|--------------|");

            foreach (var row in tableStats)
            {
                report.AppendLine($"| {row["TableName"]} | {row["RowCount"]} | {row["TotalSpaceKB"]} | {row["UsedSpaceKB"]} |");
            }

            report.AppendLine("");
            report.AppendLine("## 优化建议");
            report.AppendLine("1. 定期检查字段长度使用情况");
            report.AppendLine("2. 考虑数据类型优化以节省存储空间");
            report.AppendLine("3. 在高频查询字段上创建适当的索引");
            report.AppendLine("4. 实施数据分区策略处理大表");

            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "DatabaseOptimizationReport.md");
            await File.WriteAllTextAsync(reportPath, report.ToString());

            Console.WriteLine($"✅ 优化报告已生成: {reportPath}");
            Console.WriteLine("");
        }
    }
}
