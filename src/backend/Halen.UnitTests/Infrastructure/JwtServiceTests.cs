using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Halen.UnitTests.Infrastructure;

[TestClass]
public class JwtServiceTests
{
    private JwtService _jwtService = null!;
    private IConfiguration _configuration = null!;
    private const int ExpirationMinutes = 60;

    [TestInitialize]
    public void Initialize()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "super-secret-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "halen-test-issuer",
            ["Jwt:Audience"] = "halen-test-audience",
            ["Jwt:ExpirationMinutes"] = ExpirationMinutes.ToString()
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [TestMethod]
    public void GenerateToken_ValidUser_ContainsExpectedClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jane@example.com",
            UserName = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Patient
        };
        var roles = new List<string> { "Patient" };

        // Act
        var tokenString = _jwtService.GenerateToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "jane@example.com");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == "Jane");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == "Doe");
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Patient");
    }

    [TestMethod]
    public void GenerateToken_ValidUser_ContainsCorrectExpiry()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Patient
        };
        var roles = new List<string> { "Patient" };
        var expectedExpiry = DateTime.UtcNow.AddMinutes(ExpirationMinutes);

        // Act
        var tokenString = _jwtService.GenerateToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.ValidTo.Should().BeCloseTo(expectedExpiry, precision: TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void GenerateRefreshToken_ReturnsNonEmptyBase64String()
    {
        // Arrange — no special setup needed, method has no dependencies

        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        // Verify it is valid base64 by attempting to convert it back
        var action = () => Convert.FromBase64String(refreshToken);
        action.Should().NotThrow();

        Convert.FromBase64String(refreshToken).Length.Should().BeGreaterThan(0);
    }
}
