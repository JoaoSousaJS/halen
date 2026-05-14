using FluentAssertions;
using Halen.Application.Admin.Queries;
using Halen.Domain.Entities;
using Halen.Domain.Enums;
using Halen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Halen.UnitTests.Admin;

[TestClass]
public class ListUsersQueryHandlerTests
{
    private HalenDbContext _db = null!;
    private ListUsersQueryHandler _handler = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        var options = new DbContextOptionsBuilder<HalenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new HalenDbContext(options);

        var mayaId = Guid.NewGuid();
        var elenaId = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = mayaId, FirstName = "Maya", LastName = "Chen", Email = "maya@test.com", UserName = "maya@test.com", Role = UserRole.Patient, LastLoginAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FirstName = "Dr", LastName = "House", Email = "house@test.com", UserName = "house@test.com", Role = UserRole.Doctor, LastLoginAt = DateTime.UtcNow },
            new User { Id = elenaId, FirstName = "Elena", LastName = "Kowalski", Email = "elena@test.com", UserName = "elena@test.com", Role = UserRole.Patient, IsFlagged = true, Status = AccountStatus.PendingReview, LastLoginAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com", Role = UserRole.Admin, LastLoginAt = DateTime.UtcNow }
        );

        _db.PatientProfiles.Add(new PatientProfile { Id = Guid.NewGuid(), UserId = mayaId, SubscriptionPlan = "HALEN+" });
        _db.PatientProfiles.Add(new PatientProfile { Id = Guid.NewGuid(), UserId = elenaId, SubscriptionPlan = "Essentials" });

        await _db.SaveChangesAsync();

        _handler = new ListUsersQueryHandler(_db);
    }

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    [TestMethod]
    public async Task Handle_ReturnsAllNonAdminUsers()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, null, false), CancellationToken.None);

        result.Users.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Users.Should().NotContain(u => u.Role == "Admin");
    }

    [TestMethod]
    public async Task Handle_FiltersByRole()
    {
        var result = await _handler.Handle(new ListUsersQuery("patient", null, false), CancellationToken.None);

        result.Users.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Users.Should().OnlyContain(u => u.Role == "Patient");
    }

    [TestMethod]
    public async Task Handle_FiltersBySearchOnName()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, "maya", false), CancellationToken.None);

        result.Users.Should().HaveCount(1);
        result.Users[0].Name.Should().Be("Maya Chen");
    }

    [TestMethod]
    public async Task Handle_FiltersBySearchOnEmail()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, "house@", false), CancellationToken.None);

        result.Users.Should().HaveCount(1);
        result.Users[0].Name.Should().Be("Dr House");
    }

    [TestMethod]
    public async Task Handle_FiltersFlaggedOnly()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, null, true), CancellationToken.None);

        result.Users.Should().HaveCount(1);
        result.Users[0].Name.Should().Be("Elena Kowalski");
        result.Users[0].IsFlagged.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_PaginatesResults()
    {
        var page1 = await _handler.Handle(new ListUsersQuery(null, null, false, Page: 1, PageSize: 2), CancellationToken.None);
        var page2 = await _handler.Handle(new ListUsersQuery(null, null, false, Page: 2, PageSize: 2), CancellationToken.None);

        page1.Users.Should().HaveCount(2);
        page1.TotalCount.Should().Be(3);
        page2.Users.Should().HaveCount(1);
        page2.TotalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task Handle_DerivesIdleStatus()
    {
        var oldUser = await _db.Users.FirstAsync(u => u.Email == "maya@test.com");
        oldUser.LastLoginAt = DateTime.UtcNow.AddDays(-10);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new ListUsersQuery(null, "maya", false), CancellationToken.None);

        result.Users[0].Status.Should().Be("Idle");
    }

    [TestMethod]
    public async Task Handle_IncludesSubscriptionPlan()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, "maya", false), CancellationToken.None);

        result.Users[0].Plan.Should().Be("HALEN+");
    }

    [TestMethod]
    public async Task Handle_DoctorPlanIsNull()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, "house", false), CancellationToken.None);

        result.Users[0].Plan.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_FlaggedUsersAppearFirst()
    {
        var result = await _handler.Handle(new ListUsersQuery(null, null, false), CancellationToken.None);

        result.Users[0].IsFlagged.Should().BeTrue();
    }
}
