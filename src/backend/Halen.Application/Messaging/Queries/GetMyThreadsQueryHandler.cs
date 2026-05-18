using Halen.Application.Common;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halen.Application.Messaging.Queries;

public class GetMyThreadsQueryHandler(
    IAppDbContext db,
    ILogger<GetMyThreadsQueryHandler> logger)
    : IRequestHandler<GetMyThreadsQuery, GetMyThreadsResult>
{
    public async Task<GetMyThreadsResult> Handle(GetMyThreadsQuery request, CancellationToken ct)
    {
        var query = db.ConversationThreads
            .Include(t => t.PatientUser)
            .Include(t => t.DoctorUser)
            .ThenInclude(u => u!.DoctorProfile)
            .Include(t => t.Appointment)
            .Where(t => t.PatientUserId == request.UserId || t.DoctorUserId == request.UserId)
            .AsQueryable();

        var isDoctor = request.Role == UserRole.Doctor;

        switch (request.Filter?.ToLowerInvariant())
        {
            case "unread":
                query = isDoctor
                    ? query.Where(t => t.DoctorUnreadCount > 0)
                    : query.Where(t => t.PatientUnreadCount > 0);
                break;
            case "closed":
                query = query.Where(t => t.Status == ThreadStatus.Closed);
                break;
            case "needs_reply":
                if (isDoctor)
                    query = query.Where(t => t.DoctorUnreadCount > 0 && t.Status == ThreadStatus.Active);
                break;
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Subject, search) ||
                EF.Functions.ILike(t.LastMessagePreview ?? "", search) ||
                EF.Functions.ILike(t.PatientUser!.FirstName + " " + t.PatientUser.LastName, search) ||
                EF.Functions.ILike(t.DoctorUser!.FirstName + " " + t.DoctorUser.LastName, search));
        }

        var totalCount = await query.CountAsync(ct);

        var threads = await query
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new ThreadSummaryDto(
                t.Id,
                request.UserId == t.PatientUserId
                    ? t.DoctorUser!.FirstName + " " + t.DoctorUser.LastName
                    : t.PatientUser!.FirstName + " " + t.PatientUser.LastName,
                request.UserId == t.PatientUserId
                    ? t.DoctorUser!.DoctorProfile!.Specialty
                    : null,
                t.Subject,
                t.LastMessagePreview,
                t.LastMessageAt,
                request.UserId == t.PatientUserId
                    ? t.PatientUnreadCount
                    : t.DoctorUnreadCount,
                t.Status,
                t.Appointment!.Status,
                t.AppointmentId))
            .ToArrayAsync(ct);

        return new GetMyThreadsResult(true, threads, totalCount);
    }
}
