namespace UI.Models.Member;

public class AddBoardMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
}
