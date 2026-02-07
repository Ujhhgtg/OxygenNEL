using System.Collections.Generic;
using System.Linq;
using OxygenNEL.Entities.Web.NEL;
using OxygenNEL.Manager;

namespace OxygenNEL.Handlers.Game;

public class QueryGameSession
{
    public object Execute()
    {
        List<EntityQueryGameSessions> list = (from interceptor in GameManager.Instance.GetQueryInterceptors()
            select new EntityQueryGameSessions
            {
                Id = "interceptor-" + interceptor.Id,
                ServerName = interceptor.Server,
                Guid = interceptor.Name.ToString(),
                CharacterName = interceptor.Role,
                ServerVersion = interceptor.Version,
                StatusText = "Running",
                ProgressValue = 0,
                Type = "Interceptor",
                LocalAddress = interceptor.LocalAddress
            }).ToList();
        return new { type = "query_game_session", items = list };
    }
}
