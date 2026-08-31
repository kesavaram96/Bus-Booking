using BusBooking.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Tickets;

public class TicketTests
{
    [Fact]
    public void Constructor_WithValidArgs_GeneratesNumberAndCode()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid());

        ticket.TicketNumber.Should().StartWith("TKT" + DateTime.UtcNow.ToString("yyMMdd"));
        ticket.TicketNumber.Length.Should().Be(15);
        ticket.TicketCode.Should().NotBeNullOrWhiteSpace();
        ticket.TicketCode.Length.Should().Be(64);
    }

    [Fact]
    public void Constructor_WithEmptyBookingId_Throws()
    {
        var act = () => new Ticket(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyBookingPassengerId_Throws()
    {
        var act = () => new Ticket(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TwoTickets_GetDifferentTicketCodes()
    {
        var first = new Ticket(Guid.NewGuid(), Guid.NewGuid());
        var second = new Ticket(Guid.NewGuid(), Guid.NewGuid());

        first.TicketCode.Should().NotBe(second.TicketCode);
    }

    [Fact]
    public void TicketCode_IsNotTheEntityId()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid());

        ticket.TicketCode.Should().NotBe(ticket.Id.ToString());
        ticket.TicketCode.Should().NotContain(ticket.Id.ToString("N"));
    }
}
