using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameUrdu { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int DefaultSlaDays { get; set; }
    public PriorityLevel DefaultPriority { get; set; }
    public bool IsActive { get; set; } = true;
    public string Keywords { get; set; } = string.Empty;

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
