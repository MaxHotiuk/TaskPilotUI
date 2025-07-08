using Refit;
using UI.Models.Board;

namespace UI.Interfaces.Api;

public interface IBoardApi
{
    [Get("/api/boards/{id}")]
    Task<BoardDto> GetByIdAsync(string id);

    [Post("/api/boards")]
    Task<string> CreateAsync([Body] CreateBoardRequest request);

    [Put("/api/boards/{id}")]
    Task UpdateAsync(string id, [Body] CreateBoardRequest request);

    [Delete("/api/boards/{id}")]
    Task DeleteAsync(string id);
}
