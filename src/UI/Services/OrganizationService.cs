using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Organization;

namespace UI.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationApi _organizationApi;

    public OrganizationService(IOrganizationApi organizationApi)
    {
        _organizationApi = organizationApi;
    }

    public async Task<IEnumerable<OrganizationMemberDto>> GetMembersAsync(Guid organizationId)
    {
        return await _organizationApi.GetMembersAsync(organizationId);
    }

    public async Task AddGuestAsync(Guid organizationId, string userEmail)
    {
        var request = new AddGuestRequest { UserEmail = userEmail };
        await _organizationApi.AddGuestAsync(organizationId, request);
    }

    public async Task SendManagerRequestAsync(Guid organizationId, Guid userId, string message)
    {
        var request = new SendManagerRequestDto
        {
            UserId = userId,
            Message = message
        };
        await _organizationApi.SendManagerRequestAsync(organizationId, request);
    }

    public async Task<IEnumerable<ManagerRequestDto>> GetPendingManagerRequestsAsync()
    {
        return await _organizationApi.GetPendingManagerRequestsAsync();
    }

    public async Task ApproveManagerRequestAsync(Guid requestId)
    {
        await _organizationApi.ApproveManagerRequestAsync(requestId);
    }

    public async Task RejectManagerRequestAsync(Guid requestId, string? reviewNotes = null)
    {
        var request = new RejectManagerRequestDto { ReviewNotes = reviewNotes };
        await _organizationApi.RejectManagerRequestAsync(requestId, request);
    }

    public async Task PromoteToManagerAsync(Guid organizationId, Guid userId)
    {
        await _organizationApi.PromoteToManagerAsync(organizationId, userId);
    }

    public async Task DemoteFromManagerAsync(Guid organizationId, Guid userId)
    {
        await _organizationApi.DemoteFromManagerAsync(organizationId, userId);
    }
}
