using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto);
    Task<FeedbackDto?> GetByComplaintAsync(int complaintId);
    Task<bool> HasFeedbackAsync(int complaintId, int citizenId);
    Task<List<FeedbackDto>> GetAllAsync();
    Task<PagedResult<FeedbackDto>> GetAllPagedAsync(int page, int pageSize, string? search = null);
    Task<double> GetAverageRatingAsync();
    Task<int> GetCountAsync();
}
