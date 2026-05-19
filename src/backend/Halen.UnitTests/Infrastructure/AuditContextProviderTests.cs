using System.Net;
using System.Security.Claims;
using Halen.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Halen.UnitTests.Infrastructure;

[TestClass]
public class AuditContextProviderTests
{
    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
    private DefaultHttpContext _httpContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    private AuditContextProvider CreateSut() => new(_httpContextAccessor.Object);

    [TestMethod]
    public void ActorId_WithValidSubClaim_ReturnsGuid()
    {
        var guid = Guid.NewGuid();
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", guid.ToString())
        }));

        var sut = CreateSut();

        Assert.AreEqual(guid, sut.ActorId);
    }

    [TestMethod]
    public void ActorId_WithMissingSubClaim_ReturnsEmpty()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var sut = CreateSut();

        Assert.AreEqual(Guid.Empty, sut.ActorId);
    }

    [TestMethod]
    public void ActorId_WithNonGuidSubClaim_ReturnsEmpty()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "not-a-guid")
        }));

        var sut = CreateSut();

        Assert.AreEqual(Guid.Empty, sut.ActorId);
    }

    [TestMethod]
    public void ActorName_WithGivenAndFamilyName_ReturnsCombined()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("given_name", "Maya"),
            new Claim("family_name", "Chen")
        }));

        var sut = CreateSut();

        Assert.AreEqual("Maya Chen", sut.ActorName);
    }

    [TestMethod]
    public void ActorName_WithOnlyNameClaim_ReturnsNameClaim()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("name", "Full Name")
        }));

        var sut = CreateSut();

        Assert.AreEqual("Full Name", sut.ActorName);
    }

    [TestMethod]
    public void ActorName_WithOnlySubClaim_ReturnsSub()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-123")
        }));

        var sut = CreateSut();

        Assert.AreEqual("user-123", sut.ActorName);
    }

    [TestMethod]
    public void ActorName_WithNoClaims_ReturnsUnknown()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var sut = CreateSut();

        Assert.AreEqual("Unknown", sut.ActorName);
    }

    [TestMethod]
    public void ActorName_WithNullUser_ReturnsUnknown()
    {
        _httpContextAccessor.Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext { User = null! });

        var sut = CreateSut();

        Assert.AreEqual("Unknown", sut.ActorName);
    }

    [TestMethod]
    public void IpAddress_WithRemoteIpAddress_ReturnsIp()
    {
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.42");
        var sut = CreateSut();

        Assert.AreEqual("192.168.1.42", sut.IpAddress);
    }

    [TestMethod]
    public void IpAddress_WithNoRemoteIp_ReturnsUnknown()
    {
        _httpContext.Connection.RemoteIpAddress = null;
        var sut = CreateSut();

        Assert.AreEqual("Unknown", sut.IpAddress);
    }

    [TestMethod]
    public void IpAddress_WithNullHttpContext_ReturnsUnknown()
    {
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var sut = CreateSut();

        Assert.AreEqual("Unknown", sut.IpAddress);
    }
}
