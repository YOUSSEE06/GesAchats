using System.Collections.Generic;
using GesAchats.Core.Security;

namespace GesAchats.Core.Security;

public class PermissionBuilder
{
    private readonly Dictionary<string, List<ModulePermission>> _rolePermissions = new();
    private string? _currentRole;

    public PermissionBuilder ForRole(string roleName)
    {
        _currentRole = roleName;
        if (!_rolePermissions.ContainsKey(roleName))
        {
            _rolePermissions[roleName] = new List<ModulePermission>();
        }
        return this;
    }

    public PermissionBuilder CanRead(params AccessModule[] modules)
    {
        if (_currentRole == null) throw new InvalidOperationException("Call ForRole first.");
        foreach (var module in modules)
        {
            _rolePermissions[_currentRole].Add(new ModulePermission { Module = module, Level = AccessLevel.Read });
        }
        return this;
    }

    public PermissionBuilder CanWrite(params AccessModule[] modules)
    {
        if (_currentRole == null) throw new InvalidOperationException("Call ForRole first.");
        foreach (var module in modules)
        {
            _rolePermissions[_currentRole].Add(new ModulePermission { Module = module, Level = AccessLevel.Write });
        }
        return this;
    }

    public PermissionBuilder CanManage(params AccessModule[] modules)
    {
        if (_currentRole == null) throw new InvalidOperationException("Call ForRole first.");
        foreach (var module in modules)
        {
            _rolePermissions[_currentRole].Add(new ModulePermission { Module = module, Level = AccessLevel.Admin });
        }
        return this;
    }

    public Dictionary<string, List<ModulePermission>> Build() => _rolePermissions;
}
