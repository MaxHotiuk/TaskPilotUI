using System;
using System.Threading.Tasks;

namespace UI.Interfaces.SignalR
{
    public interface ISignalRService : IAsyncDisposable
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task JoinBoardGroupAsync(string boardId);
        Task LeaveBoardGroupAsync(string boardId);
        Task JoinTaskGroupAsync(string taskId);
        Task LeaveTaskGroupAsync(string taskId);
        void OnBoardUpdated(Action<object> handler);
        void OnTaskUpdated(Action<object> handler);
        bool IsConnected { get; }
    }
}
