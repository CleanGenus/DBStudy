using System;

namespace DBOptimizationStudy.Models
{
    /// <summary>
    /// 用户模型 - 用于数据库优化实验
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// 部门模型
    /// </summary>
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 订单模型
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// 性能测试结果模型
    /// </summary>
    public class PerformanceTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string QueryType { get; set; } = string.Empty;
        public long ExecutionTimeMs { get; set; }
        public int RecordsAffected { get; set; }
        public DateTime TestDate { get; set; }
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}
