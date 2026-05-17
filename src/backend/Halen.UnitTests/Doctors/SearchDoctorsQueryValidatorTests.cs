using FluentAssertions;
using Halen.Application.Doctors.Queries;

namespace Halen.UnitTests.Doctors;

[TestClass]
public class SearchDoctorsQueryValidatorTests
{
    private SearchDoctorsQueryValidator _validator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _validator = new SearchDoctorsQueryValidator();
    }

    [TestMethod]
    public async Task Validate_DefaultValues_PassesValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_PageSizeExceedsMax_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, null, PageSize: 51);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [TestMethod]
    public async Task Validate_PageSizeZero_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, null, PageSize: 0);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [TestMethod]
    public async Task Validate_PageLessThanOne_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, null, Page: 0);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [TestMethod]
    public async Task Validate_NegativeMinFee_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, -1, null, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinFee");
    }

    [TestMethod]
    public async Task Validate_MaxFeeLessThanMinFee_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, 200, 100, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxFee");
    }

    [TestMethod]
    public async Task Validate_InvalidSortBy_FailsValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, "invalid");

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SortBy");
    }

    [TestMethod]
    [DataRow("name")]
    [DataRow("fee_asc")]
    [DataRow("fee_desc")]
    [DataRow("experience")]
    public async Task Validate_ValidSortByValues_PassValidation(string sortBy)
    {
        var query = new SearchDoctorsQuery(null, null, null, null, null, sortBy);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_ValidFeeRange_PassesValidation()
    {
        var query = new SearchDoctorsQuery(null, null, 100, 200, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_MaxFeeWithoutMinFee_PassesValidation()
    {
        var query = new SearchDoctorsQuery(null, null, null, 200, null, null);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }
}
