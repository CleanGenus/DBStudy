using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DBOptimizationStudy.Services;
using DBOptimizationStudy.Experiments;

namespace DBOptimizationStudy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 数据库优化实验课程");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("本程序将指导您学习SQL Server数据库优化技术");
            Console.WriteLine("通过百万级数据的实际操作，掌握性能调优技能");
            Console.WriteLine("");

            // 创建主机和依赖注入容器
            var host = CreateHostBuilder(args).Build();

            try
            {
                // 获取课程管理器
                var course = host.Services.GetRequiredService<DatabaseOptimizationCourse>();

                // 显示菜单
                await ShowMenuAsync(course);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 程序执行错误: {ex.Message}");
                Console.WriteLine("请检查数据库连接配置和SQL Server是否正常运行");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static async Task ShowMenuAsync(DatabaseOptimizationCourse course)
        {
            while (true)
            {
                Console.WriteLine("\n📋 实验课程菜单：");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("1. 🎓 运行完整课程 (推荐新手)");
                Console.WriteLine("2. 📚 第1课：环境准备");
                Console.WriteLine("3. 📚 第2课：数据生成");
                Console.WriteLine("4. 📚 第3课：基础性能测试");
                Console.WriteLine("5. 📚 第4课：索引优化");
                Console.WriteLine("6. 📚 第5课：高级优化");
                Console.WriteLine("7. 📚 第6课：最终对比");
                Console.WriteLine("0. 退出程序");
                Console.WriteLine("");
                Console.Write("请选择 (0-7): ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await course.RunCompleteCourseAsync();
                            break;
                        case "2":
                            await course.RunIndividualExperimentAsync("setup");
                            break;
                        case "3":
                            await course.RunIndividualExperimentAsync("datagen");
                            break;
                        case "4":
                            await course.RunIndividualExperimentAsync("baseline");
                            break;
                        case "5":
                            await course.RunIndividualExperimentAsync("index");
                            break;
                        case "6":
                            await course.RunIndividualExperimentAsync("advanced");
                            break;
                        case "7":
                            await course.RunIndividualExperimentAsync("final");
                            break;
                        case "0":
                            Console.WriteLine("感谢使用数据库优化实验课程！👋");
                            return;
                        default:
                            Console.WriteLine("❌ 无效选择，请输入 0-7 之间的数字");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 执行错误: {ex.Message}");
                    Console.WriteLine("请检查数据库连接和配置");
                }

                if (choice != "0")
                {
                    Console.WriteLine("\n按任意键继续...");
                    Console.ReadKey();
                }
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // 注册服务
                    services.AddSingleton<DatabaseService>();
                    services.AddSingleton<DataGenerationService>();
                    services.AddSingleton<PerformanceTestService>();
                    services.AddSingleton<DatabaseOptimizationCourse>();

                    // 配置日志
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });
    }
}
