using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/photo")]
public sealed class EmployeePhotoController(
    AnujHrmsDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    private string StorageRoot
    {
        get
        {
            var configured = configuration["FileStorage:EmployeePhotoRootPath"];
            if (string.IsNullOrWhiteSpace(configured))
                configured = Path.Combine(environment.ContentRootPath, "App_Data", "EmployeePhotos");

            return Path.GetFullPath(configured);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId, ct);

        if (employee is null) return NotFound("Employee not found.");

        if (string.IsNullOrWhiteSpace(employee.ProfilePhotoStoredFileName))
            return NotFound("Employee photo not found.");

        var path = Path.Combine(StorageRoot, employee.ProfilePhotoStoredFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Stored employee photo is missing.");

        var contentType = string.IsNullOrWhiteSpace(employee.ProfilePhotoContentType)
            ? "application/octet-stream"
            : employee.ProfilePhotoContentType;

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid employeeId, IFormFile file, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(x => x.Id == employeeId, ct);
        if (employee is null) return NotFound("Employee not found.");

        if (file is null || file.Length == 0)
            return BadRequest("Please select an employee photo.");

        if (file.Length > MaxFileSize)
            return BadRequest("Maximum employee photo size is 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Allowed photo types: JPG, JPEG, PNG.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest("The uploaded file must be a JPG or PNG image.");

        Directory.CreateDirectory(StorageRoot);

        var oldStoredFileName = employee.ProfilePhotoStoredFileName;
        var id = Guid.NewGuid();
        var storedFileName = id.ToString("N") + extension.ToLowerInvariant();
        var destination = Path.Combine(StorageRoot, storedFileName);

        await using (var input = file.OpenReadStream())
        await using (var output = System.IO.File.Create(destination))
        {
            await input.CopyToAsync(output, ct);
        }

        employee.ProfilePhotoFileName = Path.GetFileName(file.FileName);
        employee.ProfilePhotoStoredFileName = storedFileName;
        employee.ProfilePhotoContentType = file.ContentType;
        employee.ProfilePhotoFileSize = file.Length;
        employee.ProfilePhotoUpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            if (System.IO.File.Exists(destination))
                System.IO.File.Delete(destination);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldStoredFileName) &&
            !oldStoredFileName.Equals(storedFileName, StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(StorageRoot, oldStoredFileName);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        return Ok(new
        {
            employee.Id,
            employee.ProfilePhotoFileName,
            employee.ProfilePhotoContentType,
            employee.ProfilePhotoFileSize,
            employee.ProfilePhotoUpdatedAtUtc
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid employeeId, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(x => x.Id == employeeId, ct);
        if (employee is null) return NotFound("Employee not found.");

        var storedFileName = employee.ProfilePhotoStoredFileName;

        employee.ProfilePhotoFileName = null;
        employee.ProfilePhotoStoredFileName = null;
        employee.ProfilePhotoContentType = null;
        employee.ProfilePhotoFileSize = null;
        employee.ProfilePhotoUpdatedAtUtc = null;

        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(storedFileName))
        {
            var path = Path.Combine(StorageRoot, storedFileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        return NoContent();
    }
}
