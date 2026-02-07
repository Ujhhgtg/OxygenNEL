using System;
using System.Collections;
using System.Collections.Generic;
using OxygenNEL.Entities.Web.NetGame;
using OxygenNEL.Manager;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Handlers.Game.NetServer;

public class GetServersDetail
{
    public ServerDetailResult Execute(string gameId)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new ServerDetailResult { NotLogin = true };
        if (string.IsNullOrWhiteSpace(gameId)) return new ServerDetailResult { Success = false, Message = "参数错误" };
        try
        {
            var detail = AppState.X19.QueryNetGameDetailById(last.UserId, last.AccessToken, gameId);
            var dataProp = detail?.GetType().GetProperty("Data");
            var dataVal = dataProp != null ? dataProp.GetValue(detail) : null;
            var imgs = new List<string>();
            if (dataVal != null)
            {
                var upProp = dataVal.GetType().GetProperty("BriefImageUrls");
                var lowProp = dataVal.GetType().GetProperty("brief_image_urls");
                var arr = upProp != null ? upProp.GetValue(dataVal) as IEnumerable : null;
                if (arr == null && lowProp != null) arr = lowProp.GetValue(dataVal) as IEnumerable;
                if (arr != null)
                {
                    foreach (var it in arr)
                    {
                        var s = it != null ? it.ToString() : string.Empty;
                        if (!string.IsNullOrWhiteSpace(s)) imgs.Add(s.Replace("`", string.Empty).Trim());
                    }
                }
            }
            return new ServerDetailResult { Success = true, Images = imgs };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取服务器详情失败: {GameId}", gameId);
            return new ServerDetailResult { Success = false, Message = "获取失败" };
        }
    }
}