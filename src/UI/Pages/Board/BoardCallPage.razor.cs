using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskPilotUI.UI.Pages.Board
{
    public partial class BoardCallPage : ComponentBase, IDisposable
    {
        [Parameter] public string? BoardId { get; set; }

        protected bool _inCall = false;
        protected bool _cameraOn = true;
        protected bool _micOn = true;
        protected bool _connectionReady = false;
        protected DotNetObjectReference<BoardCallPage>? _objRef;
        protected List<RemoteUser> _remoteUsers = new();

        [Inject] protected IJSRuntime? JS { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !string.IsNullOrEmpty(BoardId) && JS != null)
            {
                try
                {
                    _objRef = DotNetObjectReference.Create(this);
                    await JS.InvokeVoidAsync("BoardCallInterop.init", BoardId, "localVideo", _objRef);
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
                    await Task.Delay(50); // Small delay to ensure DOM is ready
                    await JS.InvokeVoidAsync("BoardCallInterop.setLocalVideoStream", "localVideo");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error ensuring local video after render: {ex.Message}");
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
        public Task AddRemoteUser(string userId, string displayName)
        {
            if (_remoteUsers.All(u => u.UserId != userId))
            {
                _remoteUsers.Add(new RemoteUser
                {
                    UserId = userId,
                    DisplayName = displayName,
                    VideoId = $"remoteVideo_{userId}",
                    ConnectionStatus = "connecting"
                });
                InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
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
                _remoteUsers.Clear();
                
                StateHasChanged();
                
                // Give the DOM time to update and then ensure local video is properly restored
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

        // Enhanced method to restore local video stream
        private async Task RestoreLocalVideo()
        {
            try
            {
                if (JS != null)
                {
                    // Use the globally available setLocalVideoStream function
                    await JS.InvokeVoidAsync("BoardCallInterop.setLocalVideoStream", "localVideo");
                    
                    // Also ensure the camera state is properly applied
                    await JS.InvokeVoidAsync("BoardCallInterop.toggleCamera", _cameraOn);
                    
                    Console.WriteLine("Local video stream restored successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring local video stream: {ex.Message}");
            }
        }

        // Method to ensure local video is visible (can be called anytime)
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

        public class RemoteUser
        {
            public string? UserId { get; set; }
            public string? DisplayName { get; set; }
            public string? VideoId { get; set; }
            public string ConnectionStatus { get; set; } = "connecting"; // connecting, connected, failed
        }
    }
}