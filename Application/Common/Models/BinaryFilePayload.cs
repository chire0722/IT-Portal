namespace IT_Form.Application.Common.Models;

public sealed record BinaryFilePayload(
    byte[] Content,
    string ContentType,
    string FileDownloadName);
