using BusBooking.Application.Bookings.DTOs;
using MediatR;

namespace BusBooking.Application.Bookings.Queries.GetBookingById;

public sealed record GetBookingByIdQuery(Guid Id) : IRequest<BookingDto>;
