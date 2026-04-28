// Project comment: BinaryFilePayload.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Models;

/// <summary>
/// Defines the BinaryFilePayload record used by the IT Form application.
/// </summary>
public sealed record BinaryFilePayload(
    byte[] Content,
    string ContentType,
    string FileDownloadName);
