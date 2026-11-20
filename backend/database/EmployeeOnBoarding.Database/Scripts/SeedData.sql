IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
    INSERT INTO dbo.Employees (Id, Name, Email, Department, ProfilePictureUrl)
    VALUES
        (NEWID(), N'Aarav Mehta', N'aarav.mehta@example.com', N'Engineering', NULL),
        (NEWID(), N'Priya Sharma', N'priya.sharma@example.com', N'People Operations', NULL),
        (NEWID(), N'Daniel Okafor', N'daniel.okafor@example.com', N'Finance', NULL),
        (NEWID(), N'Sofia Alvarez', N'sofia.alvarez@example.com', N'Customer Success', NULL);
END;
GO
