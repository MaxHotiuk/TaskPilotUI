using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Refit;
using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Attachment;

namespace UI.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IAttachmentApi _attachmentApi;
        public AttachmentService(IAttachmentApi attachmentApi)
        {
            _attachmentApi = attachmentApi;
        }

        public async Task<List<AttachmentDto>> GetAsync(Guid entityId, Guid userId)
        {
            return await _attachmentApi.GetAsync(entityId, userId);
        }

        public async Task<AttachmentDto> UploadAsync(Guid entityId, Stream fileStream, string fileName)
        {
            var streamPart = new StreamPart(fileStream, fileName);
            return await _attachmentApi.UploadAsync(entityId, streamPart);
        }

        public async Task DeleteAsync(string fileName)
        {
            await _attachmentApi.DeleteAsync(fileName);
        }
    }
}
