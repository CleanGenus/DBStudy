-- ====================================
-- 数据库优化实验 - 示例查询脚本
-- ====================================

-- 这个脚本包含了各种类型的查询示例
-- 可以在SQL Server Management Studio中运行，观察执行计划

USE DBOptimizationStudy;

-- ====================================
-- 1. 基础查询示例
-- ====================================

-- 简单SELECT查询
SELECT TOP 1000 * FROM Users;

-- WHERE条件查询
SELECT * FROM Users WHERE IsActive = 1;

-- 特定字段查询
SELECT Id, FirstName, LastName, Email FROM Users WHERE DepartmentId = 1;

-- ====================================
-- 2. JOIN查询示例
-- ====================================

-- INNER JOIN
SELECT u.FirstName, u.LastName, d.Name as DepartmentName
FROM Users u
INNER JOIN Departments d ON u.DepartmentId = d.Id
WHERE u.IsActive = 1;

-- LEFT JOIN with GROUP BY
SELECT u.FirstName, u.LastName, COUNT(o.Id) as OrderCount
FROM Users u
LEFT JOIN Orders o ON u.Id = o.UserId
WHERE u.IsActive = 1
GROUP BY u.Id, u.FirstName, u.LastName;

-- 多表JOIN
SELECT u.FirstName, u.LastName, d.Name as DepartmentName, 
       COUNT(o.Id) as OrderCount, SUM(o.TotalAmount) as TotalSpent
FROM Users u
INNER JOIN Departments d ON u.DepartmentId = d.Id
LEFT JOIN Orders o ON u.Id = o.UserId
WHERE u.IsActive = 1
GROUP BY u.Id, u.FirstName, u.LastName, d.Name
HAVING COUNT(o.Id) > 0;

-- ====================================
-- 3. 聚合查询示例
-- ====================================

-- COUNT查询
SELECT COUNT(*) FROM Users WHERE IsActive = 1;

-- SUM和AVG查询
SELECT DepartmentId, COUNT(*) as UserCount, 
       AVG(Salary) as AvgSalary, SUM(Salary) as TotalSalary
FROM Users 
WHERE IsActive = 1 
GROUP BY DepartmentId;

-- 复杂聚合查询
SELECT YEAR(OrderDate) as OrderYear, MONTH(OrderDate) as OrderMonth,
       COUNT(*) as OrderCount, SUM(TotalAmount) as MonthlyRevenue,
       AVG(TotalAmount) as AvgOrderValue
FROM Orders
GROUP BY YEAR(OrderDate), MONTH(OrderDate)
ORDER BY OrderYear DESC, OrderMonth DESC;

-- ====================================
-- 4. 排序查询示例
-- ====================================

-- 单字段排序
SELECT TOP 10000 * FROM Users ORDER BY LastName;

-- 多字段排序
SELECT TOP 10000 * FROM Users ORDER BY DepartmentId, Salary DESC, LastName;

-- 日期排序
SELECT TOP 10000 * FROM Orders ORDER BY OrderDate DESC;

-- ====================================
-- 5. 分页查询示例
-- ====================================

-- OFFSET/FETCH分页
SELECT * FROM Users 
WHERE IsActive = 1
ORDER BY Id
OFFSET 50000 ROWS FETCH NEXT 100 ROWS ONLY;

-- ROW_NUMBER分页
WITH PagedUsers AS (
    SELECT *, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
    FROM Users WHERE IsActive = 1
)
SELECT * FROM PagedUsers WHERE RowNum BETWEEN 50001 AND 50100;

-- ====================================
-- 6. 范围查询示例
-- ====================================

-- 数值范围查询
SELECT * FROM Users WHERE Salary BETWEEN 50000 AND 100000;

-- 日期范围查询
SELECT * FROM Orders 
WHERE OrderDate BETWEEN '2024-01-01' AND '2024-12-31';

-- IN查询
SELECT * FROM Users WHERE DepartmentId IN (1, 2, 3, 4, 5);

-- ====================================
-- 7. 子查询示例
-- ====================================

-- EXISTS子查询
SELECT * FROM Users u
WHERE EXISTS (
    SELECT 1 FROM Orders o 
    WHERE o.UserId = u.Id AND o.Status = 'Delivered'
);

-- IN子查询
SELECT * FROM Users 
WHERE Id IN (
    SELECT DISTINCT UserId FROM Orders 
    WHERE TotalAmount > 1000
);

-- 相关子查询
SELECT u.*, 
       (SELECT COUNT(*) FROM Orders o WHERE o.UserId = u.Id) as OrderCount
FROM Users u
WHERE u.IsActive = 1;

-- ====================================
-- 8. 窗口函数示例
-- ====================================

-- ROW_NUMBER
SELECT FirstName, LastName, Salary, DepartmentId,
       ROW_NUMBER() OVER (PARTITION BY DepartmentId ORDER BY Salary DESC) as SalaryRank
FROM Users
WHERE IsActive = 1;

-- RANK和DENSE_RANK
SELECT FirstName, LastName, Salary, DepartmentId,
       RANK() OVER (ORDER BY Salary DESC) as SalaryRank,
       DENSE_RANK() OVER (ORDER BY Salary DESC) as DenseSalaryRank
FROM Users
WHERE IsActive = 1;

-- LAG和LEAD
SELECT OrderDate, TotalAmount,
       LAG(TotalAmount) OVER (ORDER BY OrderDate) as PrevOrderAmount,
       LEAD(TotalAmount) OVER (ORDER BY OrderDate) as NextOrderAmount
FROM Orders
WHERE UserId = 1;

-- ====================================
-- 9. CTE (公用表表达式) 示例
-- ====================================

-- 简单CTE
WITH HighSalaryUsers AS (
    SELECT * FROM Users WHERE Salary > 80000
)
SELECT d.Name, COUNT(*) as HighSalaryCount
FROM HighSalaryUsers h
INNER JOIN Departments d ON h.DepartmentId = d.Id
GROUP BY d.Name;

-- 递归CTE (层级结构)
WITH DepartmentHierarchy AS (
    -- 锚点：顶级部门
    SELECT Id, Name, ManagerId, 0 as Level
    FROM Departments 
    WHERE ManagerId IS NULL
    
    UNION ALL
    
    -- 递归：子部门
    SELECT d.Id, d.Name, d.ManagerId, h.Level + 1
    FROM Departments d
    INNER JOIN DepartmentHierarchy h ON d.ManagerId = h.Id
)
SELECT * FROM DepartmentHierarchy;

-- ====================================
-- 10. 性能分析查询
-- ====================================

-- 查看索引使用情况
SELECT 
    i.name as IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE OBJECTPROPERTY(s.object_id, 'IsUserTable') = 1;

-- 查看缺失的索引
SELECT 
    mig.index_group_handle,
    mid.index_handle,
    mid.database_id,
    mid.object_id,
    OBJECT_NAME(mid.object_id) as TableName,
    migs.avg_total_user_cost,
    migs.avg_user_impact,
    migs.user_seeks,
    migs.user_scans,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID();

-- 查看查询统计信息
SELECT TOP 10
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
WHERE st.text LIKE '%Users%'
ORDER BY qs.total_elapsed_time DESC;

-- ====================================
-- 性能测试脚本
-- ====================================

-- 启用IO和时间统计
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- 测试查询（在这里放入要测试的查询）
SELECT COUNT(*) FROM Users WHERE IsActive = 1;

-- 关闭统计
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
