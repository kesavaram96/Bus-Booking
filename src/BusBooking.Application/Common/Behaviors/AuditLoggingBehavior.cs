using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using MediatR;

namespace BusBooking.Application.Common.Behaviors;

/// <summary>
/// Records one AuditLog row for any request implementing IAuditableRequest, after it succeeds
/// (ValidationBehavior, registered first so it wraps outermost, rejects invalid requests before
/// this ever runs — nothing invalid gets audited). NewValues is the handler's own response,
/// serialized through AuditJsonSerializer's redaction; OldValues is deliberately left null here
/// — capturing genuine "before" state would mean either loading every entity twice or teaching
/// every audited handler to report it, disproportionate to what this phase asks for.
///
/// This write is its own, separate transaction from the command's own SaveChangesAsync (which
/// already ran, inside the handler, before this code executes) — not truly atomic with the
/// business action it's recording. Acceptable for an observability concern the same way
/// Phase 18's notification dispatch is best-effort, not a reason to add distributed-transaction
/// machinery here.
/// </summary>
public sealed class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditLoggingBehavior(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableRequest auditable)
        {
            var entityId = auditable.AuditEntityId ?? TryExtractId(response);

            var log = new AuditLog(
                _currentUser.UserId,
                auditable.AuditAction,
                auditable.AuditEntityName,
                entityId,
                oldValues: null,
                newValues: AuditJsonSerializer.Serialize(response),
                _currentUser.IpAddress);

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    /// <summary>Tries the response's own Id first (BusDto.Id, TripDto.Id, ...), then falls back
    /// to a nested User.Id (AuthResult.User.Id, for Login) — generic enough to cover any future
    /// response shaped the same way, not hardcoded to AuthResult specifically.</summary>
    private static Guid? TryExtractId(TResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        var type = response.GetType();

        if (type.GetProperty("Id")?.GetValue(response) is Guid directId)
        {
            return directId;
        }

        if (type.GetProperty("User")?.GetValue(response) is { } userValue &&
            userValue.GetType().GetProperty("Id")?.GetValue(userValue) is Guid nestedId)
        {
            return nestedId;
        }

        return null;
    }
}
