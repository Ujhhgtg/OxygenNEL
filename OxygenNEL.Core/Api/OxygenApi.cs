using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OxygenNEL.Core.Api.Entities;
using OxygenNEL.Core.Api.Entities.Auth;
using OxygenNEL.Core.Api.Entities.System;
using Serilog;

namespace OxygenNEL.Core.Api;


public class OxygenApi : IDisposable
{
	private static readonly Lazy<OxygenApi> _instance = new(() => new OxygenApi());

	private readonly HttpClient _http;

	private readonly SemaphoreSlim _gate = new(1, 1);

	private bool _disposed;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static OxygenApi Instance => _instance.Value;

	public static Func<HttpMessageHandler>? HandlerFactory { get; set; }

	public string BaseUrl { get; }

	public OxygenApi(string baseUrl = "")
	{
		BaseUrl = "https://api.fandmc.cn";
		_http = new HttpClient((HandlerFactory == null) ? new HttpClientHandler() : HandlerFactory())
		{
			BaseAddress = new Uri(BaseUrl, UriKind.Absolute),
			Timeout = TimeSpan.FromSeconds(30L)
		};
	}

	public async Task<ApiResponse> SendRegisterMailAsync(string email, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/register_mail", new { email }, ct);
	}

	public async Task<ApiResponse> VerifyCodeAsync(string email, string code, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/verify_code", new { email, code }, ct);
	}

	public async Task<LoginResponse> RegisterAsync(string email, string username, string password, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<LoginResponse>("/auth/register_next", new { email, username, password }, ct);
	}

	public async Task<LoginResponse> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<LoginResponse>("/auth/login", new
		{
			username = usernameOrEmail, password
		}, ct);
	}

	public async Task<TokenAuthResponse> TokenAuthAsync(string token, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<TokenAuthResponse>("/auth/token_auth", new { token }, ct);
	}

	public async Task<UserInfoResponse> GetUserInfoAsync(string token, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<UserInfoResponse>("/auth/me", new { token }, ct);
	}

	public async Task<DurationResponse> GetDurationAsync(string token, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<DurationResponse>("/auth/duration", new { token }, ct);
	}

	public async Task<ApiResponse> UpdateAvatarAsync(string token, string avatar, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/update_avatar", new { token, avatar }, ct);
	}

	public async Task<ApiResponse> ChangePasswordAsync(string token, string oldPassword, string newPassword, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/change_password", new { token, oldPassword, newPassword }, ct);
	}

	public async Task<ApiResponse> SendChangeEmailCodeAsync(string token, string email, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/send_change_email_code", new { token, email }, ct);
	}

	public async Task<ApiResponse> ChangeEmailAsync(string token, string newEmail, string code, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/change_email", new { token, newEmail, code }, ct);
	}

	public async Task<ApiResponse> SendResetPasswordCodeAsync(string email, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/send_reset_password_code", new { email }, ct);
	}

	public async Task<ApiResponse> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ApiResponse>("/auth/reset_password", new { email, code, newPassword }, ct);
	}

	public async Task<CardKeyActivateResponse> ActivateCardKeyAsync(string token, string cardKey, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<CardKeyActivateResponse>("/cardkey/activate", new { token, cardKey }, ct);
	}

	public async Task<UserUrlResponse> GenerateUserUrlAsync(string accountToken, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<UserUrlResponse>("/api/generate_user_url", new { accountToken }, ct);
	}

	public async Task<CrcSaltResponse> GetCrcSaltAsync(string token, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<CrcSaltResponse>("/api/get/crcsalt", new { token }, ct);
	}

	public async Task<Stream?> DownloadFileAsync(string filename, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			var obj = await _http.GetAsync("/download/" + filename, HttpCompletionOption.ResponseHeadersRead, ct);
			obj.EnsureSuccessStatusCode();
			return await obj.Content.ReadAsStreamAsync(ct);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "下载文件失败: {Filename}", filename);
			return null;
		}
	}

	public async Task<HttpResponseMessage?> DownloadFileWithRangeAsync(string filename, long? rangeStart = null, long? rangeEnd = null, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/download/" + filename);
			if (rangeStart.HasValue)
			{
				httpRequestMessage.Headers.Range = rangeEnd.HasValue ? new RangeHeaderValue(rangeStart.Value, rangeEnd.Value) : new RangeHeaderValue(rangeStart.Value, null);
			}
			var obj = await _http.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
			obj.EnsureSuccessStatusCode();
			return obj;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "下载文件失败: {Filename}", filename);
			return null;
		}
	}

	public async Task<List<PluginInfo>> GetPluginListAsync(CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			using var response = await _http.GetAsync("/get/pluginlist", ct);
			response.EnsureSuccessStatusCode();
			var json = await response.Content.ReadAsStringAsync(ct);
			try
			{
				return JsonSerializer.Deserialize<List<PluginInfo>>(json, JsonOptions) ?? new List<PluginInfo>();
			}
			catch
			{
				var list = JsonSerializer.Deserialize<PluginListResponse>(json, JsonOptions)?.Plugins ?? new List<PluginInfo>();
				if (list.Count == 0)
				{
					try
					{
						list = JsonSerializer.Deserialize<PluginListWithItems>(json, JsonOptions)?.Items ?? new List<PluginInfo>();
					}
					catch (Exception ex)
					{
						Log.Warning("解析items字段失败: {Message}", ex.Message);
					}
				}
				Log.Information("最终插件数量: {Count}", list.Count);
				return list;
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "获取插件列表失败");
			return new List<PluginInfo>();
		}
	}

	public async Task<AnnouncementResponse> GetAnnouncementAsync(CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			using var response = await _http.GetAsync("/get/announcement", ct);
			response.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<AnnouncementResponse>(await response.Content.ReadAsStringAsync(ct), JsonOptions) ?? new AnnouncementResponse
			{
				Success = false,
				Message = "解析失败"
			};
		}
		catch (Exception ex)
		{
			Log.Error(ex, "获取公告失败");
			return new AnnouncementResponse
			{
				Success = false,
				Message = ex.Message
			};
		}
	}

	public async Task<VersionResponse> GetLatestVersionAsync(CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			using var response = await _http.GetAsync("/get/lastversion", ct);
			response.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<VersionResponse>(await response.Content.ReadAsStringAsync(ct), JsonOptions) ?? new VersionResponse
			{
				Success = false,
				Message = "解析失败"
			};
		}
		catch (Exception ex)
		{
			Log.Error(ex, "获取最新版本失败");
			return new VersionResponse
			{
				Success = false,
				Message = ex.Message
			};
		}
	}

	public async Task<ServerStatusResponse> GetServerStatusAsync(string key, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<ServerStatusResponse>("/get/status", new { key }, ct);
	}

	public async Task<string?> RecognizeCaptchaAsync(string base64Image, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			return (await PostAsync<CaptchaResponse>("/v9/captcha", new
			{
				base64 = base64Image
			}, ct)).Result;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "验证码识别失败");
			return null;
		}
	}

	public async Task<string?> RecognizeCaptchaSyncAsync(string base64Image, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			return (await PostAsync<CaptchaResponse>("/v9/captcha/sync", new
			{
				base64 = base64Image
			}, ct)).Result;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "验证码识别失败");
			return null;
		}
	}

	public async Task<SmResultResponse> Send4399SmAsync(string username, string password, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			return await PostAsync<SmResultResponse>("/4399/sm", new { username, password }, ct);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "4399自动实名认证请求失败");
			return new SmResultResponse
			{
				Success = false,
				Message = ex.Message,
				Code = 0
			};
		}
	}

	public async Task<UpdateRankResponse> UpdateUserRankAsync(string key, string token, string rank, CancellationToken ct = default(CancellationToken))
	{
		return await PostAsync<UpdateRankResponse>("/internal/update_rank", new { key, token, rank }, ct);
	}

	private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct) where T : class, new()
	{
		await _gate.WaitAsync(ct);
		try
		{
			using var resp = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
			var text = await resp.Content.ReadAsStringAsync(ct);
			try
			{
				var val = JsonSerializer.Deserialize<T>(text, JsonOptions);
				if (val != null)
				{
					return val;
				}
			}
			catch (JsonException)
			{
			}
			if (!resp.IsSuccessStatusCode)
			{
				Log.Error("网络请求失败: {Path}, StatusCode: {StatusCode}, Response: {Response}", path, resp.StatusCode, text);
				var val2 = new T();
				if (val2 is ApiResponse apiResponse)
				{
					apiResponse.Success = false;
					apiResponse.Message = $"请求失败 ({resp.StatusCode})";
				}
				return val2;
			}
			Log.Error("解析响应失败: {Path}, JSON: {Json}", path, text);
			var val3 = new T();
			if (val3 is ApiResponse apiResponse2)
			{
				apiResponse2.Success = false;
				apiResponse2.Message = "响应解析失败";
			}
			return val3;
		}
		catch (OperationCanceledException)
		{
			Log.Warning("请求被取消: {Path}", path);
			var val4 = new T();
			if (val4 is ApiResponse apiResponse3)
			{
				apiResponse3.Success = false;
				apiResponse3.Message = "请求被取消";
			}
			return val4;
		}
		catch (Exception ex3)
		{
			Log.Error(ex3, "未知错误: {Path}", path);
			var val5 = new T();
			if (val5 is ApiResponse apiResponse4)
			{
				apiResponse4.Success = false;
				apiResponse4.Message = "请求失败: " + ex3.Message;
			}
			return val5;
		}
		finally
		{
			_gate.Release();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_http.Dispose();
			_gate.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}