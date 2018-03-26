using System.Linq;
using Pirates;
using MyBot;
using MyBot.Engine;
using MyBot.Engine.Delegates;
using MyBot.Geometry;
using MyBot.Engine.Handlers;

namespace MyBot.Plugins.Pirates
{
    public class EmergencyCapsulePusherPlugin : PiratePlugin
    {
        Delegates.FilterFunction<PirateShip> Filter = (x) =>
        {
            return Bot.Engine.IsAliveAfterTurn(x, 0) && x.HasCapsule;
        };

        public EmergencyCapsulePusherPlugin()
        {
        }
        public bool DoTurn(PirateShip ship)
        {
            foreach (PirateShip p in ship.GetPiratesInPushRange().Where(x => Filter(x)))
            {
                Mothership closestMothership = Bot.Engine.EnemyMotherships.FirstBy(x => x.Distance(p));
                bool CanKill = ((Location)(((Point)p).InDirection(closestMothership, ship.MaxSpeed))).DistanceFromBorder() <= ship.PushDistance;
                if (ship.CanPush(p) && CanKill)
                {
                    if (ship.Push(p, p.ClosestBorder()))
                        return true;
                }
            }
            return false;
        }
    }
}
