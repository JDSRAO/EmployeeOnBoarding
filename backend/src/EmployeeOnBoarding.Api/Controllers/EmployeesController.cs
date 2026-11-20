using EmployeeOnBoarding.Api.Models;
using EmployeeOnBoarding.Api.Repositories;
using EmployeeOnBoarding.Api.Services;
using EmployeeOnBoarding.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EmployeeOnBoarding.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(
    IEmployeeRepository repository,
    IBlobStorageService blobStorage,
    IEventPublisher eventPublisher,
    IOptions<StorageOptions> storageOptions,
    ILogger<EmployeesController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Employee>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Employee>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await repository.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Employee>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Employee>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType<Employee>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Employee>> Create(
        [FromForm] EmployeeForm form,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateForm(form);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var now = DateTimeOffset.UtcNow;
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = form.Name.Trim(),
            Email = form.Email.Trim().ToLowerInvariant(),
            Department = form.Department.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (form.ProfilePicture is not null)
        {
            await using var stream = form.ProfilePicture.OpenReadStream();
            employee.ProfilePictureUrl = await blobStorage.UploadProfilePictureAsync(
                stream,
                form.ProfilePicture.FileName,
                form.ProfilePicture.ContentType,
                cancellationToken);
        }

        var created = await repository.CreateAsync(employee, cancellationToken);
        await eventPublisher.PublishEmployeeChangedAsync(
            EmployeeChangedEvent.Create(
                created.Id,
                created.Name,
                created.Email,
                created.Department,
                EmployeeEventType.Created),
            cancellationToken);

        logger.LogInformation("Created employee {EmployeeId}", created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType<Employee>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Employee>> Update(
        Guid id,
        [FromForm] EmployeeForm form,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateForm(form);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var previousPictureUrl = existing.ProfilePictureUrl;

        existing.Name = form.Name.Trim();
        existing.Email = form.Email.Trim().ToLowerInvariant();
        existing.Department = form.Department.Trim();
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (form.ProfilePicture is not null)
        {
            await using var stream = form.ProfilePicture.OpenReadStream();
            existing.ProfilePictureUrl = await blobStorage.UploadProfilePictureAsync(
                stream,
                form.ProfilePicture.FileName,
                form.ProfilePicture.ContentType,
                cancellationToken);
        }

        var updated = await repository.UpdateAsync(existing, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        if (form.ProfilePicture is not null)
        {
            await blobStorage.DeleteProfilePictureAsync(previousPictureUrl, cancellationToken);
        }

        await eventPublisher.PublishEmployeeChangedAsync(
            EmployeeChangedEvent.Create(
                updated.Id,
                updated.Name,
                updated.Email,
                updated.Department,
                EmployeeEventType.Updated),
            cancellationToken);

        logger.LogInformation("Updated employee {EmployeeId}", updated.Id);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        await blobStorage.DeleteProfilePictureAsync(existing.ProfilePictureUrl, cancellationToken);
        logger.LogInformation("Deleted employee {EmployeeId}", id);
        return NoContent();
    }

    private ActionResult? ValidateForm(EmployeeForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            ModelState.AddModelError(nameof(EmployeeForm.Name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(form.Email))
        {
            ModelState.AddModelError(nameof(EmployeeForm.Email), "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(form.Department))
        {
            ModelState.AddModelError(nameof(EmployeeForm.Department), "Department is required.");
        }

        var file = form.ProfilePicture;
        if (file is null || file.Length == 0)
        {
            return ModelState.IsValid ? null : ValidationProblem(ModelState);
        }

        if (file.Length > storageOptions.Value.MaxProfilePictureBytes)
        {
            ModelState.AddModelError(
                nameof(EmployeeForm.ProfilePicture),
                "The profile picture must be 5 MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType) ||
            !AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            ModelState.AddModelError(
                nameof(EmployeeForm.ProfilePicture),
                "Only JPEG, PNG and WebP images are supported.");
        }

        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }
}
