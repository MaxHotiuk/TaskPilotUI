using UI.Models.Meeting;

namespace UI.Interfaces.Services;

public interface IMeetingService
{
    Task<string> CreateMeetingAsync(CreateMeetingRequestDto dto, CancellationToken cancellationToken = default);
    Task DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MeetingCalendarItemDto>> GetMeetingCalendarItemsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<MeetingDto>> GetMeetingsByBoardIdAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task<List<MeetingDto>> GetMeetingsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateMeetingAsync(Guid id, string title, string description, DateTime scheduledAt, int duration, CancellationToken cancellationToken = default);
}