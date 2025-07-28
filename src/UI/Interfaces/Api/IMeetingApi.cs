using Refit;
using UI.Models.Meeting;

namespace UI.Interfaces.Api;

public interface IMeetingApi
{
    [Post("/api/meetings")]
    Task<ApiResponse<string>> CreateMeetingAsync(
        [Body] CreateMeetingRequestDto dto,
        CancellationToken cancellationToken = default);

    [Delete("/api/meetings/{id}")]
    Task DeleteMeetingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/users/{userId}/meetings/calendar")]
    Task<ApiResponse<List<MeetingCalendarItemDto>>> GetMeetingCalendarItemsAsync(
        Guid userId,
        [Query] DateTime startDate,
        [Query] DateTime endDate,
        CancellationToken cancellationToken = default);

    [Get("/api/boards/{boardId}/meetings")]
    Task<ApiResponse<List<MeetingDto>>> GetMeetingsByBoardIdAsync(
        Guid boardId,
        CancellationToken cancellationToken = default);

    [Get("/api/users/{userId}/meetings")]
    Task<ApiResponse<List<MeetingDto>>> GetMeetingsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    [Put("/api/meetings/{id}")]
    Task UpdateMeetingAsync(
        Guid id,
        [Query] string title,
        [Query] string description,
        [Query] DateTime scheduledAt,
        [Query] int duration,
        CancellationToken cancellationToken = default);
}