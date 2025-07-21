using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Member;

namespace UI.Services;

public class BoardMemberService : IBoardMemberService
{
    private readonly IBoardMemberApi _boardMemberApi;

    public BoardMemberService(IBoardMemberApi boardMemberApi)
    {
        _boardMemberApi = boardMemberApi;
    }

    public async Task<List<BoardMemberDto>> GetBoardMembersAsync(string boardId)
    {
        try
        {
            return await _boardMemberApi.GetBoardMembersAsync(boardId);
        }
        catch (Exception)
        {
            return new List<BoardMemberDto>();
        }
    }

    public async Task AddAsync(string boardId, AddBoardMemberRequest request)
    {
        try
        {
            await _boardMemberApi.AddAsync(boardId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request)
    {
        try
        {
            await _boardMemberApi.UpdateRoleAsync(boardId, userId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task RemoveAsync(string boardId, string userId)
    {
        try
        {
            await _boardMemberApi.RemoveAsync(boardId, userId);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
