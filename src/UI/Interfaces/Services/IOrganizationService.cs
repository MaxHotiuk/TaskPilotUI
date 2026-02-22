using UI.Models.Organization;

namespace UI.Interfaces.Services;

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid organizationId);
    Task AddGuestAsync(Guid organizationId, string userEmail);
    Task SendManagerRequestAsync(Guid organizationId, Guid userId, string message);
    Task<IEnumerable<ManagerRequestDto>> GetPendingManagerRequestsAsync();
    Task ApproveManagerRequestAsync(Guid requestId);
    Task RejectManagerRequestAsync(Guid requestId, string? reviewNotes = null);
    Task PromoteToManagerAsync(Guid organizationId, Guid userId);
    Task DemoteFromManagerAsync(Guid organizationId, Guid userId);
}
