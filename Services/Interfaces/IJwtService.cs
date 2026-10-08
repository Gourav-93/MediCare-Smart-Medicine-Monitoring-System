namespace MediCare.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken( int userId , string email , string role );
}