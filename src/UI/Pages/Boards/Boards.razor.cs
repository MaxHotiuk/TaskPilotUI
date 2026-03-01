using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Member;
using UI.Models.User;
using UI.Interfaces.Services;
using UI.Extensions;

namespace UI.Pages.Boards;

public partial class Boards : ComponentBase
{
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMessageService MessageBox { get; set; } = default!;

    private List<BoardSearchDto> _boards = new();
    private string _searchTerm = string.Empty;
    private string _filterType = "all";
    private int _currentPage = 1;
    private int _pageSize = 6;
    private bool _hasMoreData = true;
    private bool _isSearching = false;
    private bool _showCreateModal = false;
    private bool _showDeleteModal = false;
    private bool _isDeleting = false;
    private BoardSearchDto? _selectedBoard = null;
    private string _deleteConfirmation = string.Empty;
    private System.Timers.Timer? _searchTimer;
    private Guid? _selectedOrganizationId;

    protected bool IsLoading => LoadingService?.IsLoading ?? false;

    protected override async Task OnInitializedAsync()
    {
        await LoadInitialBoards();
    }

    private async Task LoadInitialBoards()
    {
        await AuthService.ExecuteWithGlobalLoadingAsync(LoadingService, async service =>
        {
            try
            {
                var isAuthenticated = await service.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    Navigation.NavigateTo("/login");
                    return;
                }

                var currentUser = service.GetCachedUser();
                if (currentUser == null)
                {
                    currentUser = await service.GetCurrentUserAsync();
                    if (currentUser == null)
                    {
                        Navigation.NavigateTo("/login");
                        return;
                    }
                }

                await SearchBoards(reset: true);
            }
            catch (Exception)
            {
            }
            finally
            {
                StateHasChanged();
            }
        });
    }

    private async Task SearchBoards(bool reset = false)
    {
        if (reset)
        {
            _currentPage = 1;
            _boards.Clear();
            _hasMoreData = true;
        }

        if (!_hasMoreData) return;

        if (!_selectedOrganizationId.HasValue)
        {
            // No organization selected - clear boards
            _boards.Clear();
            _hasMoreData = false;
            StateHasChanged();
            return;
        }

        try
        {
            _isSearching = true;
            StateHasChanged();

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null) return;

            var userId = currentUser.Id;
            IEnumerable<BoardSearchDto> results;

            if (_filterType == "owner")
            {
                results = await BoardService.SearchBoardsRangeForOwnerAsync(
                    userId, _selectedOrganizationId.Value, _searchTerm, _currentPage, _pageSize);
            }
            else if (_filterType == "member")
            {
                results = await BoardService.SearchBoardsRangeForMemberAsync(
                    userId, _selectedOrganizationId.Value, _searchTerm, _currentPage, _pageSize);
            }
            else if (_filterType == "archived")
            {
                results = await BoardService.GetArchivedBoardsRangeForUserAsync(
                    userId, _searchTerm, _currentPage, _pageSize);
            }
            else
            {
                results = await BoardService.SearchBoardsRangeForUserAsync(
                    userId, _selectedOrganizationId.Value, _searchTerm, _currentPage, _pageSize);
            }

            var resultsList = results.ToList();

            if (reset)
            {
                _boards = resultsList;
            }
            else
            {
                _boards.AddRange(resultsList);
            }

            _hasMoreData = resultsList.Count == _pageSize;

            if (!reset)
            {
                _currentPage++;
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _isSearching = false;
            StateHasChanged();
        }
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? string.Empty;
        
        _searchTimer?.Stop();
        _searchTimer?.Dispose();
        
        _searchTimer = new System.Timers.Timer(300);
        _searchTimer.Elapsed += async (_, _) =>
        {
            await InvokeAsync(async () =>
            {
                await SearchBoards(reset: true);
            });
        };
        _searchTimer.AutoReset = false;
        _searchTimer.Start();
    }

    private async Task OnFilterChanged(string value)
    {
        _filterType = value;
        await SearchBoards(reset: true);
    }

    private async Task LoadMoreBoards()
    {
        if (!_hasMoreData || _isSearching) return;
        await SearchBoards(reset: false);
    }

    private void ClearFilters()
    {
        _searchTerm = string.Empty;
        _filterType = "all";
        InvokeAsync(async () => await SearchBoards(reset: true));
    }

    private void ShowCreateModal()
    {
        _showCreateModal = true;
    }

    private async Task OnOrganizationChanged(Guid organizationId)
    {
        _selectedOrganizationId = organizationId;
        await SearchBoards(reset: true);
    }

    private async Task HandleBoardCreated(string boardId)
    {
        _showCreateModal = false;
        
        var currentUser = AuthService.GetCachedUser();
        if (currentUser != null)
        {
            await BoardService.ClearCacheAsync(currentUser.Id.ToString());
        }
        
        await SearchBoards(reset: true);
    }

    private void HandleBoardClick(string boardId)
    {
        Navigation.NavigateTo($"/board/{boardId}");
    }

    private void HandleEditBoard(string boardId)
    {
        Navigation.NavigateTo($"/boards/{boardId}/edit");
    }

    private void HandleDeleteBoard(string boardId)
    {
        _selectedBoard = _boards.FirstOrDefault(b => b.Id == boardId);
        if (_selectedBoard != null)
        {
            _deleteConfirmation = string.Empty;
            _showDeleteModal = true;
        }
    }

    private async Task ConfirmDelete()
    {
        if (_selectedBoard == null ||
            !_deleteConfirmation.Trim().Equals(_selectedBoard.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Error(UI.Resources.I18n.DeleteBoardConfirmationMismatch);
            return;
        }

        try
        {
            _isDeleting = true;
            StateHasChanged();

            await BoardService.DeleteAsync(_selectedBoard.Id);
            
            var currentUser = AuthService.GetCachedUser();
            if (currentUser != null)
            {
                await BoardService.ClearCacheAsync(currentUser.Id.ToString());
            }
            
            await SearchBoards(reset: true);
            
            _showDeleteModal = false;
            _selectedBoard = null;
            _deleteConfirmation = string.Empty;
        }
        catch (Exception)
        {
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }

    private void CancelDelete()
    {
        _showDeleteModal = false;
        _selectedBoard = null;
        _deleteConfirmation = string.Empty;
    }

    private string GetEmptyDescription()
    {
        if (!_boards.Any())
        {
            if (!string.IsNullOrWhiteSpace(_searchTerm))
                return string.Format(UI.Resources.I18n.NoBoardsFoundMatching, _searchTerm);

            return _filterType switch
            {
                "owner" => UI.Resources.I18n.YouDontOwnAnyBoardsYetCreateFirst,
                "member" => UI.Resources.I18n.YouAreNotMemberOfAnyBoards,
                _ => UI.Resources.I18n.YouDontHaveAnyBoardsCreateFirst
            };
        }
        
        return UI.Resources.I18n.NoBoardsFound;
    }

    private async Task RefreshBoards()
    {
        var currentUser = AuthService.GetCachedUser();
        if (currentUser != null)
        {
            await BoardService.ClearCacheAsync(currentUser.Id.ToString());
        }
        await SearchBoards(reset: true);
    }

    private bool IsCurrentUserOwner(string ownerId)
    {
        var currentUser = AuthService.GetCachedUser();
        return currentUser?.Id.ToString() == ownerId;
    }

    public void Dispose()
    {
        _searchTimer?.Stop();
        _searchTimer?.Dispose();
    }
}