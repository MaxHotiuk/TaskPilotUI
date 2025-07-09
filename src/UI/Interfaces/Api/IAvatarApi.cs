using System;
using System.Threading.Tasks;
using Refit;
using UI.Models.Avatar;

namespace UI.Interfaces.Api;

public interface IAvatarApi
{
    [Multipart]
    [Post("/api/avatars/{userId}")]
    Task<AvatarDto> UploadAsync(Guid userId, [AliasAs("file")] StreamPart file);

    [Multipart]
    [Put("/api/avatars/{userId}")]
    Task<AvatarDto> UpdateAsync(Guid userId, [AliasAs("file")] StreamPart file);

    [Delete("/api/avatars/{userId}")]
    Task DeleteAsync(Guid userId);

    [Get("/api/avatars/{userId}")]
    Task<AvatarDto> GetAsync(Guid userId);
}
