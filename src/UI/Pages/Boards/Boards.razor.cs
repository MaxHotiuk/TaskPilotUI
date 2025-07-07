using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using UI.Models.Member;
using UI.Models.User;
using UI.Services;

namespace UI.Pages.Boards;

public partial class Boards : ComponentBase
{
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<BoardWithStats> _boards = new();
    private List<BoardWithStats> _filteredBoards = new();
    private bool _isLoading = true;
    private string _searchTerm = string.Empty;
    private string _filterType = "all";
    private bool _showCreateModal = false;
    private bool _showDeleteModal = false;
    private bool _isDeleting = false;
    private BoardWithStats? _selectedBoard = null;
    private string _deleteConfirmation = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadBoardsWithCache();
    }

    private async Task LoadBoardsWithCache()
    {
        try
        {
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null)
            {
                currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    Navigation.NavigateTo("/login");
                    return;
                }
            }

            await LoadCachedBoards(currentUser.Id);
            
            _ = Task.Run(async () => await LoadFreshBoards(currentUser.Id));
        }
        catch (Exception)
        {
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadCachedBoards(string userId)
    {
        var cachedBoards = await BoardService.GetCachedUserBoardsAsync(userId);
        if (cachedBoards.Any())
        {
            _boards = new List<BoardWithStats>();
            foreach (var board in cachedBoards)
            {
                var cachedStats = await BoardService.GetCachedBoardWithStatsAsync(board.Id);
                if (cachedStats.Board?.Id != null)
                {
                    _boards.Add(cachedStats);
                }
                else
                {
                    _boards.Add(new BoardWithStats
                    {
                        Board = board,
                        TaskCount = 0,
                        MemberCount = 1,
                        Members = new List<BoardMemberDto>(),
                        IsOwner = board.OwnerId == userId
                    });
                }
            }
            ApplyFilters();
        }
    }

    private async Task LoadFreshBoards(string userId)
    {
        try
        {
            var userBoards = await BoardService.GetUserBoardsAsync(userId);
            var freshBoards = new List<BoardWithStats>();

            foreach (var board in userBoards)
            {
                var boardStats = await BoardService.GetBoardWithStatsAsync(board.Id);
                freshBoards.Add(boardStats);
            }

            if (freshBoards.Any() && !AreBoardListsEqual(_boards, freshBoards))
            {
                await InvokeAsync(() =>
                {
                    _boards = freshBoards;
                    ApplyFilters();
                    StateHasChanged();
                });
            }
        }
        catch (Exception)
        {
        }
    }

    private bool AreBoardListsEqual(List<BoardWithStats> list1, List<BoardWithStats> list2)
    {
        if (list1.Count != list2.Count) return false;
        
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i].Board.Id != list2[i].Board.Id ||
                list1[i].Board.UpdatedAt != list2[i].Board.UpdatedAt ||
                list1[i].TaskCount != list2[i].TaskCount ||
                list1[i].MemberCount != list2[i].MemberCount)
            {
                return false;
            }
        }
        return true;
    }

    private async Task LoadBoards()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var currentUser = AuthService.GetCachedUser();
            if (currentUser == null)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            var userBoards = await BoardService.GetUserBoardsAsync(currentUser.Id);
            _boards = new List<BoardWithStats>();

            foreach (var board in userBoards)
            {
                var boardStats = await BoardService.GetBoardWithStatsAsync(board.Id);
                _boards.Add(boardStats);
            }

            ApplyFilters();
        }
        catch (Exception)
        {
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void ApplyFilters()
    {
        _filteredBoards = _boards.Where(board =>
        {
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                var searchLower = _searchTerm.ToLower();
                if (!board.Board.Name.ToLower().Contains(searchLower) &&
                    !board.Board.Description?.ToLower().Contains(searchLower) == true)
                {
                    return false;
                }
            }

            return _filterType switch
            {
                "owner" => board.IsOwner,
                "member" => !board.IsOwner,
                _ => true
            };
        }).ToList();

        StateHasChanged();
    }

    private void OnSearchChanged(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    private void OnFilterChanged(string value)
    {
        _filterType = value;
        ApplyFilters();
    }

    private void ClearFilters()
    {
        _searchTerm = string.Empty;
        _filterType = "all";
        ApplyFilters();
    }

    private void ShowCreateModal()
    {
        _showCreateModal = true;
    }

    private async Task HandleBoardCreated(string boardId)
    {
        _showCreateModal = false;
        
        var currentUser = AuthService.GetCachedUser();
        if (currentUser != null)
        {
            await BoardService.ClearBoardCacheAsync(currentUser.Id);
        }
        
        await LoadBoards();
    }

    private void HandleBoardClick(string boardId)
    {
        Navigation.NavigateTo($"/boards/{boardId}");
    }

    private void HandleEditBoard(string boardId)
    {
        Navigation.NavigateTo($"/boards/{boardId}/edit");
    }

    private void HandleDeleteBoard(string boardId)
    {
        _selectedBoard = _boards.FirstOrDefault(b => b.Board.Id == boardId);
        if (_selectedBoard != null)
        {
            _deleteConfirmation = string.Empty;
            _showDeleteModal = true;
        }
    }

    private async Task ConfirmDelete()
    {
        if (_selectedBoard == null || _deleteConfirmation != _selectedBoard.Board.Name)
        {
            return;
        }

        try
        {
            _isDeleting = true;
            StateHasChanged();

            await BoardService.DeleteBoardAsync(_selectedBoard.Board.Id);
            
            var currentUser = AuthService.GetCachedUser();
            if (currentUser != null)
            {
                await BoardService.ClearBoardCacheAsync(currentUser.Id);
            }
            
            await LoadBoards();
            
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
            return "You don't have any boards yet. Create your first board to get started!";
        
        if (!string.IsNullOrWhiteSpace(_searchTerm))
            return $"No boards found matching \"{_searchTerm}\"";
        
        return _filterType switch
        {
            "owner" => "You don't own any boards",
            "member" => "You're not a member of any boards",
            _ => "No boards found"
        };
    }

    private async Task RefreshBoards()
    {
        var currentUser = AuthService.GetCachedUser();
        if (currentUser != null)
        {
            await BoardService.ClearBoardCacheAsync(currentUser.Id);
        }
        await LoadBoards();
    }
}
