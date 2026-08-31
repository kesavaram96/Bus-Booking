using BusBooking.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Audit;

public class AuditLogTests
{
    [Fact]
    public void Constructor_WithValidArgs_SetsFieldsAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var log = new AuditLog(userId, "CreateBus", "Bus", entityId, null, "{\"id\":1}", "127.0.0.1");

        log.UserId.Should().Be(userId);
        log.Action.Should().Be("CreateBus");
        log.EntityName.Should().Be("Bus");
        log.EntityId.Should().Be(entityId);
        log.NewValues.Should().Be("{\"id\":1}");
        log.IPAddress.Should().Be("127.0.0.1");
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ForSystemTriggeredAction_AllowsNullUserId()
    {
        var log = new AuditLog(null, "CancelTrip", "Trip", Guid.NewGuid(), null, null, null);

        log.UserId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyAction_Throws()
    {
        var act = () => new AuditLog(null, " ", "Trip", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyEntityName_Throws()
    {
        var act = () => new AuditLog(null, "CreateBus", " ", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }
}
