using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DBOptimizationStudy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DBOptimizationStudy.Experiments
{
    /// <summary>
    /// 数据库优化实验课程管理器
    /// </summary>
    public class DatabaseOptimizationCourse
    {
        private readonly DatabaseService _databaseService;
        private readonly DataGenerationService _dataGenerationService;
        private readonly PerformanceTestService _performanceTestService;
        private readonly ILogger<DatabaseOptimizationCourse> _logger;

        public DatabaseOptimizationCourse(
            DatabaseService databaseService,
            DataGenerationService dataGenerationService,
            PerformanceTestService performanceTestService,
            ILogger<DatabaseOptimizationCourse> logger)
        {
            _databaseService = databaseService;
            _dataGenerationService = dataGenerationService;
            _performanceTestService = performanceTestService;
            _logger = logger;
        }

        /// <summary>
        /// 运行完整的数据库优化实验课程
        /// </summary>
        public async Task RunCompleteCourseAsync()
        {
            _logger.LogInformation("🎓 欢迎来到数据库优化实验课程！");
            _logger.LogInformation("本课程将通过实际操作学习数据库性能优化技术");
            _logger.LogInformation("");

            try
            {
                // 第1课：环境准备
                await Lesson01_EnvironmentSetupAsync();

                // 第2课：数据生成
                await Lesson02_DataGenerationAsync();

                // 第3课：基础性能测试
                await Lesson03_BaselinePerformanceTestAsync();

                // 第4课：索引优化
                await Lesson04_IndexOptimizationAsync();

                // 第5课：高级优化技术
                await Lesson05_AdvancedOptimizationAsync();

                // 第6课：最终性能对比
                await Lesson06_FinalPerformanceComparisonAsync();

                // 课程总结
                PrintCourseSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实验课程执行过程中发生错误");
                throw;
            }
        }

        /// <summary>
        /// 第1课：环境准备
        /// </summary>
        private async Task Lesson01_EnvironmentSetupAsync()
        {
            _logger.LogInformation("📚 第1课：环境准备与数据库初始化");
            _logger.LogInformation("学习目标：了解实验环境，创建数据库结构");
            _logger.LogInformation("");

            // 1. 测试数据库连接
            _logger.LogInformation("1.1 测试数据库连接...");
            var connectionOk = await _databaseService.TestConnectionAsync();
            if (!connectionOk)
            {
                throw new Exception("数据库连接失败，请检查连接字符串");
            }

            // 2. 创建数据库
            _logger.LogInformation("1.2 创建数据库...");
            await _databaseService.CreateDatabaseIfNotExistsAsync();

            // 3. 执行表结构创建脚本
            _logger.LogInformation("1.3 创建表结构...");
            var createTablesScript = await File.ReadAllTextAsync("Scripts/01-CreateTables.sql");
            await _databaseService.ExecuteScriptAsync(createTablesScript);

            _logger.LogInformation("✅ 第1课完成！数据库环境准备就绪");
            _logger.LogInformation("");

            await Task.Delay(2000); // 暂停以便观察
        }

        /// <summary>
        /// 第2课：数据生成
        /// </summary>
        private async Task Lesson02_DataGenerationAsync()
        {
            _logger.LogInformation("📚 第2课：百万级测试数据生成");
            _logger.LogInformation("学习目标：了解大数据量对性能的影响，学习数据生成技术");
            _logger.LogInformation("");

            var userCount = await _databaseService.GetTableRowCountAsync("Users");
            var orderCount = await _databaseService.GetTableRowCountAsync("Orders");
            var departmentCount = await _databaseService.GetTableRowCountAsync("Departments");
            _logger.LogInformation($"数据统计：部门 {departmentCount} 条，用户 {userCount} 条，订单 {orderCount} 条");
            
            // 1. 生成部门数据
            _logger.LogInformation("2.1 生成部门数据...");
            if (departmentCount < 50)
            {
                await _dataGenerationService.GenerateDepartmentsAsync(50-userCount);
            }
            

            // 2. 生成用户数据
            _logger.LogInformation("2.2 生成用户数据（这可能需要几分钟）...");
            if(userCount < 1000000)
            {
                await _dataGenerationService.GenerateUsersAsync(1000000 - userCount);
            }

            // 3. 生成订单数据
            _logger.LogInformation("2.3 生成订单数据（这可能需要更长时间）...");
            if(orderCount < 2000000)
            {
                await _dataGenerationService.GenerateOrdersAsync(2000000 - orderCount);
            }

            // 4. 验证数据量
            _logger.LogInformation("2.4 验证生成的数据量...");
            userCount = await _databaseService.GetTableRowCountAsync("Users");
            orderCount = await _databaseService.GetTableRowCountAsync("Orders");
            departmentCount = await _databaseService.GetTableRowCountAsync("Departments");

            _logger.LogInformation($"数据统计：部门 {departmentCount} 条，用户 {userCount} 条，订单 {orderCount} 条");
            _logger.LogInformation("✅ 第2课完成！测试数据生成完毕");
            _logger.LogInformation("");

            await Task.Delay(2000);
        }

        /// <summary>
        /// 第3课：基础性能测试
        /// </summary>
        private async Task Lesson03_BaselinePerformanceTestAsync()
        {
            _logger.LogInformation("📚 第3课：基础性能测试（优化前基准）");
            _logger.LogInformation("学习目标：了解优化前的性能表现，识别性能瓶颈");
            _logger.LogInformation("");

            _logger.LogInformation("3.1 执行基础性能测试（无索引优化）...");
            _logger.LogInformation("注意：这些查询可能会比较慢，因为还没有优化索引");
            _logger.LogInformation("");

            await _performanceTestService.RunAllPerformanceTestsAsync();

            _logger.LogInformation("✅ 第3课完成！基础性能测试完毕");
            _logger.LogInformation("📝 学习要点：");
            _logger.LogInformation("   - 没有适当索引的查询性能很差");
            _logger.LogInformation("   - 大表的全表扫描非常耗时");
            _logger.LogInformation("   - JOIN操作在没有索引时性能低下");
            _logger.LogInformation("");

            await Task.Delay(3000);
        }

        /// <summary>
        /// 第4课：索引优化
        /// </summary>
        private async Task Lesson04_IndexOptimizationAsync()
        {
            _logger.LogInformation("📚 第4课：索引优化技术");
            _logger.LogInformation("学习目标：掌握各种索引类型的使用场景和优化效果");
            _logger.LogInformation("");

            // 1. 创建基础索引
            _logger.LogInformation("4.1 创建优化索引...");
            var indexScript = await File.ReadAllTextAsync("Scripts/02-CreateIndexes.sql");
            await _databaseService.ExecuteScriptAsync(indexScript);

            // 2. 重新测试性能
            _logger.LogInformation("4.2 重新测试性能（索引优化后）...");
            await _performanceTestService.RunAllPerformanceTestsAsync();

            _logger.LogInformation("✅ 第4课完成！索引优化实施完毕");
            _logger.LogInformation("📝 学习要点：");
            _logger.LogInformation("   - 单列索引提高单字段查询性能");
            _logger.LogInformation("   - 复合索引优化多字段查询");
            _logger.LogInformation("   - 覆盖索引减少键查找操作");
            _logger.LogInformation("   - 外键索引提高JOIN性能");
            _logger.LogInformation("");

            await Task.Delay(3000);
        }

        /// <summary>
        /// 第5课：高级优化技术
        /// </summary>
        private async Task Lesson05_AdvancedOptimizationAsync()
        {
            _logger.LogInformation("📚 第5课：高级优化技术");
            _logger.LogInformation("学习目标：掌握高级数据库优化技术");
            _logger.LogInformation("");

            // 1. 应用高级优化
            _logger.LogInformation("5.1 应用高级优化技术...");
            var advancedScript = await File.ReadAllTextAsync("Scripts/03-AdvancedOptimizations.sql");
            await _databaseService.ExecuteScriptAsync(advancedScript);

            // 2. 测试高级优化效果
            _logger.LogInformation("5.2 测试高级优化效果...");
            await _performanceTestService.RunAllPerformanceTestsAsync();

            _logger.LogInformation("✅ 第5课完成！高级优化技术应用完毕");
            _logger.LogInformation("📝 学习要点：");
            _logger.LogInformation("   - 列存储索引适用于分析查询");
            _logger.LogInformation("   - 过滤索引节省存储空间");
            _logger.LogInformation("   - 索引视图预计算复杂查询");
            _logger.LogInformation("   - 数据压缩节省存储空间");
            _logger.LogInformation("   - 统计信息影响查询计划");
            _logger.LogInformation("");

            await Task.Delay(3000);
        }

        /// <summary>
        /// 第6课：最终性能对比
        /// </summary>
        private async Task Lesson06_FinalPerformanceComparisonAsync()
        {
            _logger.LogInformation("📚 第6课：最终性能对比与分析");
            _logger.LogInformation("学习目标：对比优化前后的性能差异，总结优化经验");
            _logger.LogInformation("");

            _logger.LogInformation("6.1 执行最终性能测试...");
            await _performanceTestService.RunAllPerformanceTestsAsync();

            _logger.LogInformation("✅ 第6课完成！");
            _logger.LogInformation("");
        }

        /// <summary>
        /// 打印课程总结
        /// </summary>
        private void PrintCourseSummary()
        {
            _logger.LogInformation("🎉 恭喜！数据库优化实验课程全部完成！");
            _logger.LogInformation("");
            _logger.LogInformation("📋 课程总结：");
            _logger.LogInformation("═══════════════════════════════════════");
            _logger.LogInformation("✓ 第1课：环境准备与数据库初始化");
            _logger.LogInformation("✓ 第2课：百万级测试数据生成");
            _logger.LogInformation("✓ 第3课：基础性能测试（优化前基准）");
            _logger.LogInformation("✓ 第4课：索引优化技术");
            _logger.LogInformation("✓ 第5课：高级优化技术");
            _logger.LogInformation("✓ 第6课：最终性能对比与分析");
            _logger.LogInformation("");
            _logger.LogInformation("🎯 主要学习成果：");
            _logger.LogInformation("───────────────────────────────────────");
            _logger.LogInformation("• 掌握了百万级数据的生成和管理");
            _logger.LogInformation("• 学会了各种索引类型的设计和应用");
            _logger.LogInformation("• 了解了查询性能优化的基本原则");
            _logger.LogInformation("• 实践了高级数据库优化技术");
            _logger.LogInformation("• 学会了性能测试和分析方法");
            _logger.LogInformation("");
            _logger.LogInformation("🔍 进阶学习建议：");
            _logger.LogInformation("───────────────────────────────────────");
            _logger.LogInformation("• 学习执行计划分析");
            _logger.LogInformation("• 深入了解查询优化器原理");
            _logger.LogInformation("• 实践分区表设计");
            _logger.LogInformation("• 学习数据库监控和调优");
            _logger.LogInformation("• 了解内存优化技术");
            _logger.LogInformation("");
            _logger.LogInformation("📚 推荐资源：");
            _logger.LogInformation("───────────────────────────────────────");
            _logger.LogInformation("• SQL Server 官方文档");
            _logger.LogInformation("• 《高性能SQL Server》");
            _logger.LogInformation("• 《SQL Server查询性能优化》");
            _logger.LogInformation("• Microsoft Learn 数据库课程");
            _logger.LogInformation("");
            _logger.LogInformation("感谢参与本次数据库优化实验课程！🚀");
        }

        /// <summary>
        /// 运行单独的实验
        /// </summary>
        public async Task RunIndividualExperimentAsync(string experimentName)
        {
            switch (experimentName.ToLower())
            {
                case "setup":
                    await Lesson01_EnvironmentSetupAsync();
                    break;
                case "datagen":
                    await Lesson02_DataGenerationAsync();
                    break;
                case "baseline":
                    await Lesson03_BaselinePerformanceTestAsync();
                    break;
                case "index":
                    await Lesson04_IndexOptimizationAsync();
                    break;
                case "advanced":
                    await Lesson05_AdvancedOptimizationAsync();
                    break;
                case "final":
                    await Lesson06_FinalPerformanceComparisonAsync();
                    break;
                default:
                    _logger.LogWarning($"未知的实验名称: {experimentName}");
                    break;
            }
        }
    }
}
