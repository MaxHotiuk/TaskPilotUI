namespace UI.Models.Member;

public class AddMultipleBoardMembersRequest
{
    public List<AddBoardMemberRequest> Members { get; set; } = new();
}
