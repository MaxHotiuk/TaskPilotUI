using Refit;
using UI.Models.GoogleCalendar;

namespace UI.Interfaces.Api;

public interface IGoogleCalendarApi
{
    [Get("/api/users/{userId}/google-calendar/auth-url")]
    Task<AuthUrlResponseDto> GetAuthorizationUrlAsync(Guid userId);

    [Post("/api/google-calendar/connect")]
    Task ConnectAsync([Body] ConnectGoogleCalendarRequest request);

    [Post("/api/users/{userId}/google-calendar/sync")]
    Task SyncCalendarAsync(Guid userId, [Body] SyncCalendarRequestDto dto);
}
