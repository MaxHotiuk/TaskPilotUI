using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using UI.Models.Attachment;

namespace UI.Interfaces.Api;

public interface IAttachmentApi
{
    [Get("/api/attachments/{entityId}")]
    Task<List<AttachmentDto>> GetAsync(Guid entityId);

    [Multipart]
    [Post("/api/attachments/{entityId}")]
    Task<AttachmentDto> UploadAsync(Guid entityId, [AliasAs("file")] StreamPart file);

    [Delete("/api/attachments/{fileName}")]
    Task DeleteAsync(string fileName);
}
