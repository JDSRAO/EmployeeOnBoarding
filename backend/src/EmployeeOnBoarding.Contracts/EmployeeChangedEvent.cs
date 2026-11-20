namespace EmployeeOnBoarding.Contracts;

public sealed record EmployeeChangedEvent(
    Guid EventId,
    Guid EmployeeId,
    string Name,
    string Email,
    string Department,
    string EventType,
    DateTimeOffset OccurredAtUtc)
{
    public static EmployeeChangedEvent Create(
        Guid employeeId,
        string name,
        string email,
        string department,
        EmployeeEventType eventType) =>
        new(
            Guid.NewGuid(),
            employeeId,
            name,
            email,
            department,
            eventType.ToString(),
            DateTimeOffset.UtcNow);
}

public enum EmployeeEventType
{
    Created,
    Updated
}
