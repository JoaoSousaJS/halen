using FluentAssertions;
using FluentValidation.TestHelper;
using Halen.Application.AuditTrail.Queries;
using Halen.Application.Interfaces;
using Moq;

namespace Halen.UnitTests.AuditTrail;

[TestClass]
public class ExportAuditLogsCsvQueryValidatorTests
{
    private ExportAuditLogsCsvQueryValidator _validator = null!;
    private Mock<ITenantContext> _tenantContextMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(t => t.IsPlatformAdmin).Returns(false);
        _validator = new ExportAuditLogsCsvQueryValidator(_tenantContextMock.Object);
    }

    [TestMethod]
    public void Validate_ValidQuery_Passes()
    {
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), null, null);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validate_MissingFrom_Fails()
    {
        var query = new ExportAuditLogsCsvQuery(null, null, null, null, null, null);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.From);
    }

    [TestMethod]
    public void Validate_FromAfterTo_Fails()
    {
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new ExportAuditLogsCsvQuery(null, null, null, from, to, null);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.From);
    }

    [TestMethod]
    public void Validate_ClinicIdSetByNonPlatformAdmin_Fails()
    {
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), null, Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.ClinicId);
    }

    [TestMethod]
    public void Validate_ClinicIdSetByPlatformAdmin_Passes()
    {
        _tenantContextMock.Setup(t => t.IsPlatformAdmin).Returns(true);
        _validator = new ExportAuditLogsCsvQueryValidator(_tenantContextMock.Object);
        var query = new ExportAuditLogsCsvQuery(null, null, null,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), null, Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.ClinicId);
    }
}
