namespace SmartMeterReadingDash.Models.Dashboard
{
    public class UserAccessScope
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string? Company { get; set; }
        public string? Department { get; set; }
        public bool IsSuperAdmin => string.Equals( Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase);
        public bool HasCompanyRestriction => !IsSuperAdmin &&  !string.IsNullOrWhiteSpace(Company);             
        public bool HasDepartmentRestriction => !IsSuperAdmin && !string.IsNullOrWhiteSpace(Department); 
    }
}
