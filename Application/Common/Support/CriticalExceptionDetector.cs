// Project comment: CriticalExceptionDetector.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Support;

/// <summary>
/// Defines the CriticalExceptionDetector class used by the IT Form application.
/// </summary>
public static class CriticalExceptionDetector
{
    /// <summary>
    /// Runs the IsCritical operation.
    /// </summary>
    public static bool IsCritical(Exception exception) =>
        exception is OutOfMemoryException
        or AccessViolationException
        or AppDomainUnloadedException
        or BadImageFormatException
        or CannotUnloadAppDomainException;
}
