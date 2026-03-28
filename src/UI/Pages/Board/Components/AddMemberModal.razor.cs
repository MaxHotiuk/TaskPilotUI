using Microsoft.AspNetCore.Components;
using UI.Models.User;
using UI.Models.Member;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components;

public partial class AddMemberModal : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public AddMemberForm FormModel { get; set; } = new();
    [Parameter] public List<BoardMemberDto> ExistingMembers { get; set; } = new();
    [Parameter] public string? BoardOwnerId { get; set; }
    [Parameter] public Guid? OrganizationId { get; set; }
    [Parameter] public List<UserDto>? OrganizationUsers { get; set; } // Pre-loaded users
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    private string SearchText { get; set; } = string.Empty;
    private List<UserDto> SearchResults { get; set; } = new();
    private List<UserDto> AllUsers { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (OrganizationUsers != null && OrganizationUsers.Any())
        {
            AllUsers = OrganizationUsers;
        }
        else
        {
            await LoadAllUsers();
        }
    }

    private async Task LoadAllUsers()
    {
        try
        {
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            if (isAuthenticated && OrganizationId.HasValue)
            {
                AllUsers = await UserService.GetAllUsersAsync(OrganizationId.Value);
            }
            else if (!OrganizationId.HasValue)
            {
                Console.WriteLine($"AddMemberModal - OrganizationId is not set, cannot load users");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
        }
    }

    private void OnSearch(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            SearchResults.Clear();
            return;
        }

        try
        {
            var lowerSearchText = searchText.ToLowerInvariant();
            var existingMemberIds = ExistingMembers.Select(m => m.UserId).ToHashSet();
            
            SearchResults = AllUsers
                .Where(u => u.Email.ToLowerInvariant().Contains(lowerSearchText) || 
                           u.Username.ToLowerInvariant().Contains(lowerSearchText))
                .Where(u => !FormModel.SelectedUsers.Any(su => su.Id == u.Id))
                .Where(u => !existingMemberIds.Contains(u.Id.ToString()))
                .Where(u => u.Id.ToString() != BoardOwnerId)
                .Take(10)
                .ToList();

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching users: {ex.Message}");
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        var searchText = e.Value?.ToString() ?? string.Empty;
        SearchText = searchText;
        OnSearch(searchText);
    }

    private void OnUserSelect(string value)
    {
        var selectedUser = SearchResults.FirstOrDefault(u => u.Email == value || u.Username == value);
        if (selectedUser != null && !FormModel.SelectedUsers.Any(u => u.Id == selectedUser.Id))
        {
            FormModel.SelectedUsers.Add(selectedUser);
            SearchText = string.Empty;
            SearchResults.Clear();
            StateHasChanged();
        }
    }

    private void SelectUser(UserDto user)
    {
        if (!FormModel.SelectedUsers.Any(u => u.Id == user.Id))
        {
            FormModel.SelectedUsers.Add(user);
            SearchText = string.Empty;
            SearchResults.Clear();
            StateHasChanged();
        }
    }

    private void RemoveUser(UserDto user)
    {
        FormModel.SelectedUsers.Remove(user);
        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Update AllUsers when OrganizationUsers changes
        if (OrganizationUsers != null && OrganizationUsers.Any() && AllUsers.Count == 0)
        {
            AllUsers = OrganizationUsers;
        }

        if (!IsVisible)
        {
            SearchText = string.Empty;
            SearchResults.Clear();
        }
        else if (IsVisible && OrganizationId.HasValue && AllUsers.Count == 0 && (OrganizationUsers == null || !OrganizationUsers.Any()))
        {
            // Only reload if we don't have pre-loaded users
            _ = LoadAllUsers();
        }
    }

    public class AddMemberForm
    {
        public List<UserDto> SelectedUsers { get; set; } = new();
        public string Role { get; set; } = "Member";
    }
}
