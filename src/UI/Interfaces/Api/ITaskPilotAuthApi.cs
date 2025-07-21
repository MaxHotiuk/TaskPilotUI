using Refit;
using UI.Models.User;

namespace UI.Interfaces.Api;

public interface ITaskPilotAuthApi
{
    [Get("/api/users/me")]
    Task<UserDto> GetCurrentAsync([Header("Authorization")] string authorization);
}
