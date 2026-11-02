USE MediCareDbdotnet;

-- ============================================================================
-- SECURITY WARNING:
-- This script contains placeholder values. Do NOT execute this script directly.
-- Generate a secure PBKDF2 password hash (e.g. using ASP.NET Core PasswordHasher<User>)
-- and replace '<REPLACE_WITH_SECURE_PBKDF2_PASSWORD_HASH>' before executing.
-- ============================================================================

INSERT INTO Users (Name, Email, Role, PasswordHash)
VALUES (
        'Admin User',
        'admin@medicare.com',
        'ADMIN',
        '<REPLACE_WITH_SECURE_PBKDF2_PASSWORD_HASH>'
    ) ON DUPLICATE KEY
UPDATE Role = 'ADMIN';