using Microsoft.AspNetCore.Components;
using NotificationType = UI.Models.Notification.NotificationType;

namespace UI.Components.Shared
{
    public partial class NotificationToast : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public NotificationType NotificationType { get; set; }
        [Parameter] public EventCallback OnClick { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
    }
}
