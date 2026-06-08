using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;

namespace CivicPulse.Core.Interfaces;

public interface ISlaService
{
    Task CheckAllSlasAsync();
    TimeSpan GetRemainingTime(Complaint complaint);
    double GetElapsedPercent(Complaint complaint);
    string GetSlaStatusColor(Complaint complaint);
}
