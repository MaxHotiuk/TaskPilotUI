using UI.Models.Meeting;
using UI.Interfaces.Services;
using UI.Interfaces.Api;
using Refit;

namespace UI.Services;

public class MeetingService : IMeetingService
{
    private readonly IMeetingApi _meetingApi;

    public MeetingService(IMeetingApi meetingApi)
    {
        _meetingApi = meetingApi;
    }

    public async Task<string> CreateMeetingAsync(CreateMeetingRequestDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _meetingApi.CreateMeetingAsync(dto, cancellationToken);
        return response.Content ?? throw new Exception("Failed to create meeting");
    }

    public async Task DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _meetingApi.DeleteMeetingAsync(id, cancellationToken);
    }

    public async Task<List<MeetingCalendarItemDto>> GetMeetingCalendarItemsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var response = await _meetingApi.GetMeetingCalendarItemsAsync(userId, startDate, endDate, cancellationToken);
        return response.Content ?? [];
    }

    public async Task<List<MeetingDto>> GetMeetingsByBoardIdAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var response = await _meetingApi.GetMeetingsByBoardIdAsync(boardId, cancellationToken);
        return response.Content ?? [];
    }

    public async Task<List<MeetingDto>> GetMeetingsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _meetingApi.GetMeetingsByUserIdAsync(userId, cancellationToken);
        return response.Content ?? [];
    }

    public async Task UpdateMeetingAsync(Guid id, string title, string description, DateTime scheduledAt, int duration, CancellationToken cancellationToken = default)
    {
        await _meetingApi.UpdateMeetingAsync(id, title, description, scheduledAt, duration, cancellationToken);
    }
}