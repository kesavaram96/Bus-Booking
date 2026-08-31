using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Queries.GetSeatLayoutById;

public sealed record GetSeatLayoutByIdQuery(Guid Id) : IRequest<SeatLayoutDto>;
