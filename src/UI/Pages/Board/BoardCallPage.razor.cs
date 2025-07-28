using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using UI.Interfaces.Services;
using UI.Models.User;

namespace TaskPilotUI.UI.Pages.Board
{
    public partial class BoardCallPage : ComponentBase, IDisposable
    {
        [Parameter] public string? BoardId { get; set; }
        [Parameter] public string? MeetingId { get; set; }

        protected bool _inCall = false;
        protected bool _cameraOn = true;
        protected bool _micOn = true;
        protected bool _screenSharing = false;
        protected bool _connectionReady = false;
        protected bool _forbidden = false;
        protected DotNetObjectReference<BoardCallPage>? _objRef;
        protected List<RemoteUser> _remoteUsers = new();

        [Inject] protected IAuthService AuthService { get; set; } = default!;
        [Inject] protected IUserService UserService { get; set; } = default!;
        [Inject] protected IBoardService BoardService { get; set; } = default!;
        [Inject] protected IMeetingService MeetingService { get; set; } = default!;
        [Inject] protected IMeetingMemberService MeetingMemberService { get; set; } = default!;
        [Inject] protected IJSRuntime? JS { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_forbidden)
            {
                return;
            }

            if (firstRender && !string.IsNullOrEmpty(BoardId) && JS != null)
            {
                try
                {
                    var user = await AuthService.GetCurrentUserAsync();
                    if (user == null)
                    {
                        Console.WriteLine("User not authenticated.");
                        return;
                    }

                    _objRef = DotNetObjectReference.Create(this);
                    await JS.InvokeVoidAsync("BoardCallInterop.init", BoardId, "localVideo", _objRef, user.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing BoardCallInterop: {ex.Message}");
                }
            }

            if (!firstRender && _connectionReady && JS != null)
            {
                try
                {
                    await Task.Delay(50);
                    await JS.InvokeVoidAsync("BoardCallInterop.setLocalVideoStream", "localVideo");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error ensuring local video after render: {ex.Message}");
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(BoardId))
            {
                try
                {
                    var user = await AuthService.GetCurrentUserAsync();
                    if (user == null)
                    {
                        Console.WriteLine("User not authenticated.");
                        NavigationManager.NavigateTo("/login");
                    }

                    if (Guid.TryParse(MeetingId, out var meetingGuid))
                    {
                        var meetingMembers = await MeetingMemberService.GetMeetingMembersByMeetingIdAsync(meetingGuid);

                        if (!meetingMembers.Any(m => m.UserId.ToString() == user!.Id))
                        {
                            _forbidden = true;
                            NavigationManager.NavigateTo("/forbidden");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid MeetingId format.");
                        NavigationManager.NavigateTo("/boards");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing BoardCallPage: {ex.Message}");
                }
            }
        }

        [JSInvokable]
        public Task OnWebRtcConnected()
        {
            _connectionReady = true;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnScreenShareStatusChanged(bool isSharing)
        {
            _screenSharing = isSharing;
            InvokeAsync(StateHasChanged);
            return Task.CompletedTask;
        }

        [JSInvokable]
        public async Task AddRemoteUser(string userId, string displayName)
        {
            if (_remoteUsers.All(u => u.UserId != userId))
            {
                string resolvedDisplayName = displayName;
                try
                {
                    var user = await UserService.GetByIdAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.Username))
                    {
                        resolvedDisplayName = user.Username;
                    }
                }
                catch
                {
                }

                _remoteUsers.Add(new RemoteUser
                {
                    UserId = userId,
                    DisplayName = resolvedDisplayName,
                    VideoId = $"remoteVideo_{userId}",
                    ConnectionStatus = "connecting",
                    IsScreenSharing = false
                });
                await InvokeAsync(StateHasChanged);
            }
        }

        [JSInvokable]
        public Task RemoveRemoteUser(string userId)
        {
            var user = _remoteUsers.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                _remoteUsers.Remove(user);
                InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task UpdateUserConnectionStatus(string userId, string status)
        {
            var user = _remoteUsers.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                user.ConnectionStatus = status;
                InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task UpdateUserScreenShareStatus(string userId, bool isScreenSharing)
        {
            var user = _remoteUsers.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                user.IsScreenSharing = isScreenSharing;
                InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        protected async Task StartCall()
        {
            try
            {
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.startCall");
                }
                _inCall = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting call: {ex.Message}");
            }
        }

        protected async Task HangUp()
        {
            try
            {
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.hangUp");
                }
                _inCall = false;
                _screenSharing = false;
                _remoteUsers.Clear();
                
                StateHasChanged();
                
                await Task.Delay(200);
                await RestoreLocalVideo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hanging up: {ex.Message}");
            }
        }

        protected async Task ToggleCamera()
        {
            try
            {
                _cameraOn = !_cameraOn;
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.toggleCamera", _cameraOn);
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling camera: {ex.Message}");
            }
        }

        protected async Task ToggleMic()
        {
            try
            {
                _micOn = !_micOn;
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.toggleMic", _micOn);
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling mic: {ex.Message}");
            }
        }

        protected async Task ToggleScreenShare()
        {
            try
            {
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.toggleScreenShare");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling screen share: {ex.Message}");
            }
        }

        private async Task RestoreLocalVideo()
        {
            try
            {
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.setLocalVideoStream", "localVideo");
                    
                    await JS.InvokeVoidAsync("BoardCallInterop.toggleCamera", _cameraOn);
                    
                    Console.WriteLine("Local video stream restored successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring local video stream: {ex.Message}");
            }
        }

        protected async Task EnsureLocalVideoVisible()
        {
            try
            {
                if (JS != null)
                {
                    await JS.InvokeVoidAsync("BoardCallInterop.setLocalVideoStream", "localVideo");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring local video visibility: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _objRef?.Dispose();
        }
    }
}