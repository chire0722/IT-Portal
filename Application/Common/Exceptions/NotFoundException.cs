// Project comment: NotFoundException.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Exceptions;

/// <summary>
/// Defines the NotFoundException class used by the IT Form application.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
