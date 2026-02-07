using Codexus.Development.SDK.Manager;

namespace OxygenNEL.Handlers.Plugin;

public class RestartGateway
{
    public object Execute()
    {
        PluginManager.RestartGateway();
        return new { type = "restart_ack" };
    }
}