using UI.Models.Member;

namespace UI.Interfaces.Services;

public interface IBoardMemberService
{
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId);
    Task AddAsync(string boardId, AddBoardMemberRequest request);
    Task UpdateRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request);
    Task RemoveAsync(string boardId, string userId);
}
