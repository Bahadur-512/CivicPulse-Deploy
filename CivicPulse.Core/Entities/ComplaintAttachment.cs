namespace CivicPulse.Core.Entities;

public class ComplaintAttachment : BaseEntity
{
    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int UploadedById { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
}
