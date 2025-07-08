using Refit;
using UI.Models.Board;

namespace UI.Interfaces.Api;

public interface IBoardApi
{
    [Get("/api/boards/{id}")]
    Task<BoardDto> GetBoardByIdAsync(string id);

    [Post("/api/boards")]
    Task<string> CreateBoardAsync([Body] CreateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task UpdateBoardAsync(string id, [Body] CreateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteBoardAsync(string id);
}
