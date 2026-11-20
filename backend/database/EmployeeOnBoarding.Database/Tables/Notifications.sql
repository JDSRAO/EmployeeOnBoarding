CREATE TABLE [dbo].[Notifications]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [EventId] UNIQUEIDENTIFIER NOT NULL,
    [EmployeeId] UNIQUEIDENTIFIER NOT NULL,
    [EmployeeName] NVARCHAR(150) NOT NULL,
    [EventType] NVARCHAR(20) NOT NULL,
    [Message] NVARCHAR(400) NOT NULL,
    [CreatedAtUtc] DATETIMEOFFSET(0) NOT NULL CONSTRAINT [DF_Notifications_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Notifications_EventId] UNIQUE NONCLUSTERED ([EventId])
);
GO

CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAtUtc]
    ON [dbo].[Notifications] ([CreatedAtUtc] DESC);
GO
