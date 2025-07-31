using System;
using System.Threading.Tasks;

namespace UI.Interfaces.SignalR
{
    public interface INotificationSignalRService : IAsyncDisposable
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task JoinUserGroupAsync(string userId);
        Task LeaveUserGroupAsync(string userId);
        void OnNotificationReceived(Action<object> handler);
        bool IsConnected { get; }
    }
}
