using FluentAssertions;
using Halen.Application.Admin.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Admin;

[TestClass]
public class ReviewKycCommandValidatorTests
{
    private readonly ReviewKycCommandValidator _validator = new();

    [TestMethod]
    public void ValidApproval_Passes()
    {
        var command = new ReviewKycCommand(Guid.NewGuid(), Guid.NewGuid(), KycDecision.Approved, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void ValidRejectionWithReason_Passes()
    {
        var command = new ReviewKycCommand(Guid.NewGuid(), Guid.NewGuid(), KycDecision.Rejected, "Blurry photo");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void EmptyDoctorProfileId_Fails()
    {
        var command = new ReviewKycCommand(Guid.NewGuid(), Guid.Empty, KycDecision.Approved, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void RejectedWithoutReason_Fails()
    {
        var command = new ReviewKycCommand(Guid.NewGuid(), Guid.NewGuid(), KycDecision.Rejected, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Rejection reason"));
    }

    [TestMethod]
    public void ApprovedWithReason_StillPasses()
    {
        var command = new ReviewKycCommand(Guid.NewGuid(), Guid.NewGuid(), KycDecision.Approved, "Looks good");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
