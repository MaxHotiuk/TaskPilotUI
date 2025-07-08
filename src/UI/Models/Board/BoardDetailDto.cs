using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
using UI.Models.State;

namespace UI.Models.Board;

public class BoardDetailDto : BoardDto
{
    public List<BoardMemberDto> Members { get; set; } = new();
    public List<TaskItemDto> Tasks { get; set; } = new();
    public List<StateDto> States { get; set; } = new();
}
