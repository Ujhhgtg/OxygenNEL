using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Manager;

public class RecentPlayManager
{
    private const string CacheFolder = "cache";
    private const string ImageFolder = "cache/images";
    private const string RecentPlayFile = "recent_play.json";
    private const int MaxRecentItems = 10;
    private static readonly string FilePath = Path.Combine(CacheFolder, RecentPlayFile);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Lazy<RecentPlayManager> _lazy = new(() => new RecentPlayManager());
    public static RecentPlayManager Instance => _lazy.Value;

    private List<RecentPlayItem> _recentItems = new();
    private static readonly HttpClient _httpClient = new();

    private RecentPlayManager()
    {
        EnsureCacheFolder();
        Load();
    }

    private static void EnsureCacheFolder()
    {
        if (!Directory.Exists(CacheFolder))
            Directory.CreateDirectory(CacheFolder);
        if (!Directory.Exists(ImageFolder))
            Directory.CreateDirectory(ImageFolder);
    }

    public List<RecentPlayItem> GetRecentItems() => _recentItems.ToList();

    public void AddOrUpdate(string serverId, string serverName, string serverType, string? imageUrl = null)
    {
        var existing = _recentItems.FirstOrDefault(x => x.ServerId == serverId && x.ServerType == serverType);
        if (existing != null)
        {
            existing.ServerName = serverName;
            existing.LastPlayTime = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(imageUrl))
                _ = CacheImageAsync(serverId, imageUrl, existing);
        }
        else
        {
            var item = new RecentPlayItem
            {
                ServerId = serverId,
                ServerName = serverName,
                ServerType = serverType,
                LastPlayTime = DateTime.Now
            };
            _recentItems.Insert(0, item);
            if (!string.IsNullOrWhiteSpace(imageUrl))
                _ = CacheImageAsync(serverId, imageUrl, item);
        }

        _recentItems = _recentItems.OrderByDescending(x => x.LastPlayTime).Take(MaxRecentItems).ToList();
        Save();
    }

    private async Task CacheImageAsync(string serverId, string imageUrl, RecentPlayItem item)
    {
        try
        {
            var ext = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            var fileName = $"{serverId}{ext}";
            var localPath = Path.Combine(ImageFolder, fileName);

            var bytes = await _httpClient.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(localPath, bytes);
            item.ImagePath = Path.GetFullPath(localPath);
            Save();
            Log.Debug("已缓存服务器图片: {Path}", localPath);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "缓存图片失败: {Url}", imageUrl);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var json = File.ReadAllText(FilePath);
            _recentItems = JsonSerializer.Deserialize<List<RecentPlayItem>>(json) ?? new();
            Log.Information("已加载 {Count} 条最近游玩记录", _recentItems.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载最近游玩记录失败");
            _recentItems = new();
        }
    }

    private void Save()
    {
        try
        {
            EnsureCacheFolder();
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_recentItems, JsonOptions));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存最近游玩记录失败");
        }
    }
}
