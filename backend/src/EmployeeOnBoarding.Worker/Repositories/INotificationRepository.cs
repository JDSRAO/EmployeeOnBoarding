using EmployeeOnBoarding.Contracts;

namespace EmployeeOnBoarding.Worker.Repositories;

public interface INotificationRepository
{
    Task<bool> CreateIfNotExistsAsync(EmployeeChangedEvent employeeChangedEvent, CancellationToken cancellationToken);
}
