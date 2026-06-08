using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IAttachmentService
{
    Task<List<AttachmentDto>> UploadAsync(int complaintId, int userId, IEnumerable<FileUploadDto> files);
    Task<bool> DeleteAsync(int attachmentId);
    Task<AttachmentDto?> GetByIdAsync(int id);
}

public class FileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}
