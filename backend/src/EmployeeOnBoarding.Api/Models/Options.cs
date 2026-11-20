namespace EmployeeOnBoarding.Api.Models;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string ContainerName { get; set; } = "profile-pictures";
    public long MaxProfilePictureBytes { get; set; } = 5 * 1024 * 1024;
}

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string QueueName { get; set; } = "employee-events";
}
