using Codexus.Cipher.Protocol;

namespace OxygenNEL.type;

internal static class AppState
{
    private static WPFLauncher? _x19;

    public static WPFLauncher X19
    {
        get
        {
            if (_x19 == null) _x19 = new WPFLauncher();
            return _x19;
        }
    }

    public static void ResetX19()
    {
        _x19?.Dispose();
        _x19 = new WPFLauncher();
    }

    public static Services? Services;
    public static bool Debug;
    public static string AutoDisconnectOnBan = string.Empty;
    public static bool IrcEnabled = true;
}