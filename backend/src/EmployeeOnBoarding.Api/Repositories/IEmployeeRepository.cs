using EmployeeOnBoarding.Api.Models;

namespace EmployeeOnBoarding.Api.Repositories;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken);
    Task<Employee?> UpdateAsync(Employee employee, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
