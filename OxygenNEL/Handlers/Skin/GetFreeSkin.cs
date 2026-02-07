using System;
using System.Collections.Generic;
using System.Linq;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame.Skin;
using OxygenNEL.Manager;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Handlers.Skin;

public class GetFreeSkinResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<SkinItemData> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public bool NotLogin { get; set; }
}

public class SkinItemData
{
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
}

public class GetFreeSkin
{
    public GetFreeSkinResult Execute(int offset, int length = 20)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new GetFreeSkinResult { NotLogin = true };

        try
        {
            Log.Information("免费皮肤请求 offset={Offset} length={Length}", offset, length);

            Entities<EntitySkin> list;
            try
            {
                list = AppState.X19.GetFreeSkinList(last.UserId, last.AccessToken, offset, length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取皮肤列表失败: {Message}", ex.Message);
                return new GetFreeSkinResult { Success = false, Message = ex.Message };
            }

            var baseData = list.Data;
            var baseCount = baseData.Length;

            if (baseCount == 0)
                return new GetFreeSkinResult { Success = true, Items = new List<SkinItemData>(), HasMore = false };

            Entities<EntitySkin>? detailed = null;
            try
            {
                detailed = AppState.X19.GetSkinDetails(last.UserId, last.AccessToken, list);
            }
            catch (Exception ex)
            {
                if (AppState.Debug) Log.Error(ex, "皮肤详情获取失败，退回基础列表");
            }

            var data = detailed?.Data ?? baseData;
            Log.Information("皮肤详情数量={Count}", data.Length);

            var items = data.Select(s => new SkinItemData
            {
                EntityId = s.EntityId,
                Name = s.Name,
                PreviewUrl = s.TitleImageUrl
            }).ToList();

            var hasMore = baseCount >= length;
            Log.Information("皮肤返回条目数={Count} hasMore={HasMore}", items.Count, hasMore);

            return new GetFreeSkinResult { Success = true, Items = items, HasMore = hasMore };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取皮肤列表失败");
            return new GetFreeSkinResult { Success = false, Message = "获取失败" };
        }
    }
}