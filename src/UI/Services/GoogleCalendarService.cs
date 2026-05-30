using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.GoogleCalendar;

namespace UI.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IGoogleCalendarApi _googleCalendarApi;

    public GoogleCalendarService(IGoogleCalendarApi googleCalendarApi)
    {
        _googleCalendarApi = googleCalendarApi;
    }

    public async Task<string> GetAuthorizationUrlAsync(Guid userId)
    {
        var response = await _googleCalendarApi.GetAuthorizationUrlAsync(userId);
        return response.Url;
    }

    public async Task ConnectAsync(string code)
    {
        await _googleCalendarApi.ConnectAsync(new ConnectGoogleCalendarRequest { Code = code });
    }

    public async Task SyncCalendarAsync(Guid userId, DateTime month)
    {
        await _googleCalendarApi.SyncCalendarAsync(userId, new SyncCalendarRequestDto { Month = month });
    }
}
