using BusBooking.Application.Buses.DTOs;
using MediatR;

namespace BusBooking.Application.Buses.Queries.GetBusById;

public sealed record GetBusByIdQuery(Guid Id) : IRequest<BusDto>;
