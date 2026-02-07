using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Component;
using OxygenNEL.Core.Api;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Utils.Update;

public class UpdateManager
{
    private static async Task<T> RetryHttpRequest<T>(Func<Task<T>> httpRequestFunc, string requestType, int maxRetries = 3)
    {
        var retryDelayMs = 2000;
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await httpRequestFunc();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                Log.Warning(ex, "{RequestType} 请求失败，第 {Attempt} 次尝试，将在 {RetryDelayMs}ms 后重试", requestType, attempt + 1, retryDelayMs);
                await Task.Delay(retryDelayMs);
                
                retryDelayMs *= 2;
            }
        }
        
        throw new InvalidOperationException($"{requestType} 请求在多次重试后仍然失败");
    }
    
    private static async Task ExecuteWithRetry(Func<Task> operation, string operationType, int maxRetries = 3)
    {
        var retryDelayMs = 2000;
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation();
                return; 
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                Log.Warning(ex, "{OperationType} 操作失败，第 {Attempt} 次尝试，将在 {RetryDelayMs}ms 后重试", operationType, attempt + 1, retryDelayMs);
                await Task.Delay(retryDelayMs);
                
                retryDelayMs *= 2;
            }
        }
        
        throw new InvalidOperationException($"{operationType} 操作在多次重试后仍然失败");
    }
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        MaxConnectionsPerServer = 16
    })
    {
        Timeout = TimeSpan.FromMinutes(1)
    };
    
    public static async Task CheckForUpdatesAsync(Window window)
    {
        await Task.Delay(1000);
        
        try
        {
            var result = await OxygenApi.Instance.GetLatestVersionAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Version))
            {
                if (!string.Equals(result.Version, AppInfo.AppVersion, StringComparison.OrdinalIgnoreCase))
                {
                    var dialog = new ThemedContentDialog
                    {
                        // Title = "检测到新版本",
                        // Content = $"检测到新版本 {result.Version}\n是否更新？",
                        Title = "更新已禁用",
                        Content = "你按哪个都没用",
                        PrimaryButtonText = "确定",
                        CloseButtonText = "取消",
                        XamlRoot = window.Content.XamlRoot
                    };

                    // var dialogResult = await dialog.ShowAsync();
                    // if (dialogResult != ContentDialogResult.Primary) return;
                    await dialog.ShowAsync();
                    
                    // await DownloadAndApplyUpdateAsync(result.DownloadUrl!, window);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "检查版本失败");
        }
    }

    private static async Task DownloadAndApplyUpdateAsync(string downloadUrl, Window window)
    {
        var progressDialog = new ThemedContentDialog
        {
            Title = "更新中",
            XamlRoot = window.Content.XamlRoot
        };

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Width = 300,
            IsIndeterminate = false
        };
        var statusText = new TextBlock { Text = "正在下载..." };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(statusText);
        panel.Children.Add(progressBar);
        progressDialog.Content = panel;

        _ = progressDialog.ShowAsync();

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            var exeDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            var exeName = Path.GetFileName(exePath) ?? "OxygenNEL.exe";
            var updateDir = Path.Combine(exeDir, "update");
            Directory.CreateDirectory(updateDir);
            var newExePath = Path.Combine(updateDir, exeName);

            var headResponse = await RetryHttpRequest(async () =>
            {
                var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadUrl));
                response.EnsureSuccessStatusCode();
                return response;
            }, "HEAD");
            
            var totalBytes = headResponse.Content.Headers.ContentLength ?? -1;
            
            if (totalBytes <= 0)
            {
                throw new InvalidOperationException("无法获取文件大小");
            }

            var acceptRanges = headResponse.Headers.GetValues("Accept-Ranges").FirstOrDefault();
                        
            var tempFilesToClean = new List<string>();
                        
            try
            {
                if (acceptRanges != "bytes")
                {
                    await ExecuteWithRetry(async () =>
                    {
                        using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();
                                    
                        var buffer = new byte[64 * 1024];
                        long downloadedBytes = 0;
                                                
                        await using var contentStream = await response.Content.ReadAsStreamAsync();
                        await using var fileStream = new FileStream(newExePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                                
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            downloadedBytes += bytesRead;
                                                    
                            var percent = (double)downloadedBytes / totalBytes * 100;
                            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                            {
                                progressBar.Value = percent;
                                statusText.Text = $"正在下载... {percent:F1}%";
                            });
                        }
                    }, "GET");
                }
                else
                {
                    const int maxThreadCount = 8; 
                    var threadCount = Math.Min(maxThreadCount, Environment.ProcessorCount);
                    var minChunkSize = 1024 * 1024;
                    var calculatedChunkSize = totalBytes / threadCount;
                                    
                    if (calculatedChunkSize < minChunkSize)
                    {
                        threadCount = Math.Max(1, (int)(totalBytes / minChunkSize));
                    }
                                    
                    var chunkSize = totalBytes / threadCount;
                    var tasks = new Task[threadCount];
                    var tempFiles = new string[threadCount];
                    var downloadedBytesArray = new long[threadCount];
            
                    for (var i = 0; i < threadCount; i++)
                    {
                        var start = i * chunkSize;
                        var end = i == threadCount - 1 ? totalBytes - 1 : (i + 1) * chunkSize - 1;
                        var range = $"bytes={start}-{end}";
                        tempFiles[i] = Path.GetTempFileName();
                        tempFilesToClean.Add(tempFiles[i]);
                                    
                        tasks[i] = DownloadChunkAsync(downloadUrl, range, tempFiles[i], start, end, totalBytes, progressBar, statusText, downloadedBytesArray, i);
                    }
            
                    await Task.WhenAll(tasks);
            
                    await ExecuteWithRetry(async () =>
                    {
                        await MergeChunksAsync(tempFiles, newExePath);
                    }, "合并分块");
                }
            }
            finally
            {
                foreach (var tempFile in tempFilesToClean)
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }

            DispatcherQueue.GetForCurrentThread().TryEnqueue(() => statusText.Text = "下载完成，正在准备更新...");

            var batPath = Path.Combine(exeDir, "update.bat");
            var batContent = $@"@echo off
chcp 65001 >nul
echo 正在更新，请稍候...
ping 127.0.0.1 -n 2 >nul
copy /Y ""{newExePath}"" ""{exePath}""
if %errorlevel% equ 0 (
    echo 更新成功，正在启动...
    start """" ""{exePath}""
    rd /s /q ""{updateDir}""
) else (
    echo 更新失败，请手动替换
    pause
)
del ""%~f0""
";
            await File.WriteAllTextAsync(batPath, batContent);

            progressDialog.Hide();

            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                CreateNoWindow = false
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载更新失败");
            progressDialog.Hide();
            NotificationHost.ShowGlobal($"下载更新失败: {ex.Message}", ToastLevel.Error);
        }
    }

    private static async Task DownloadChunkAsync(string downloadUrl, string range, string tempFile, long start, long end, long totalBytes, ProgressBar progressBar, TextBlock statusText, long[] downloadedBytesArray, int threadIndex)
    {
        var maxRetries = 3;
        var retryDelayMs = 2000;
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Range = new RangeHeaderValue(start, end);
                
                using var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var buffer = new byte[64 * 1024];
                long chunkDownloadedBytes = 0;
                
                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    chunkDownloadedBytes += bytesRead;
                    
                    Volatile.Write(ref downloadedBytesArray[threadIndex], chunkDownloadedBytes);
                    
                    var totalDownloaded = 0L;
                    for (var i = 0; i < downloadedBytesArray.Length; i++)
                    {
                        totalDownloaded += Volatile.Read(ref downloadedBytesArray[i]);
                    }
                    
                    var percent = (double)totalDownloaded / totalBytes * 100;
                    DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        progressBar.Value = percent;
                        statusText.Text = $"正在下载... {percent:F1}%";
                    });
                }
                
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                Log.Warning(ex, "分块下载失败，第 {Attempt} 次尝试，将在 {RetryDelayMs}ms 后重试。范围: {Range}", attempt + 1, retryDelayMs, range);
                
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
                
                await Task.Delay(retryDelayMs);
                
                retryDelayMs *= 2;
            }
        }
    }

    private static async Task MergeChunksAsync(string[] tempFiles, string outputPath)
    {
        await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        foreach (var tempFile in tempFiles)
        {
            if (!File.Exists(tempFile))
            {
                throw new FileNotFoundException($"分块文件不存在: {tempFile}");
            }
            
            await using var inputStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            await inputStream.CopyToAsync(outputStream);
        }
    }
}