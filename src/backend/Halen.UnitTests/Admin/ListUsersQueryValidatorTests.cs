using FluentAssertions;
using Halen.Application.Admin.Queries;

namespace Halen.UnitTests.Admin;

[TestClass]
public class ListUsersQueryValidatorTests
{
    private ListUsersQueryValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new ListUsersQueryValidator();
    }

    [TestMethod]
    public async Task Validate_AllNulls_PassesValidation()
    {
        var query = new ListUsersQuery(null, null, false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("patient")]
    [DataRow("doctor")]
    [DataRow("Patient")]
    [DataRow("Doctor")]
    public async Task Validate_ValidRole_PassesValidation(string role)
    {
        var query = new ListUsersQuery(role, null, false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("admin")]
    [DataRow("superuser")]
    [DataRow("unknown")]
    public async Task Validate_InvalidRole_FailsValidation(string role)
    {
        var query = new ListUsersQuery(role, null, false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [TestMethod]
    public async Task Validate_SearchWithinLimit_PassesValidation()
    {
        var query = new ListUsersQuery(null, "maya chen", false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_SearchExceedsMaxLength_FailsValidation()
    {
        var longSearch = new string('a', 201);
        var query = new ListUsersQuery(null, longSearch, false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Search");
    }

    [TestMethod]
    public async Task Validate_FlaggedOnlyTrue_PassesValidation()
    {
        var query = new ListUsersQuery(null, null, true);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_EmptyRole_PassesValidation()
    {
        var query = new ListUsersQuery("", null, false);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_PageZero_FailsValidation()
    {
        var query = new ListUsersQuery(null, null, false, Page: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [TestMethod]
    public async Task Validate_PageSizeOverLimit_FailsValidation()
    {
        var query = new ListUsersQuery(null, null, false, PageSize: 101);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [TestMethod]
    public async Task Validate_ValidPagination_PassesValidation()
    {
        var query = new ListUsersQuery(null, null, false, Page: 3, PageSize: 50);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
