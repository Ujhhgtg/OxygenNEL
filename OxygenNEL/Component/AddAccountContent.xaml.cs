using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Handlers.Login;
using OxygenNEL.Manager;

namespace OxygenNEL.Component;

public sealed partial class AddAccountContent : UserControl
{
#pragma warning disable CS0067
    public event Action? AutoLoginSucceeded;
    public event Action? ConfirmRequested;
    public event Action? CancelRequested;
    public event Action<string, string, string, string>? CaptchaRequired;
#pragma warning restore CS0067
    public Func<string, Task<string>>? CaptchaInputRequested;
#pragma warning disable CS0649
#pragma warning restore CS0649
    public AddAccountContent()
    {
        InitializeComponent();
        var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
        var t = ElementTheme.Default;
        if (mode == "light") t = ElementTheme.Light;
        else if (mode == "dark") t = ElementTheme.Dark;
        RequestedTheme = t;
    }

    public string? SelectedType => (AccountTypePivot.SelectedItem as PivotItem)?.Header?.ToString();

    public string CookieText => CookieInput.Text;

    public string Pc4399User => Pc4399Username.Text;
    public string Pc4399Pass => Pc4399Password.Password;
    public string? Pc4399SessionId { get; }

    public string? Pc4399CaptchaUrl { get; }

    public string Pc4399Captcha => Pc4399CaptchaInput?.Text ?? string.Empty;

    public string NeteaseMail => NeteaseEmail.Text;
    public string NeteasePass => NeteasePassword.Password;

    public bool TryDetectSuccess(object result)
    {
        if (result == null) return false;
        var tProp = result.GetType().GetProperty("type");
        if (tProp != null)
        {
            var tVal = tProp.GetValue(result) as string;
            if (string.Equals(tVal, "login_error", StringComparison.OrdinalIgnoreCase))
            {
                var mProp = result.GetType().GetProperty("message");
                var msg = mProp?.GetValue(result) as string ?? "登录失败";
                NotificationHost.ShowGlobal(msg, ToastLevel.Error);
                return false;
            }

            if (string.Equals(tVal, "login_4399_error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tVal, "login_x19_error", StringComparison.OrdinalIgnoreCase))
            {
                var mProp = result.GetType().GetProperty("message");
                var msg = mProp?.GetValue(result) as string ?? "登录失败";
                NotificationHost.ShowGlobal(msg, ToastLevel.Error);
                return false;
            }
        }

        if (result is IEnumerable en)
            foreach (var item in en)
            {
                var p = item?.GetType().GetProperty("type");
                var v = p != null ? p.GetValue(item) as string : null;
                if (string.Equals(v, "Success_login", StringComparison.OrdinalIgnoreCase)) return true;
            }

        if (GetAccount.HasAuthorizedUser()) return true;
        return false;
    }
}