using System;
using System.Collections.Generic;
using System.Linq;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using OxygenNEL.Entities.Web.NetGame;
using OxygenNEL.Manager;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Handlers.Game.NetServer;

public class SearchServers
{
    public ListServersResult Execute(string keyword, int offset, int pageSize)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new ListServersResult { NotLogin = true };
        try
        {
            var all = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, 0, 500);
            var data = all.Data?.ToList() ?? new List<EntityNetGameItem>();
            var q = string.IsNullOrWhiteSpace(keyword)
                ? data.ToList()
                : data.Where(s => (s.Name ?? string.Empty).IndexOf(keyword!, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            var pageItems = q.Skip(offset).Take(pageSize).Select(s => new ServerItem
                { EntityId = s.EntityId, Name = s.Name, OnlineCount = s.OnlineCount ?? string.Empty }).ToList();
            var hasMore = offset + pageSize < q.Count;
            return new ListServersResult { Success = true, Items = pageItems, HasMore = hasMore };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new ListServersResult { Success = false, Message = "获取失败" };
        }
    }
}