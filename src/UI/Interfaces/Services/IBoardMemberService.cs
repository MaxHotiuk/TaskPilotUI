using UI.Models.Member;

namespace UI.Interfaces.Services;

public interface IBoardMemberService
{
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId);
    Task AddBoardMemberAsync(string boardId, AddBoardMemberRequest request);
    Task UpdateBoardMemberRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request);
    Task RemoveBoardMemberAsync(string boardId, string userId);
}
