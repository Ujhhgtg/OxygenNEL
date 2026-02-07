using Codexus.Cipher.Protocol;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using OxygenNEL.Manager;

namespace OxygenNEL.type;

internal class Services(StandardYggdrasil yggdrasil)
{ 
    public StandardYggdrasil Yggdrasil { get; private set; } = yggdrasil;

    public void RefreshYggdrasil()
    {
        var salt = AuthManager.Instance.CachedSalt;
        Yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = WPFLauncher.GetLatestVersionAsync().GetAwaiter().GetResult(),
            Channel = "netease",
            CrcSalt = salt
        });
    }
}