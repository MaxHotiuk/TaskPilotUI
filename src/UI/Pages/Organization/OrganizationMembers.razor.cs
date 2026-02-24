using Microsoft.AspNetCore.Components;
using UI.Models.Organization;
using UI.Interfaces.Services;
using UI.Services;
using UI.Components.Base;

namespace UI.Pages.Organization;

public partial class OrganizationMembers : BaseComponentWithLoading
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IPublicDomainService PublicDomainService { get; set; } = default!;

    [Parameter] public string OrganizationId { get; set; } = string.Empty;

    private List<OrganizationMemberDto> _members = new();
    private string _organizationName = string.Empty;
    private string _organizationDomain = string.Empty;
    private bool _isPublicOrganization = false;
    private string _currentUserRole = string.Empty;
    private Guid _currentUserId = Guid.Empty;
    private bool _isAdmin = false;
    private bool _hasManagers = false;
    private string? _errorMessage;
    private string? _successMessage;

    private bool _showAddGuestModal = false;
    private bool _showManagerRequestModal = false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadCurrentUser();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Переконаємось, що currentUserId встановлений
        if (_currentUserId == Guid.Empty)
        {
            await LoadCurrentUser();
        }

        if (!string.IsNullOrEmpty(OrganizationId))
        {
            await LoadMembersAsync();
        }
    }

    private async Task LoadCurrentUser()
    {
        var currentUser = AuthService.GetCachedUser();
        if (currentUser == null)
        {
            // Спробуємо завантажити користувача, якщо кеш порожній
            currentUser = await AuthService.GetCurrentUserAsync();
        }

        if (currentUser != null)
        {
            _currentUserId = currentUser.Id;
            _isAdmin = currentUser.Role == "Admin";
            StateHasChanged();
        }
    }

    private async Task LoadMembersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                var orgGuid = Guid.Parse(OrganizationId);
                var members = await OrganizationService.GetMembersAsync(orgGuid);
                _members = members.ToList();

                var currentUser = AuthService.GetCachedUser();
                if (currentUser != null)
                {
                    var currentMember = _members.FirstOrDefault(m => m.UserId == currentUser.Id);
                    if (currentMember != null)
                    {
                        _currentUserRole = currentMember.Role;
                    }

                    var org = currentUser.Organizations?.FirstOrDefault(o => o.Id == orgGuid);
                    if (org != null)
                    {
                        _organizationName = org.Name;
                        // Organization name IS the domain
                        _organizationDomain = org.Name;
                        _isPublicOrganization = PublicDomainService.IsPublicDomain(org.Name);
                    }
                }

                _hasManagers = _members.Any(m => m.Role == "Manager");
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        });
    }

    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        if (LoadingService != null)
        {
            await LoadingService.ExecuteWithLoadingAsync(action);
        }
        else
        {
            await action();
        }
    }

    private bool IsLastManager(Guid userId)
    {
        var managerCount = _members.Count(m => m.Role == "Manager");
        var isManager = _members.FirstOrDefault(m => m.UserId == userId)?.Role == "Manager";
        return isManager && managerCount == 1;
    }

    private async Task PromoteMember(Guid userId)
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                var orgGuid = Guid.Parse(OrganizationId);
                await OrganizationService.PromoteToManagerAsync(orgGuid, userId);
                _successMessage = UI.Resources.I18n.MemberPromotedSuccessfully;
                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        });
    }

    private async Task DemoteMember(Guid userId)
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                var orgGuid = Guid.Parse(OrganizationId);
                await OrganizationService.DemoteFromManagerAsync(orgGuid, userId);
                _successMessage = UI.Resources.I18n.MemberDemotedSuccessfully;
                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
        });
    }

    private void ShowAddGuestModal()
    {
        _showAddGuestModal = true;
    }

    private void ShowManagerRequestModal()
    {
        // Діагностика - переконаємось що userId встановлений
        if (_currentUserId == Guid.Empty)
        {
            _errorMessage = "Unable to open request form: User ID is not loaded. Please refresh the page.";
            return;
        }

        _showManagerRequestModal = true;
    }

    private async Task HandleGuestAdded()
    {
        _successMessage = "Guest invitation sent successfully";
        StateHasChanged();
    }

    private async Task HandleManagerRequestSent()
    {
        _successMessage = UI.Resources.I18n.ManagerRequestSentSuccessfully;
        StateHasChanged();
    }
}
