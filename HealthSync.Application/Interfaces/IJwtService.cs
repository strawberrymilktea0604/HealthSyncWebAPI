using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
}