-- ====================================
-- 数据库优化实验 - 初始化脚本
-- ====================================

-- 删除已存在的表（如果存在）
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Departments', 'U') IS NOT NULL DROP TABLE Departments;
IF OBJECT_ID('PerformanceTestResults', 'U') IS NOT NULL DROP TABLE PerformanceTestResults;

-- 创建部门表
CREATE TABLE Departments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ManagerId INT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

-- 创建用户表
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,        -- 增加长度
    LastName NVARCHAR(100) NOT NULL,         -- 增加长度
    Email NVARCHAR(255) NOT NULL,            -- 增加长度以支持长邮箱
    PhoneNumber NVARCHAR(50),                -- 增加长度以支持国际号码
    DateOfBirth DATE NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME2 NULL,
    City NVARCHAR(100),                      -- 增加长度
    State NVARCHAR(100),                     -- 增加长度
    Country NVARCHAR(100),                   -- 增加长度
    ZipCode NVARCHAR(20),                    -- 增加长度
    Salary DECIMAL(18,2) NOT NULL,
    DepartmentId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Notes NVARCHAR(MAX),
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

-- 创建订单表
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL,            -- 增加长度
    ShippingAddress NVARCHAR(1000),          -- 增加长度以支持完整地址
    ShippedDate DATETIME2 NULL,
    DeliveredDate DATETIME2 NULL,
    Notes NVARCHAR(MAX),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 创建性能测试结果表
CREATE TABLE PerformanceTestResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TestName NVARCHAR(200) NOT NULL,
    QueryType NVARCHAR(50) NOT NULL,
    ExecutionTimeMs BIGINT NOT NULL,
    RecordsAffected INT NOT NULL DEFAULT 0,
    TestDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    AdditionalInfo NVARCHAR(MAX)
);

-- 基础索引（仅主键自动创建的聚集索引）
-- 这是优化前的状态，我们将在实验中逐步添加优化索引

PRINT '数据库表结构创建完成！';
PRINT '表信息:';
PRINT '- Departments: 部门表';
PRINT '- Users: 用户表';
PRINT '- Orders: 订单表';
PRINT '- PerformanceTestResults: 性能测试结果表';
PRINT '';
PRINT '下一步: 运行数据生成程序创建测试数据';
