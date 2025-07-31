using Refit;
using UI.Models.Meeting;

namespace UI.Interfaces.Api;

public interface IMeetingMemberApi
{
    [Post("/api/meetings/{meetingId}/members")]
    Task AddMeetingMemberAsync(
        Guid meetingId,
        [Query] Guid userId,
        CancellationToken cancellationToken = default);

    [Delete("/api/meetings/{meetingId}/members/{userId}")]
    Task RemoveMeetingMemberAsync(
        Guid meetingId,
        Guid userId,
        CancellationToken cancellationToken = default);

    [Get("/api/meetings/{meetingId}/members")]
    Task<List<MeetingMemberDto>> GetMeetingMembersByMeetingIdAsync(
        Guid meetingId,
        CancellationToken cancellationToken = default);
    
    [Put("/api/meetings/{meetingId}/members/{userId}/status")]
    Task UpdateMeetingMemberStatusAsync(
        Guid meetingId,
        Guid userId,
        [Query] string status,
        CancellationToken cancellationToken = default);
}