-- ====================================
-- 数据库优化实验 - 高级优化技术
-- ====================================

PRINT '开始应用高级优化技术...';

-- ====================================
-- 1. 分区表实验（按日期分区订单表）
-- ====================================

-- 创建分区函数
CREATE PARTITION FUNCTION OrderDatePartitionFunction (DATETIME2)
AS RANGE RIGHT FOR VALUES 
('2024-01-01', '2024-04-01', '2024-07-01', '2024-10-01', '2025-01-01');

-- 创建分区方案
CREATE PARTITION SCHEME OrderDatePartitionScheme
AS PARTITION OrderDatePartitionFunction
ALL TO ([PRIMARY]);

-- 注意：实际应用中，分区表需要重新创建表结构
-- 这里仅作为演示，实际项目中需要谨慎操作

-- ====================================
-- 2. 统计信息优化
-- ====================================

-- 更新统计信息
UPDATE STATISTICS Users;
UPDATE STATISTICS Orders;
UPDATE STATISTICS Departments;

-- 创建筛选统计信息
CREATE STATISTICS ST_Users_ActiveSalary 
ON Users (Salary) 
WHERE IsActive = 1;

CREATE STATISTICS ST_Orders_RecentOrders 
ON Orders (TotalAmount, Status) 
WHERE OrderDate >= '2024-01-01';

-- ====================================
-- 3. 列存储索引（用于分析查询）
-- ====================================

-- 创建非聚集列存储索引用于分析查询
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_Orders_Columnstore
ON Orders (UserId, OrderDate, TotalAmount, Status);

-- ====================================
-- 4. 过滤索引
-- ====================================

-- 仅为活跃用户创建索引
CREATE NONCLUSTERED INDEX IX_Users_Active_Salary 
ON Users (Salary) 
WHERE IsActive = 1;

-- 仅为已完成订单创建索引
CREATE NONCLUSTERED INDEX IX_Orders_Delivered_Date 
ON Orders (OrderDate, TotalAmount) 
WHERE Status = 'Delivered';

-- ====================================
-- 5. 内存优化表实验（需要MEMORY_OPTIMIZED_DATA文件组）
-- ====================================

-- 注意：这需要数据库配置内存优化文件组
-- 这里仅作为概念演示，实际使用需要额外配置

/*
-- 创建内存优化表用于实时统计
CREATE TABLE UserStatistics_Memory (
    UserId INT NOT NULL PRIMARY KEY NONCLUSTERED,
    OrderCount INT NOT NULL,
    TotalSpent DECIMAL(18,2) NOT NULL,
    LastOrderDate DATETIME2 NOT NULL,
    INDEX IX_UserStatistics_Memory_LastOrderDate NONCLUSTERED (LastOrderDate)
) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
*/

-- ====================================
-- 6. 查询提示和优化
-- ====================================

-- 创建视图用于复杂查询优化
CREATE VIEW vw_UserOrderSummary
WITH SCHEMABINDING
AS
SELECT 
    u.Id as UserId,
    u.FirstName,
    u.LastName,
    u.Email,
    d.Name as DepartmentName,
    COUNT_BIG(o.Id) as OrderCount,
    SUM(o.TotalAmount) as TotalSpent,
    MAX(o.OrderDate) as LastOrderDate
FROM dbo.Users u
INNER JOIN dbo.Departments d ON u.DepartmentId = d.Id
LEFT JOIN dbo.Orders o ON u.Id = o.UserId
WHERE u.IsActive = 1
GROUP BY u.Id, u.FirstName, u.LastName, u.Email, d.Name;

-- 为视图创建唯一聚集索引（使其成为索引视图）
CREATE UNIQUE CLUSTERED INDEX IX_vw_UserOrderSummary_UserId 
ON vw_UserOrderSummary (UserId);

-- ====================================
-- 7. 全文索引（用于文本搜索）
-- ====================================

-- 创建全文目录
CREATE FULLTEXT CATALOG ft_DBOptimizationStudy;

-- 为用户表的Notes字段创建全文索引
CREATE FULLTEXT INDEX ON Users (Notes)
KEY INDEX PK__Users__3214EC0701234567  -- 使用主键
ON ft_DBOptimizationStudy;

-- ====================================
-- 8. 存储过程优化示例
-- ====================================

-- 优化的用户查询存储过程
CREATE PROCEDURE sp_GetUsersByDepartmentOptimized
    @DepartmentId INT,
    @IsActive BIT = 1,
    @PageNumber INT = 1,
    @PageSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 使用参数化查询和索引提示
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    SELECT 
        u.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Salary,
        d.Name as DepartmentName
    FROM Users u WITH (INDEX(IX_Users_IsActive_DepartmentId))
    INNER JOIN Departments d ON u.DepartmentId = d.Id
    WHERE u.DepartmentId = @DepartmentId 
      AND u.IsActive = @IsActive
    ORDER BY u.LastName, u.FirstName
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    -- 返回总记录数
    SELECT COUNT(*) as TotalRecords
    FROM Users u WITH (INDEX(IX_Users_IsActive_DepartmentId))
    WHERE u.DepartmentId = @DepartmentId 
      AND u.IsActive = @IsActive;
END;

-- ====================================
-- 9. 数据压缩
-- ====================================

-- 启用页面压缩以节省存储空间
ALTER TABLE Orders REBUILD WITH (DATA_COMPRESSION = PAGE);
ALTER TABLE Users REBUILD WITH (DATA_COMPRESSION = PAGE);

-- ====================================
-- 10. 执行计划分析辅助视图
-- ====================================

-- 创建性能监控视图
CREATE VIEW vw_QueryPerformanceStats
AS
SELECT 
    qs.sql_handle,
    qs.plan_handle,
    qs.creation_time,
    qs.execution_count,
    qs.total_worker_time / 1000 as total_cpu_time_ms,
    qs.total_elapsed_time / 1000 as total_elapsed_time_ms,
    qs.total_logical_reads,
    qs.total_physical_reads,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(st.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.text LIKE '%Users%' OR st.text LIKE '%Orders%' OR st.text LIKE '%Departments%';

PRINT '高级优化技术应用完成！';
PRINT '';
PRINT '应用的优化技术:';
PRINT '1. 分区表设计（概念演示）';
PRINT '2. 统计信息优化';
PRINT '3. 列存储索引';
PRINT '4. 过滤索引';
PRINT '5. 索引视图';
PRINT '6. 全文索引';
PRINT '7. 优化存储过程';
PRINT '8. 数据压缩';
PRINT '9. 性能监控视图';
PRINT '';
PRINT '下一步: 运行性能测试验证优化效果';
