using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/documents")]
public sealed class EmployeeDocumentsController(
    AnujHrmsDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    private const long MaxFileSize = 10 * 1024 * 1024;

    private string StorageRoot
    {
        get
        {
            var configured = configuration["FileStorage:RootPath"];
            if (string.IsNullOrWhiteSpace(configured))
                configured = Path.Combine(environment.ContentRootPath, "App_Data", "EmployeeDocuments");

            return Path.GetFullPath(configured);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(Guid employeeId, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId, ct))
            return NotFound("Employee not found.");

        var documents = await db.EmployeeDocuments
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.IsActive)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new
            {
                x.Id, x.DocumentType, x.DocumentNumber, x.OriginalFileName,
                x.ContentType, x.FileSize, x.IssueDate, x.ExpiryDate,
                x.Remarks, x.UploadedAtUtc, x.IsActive
            })
            .ToListAsync(ct);

        return Ok(documents);
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> Upload(
        Guid employeeId,
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] string? documentNumber,
        [FromForm] DateOnly? issueDate,
        [FromForm] DateOnly? expiryDate,
        [FromForm] string? remarks,
        CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId, ct))
            return NotFound("Employee not found.");

        if (file is null || file.Length == 0)
            return BadRequest("Please select a document.");

        if (file.Length > MaxFileSize)
            return BadRequest("Maximum document size is 10 MB.");

        if (string.IsNullOrWhiteSpace(documentType))
            return BadRequest("DocumentType is required.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Allowed file types: PDF, JPG, JPEG, PNG, DOC, DOCX.");

        if (expiryDate.HasValue && issueDate.HasValue && expiryDate < issueDate)
            return BadRequest("Expiry date cannot be before issue date.");

        Directory.CreateDirectory(StorageRoot);

        var id = Guid.NewGuid();
        var storedFileName = id.ToString("N") + extension.ToLowerInvariant();
        var destination = Path.Combine(StorageRoot, storedFileName);

        await using (var input = file.OpenReadStream())
        await using (var output = System.IO.File.Create(destination))
        {
            await input.CopyToAsync(output, ct);
        }

        var document = new EmployeeDocument
        {
            Id = id,
            EmployeeId = employeeId,
            DocumentType = documentType.Trim(),
            DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim(),
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSize = file.Length,
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            UploadedAtUtc = DateTime.UtcNow
        };

        try
        {
            db.EmployeeDocuments.Add(document);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            if (System.IO.File.Exists(destination))
                System.IO.File.Delete(destination);
            throw;
        }

        return Ok(new
        {
            document.Id,
            document.DocumentType,
            document.DocumentNumber,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.IssueDate,
            document.ExpiryDate,
            document.Remarks,
            document.UploadedAtUtc
        });
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var document = await db.EmployeeDocuments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && x.IsActive, ct);

        if (document is null) return NotFound("Document not found.");

        var path = Path.Combine(StorageRoot, document.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound("Stored document file is missing.");

        return PhysicalFile(path, document.ContentType, document.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Deactivate(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        var document = await db.EmployeeDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId, ct);

        if (document is null) return NotFound("Document not found.");

        document.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}