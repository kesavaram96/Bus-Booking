using BusBooking.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Domain;

public class SegmentOverlapTests
{
    // Route used across these cases: Colombo(0) - Kurunegala(1) - Dambulla(2) - Anuradhapura(3) - Jaffna(4)

    [Fact]
    public void Overlaps_ExactSameSegment_ReturnsTrue()
    {
        SegmentOverlap.Overlaps(0, 1, 0, 1).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_PartialOverlap_ReturnsTrue()
    {
        // Existing: Colombo(0) -> Dambulla(2); New: Kurunegala(1) -> Jaffna(4)
        SegmentOverlap.Overlaps(1, 4, 0, 2).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_CompletelyOverlappingSegment_ReturnsTrue()
    {
        // Existing: Colombo(0) -> Jaffna(4) fully contains new Kurunegala(1) -> Dambulla(2)
        SegmentOverlap.Overlaps(1, 2, 0, 4).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_NonOverlappingSegment_ReturnsFalse()
    {
        // Existing: Colombo(0) -> Kurunegala(1); New: Dambulla(2) -> Jaffna(4)
        SegmentOverlap.Overlaps(2, 4, 0, 1).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_AdjacentSegments_ReturnsFalse()
    {
        // Existing: Colombo(0) -> Kurunegala(1); New: Kurunegala(1) -> Jaffna(4) — shares only
        // the boundary stop, so the seat is free for the whole leg past it.
        SegmentOverlap.Overlaps(1, 4, 0, 1).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_DocExample_DambullaToJaffnaOverlapsAnuradhapuraSegment_ReturnsTrue()
    {
        // Existing: Dambulla(2) -> Anuradhapura(3); New: Dambulla(2) -> Jaffna(4)
        SegmentOverlap.Overlaps(2, 4, 2, 3).Should().BeTrue();
    }
}
