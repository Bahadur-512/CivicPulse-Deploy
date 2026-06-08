using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class SlaConfig : BaseEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public PriorityLevel Priority { get; set; }
    public int ResolutionDays { get; set; }
    public int WarningThresholdPercent { get; set; } = 80;
    public bool IsActive { get; set; } = true;
}
