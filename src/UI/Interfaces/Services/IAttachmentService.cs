using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Models.Attachment;

namespace UI.Interfaces.Services;

public interface IAttachmentService
{
    Task<List<AttachmentDto>> GetAsync(Guid entityId);
    Task<AttachmentDto> UploadAsync(Guid entityId, Stream fileStream, string fileName);
    Task DeleteAsync(string fileName);
}
