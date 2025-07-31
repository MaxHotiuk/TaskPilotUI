using UI.Models.Meeting;
using UI.Interfaces.Services;
using UI.Interfaces.Api;
using Refit;

namespace UI.Services;

public class MeetingMemberService : IMeetingMemberService
{
    private readonly IMeetingMemberApi _meetingApi;

    public MeetingMemberService(IMeetingMemberApi meetingApi)
    {
        _meetingApi = meetingApi;
    }

    public async Task AddMeetingMemberAsync(Guid meetingId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _meetingApi.AddMeetingMemberAsync(meetingId, userId, cancellationToken);
    }

    public async Task RemoveMeetingMemberAsync(Guid meetingId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _meetingApi.RemoveMeetingMemberAsync(meetingId, userId, cancellationToken);
    }

    public async Task<List<MeetingMemberDto>> GetMeetingMembersByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var response = await _meetingApi.GetMeetingMembersByMeetingIdAsync(meetingId, cancellationToken);
        return response ?? [];
    }

    public async Task UpdateMeetingMemberStatusAsync(Guid meetingId, Guid userId, string status, CancellationToken cancellationToken = default)
    {
        await _meetingApi.UpdateMeetingMemberStatusAsync(meetingId, userId, status, cancellationToken);
    }
}