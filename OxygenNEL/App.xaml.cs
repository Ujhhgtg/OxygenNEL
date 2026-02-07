using System;
using System.IO;
using System.Threading.Tasks;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using Microsoft.UI.Xaml;
using OxygenNEL.IRC;
using OxygenNEL.Manager;
using OxygenNEL.type;
using OxygenNEL.Utils;
using Serilog;
using Serilog.Events;
using FileUtil = OxygenNEL.Utils.FileUtil;
using LFileUtil = Codexus.Game.Launcher.Utils.FileUtil;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace OxygenNEL;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindow { get; private set; }
    public static Task? InitializationTask { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ConfigureLogger();
        _window = new MainWindow();
        MainWindow = _window;
        _window.Activate();
        InitializationTask = Task.Run(async () =>
        {
            try
            {
                LFileUtil.CreateDirectorySafe(PathUtil.CustomModsPath);
                AppState.Debug = SettingManager.Instance.Get()?.Debug ?? false;
                AppState.AutoDisconnectOnBan = SettingManager.Instance.Get().AutoDisconnectOnBan;

                // KillVeta.Run();
                AppState.Services = await CreateServicesAsync();
                InternalQuery.Initialize();

                await InitializeSystemComponentsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "应用初始化失败");
            }
        });
    }

    private void ConfigureLogger()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            var logDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logDir);
            var fileName = DateTime.Now.ToString("yyyy-MM-dd-HHmm-ss") + ".log";
            var filePath = Path.Combine(logDir, fileName);
            var isDebug = SettingManager.Instance.Get().Debug;
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(isDebug ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.Sink(UiLog.CreateSink())
                .WriteTo.File(filePath);
            Log.Logger = logConfig.CreateLogger();
            Log.Information("日志已创建: {filePath}, Debug={isDebug}", filePath, isDebug);
        }
        catch (Exception ex)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.Sink(UiLog.CreateSink())
                .CreateLogger();
            Log.Error(ex, "日志初始化失败");
        }
    }

    private static async Task InitializeSystemComponentsAsync()
    {
        var pluginDir = FileUtil.GetPluginDirectory();
        Directory.CreateDirectory(pluginDir);
        UserManager.Instance.ReadUsersFromDisk();
        Interceptor.EnsureLoaded();
        PacketManager.Instance.RegisterPacketFromAssembly(typeof(App).Assembly);
        PacketManager.Instance.RegisterPacketFromAssembly(typeof(IrcManager).Assembly);
        PacketManager.Instance.EnsureRegistered();
        RegisterIrcHandler();
        try
        {
            PluginManager.Instance.EnsureUninstall();
            PluginManager.Instance.LoadPlugins(pluginDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "插件加载失败");
        }

        await Task.CompletedTask;
    }

    private static void RegisterIrcHandler()
    {
        IrcManager.Enabled = SettingManager.Instance.Get().IrcEnabled;
        IrcEventHandler.Register(() => AuthManager.Instance.Token ?? "");
    }

    private async Task<Services> CreateServicesAsync()
    {
        var yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = WPFLauncher.GetLatestVersionAsync().GetAwaiter().GetResult(),
            Channel = "netease",
            CrcSalt = await AuthManager.Instance.GetCrcSaltAsyncIfNeeded()
        });
        return new Services(yggdrasil);
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "未处理异常");
        }
        catch
        {
        }

        e.Handled = true;
    }
}