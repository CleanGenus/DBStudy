-- ====================================
-- 数据库优化实验 - 索引优化脚本
-- ====================================

PRINT '开始创建优化索引...';

-- ====================================
-- 用户表索引优化
-- ====================================

-- 1. 频繁查询字段的非聚集索引
CREATE NONCLUSTERED INDEX IX_Users_Email 
ON Users (Email);

CREATE NONCLUSTERED INDEX IX_Users_IsActive 
ON Users (IsActive);

CREATE NONCLUSTERED INDEX IX_Users_DepartmentId 
ON Users (DepartmentId);

-- 2. 复合索引 - 用于多条件查询
CREATE NONCLUSTERED INDEX IX_Users_IsActive_DepartmentId 
ON Users (IsActive, DepartmentId);

-- 3. 包含列索引 - 用于覆盖索引
CREATE NONCLUSTERED INDEX IX_Users_DepartmentId_Covering 
ON Users (DepartmentId) 
INCLUDE (FirstName, LastName, Email, Salary);

-- 4. 日期范围查询索引
CREATE NONCLUSTERED INDEX IX_Users_CreatedDate 
ON Users (CreatedDate);

CREATE NONCLUSTERED INDEX IX_Users_LastLoginDate 
ON Users (LastLoginDate);

-- 5. 薪资范围查询索引
CREATE NONCLUSTERED INDEX IX_Users_Salary 
ON Users (Salary);

-- ====================================
-- 订单表索引优化
-- ====================================

-- 1. 外键索引
CREATE NONCLUSTERED INDEX IX_Orders_UserId 
ON Orders (UserId);

-- 2. 订单日期索引
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate 
ON Orders (OrderDate);

-- 3. 订单状态索引
CREATE NONCLUSTERED INDEX IX_Orders_Status 
ON Orders (Status);

-- 4. 复合索引 - 用户和日期
CREATE NONCLUSTERED INDEX IX_Orders_UserId_OrderDate 
ON Orders (UserId, OrderDate);

-- 5. 覆盖索引 - 用于订单统计
CREATE NONCLUSTERED INDEX IX_Orders_UserId_Covering 
ON Orders (UserId) 
INCLUDE (OrderDate, TotalAmount, Status);

-- 6. 日期和状态复合索引
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate_Status 
ON Orders (OrderDate, Status);

-- ====================================
-- 部门表索引优化
-- ====================================

-- 1. 部门名称索引（用于查找和JOIN）
CREATE NONCLUSTERED INDEX IX_Departments_Name 
ON Departments (Name);

-- 2. 活跃状态索引
CREATE NONCLUSTERED INDEX IX_Departments_IsActive 
ON Departments (IsActive);

-- ====================================
-- 性能测试结果表索引
-- ====================================

-- 1. 测试日期索引
CREATE NONCLUSTERED INDEX IX_PerformanceTestResults_TestDate 
ON PerformanceTestResults (TestDate);

-- 2. 测试名称索引
CREATE NONCLUSTERED INDEX IX_PerformanceTestResults_TestName 
ON PerformanceTestResults (TestName);

PRINT '基础索引创建完成！';
PRINT '';
PRINT '创建的索引类型:';
PRINT '1. 单列索引 - 提高单字段查询性能';
PRINT '2. 复合索引 - 提高多字段查询性能';
PRINT '3. 覆盖索引 - 减少键查找，提高查询效率';
PRINT '4. 外键索引 - 提高JOIN操作性能';
PRINT '';
PRINT '下一步: 运行性能测试比较优化前后的差异';
