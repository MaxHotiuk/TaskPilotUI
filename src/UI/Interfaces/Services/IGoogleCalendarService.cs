namespace UI.Interfaces.Services;

public interface IGoogleCalendarService
{
    Task<string> GetAuthorizationUrlAsync(Guid userId);
    Task ConnectAsync(string code);
    Task SyncCalendarAsync(Guid userId, DateTime month);
}
