using System.Security;
using Tizen.Applications;
using Tizen.Security;

namespace Avalonia.Tizen.Platform;
internal class Permissions
{
    public record Privilege (string Path, bool IsRuntime);

    public static readonly Privilege InternetPrivilege = new("http://tizen.org/privilege/internet", false);
    public static readonly Privilege NetworkPrivilege = new("http://tizen.org/privilege/network.get", false);
    public static readonly Privilege CameraPrivilege = new("http://tizen.org/privilege/camera", false);
    public static readonly Privilege ContactReadPrivilege = new("http://tizen.org/privilege/contact.read", true);
    public static readonly Privilege ContactWritePrivilege = new("http://tizen.org/privilege/contact.write", true);
    public static readonly Privilege LedPrivilege = new("http://tizen.org/privilege/led", false);
    public static readonly Privilege AppManagerLaunchPrivilege = new("http://tizen.org/privilege/appmanager.launch", false);
    public static readonly Privilege LocationPrivilege = new("http://tizen.org/privilege/location", true);
    public static readonly Privilege MapServicePrivilege = new("http://tizen.org/privilege/mapservice", false);
    public static readonly Privilege MediaStoragePrivilege = new("http://tizen.org/privilege/mediastorage", true);
    public static readonly Privilege RecorderPrivilege = new("http://tizen.org/privilege/recorder", false);
    public static readonly Privilege HapticPrivilege = new("http://tizen.org/privilege/haptic", false);
    public static readonly Privilege LaunchPrivilege = new("http://tizen.org/privilege/appmanager.launch", false);

    public static readonly Privilege[] NetworkPrivileges = { InternetPrivilege, NetworkPrivilege };
    public static readonly Privilege[] MapsPrivileges = { InternetPrivilege, MapServicePrivilege, NetworkPrivilege };

    public static Package CurrentPackage
    {
        get
        {
            var packageId = global::Tizen.Applications.Application.Current.ApplicationInfo.PackageId;
            return PackageManager.GetPackage(packageId);
        }
    }

    public static bool IsPrivilegeDeclared(string? tizenPrivilege)
    {
        var tizenPrivileges = tizenPrivilege;

        if (tizenPrivileges == null || !tizenPrivileges.Any())
            return false;

        var package = CurrentPackage;

        if (!package.Privileges.Contains(tizenPrivilege))
            return false;

        return true;
    }

    public static void EnsureDeclared(params Privilege[]? requiredPrivileges)
    {
        if (requiredPrivileges?.Any() != true)
            return;

        foreach (var (tizenPrivilege, _) in requiredPrivileges)
        {
            if (!IsPrivilegeDeclared(tizenPrivilege))
                throw new SecurityException($"You need to declare the privilege: `{tizenPrivilege}` in your tizen-manifest.xml");
        }
    }

    public static Task<bool> CheckPrivilegeAsync(params Privilege[]? requiredPrivileges) => CheckPrivilegeAsync(requiredPrivileges, false);
    public static Task<bool> RequestPrivilegeAsync(params Privilege[]? requiredPrivileges) => CheckPrivilegeAsync(requiredPrivileges, true);
    private static async Task<bool> CheckPrivilegeAsync(Privilege[]? requiredPrivileges, bool ask)
    {
        var ret = global::Tizen.System.Information.TryGetValue("http://tizen.org/feature/profile", out string profile);
        if (!ret || (ret && (!profile.Equals("mobile") || !profile.Equals("wearable"))))
        {
            return true;
        }

        if (requiredPrivileges == null || !requiredPrivileges.Any())
            return true;

        EnsureDeclared();

        var tizenPrivileges = requiredPrivileges.Where(p => p.IsRuntime);

        foreach (var (tizenPrivilege, _) in tizenPrivileges)
        {
            var checkResult = PrivacyPrivilegeManager.CheckPermission(tizenPrivilege);
            if (checkResult == CheckResult.Ask)
            {
                if (ask)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    PrivacyPrivilegeManager.GetResponseContext(tizenPrivilege)
                        .TryGetTarget(out var context);

                    void OnResponseFetched(object? sender, RequestResponseEventArgs e)
                    {
                        tcs.TrySetResult(e.result == RequestResult.AllowForever);
                    }

                    if (context != null)
                    {
                        context.ResponseFetched += OnResponseFetched;
                        PrivacyPrivilegeManager.RequestPermission(tizenPrivilege);
                        var result = await tcs.Task;
                        context.ResponseFetched -= OnResponseFetched;
                        if (result)
                            continue;
                    }
                }
                return false;
            }
            else if (checkResult == CheckResult.Deny)
            {
                return false;
            }
        }
        return true;
    }
}
