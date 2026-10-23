USE MediCareDbdotnet;
INSERT INTO Users (Name, Email, Role, PasswordHash)
VALUES ('Admin User', 'admin@medicare.com', 'ADMIN', 'AQAAAAIAAYagAAAAEL3pGl/7dPYE3H+wCTVcDY7RztJ2AK2SeOkotXB8+HbOKCEwcBCdQSJrvS1jtDUULA==')
ON DUPLICATE KEY UPDATE Role = 'ADMIN';
