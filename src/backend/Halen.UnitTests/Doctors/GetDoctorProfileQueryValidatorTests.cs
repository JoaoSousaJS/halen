using FluentAssertions;
using Halen.Application.Doctors.Queries;

namespace Halen.UnitTests.Doctors;

[TestClass]
public class GetDoctorProfileQueryValidatorTests
{
    private readonly GetDoctorProfileQueryValidator _validator = new();

    [TestMethod]
    public void Valid_query_passes()
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid());
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void Empty_DoctorProfileId_fails()
    {
        var query = new GetDoctorProfileQuery(Guid.Empty);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void ReviewPage_less_than_1_fails()
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid(), ReviewPage: 0);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void ReviewPageSize_less_than_1_fails()
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid(), ReviewPageSize: 0);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void ReviewPageSize_greater_than_50_fails()
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid(), ReviewPageSize: 51);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Invalid_ReviewSortBy_fails()
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid(), ReviewSortBy: "invalid");
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("newest")]
    [DataRow("highest")]
    [DataRow("lowest")]
    [DataRow("helpful")]
    public void Valid_ReviewSortBy_values_pass(string sortBy)
    {
        var query = new GetDoctorProfileQuery(Guid.NewGuid(), ReviewSortBy: sortBy);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }
}
