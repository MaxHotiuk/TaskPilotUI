using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using AntDesign;

namespace UI.Pages;

public partial class GoogleCallback : ComponentBase
{
    [Inject] private IGoogleCalendarService GoogleCalendarService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "code")]
    public string? Code { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(Code))
        {
            try
            {
                await GoogleCalendarService.ConnectAsync(Code);
                Message.Success("Google Calendar connected successfully.");
            }
            catch (Exception ex)
            {
                Message.Error($"Failed to connect Google Calendar: {ex.Message}");
            }
        }
        else
        {
            Message.Error("No authorization code received from Google.");
        }

        Navigation.NavigateTo("/profile");
    }
}
