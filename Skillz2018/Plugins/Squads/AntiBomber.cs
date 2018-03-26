using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Engine;
using MyBot.Plugins.Squads;
using Pirates;

namespace MyBot.Plugins.Squads
{
    public class AntiBomber : SquadPlugin
    {
        public AntiBomber()
        {

        }
        public bool DoTurn(Squad squad)
        {
            SquadPushPlugin pusher = new SquadPushPlugin(x => x.StickyBombs.Length > 0 ? 1 : 0, true,
                (obj, attackers) => Bot.Engine.CanKill(attackers, obj)
                    ? PushMapping.ByDistance(attackers, obj.DistanceFromBorder(), obj.ClosestBorder())
                    : new PushMapping());

            bool res = pusher.DoTurn(squad);
            if (GameEngine.UNSAFE_SQUAD_PLUGINS)
                return false;
            else
                return res;
        }
    }
}
