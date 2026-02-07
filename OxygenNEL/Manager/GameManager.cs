using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Interceptors;
using OxygenNEL.Entities.Web.NEL;

namespace OxygenNEL.Manager;

internal class GameManager
{
    private readonly Lock _lock = new();
    private static readonly Dictionary<Guid, LauncherService> Launchers = new();
    private static readonly Dictionary<Guid, Interceptor> Interceptors = new();
    private static readonly object Lock = new();
    public static GameManager Instance { get; } = new();

    public sealed class LockScope : IDisposable
    {
        private readonly object l;

        public LockScope(object o)
        {
            l = o;
            Monitor.Enter(l);
        }

        public void Dispose()
        {
            Monitor.Exit(l);
        }
    }

    public static LockScope EnterScope(object o)
    {
        return new LockScope(o);
    }

    public List<EntityQueryInterceptors> GetQueryInterceptors()
    {
        return Interceptors.Values.Select((interceptor, index) => new EntityQueryInterceptors
        {
            Id = index.ToString(),
            Name = interceptor.Identifier,
            Address = $"{interceptor.ForwardAddress}:{interceptor.ForwardPort}",
            Role = interceptor.NickName,
            Server = interceptor.ServerName,
            Version = interceptor.ServerVersion,
            LocalAddress = $"{interceptor.LocalAddress}:{interceptor.LocalPort}"
        }).ToList();
    }

    public void ShutdownInterceptor(Guid identifier)
    {
        Interceptor? value = null;
        var has = false;
        using (EnterScope(Lock))
        {
            if (Interceptors.TryGetValue(identifier, out value))
            {
                Interceptors.Remove(identifier);
                has = true;
            }
        }

        if (has && value != null) value.ShutdownAsync();
    }

    public void AddInterceptor(Interceptor interceptor)
    {
        using (_lock.EnterScope())
        {
            Interceptors.Add(interceptor.Identifier, interceptor);
        }
    }

    public Interceptor? GetInterceptor(Guid identifier)
    {
        using (EnterScope(Lock))
        {
            return Interceptors.TryGetValue(identifier, out var interceptor) ? interceptor : null;
        }
    }

    public void AddLauncher(LauncherService launcher)
    {
        using (_lock.EnterScope())
        {
            Launchers.Add(launcher.Identifier, launcher);
        }
    }
}