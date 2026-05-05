// Project comment: QuotaReadOnlyBehavior.cs blocks write commands while quota areas are read-only.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using MediatR;

namespace IT_Form.Application.Common.Behaviors;

public sealed class QuotaReadOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IQuotaControl _quotaControl;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IMaintenanceModeService? _maintenanceModeService;

    public QuotaReadOnlyBehavior(
        IQuotaControl quotaControl,
        ICurrentUserAccessor currentUserAccessor,
        IMaintenanceModeService? maintenanceModeService = null)
    {
        _quotaControl = quotaControl;
        _currentUserAccessor = currentUserAccessor;
        _maintenanceModeService = maintenanceModeService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var quotaKeys = GetWriteQuotaKeys(requestType);
        if (IsWriteCommand(requestType) && !IsMaintenanceExcluded(requestType) && _maintenanceModeService != null)
        {
            await _maintenanceModeService.EnsureWritableAsync(_currentUserAccessor.User, cancellationToken);
        }

        foreach (var quotaKey in quotaKeys)
        {
            await _quotaControl.EnsureWritableAsync(quotaKey, _currentUserAccessor.User, cancellationToken);
        }

        return await next();
    }

    private static IReadOnlyCollection<string> GetWriteQuotaKeys(Type requestType)
    {
        var fullName = requestType.FullName ?? requestType.Name;
        var name = requestType.Name;

        if (!IsWriteCommand(requestType))
        {
            return [];
        }

        if (fullName.Contains(".UserManagement.", StringComparison.Ordinal))
        {
            return [QuotaControl.Users];
        }

        if (fullName.Contains(".ITAsset.", StringComparison.Ordinal))
        {
            return [QuotaControl.Assets];
        }

        if (fullName.Contains(".Company.", StringComparison.Ordinal))
        {
            return [QuotaControl.Companies];
        }

        if (fullName.Contains(".Departments.", StringComparison.Ordinal) ||
            fullName.Contains(".Department.ImportDepartments.", StringComparison.Ordinal))
        {
            return [QuotaControl.Departments];
        }

        if (fullName.Contains(".EmailGroups.", StringComparison.Ordinal) ||
            fullName.Contains(".EmailGroup.ImportEmailGroups.", StringComparison.Ordinal) ||
            fullName.Contains(".EmailGroup.AddCurrentEmployeeToEmailGroup.", StringComparison.Ordinal))
        {
            return [QuotaControl.EmailGroups];
        }

        if (fullName.Contains(".SharedMailboxes.", StringComparison.Ordinal) ||
            fullName.Contains(".SharedMailbox.ImportSharedMailboxes.", StringComparison.Ordinal) ||
            fullName.Contains(".SharedMailbox.AddCurrentEmployeeToSharedMailbox.", StringComparison.Ordinal))
        {
            return [QuotaControl.SharedMailboxes];
        }

        if (fullName.Contains(".ServerFolders.", StringComparison.Ordinal) ||
            fullName.Contains(".ServerFolder.BulkImportServerFolders.", StringComparison.Ordinal) ||
            fullName.Contains(".ServerFolder.AddCurrentEmployeeToServerFolder.", StringComparison.Ordinal))
        {
            return [QuotaControl.ServerFolders];
        }

        if (fullName.Contains(".StandardSoftware.", StringComparison.Ordinal))
        {
            if (fullName.Contains(".Writes.CreateStandardSoftware.", StringComparison.Ordinal) ||
                fullName.Contains(".Writes.UpdateStandardSoftware.", StringComparison.Ordinal) ||
                fullName.Contains(".Writes.DeleteStandardSoftware.", StringComparison.Ordinal) ||
                fullName.Contains(".ImportStandardSoftware.", StringComparison.Ordinal))
            {
                return [QuotaControl.Software];
            }

            return [QuotaControl.SoftwareVersions];
        }

        return [];
    }

    private static bool IsWriteCommand(Type requestType)
        => requestType.Name.EndsWith("Command", StringComparison.Ordinal);

    private static bool IsMaintenanceExcluded(Type requestType)
    {
        var fullName = requestType.FullName ?? requestType.Name;
        return fullName.Contains(".Account.", StringComparison.Ordinal);
    }
}
