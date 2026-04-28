// Project comment: AddSoftwareKeyResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    /// <summary>
    /// Lists the supported AddSoftwareKeyStatus values used by the application.
    /// </summary>
    public enum AddSoftwareKeyStatus
    {
        Added,
        Failed
    }

    /// <summary>
    /// Represents the result returned by the AddSoftwareKey workflow.
    /// </summary>
    public sealed record AddSoftwareKeyResult(
        AddSoftwareKeyStatus Status,
        string Message);
}
