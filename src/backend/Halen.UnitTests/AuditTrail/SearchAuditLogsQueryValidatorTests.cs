using FluentAssertions;
using FluentValidation.TestHelper;
using Halen.Application.AuditTrail.Queries;
using Halen.Application.Interfaces;
using Moq;

namespace Halen.UnitTests.AuditTrail;

[TestClass]
public class SearchAuditLogsQueryValidatorTests
{
    private SearchAuditLogsQueryValidator _validator = null!;
    private Mock<ITenantContext> _tenantContextMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(t => t.IsPlatformAdmin).Returns(false);
        _validator = new SearchAuditLogsQueryValidator(_tenantContextMock.Object);
    }

    [TestMethod]
    public void Validate_ValidQuery_Passes()
    {
        var query = new SearchAuditLogsQuery(null, null, null, null, null, null, 1, 50);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validate_PageLessThanOne_Fails()
    {
        var query = new SearchAuditLogsQuery(null, null, null, null, null, null, 0, 50);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [TestMethod]
    public void Validate_PageSizeExceeds100_Fails()
    {
        var query = new SearchAuditLogsQuery(null, null, null, null, null, null, 1, 101);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [TestMethod]
    public void Validate_FromAfterTo_Fails()
    {
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new SearchAuditLogsQuery(null, null, null, from, to, null);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.From);
    }

    [TestMethod]
    public void Validate_ClinicIdSetByNonPlatformAdmin_Fails()
    {
        _tenantContextMock.Setup(t => t.IsPlatformAdmin).Returns(false);
        _validator = new SearchAuditLogsQueryValidator(_tenantContextMock.Object);
        var query = new SearchAuditLogsQuery(null, null, null, null, null, Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.ClinicId);
    }

    [TestMethod]
    public void Validate_ClinicIdSetByPlatformAdmin_Passes()
    {
        _tenantContextMock.Setup(t => t.IsPlatformAdmin).Returns(true);
        _validator = new SearchAuditLogsQueryValidator(_tenantContextMock.Object);
        var query = new SearchAuditLogsQuery(null, null, null, null, null, Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.ClinicId);
    }
}
