using Refit;
using UI.Models.Organization;

namespace UI.Interfaces.Api;

public interface IOrganizationApi
{
    [Get("/api/organizations/{organizationId}")]
    Task<OrganizationDto> GetOrganizationAsync(Guid organizationId);

    [Get("/api/organizations/{organizationId}/members")]
    Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid organizationId);

    [Post("/api/organizations/{organizationId}/guests")]
    Task AddGuestAsync(Guid organizationId, [Body] AddGuestRequest request);

    [Post("/api/organizations/{organizationId}/manager-request")]
    Task SendManagerRequestAsync(Guid organizationId, [Body] SendManagerRequestDto request);

    [Get("/api/organizations/manager-requests/pending")]
    Task<IEnumerable<ManagerRequestDto>> GetPendingManagerRequestsAsync();

    [Post("/api/organizations/manager-requests/{requestId}/approve")]
    Task ApproveManagerRequestAsync(Guid requestId);

    [Post("/api/organizations/manager-requests/{requestId}/reject")]
    Task RejectManagerRequestAsync(Guid requestId, [Body] RejectManagerRequestDto request);

    [Post("/api/organizations/{organizationId}/members/{userId}/promote")]
    Task PromoteToManagerAsync(Guid organizationId, Guid userId);

    [Post("/api/organizations/{organizationId}/members/{userId}/demote")]
    Task DemoteFromManagerAsync(Guid organizationId, Guid userId);
}
