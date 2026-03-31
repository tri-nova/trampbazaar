USE TrampBazaar;
GO

DECLARE @Email NVARCHAR(256) = N'admin@example.com';
DECLARE @RoleName NVARCHAR(64) = N'Admin';

DECLARE @UserId UNIQUEIDENTIFIER;
DECLARE @RoleId UNIQUEIDENTIFIER;

SELECT TOP 1 @UserId = u.Id
FROM dbo.Users u
WHERE u.Email = @Email;

IF @UserId IS NULL
BEGIN
    THROW 51000, 'Belirtilen e-posta ile kullanici bulunamadi.', 1;
END;

SELECT TOP 1 @RoleId = r.Id
FROM dbo.Roles r
WHERE r.Name = @RoleName;

IF @RoleId IS NULL
BEGIN
    THROW 51000, 'Belirtilen rol bulunamadi.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserRoles ur
    WHERE ur.UserId = @UserId
      AND ur.RoleId = @RoleId
)
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt)
    VALUES (@UserId, @RoleId, SYSUTCDATETIME());
END;
GO
