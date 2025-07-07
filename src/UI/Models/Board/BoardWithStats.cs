namespace UI.Models.Board;

using UI.Models.Member;

public class BoardWithStats
{
    public BoardDto Board { get; set; } = new();
    public int TaskCount { get; set; }
    public int MemberCount { get; set; }
    public List<BoardMemberDto> Members { get; set; } = new();
    public bool IsOwner { get; set; }
}
