using UI.Models.Board;
using UI.Models.Member;
using UI.Models.Task;
using UI.Models.State;
using UI.Models.Tag;

namespace UI.Models.Board;

public class BoardDetailDto : BoardDto
{
    public List<BoardMemberDto> Members { get; set; } = new();
    public List<TaskItemDto> Tasks { get; set; } = new();
    public IEnumerable<TagDto> Tags { get; set; } = new List<TagDto>();
    public List<StateDto> States { get; set; } = new();
}
