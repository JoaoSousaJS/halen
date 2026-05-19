using FluentAssertions;
using FluentValidation.TestHelper;
using Halen.Application.Clinics.Commands;
using Halen.Domain.Enums;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class ClinicValidatorTests
{
    [TestMethod]
    public void CreateClinic_ValidData_Passes()
    {
        var validator = new CreateClinicCommandValidator();
        var result = validator.TestValidate(new CreateClinicCommand("Good Clinic", "good-clinic"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateClinic_EmptyName_Fails()
    {
        var validator = new CreateClinicCommandValidator();
        var result = validator.TestValidate(new CreateClinicCommand("", "valid-slug"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void CreateClinic_InvalidSlug_Fails()
    {
        var validator = new CreateClinicCommandValidator();
        var result = validator.TestValidate(new CreateClinicCommand("Name", "INVALID SLUG!"));
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [TestMethod]
    public void CreateClinic_SlugTooShort_Fails()
    {
        var validator = new CreateClinicCommandValidator();
        var result = validator.TestValidate(new CreateClinicCommand("Name", "ab"));
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [TestMethod]
    public void SetFeatureFlag_ValidKey_Passes()
    {
        var validator = new SetFeatureFlagCommandValidator();
        var result = validator.TestValidate(new SetFeatureFlagCommand(Guid.NewGuid(), "prescriptions", true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void SetFeatureFlag_InvalidKey_Fails()
    {
        var validator = new SetFeatureFlagCommandValidator();
        var result = validator.TestValidate(new SetFeatureFlagCommand(Guid.NewGuid(), "nonexistent", true));
        result.ShouldHaveValidationErrorFor(x => x.FeatureKey);
    }

    [TestMethod]
    public void CreateUser_ValidPatient_Passes()
    {
        var validator = new CreateUserInClinicCommandValidator();
        var result = validator.TestValidate(new CreateUserInClinicCommand(
            "user@test.com", "John", "Doe", "Password1!", UserRole.Patient));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateUser_ClinicAdminRole_Fails()
    {
        var validator = new CreateUserInClinicCommandValidator();
        var result = validator.TestValidate(new CreateUserInClinicCommand(
            "user@test.com", "John", "Doe", "Password1!", UserRole.ClinicAdmin));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [TestMethod]
    public void CreateUser_PlatformAdminRole_Fails()
    {
        var validator = new CreateUserInClinicCommandValidator();
        var result = validator.TestValidate(new CreateUserInClinicCommand(
            "user@test.com", "John", "Doe", "Password1!", UserRole.PlatformAdmin));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [TestMethod]
    public void CreateUser_WeakPassword_Fails()
    {
        var validator = new CreateUserInClinicCommandValidator();
        var result = validator.TestValidate(new CreateUserInClinicCommand(
            "user@test.com", "John", "Doe", "weak", UserRole.Patient));
        result.ShouldHaveValidationErrorFor(x => x.TemporaryPassword);
    }

    [TestMethod]
    public void CreateUser_InvalidEmail_Fails()
    {
        var validator = new CreateUserInClinicCommandValidator();
        var result = validator.TestValidate(new CreateUserInClinicCommand(
            "not-an-email", "John", "Doe", "Password1!", UserRole.Patient));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [TestMethod]
    public void CreateClinicAdmin_ValidData_Passes()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.NewGuid(), "admin@test.com", "Jane", "Doe", "Admin1234!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateClinicAdmin_EmptyClinicId_Fails()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.Empty, "admin@test.com", "Jane", "Doe", "Admin1234!"));
        result.ShouldHaveValidationErrorFor(x => x.ClinicId);
    }

    [TestMethod]
    public void CreateClinicAdmin_EmptyEmail_Fails()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.NewGuid(), "", "Jane", "Doe", "Admin1234!"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [TestMethod]
    public void CreateClinicAdmin_InvalidEmail_Fails()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.NewGuid(), "not-an-email", "Jane", "Doe", "Admin1234!"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [TestMethod]
    public void CreateClinicAdmin_WeakPassword_Fails()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.NewGuid(), "admin@test.com", "Jane", "Doe", "weak"));
        result.ShouldHaveValidationErrorFor(x => x.TemporaryPassword);
    }

    [TestMethod]
    public void CreateClinicAdmin_NameTooLong_Fails()
    {
        var validator = new CreateClinicAdminCommandValidator();
        var longName = new string('a', 101);
        var result = validator.TestValidate(new CreateClinicAdminCommand(
            Guid.NewGuid(), "admin@test.com", longName, "Doe", "Admin1234!"));
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
}
