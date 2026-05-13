using Halen.Domain.Entities;

namespace Halen.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, IList<string> roles);
    string GenerateRefreshToken();
}
