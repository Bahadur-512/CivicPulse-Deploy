using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository<ComplaintAttachment> _attachmentRepo;
    private readonly IConfiguration _configuration;

    public AttachmentService(IRepository<ComplaintAttachment> attachmentRepo, IConfiguration configuration)
    {
        _attachmentRepo = attachmentRepo;
        _configuration = configuration;
    }

    public async Task<List<AttachmentDto>> UploadAsync(int complaintId, int userId, IEnumerable<FileUploadDto> files)
    {
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration.GetValue<string>("AppSettings:UploadPath") ?? "wwwroot/uploads");
        Directory.CreateDirectory(uploadPath);

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var maxSizeBytes = 5 * 1024 * 1024;
        var maxFiles = 5;

        var existingCount = await _attachmentRepo.CountAsync(a => a.ComplaintId == complaintId);
        var results = new List<AttachmentDto>();

        foreach (var file in files.Take(maxFiles - existingCount))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) continue;
            if (file.Size > maxSizeBytes || file.Size == 0) continue;

            var uuid = Guid.NewGuid().ToString("N");
            var savedFileName = $"{uuid}{ext}";
            var filePath = Path.Combine(uploadPath, savedFileName);

            await System.IO.File.WriteAllBytesAsync(filePath, file.Content);

            var attachment = new ComplaintAttachment
            {
                ComplaintId = complaintId,
                FileName = file.FileName,
                FilePath = $"/uploads/{savedFileName}",
                FileType = file.ContentType,
                FileSizeBytes = file.Size,
                UploadedById = userId
            };

            await _attachmentRepo.AddAsync(attachment);
            results.Add(new AttachmentDto
            {
                FileName = file.FileName,
                FilePath = $"/uploads/{savedFileName}",
                FileType = file.ContentType,
                FileSizeBytes = file.Size
            });
        }

        await _attachmentRepo.SaveChangesAsync();
        return results;
    }

    public async Task<bool> DeleteAsync(int attachmentId)
    {
        var attachment = await _attachmentRepo.GetByIdAsync(attachmentId);
        if (attachment == null) return false;

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

        _attachmentRepo.Remove(attachment);
        await _attachmentRepo.SaveChangesAsync();
        return true;
    }

    public async Task<AttachmentDto?> GetByIdAsync(int id)
    {
        var attachment = await _attachmentRepo.GetByIdAsync(id);
        return attachment == null ? null : new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            FileType = attachment.FileType,
            FileSizeBytes = attachment.FileSizeBytes
        };
    }
}
