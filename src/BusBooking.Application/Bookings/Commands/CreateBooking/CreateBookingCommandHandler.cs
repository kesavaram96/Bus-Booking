using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Common;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;

    public CreateBookingCommandHandler(IApplicationDbContext context, ISeatLockService seatLockService)
    {
        _context = context;
        _seatLockService = seatLockService;
    }

    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        if (trip.Status != TripStatus.Scheduled)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.TripId), "This trip is not currently bookable.")]);
        }

        var tripSeatIds = request.Passengers.Select(p => p.TripSeatId).ToList();
        var tripSeats = await _context.TripSeats
            .Include(ts => ts.Seat)
            .Where(ts => ts.TripId == request.TripId && tripSeatIds.Contains(ts.Id))
            .ToDictionaryAsync(ts => ts.Id, cancellationToken);

        // The whole route, not just the requested stops: existing bookings on these seats may
        // reference other stops on the route, and their StopOrder is needed to check overlap.
        var routeStops = await _context.RouteStops
            .Where(rs => rs.RouteId == trip.RouteId)
            .ToDictionaryAsync(rs => rs.Id, cancellationToken);

        // A seat is unavailable only for journey segments that overlap an existing, non-
        // cancelled booking on it — never globally (Phase 13). One batched query for every
        // seat in this request, rather than one query per passenger.
        var seatIds = tripSeats.Values.Select(ts => ts.SeatId).Distinct().ToList();
        var existingPassengers = await _context.Bookings
            .Where(b => b.TripId == request.TripId && b.Status != BookingStatus.Cancelled)
            .SelectMany(b => b.Passengers)
            .Where(p => seatIds.Contains(p.SeatId))
            .Select(p => new { p.SeatId, p.PickupStopId, p.DropOffStopId })
            .ToListAsync(cancellationToken);

        var existingSegmentsBySeat = existingPassengers
            .GroupBy(p => p.SeatId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => (PickupOrder: routeStops[p.PickupStopId].StopOrder, DropOffOrder: routeStops[p.DropOffStopId].StopOrder)).ToList());

        var booking = new Booking(request.TripId, request.CustomerId);

        foreach (var input in request.Passengers)
        {
            if (!tripSeats.TryGetValue(input.TripSeatId, out var tripSeat))
            {
                throw new NotFoundException("TripSeat", input.TripSeatId);
            }

            if (!routeStops.TryGetValue(input.PickupStopId, out var pickupStop))
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(input.PickupStopId), "Pickup stop does not belong to this trip's route.")]);
            }

            if (!routeStops.TryGetValue(input.DropOffStopId, out var dropOffStop))
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(input.DropOffStopId), "Drop-off stop does not belong to this trip's route.")]);
            }

            if (pickupStop.StopOrder >= dropOffStop.StopOrder)
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(input.DropOffStopId), "Pickup must occur before drop-off.")]);
            }

            // This is what makes "the Redis lock belongs to the booking" an enforced rule: a
            // seat can only be booked while held under the exact token the caller presents,
            // for every actor (registered customer, guest, or staff) via this one code path.
            if (tripSeat.Status != TripSeatStatus.Held || tripSeat.LockId != input.LockId)
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(input.TripSeatId), "This seat is not currently held by you. Lock the seat before booking.")]);
            }

            if (existingSegmentsBySeat.TryGetValue(tripSeat.SeatId, out var existingSegments) &&
                existingSegments.Any(seg => SegmentOverlap.Overlaps(pickupStop.StopOrder, dropOffStop.StopOrder, seg.PickupOrder, seg.DropOffOrder)))
            {
                throw new ValidationException(
                    [new ValidationFailure(nameof(input.TripSeatId), "This seat is not available for the selected journey segment.")]);
            }

            var passenger = new BookingPassenger(
                booking.Id,
                input.FullName,
                input.PhoneNumber,
                input.Gender,
                input.NIC,
                input.Email,
                input.PickupStopId,
                input.DropOffStopId,
                tripSeat.SeatId,
                trip.Fare); // Fare is always server-calculated from the trip — never client-supplied.

            booking.AddPassenger(passenger);

            // Reverts to Available, not a "Booked" state: the seat itself has no memory of
            // this booking, so a later request for a different, non-overlapping segment on the
            // same seat can still lock and book it. Also fold this segment into the overlap set
            // immediately, so two passengers in this same request can't double-book one seat.
            tripSeat.ReleaseHold();
            if (existingSegmentsBySeat.TryGetValue(tripSeat.SeatId, out var seatSegments))
            {
                seatSegments.Add((pickupStop.StopOrder, dropOffStop.StopOrder));
            }
            else
            {
                existingSegmentsBySeat[tripSeat.SeatId] = [(pickupStop.StopOrder, dropOffStop.StopOrder)];
            }
        }

        _context.Bookings.Add(booking);

        // Everything above is tracked on this one DbContext and committed by this single
        // SaveChangesAsync — EF Core wraps it in one database transaction, satisfying "use
        // database transactions" without an explicit BeginTransactionAsync.
        await _context.SaveChangesAsync(cancellationToken);

        // Best-effort Redis cleanup, deliberately after the DB commit: the booking already
        // succeeded (the database is the source of truth for confirmed bookings), so a Redis
        // hiccup releasing these now-redundant locks must never surface as a failed booking to
        // the client. The lock's own TTL cleans it up regardless either way.
        foreach (var input in request.Passengers)
        {
            try
            {
                await _seatLockService.ReleaseAsync(input.TripSeatId, input.LockId, cancellationToken);
            }
            catch
            {
                // Swallowed deliberately — see comment above.
            }
        }

        return booking.ToDto();
    }
}
