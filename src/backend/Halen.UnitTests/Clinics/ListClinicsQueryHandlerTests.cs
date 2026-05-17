using FluentAssertions;
using Halen.Application.Clinics.Queries;
using Halen.Domain.Entities;
using Halen.Infrastructure.Persistence;
using Halen.UnitTests.Helpers;

namespace Halen.UnitTests.Clinics;

[TestClass]
public class ListClinicsQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private ListClinicsQueryHandler _handler = null!;

    [TestInitialize]
    public void Initialize()
    {
        _db = TestDbFactory.Create();
        _handler = new ListClinicsQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ReturnsAllClinics()
    {
        _db.Clinics.AddRange(
            new Clinic { Name = "Alpha Clinic", Slug = "alpha-clinic" },
            new Clinic { Name = "Beta Clinic", Slug = "beta-clinic" },
            new Clinic { Name = "Gamma Clinic", Slug = "gamma-clinic" }
        );
        await _db.SaveChangesAsync();

        var query = new ListClinicsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Clinics.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_SearchByName_FiltersCorrectly()
    {
        _db.Clinics.AddRange(
            new Clinic { Name = "Downtown Health Center", Slug = "downtown-health" },
            new Clinic { Name = "Uptown Clinic", Slug = "uptown-clinic" },
            new Clinic { Name = "Downtown Urgent Care", Slug = "downtown-urgent" }
        );
        await _db.SaveChangesAsync();

        var query = new ListClinicsQuery(Search: "downtown");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Clinics.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Clinics.Should().AllSatisfy(c => c.Name.Should().Contain("Downtown"));
    }

    [TestMethod]
    public async Task Handle_Pagination_Works()
    {
        for (var i = 0; i < 5; i++)
        {
            _db.Clinics.Add(new Clinic { Name = $"Clinic {i:D2}", Slug = $"clinic-{i:D2}" });
        }
        await _db.SaveChangesAsync();

        var query = new ListClinicsQuery(Page: 2, PageSize: 2);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Clinics.Should().HaveCount(2);
    }
}
