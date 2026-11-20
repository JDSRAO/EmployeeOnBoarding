using EmployeeOnBoarding.Api.Models;
using EmployeeOnBoarding.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOnBoarding.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Notification>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Notification>>> GetRecent(
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var notifications = await repository.GetRecentAsync(take, cancellationToken);
        return Ok(notifications);
    }
}
