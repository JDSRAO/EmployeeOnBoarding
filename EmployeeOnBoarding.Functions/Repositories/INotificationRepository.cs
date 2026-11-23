using EmployeeOnBoarding.Contracts;

namespace EmployeeOnBoarding.Functions.Repositories;
//avoid duplicate
public interface INotificationRepository
{
    Task<bool> CreateIfNotExistsAsync(
        EmployeeChangedEvent employeeChangedEvent,
        CancellationToken cancellationToken);
}
