using System;
using System.Threading.Tasks;
using UI.Models.Avatar;

namespace UI.Interfaces.Services
{
    public interface IAvatarService
    {
        Task<AvatarDto> UploadAsync(Guid userId, Stream fileStream, string fileName);
        Task<AvatarDto> UpdateAsync(Guid userId, Stream fileStream, string fileName);
        Task DeleteAsync(Guid userId);
        Task<AvatarDto?> GetAvatarOrNullAsync(Guid userId);
    }
}
