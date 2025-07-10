# 数据库优化实验课程

🎓 **一个完整的SQL Server数据库性能优化学习项目**

通过百万级真实数据，实践学习数据库优化技术，从基础索引到高级优化技术的全面覆盖。

## 📋 课程概述

本实验课程设计用于帮助开发者和DBA深入理解数据库性能优化技术。通过6个循序渐进的实验课程，您将掌握：

- 🔧 数据库环境配置与管理
- 📊 百万级测试数据生成技术
- 🚀 各种索引类型的设计与应用
- 🎯 查询性能测试与分析
- ⚡ 高级数据库优化技术
- 📈 性能监控与调优方法

## 🛠️ 技术栈

- **数据库**: SQL Server (LocalDB/完整版)
- **开发语言**: C# (.NET 8.0)
- **数据生成**: Bogus 库
- **配置管理**: Microsoft.Extensions.Configuration
- **日志系统**: Microsoft.Extensions.Logging

## 📁 项目结构

```
DBOptimizationStudy/
├── Models/                    # 数据模型
│   └── DataModels.cs         # 用户、部门、订单等实体
├── Services/                  # 核心服务
│   ├── DatabaseService.cs    # 数据库连接与操作
│   ├── DataGenerationService.cs # 测试数据生成
│   └── PerformanceTestService.cs # 性能测试
├── Experiments/              # 实验课程
│   └── DatabaseOptimizationCourse.cs # 课程管理器
├── Scripts/                  # SQL脚本
│   ├── 01-CreateTables.sql   # 表结构创建
│   ├── 02-CreateIndexes.sql  # 索引优化
│   └── 03-AdvancedOptimizations.sql # 高级优化
├── appsettings.json         # 配置文件
└── Program.cs               # 程序入口
```

## 🚀 快速开始

### 前置要求

1. **.NET 8.0 SDK**
2. **SQL Server** (LocalDB 或完整版)
3. **Visual Studio 2022** 或 **VS Code**

### 安装步骤

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd DBOptimizationStudy
   ```

2. **配置数据库连接**
   
   编辑 `appsettings.json` 文件中的连接字符串：
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DBOptimizationStudy;Integrated Security=true;"
     }
   }
   ```

3. **安装依赖包**
   ```bash
   dotnet restore
   ```

4. **运行程序**
   ```bash
   dotnet run
   ```

## 📚 课程内容

### 第1课：环境准备
- 数据库连接测试
- 创建数据库和表结构
- 了解实验数据模型

### 第2课：数据生成
- 生成50个部门
- 生成100万用户记录
- 生成200万订单记录
- 学习批量数据处理技术

### 第3课：基础性能测试
- 执行无索引的查询测试
- 测量基准性能指标
- 识别性能瓶颈

### 第4课：索引优化
- 创建单列索引
- 设计复合索引
- 实现覆盖索引
- 对比优化前后性能

### 第5课：高级优化技术
- 列存储索引
- 过滤索引
- 索引视图
- 数据压缩
- 统计信息优化

### 第6课：最终性能对比
- 全面性能测试
- 优化效果分析
- 最佳实践总结

## 🎯 实验数据

项目将生成以下测试数据：

| 表名 | 记录数 | 用途 |
|------|--------|------|
| Departments | 50 | 部门信息 |
| Users | 1,000,000 | 用户信息 |
| Orders | 2,000,000 | 订单信息 |

## 📊 性能测试类型

- **基础查询**: SELECT、WHERE、特定字段查询
- **JOIN查询**: INNER JOIN、LEFT JOIN、多表JOIN
- **聚合查询**: COUNT、SUM、AVG、GROUP BY
- **排序查询**: 单字段和多字段排序
- **分页查询**: OFFSET/FETCH、ROW_NUMBER
- **范围查询**: BETWEEN、IN、日期范围

## 💡 学习重点

### 索引优化
- **单列索引**: 提高单字段查询性能
- **复合索引**: 优化多字段查询条件
- **覆盖索引**: 减少键查找，提高查询效率
- **过滤索引**: 针对特定条件的优化

### 查询优化
- **执行计划分析**: 理解查询执行过程
- **统计信息**: 影响查询优化器决策
- **查询重写**: 优化查询逻辑
- **存储过程**: 预编译查询优化

### 高级技术
- **列存储索引**: 分析型查询优化
- **分区表**: 大表管理策略
- **数据压缩**: 存储空间优化
- **内存优化**: 高性能数据访问

## 🔧 配置说明

### 数据库配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DBOptimizationStudy;Integrated Security=true;",
    "SqlServerConnection": "Server=.;Database=DBOptimizationStudy;Integrated Security=true;"
  }
}
```

### 实验参数
```json
{
  "DatabaseSettings": {
    "BatchSize": 10000,      // 批量插入大小
    "MaxRecords": 1000000,   // 最大记录数
    "TestIterations": 5      // 性能测试迭代次数
  }
}
```

## 📈 性能指标

程序会测量并报告以下性能指标：

- **执行时间** (毫秒)
- **CPU使用时间**
- **逻辑读取次数**
- **物理读取次数**
- **影响记录数**

## 🎓 学习建议

1. **按顺序执行课程**: 从第1课开始，循序渐进
2. **观察性能变化**: 注意每次优化后的性能提升
3. **分析执行计划**: 使用SQL Server Management Studio查看执行计划
4. **实验不同场景**: 尝试修改查询条件和数据量
5. **记录学习笔记**: 总结优化技术的适用场景

## 🔍 故障排除

### 常见问题

1. **数据库连接失败**
   - 检查SQL Server服务是否启动
   - 验证连接字符串是否正确
   - 确认用户权限

2. **内存不足**
   - 减少批量插入大小 (BatchSize)
   - 降低测试数据量
   - 增加系统内存

3. **查询超时**
   - 增加命令超时时间
   - 检查索引是否创建成功
   - 优化查询语句

## 📖 扩展学习

完成本课程后，建议进一步学习：

- **查询调优**: 深入理解执行计划
- **索引维护**: 重建和重组策略
- **监控工具**: DMV查询和性能计数器
- **高可用性**: 复制、镜像、Always On
- **云数据库**: Azure SQL Database优化

## 🤝 贡献

欢迎提交问题和改进建议！

## 📄 许可证

本项目采用 MIT 许可证。

---

**开始您的数据库优化学习之旅吧！** 🚀
