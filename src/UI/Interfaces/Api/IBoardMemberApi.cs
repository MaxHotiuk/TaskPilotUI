using Refit;
using UI.Models.Member;

namespace UI.Interfaces.Api;

public interface IBoardMemberApi
{
    [Get("/api/boards/{boardId}/members")]
    Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId);

    [Post("/api/boards/{boardId}/members")]
    Task AddBoardMemberAsync(string boardId, [Body] AddBoardMemberRequest request);

    [Put("/api/boards/{boardId}/members/{userId}/role")]
    Task UpdateBoardMemberRoleAsync(string boardId, string userId, [Body] UpdateBoardMemberRoleRequest request);

    [Delete("/api/boards/{boardId}/members/{userId}")]
    Task RemoveBoardMemberAsync(string boardId, string userId);
}
