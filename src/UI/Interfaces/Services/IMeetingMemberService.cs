using UI.Models.Meeting;

namespace UI.Interfaces.Services;

public interface IMeetingMemberService
{
    Task AddMeetingMemberAsync(Guid meetingId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveMeetingMemberAsync(Guid meetingId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<MeetingMemberDto>> GetMeetingMembersByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task UpdateMeetingMemberStatusAsync(Guid meetingId, Guid userId, string status, CancellationToken cancellationToken = default);
}