using System;
using System.IO;
using System.Threading.Tasks;
using Refit;
using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Avatar;

namespace UI.Services
{
    public class AvatarService : IAvatarService
    {
        private readonly IAvatarApi _avatarApi;
        public AvatarService(IAvatarApi avatarApi)
        {
            _avatarApi = avatarApi;
        }

        public async Task<AvatarDto> UploadAsync(Guid userId, Stream fileStream, string fileName)
        {
            var streamPart = new StreamPart(fileStream, fileName);
            return await _avatarApi.UploadAsync(userId, streamPart);
        }

        public async Task<AvatarDto> UpdateAsync(Guid userId, Stream fileStream, string fileName)
        {
            var streamPart = new StreamPart(fileStream, fileName);
            return await _avatarApi.UpdateAsync(userId, streamPart);
        }

        public async Task DeleteAsync(Guid userId)
        {
            await _avatarApi.DeleteAsync(userId);
        }

        public async Task<AvatarDto?> GetAvatarOrNullAsync(Guid userId)
        {
            try
            {
                return await _avatarApi.GetAsync(userId);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
