namespace IT_Form.Application.Common.Support;

public static class CriticalExceptionDetector
{
    public static bool IsCritical(Exception exception) =>
        exception is OutOfMemoryException
        or AccessViolationException
        or AppDomainUnloadedException
        or BadImageFormatException
        or CannotUnloadAppDomainException;
}
