using Pirates;
using MyBot.Engine.Handlers;
using MyBot.Engine;
using System.Linq;

namespace MyBot.Strategies.Week3Custom
{
    class EtgarimWeek3Plugin : PiratePlugin
    {
        public bool DoTurn(PirateShip ship)
        {
            if (Bot.Engine.Enemy.BotName == "25772")
            {
                if (Bot.Engine.MyCapsules[0].IsAlive() && Bot.Engine.MyCapsules[0].IsHeld() && ship.InPushRange(Bot.Engine.MyLivingCapsules[0]) && ship.IsHeavy)
                    ship.Push(Bot.Engine.MyLivingCapsules[0].Holder, Bot.Engine.MyMotherships[0]);
                else if (ship.IsNormal)
                    ship.Sail(Bot.Engine.MyMotherships[0]);
                return true;
            }
            else if (Bot.Engine.Enemy.BotName == "25766")
            {
                PirateShip p = ship;
                Capsule capsule = Bot.Engine.MyCapsules[0];
                if (p.Id == 0)
                {
                    PirateShip other = Bot.Engine.GetMyPirateById(1);
                    Location dest = Bot.Engine.Self.Score >= 3 ? new Location(3797, 4358) : new Location(2534, 3936);
                    if (!p.Sail(dest) && p.InPushRange(other))
                        p.Push(other, capsule);
                }
                else
                {
                    PirateShip other = Bot.Engine.GetMyPirateById(0);
                    bool IsHeld = capsule.IsAlive() && capsule.IsHeld();
                    if (p.IsNormal)
                    {
                        if (IsHeld)
                            ((Pirate)p).SwapStates(other);
                        else
                            p.Sail(capsule);
                    }
                    else
                    {
                        if (IsHeld)
                            p.Sail(Bot.Engine.MyMotherships[0]);
                        else
                            ((Pirate)p).SwapStates(other);
                    }
                }
                return true;
            }
            else if (Bot.Engine.Enemy.BotName == "26069")
            {
                int id = ship.Id;
                if (id == 0 || id == 2)
                {
                    bool Sailed = ship.Sail(id == 0 ? new Location(3646, 2200) : new Location(3646, 4000));
                    Location target = id == 0 ? new Location(4700, 1200) : new Location(4700, 5200);
                    if (!Sailed && Bot.Engine.GetEnemyPiratesInRange(target, 500).Count >= 2)
                    {
                        Asteroid a = Bot.Engine.AllAsteroids[id / 2];
                        ship.Push(a, target.Add(new Location(225, 0)));
                    }
                }
                else
                {
                    Squad s = Bot.Engine.GetEnemyPiratesInRange(ship, Bot.Engine.Game.StickBombRange);
                    if (s.Count > 0)
                    {
                        ((Pirate)ship).StickBomb(s.First());
                    }
                }
                return true;
            }
            return false;
        }
    }
}
