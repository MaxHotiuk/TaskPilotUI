namespace UI.Models.Organization;

public enum OrganizationRole
{
    Guest = 0,
    Member = 1,
    Manager = 2
}

public static class OrganizationRoleExtensions
{
    public static bool CanCreateBoards(this string role)
    {
        return role != "Guest";
    }

    public static bool CanCreateGroupChats(this string role)
    {
        return role != "Guest";
    }

    public static bool CanManageOrganization(this string role)
    {
        return role == "Manager";
    }

    public static bool CanRequestManagerRole(this string role)
    {
        return role == "Member";
    }
}
