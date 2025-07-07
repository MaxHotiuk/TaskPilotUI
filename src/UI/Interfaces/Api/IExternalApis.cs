using Refit;
using System.Text.Json;

namespace UI.Interfaces.Api;

public interface IMicrosoftGraphApi
{
    [Get("/v1.0/me")]
    Task<JsonElement> GetMeAsync([Header("Authorization")] string authorization);
}

public interface IAzureAdTokenApi
{
    [Post("/oauth2/v2.0/token")]
    Task<JsonElement> GetTokenAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> request);
}
