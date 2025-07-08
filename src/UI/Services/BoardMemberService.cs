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

    public async Task AddBoardMemberAsync(string boardId, AddBoardMemberRequest request)
    {
        try
        {
            await _boardMemberApi.AddBoardMemberAsync(boardId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateBoardMemberRoleAsync(string boardId, string userId, UpdateBoardMemberRoleRequest request)
    {
        try
        {
            await _boardMemberApi.UpdateBoardMemberRoleAsync(boardId, userId, request);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task RemoveBoardMemberAsync(string boardId, string userId)
    {
        try
        {
            await _boardMemberApi.RemoveBoardMemberAsync(boardId, userId);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
