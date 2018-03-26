using Pirates;
using MyBot.Engine;
using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Plugins.Squads;
using MyBot.Engine.Delegates;

namespace MyBot.Plugins.Pirates
{
    public class PortalOptimizer : PiratePlugin
    {
        SquadPushPlugin pusher;
        public PortalOptimizer()
        {
            pusher = new SquadPushPlugin(x =>
            {
                if (x is Wormhole w)
                    return 1;
                return 0;
            }, true, (obj, attackers) => Bot.Engine.DefaultPush(obj, attackers, false));
        }
        public PortalOptimizer(System.Func<Wormhole, Location> optimizer)
        {
            pusher = new SquadPushPlugin(x =>
            {
                if (x is Wormhole w)
                    return 1;
                return 0;
            }, true, (obj, attackers) =>
            {
                if (obj is Wormhole w)
                    return PushMapping.To(attackers, obj, optimizer(w));
                return new PushMapping();
            });
        }
        public bool DoTurn(PirateShip ship)
        {
            return pusher.DoTurn(new Squad(ship));
        }
    }
}
