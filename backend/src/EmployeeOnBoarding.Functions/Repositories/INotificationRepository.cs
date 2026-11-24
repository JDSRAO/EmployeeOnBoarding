using EmployeeOnBoarding.Contracts;

namespace EmployeeOnBoarding.Functions.Repositories;

public interface INotificationRepository
{
  Task<bool> CreateIfNotExistsAsync(
      EmployeeChangedEvent employeeChangedEvent,
      CancellationToken cancellationToken);
}
