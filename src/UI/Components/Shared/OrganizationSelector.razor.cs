using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Models.Organization;

namespace UI.Components.Shared;

public partial class OrganizationSelector : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorageService { get; set; } = default!;

    [Parameter] public Guid? SelectedOrganizationId { get; set; }
    [Parameter] public EventCallback<Guid> SelectedOrganizationIdChanged { get; set; }
    [Parameter] public bool ShowLabel { get; set; } = true;
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public bool Required { get; set; } = false;
    [Parameter] public bool ExcludeGuestOrganizations { get; set; } = false; // New parameter

    private const string SELECTED_ORG_KEY = "selectedOrganizationId";
    private List<OrganizationSummaryDto> _organizations = new();
    private List<OrganizationSummaryDto> _filteredOrganizations = new();
    private bool _isLoading = true;
    private Guid _selectedValue = Guid.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrganizations();
    }

    private async Task LoadOrganizations()
    {
        try
        {
            _isLoading = true;
            var currentUser = await AuthService.GetCurrentUserAsync();

            if (currentUser?.Organizations != null)
            {
                _organizations = currentUser.Organizations.ToList();

                Console.WriteLine($"OrganizationSelector - Total organizations loaded: {_organizations.Count}");
                foreach (var org in _organizations)
                {
                    Console.WriteLine($"  - {org.Name} (Role: '{org.Role}', IsGuest: {org.Role == "Guest"})");
                }

                // Filter organizations based on ExcludeGuestOrganizations parameter
                if (ExcludeGuestOrganizations)
                {
                    _filteredOrganizations = _organizations
                        .Where(o => !string.IsNullOrEmpty(o.Role) && o.Role != "Guest")
                        .ToList();

                    Console.WriteLine($"OrganizationSelector - ExcludeGuestOrganizations=true");
                    Console.WriteLine($"OrganizationSelector - Filtered organizations: {_filteredOrganizations.Count}");
                    foreach (var org in _filteredOrganizations)
                    {
                        Console.WriteLine($"  - Available: {org.Name} (Role: {org.Role})");
                    }
                }
                else
                {
                    _filteredOrganizations = _organizations;
                    Console.WriteLine($"OrganizationSelector - ExcludeGuestOrganizations=false, showing all {_filteredOrganizations.Count} organizations");
                }

                // Auto-select if user has only one eligible organization
                if (_filteredOrganizations.Count == 1 && !SelectedOrganizationId.HasValue)
                {
                    Console.WriteLine($"OrganizationSelector - Auto-selecting only organization: {_filteredOrganizations[0].Name}");
                    await HandleOrganizationChanged(_filteredOrganizations[0].Id);
                }
                // Try to restore last selected organization (if it's still eligible)
                else if (!SelectedOrganizationId.HasValue)
                {
                    var savedOrgId = await LocalStorageService.GetItemAsync<Guid?>(SELECTED_ORG_KEY);
                    if (savedOrgId.HasValue && _filteredOrganizations.Any(o => o.Id == savedOrgId.Value))
                    {
                        Console.WriteLine($"OrganizationSelector - Restoring saved organization: {savedOrgId.Value}");
                        await HandleOrganizationChanged(savedOrgId.Value);
                    }
                }
            }
            else
            {
                Console.WriteLine($"OrganizationSelector - No organizations found in user data");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OrganizationSelector - Error loading organizations: {ex.Message}");
            Console.WriteLine($"OrganizationSelector - Stack trace: {ex.StackTrace}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleOrganizationChanged(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            Console.WriteLine("WARNING: Attempting to select Guid.Empty as organization");
            return;
        }

        _selectedValue = organizationId;
        SelectedOrganizationId = organizationId;
        await LocalStorageService.SetItemAsync(SELECTED_ORG_KEY, organizationId);
        await SelectedOrganizationIdChanged.InvokeAsync(organizationId);
        Console.WriteLine($"Organization selected: {organizationId}");
    }

    private async Task HandleSelectedItemChanged(OrganizationSummaryDto organization)
    {
        if (organization != null && organization.Id != Guid.Empty)
        {
            await HandleOrganizationChanged(organization.Id);
        }
    }

    protected override void OnParametersSet()
    {
        if (SelectedOrganizationId.HasValue && SelectedOrganizationId.Value != Guid.Empty)
        {
            _selectedValue = SelectedOrganizationId.Value;
            Console.WriteLine($"OrganizationSelector initialized with: {SelectedOrganizationId.Value}");
        }
    }
}
