using MyBot.Engine.Handlers;
using Pirates;
using MyBot.Engine;
using System.Linq;
using MyBot.Plugins.Pirates;
using MyBot.Plugins.Squads;
using MyBot.Engine.Delegates;
using MyBot.Geometry;

namespace MyBot.Strategies
{
    class TemplatedBasicSquadStrategy : SquadStrategyTemplate
    {
        public override Engine.Tuple<int, int> AssignSizes()
        {
            int Count = Bot.Engine.MyPirates.Count;
            return new Tuple<int, int>(Count / 2 + Count % 2, Count / 2);
        }

        public override PirateLogic AttackBaseLogic()
        {
            AsteroidDodger dodger = new AsteroidDodger();
            PirateLogic logic = new PirateLogic(dodger);
            return logic;
        }
        public override PirateLogic DefenseBaseLogic()
        {
            AsteroidDodger dodger = new AsteroidDodger();
            PirateLogic logic = new PirateLogic(dodger);
            return logic;
        }


        public override SquadLogic CapsuleChaserLogic(Capsule Target)
        {
            BoostedCapsuleSquadPlugin collector = new BoostedCapsuleSquadPlugin(Bot.Engine.MyCapsules[0],
                Bot.Engine.PushRange);
            SquadLogic logic = new SquadLogic(collector);
            return logic;
        }

        public override SquadLogic CapsuleCamperLogic(Capsule Target)
        {
            SquadCamperPlugin camper = new SquadCamperPlugin(Target.InitialLocation);
            SquadLogic logic = new SquadLogic(camper);
            return logic;
        }

        public override SquadLogic DefenseLogic(Mothership mothership)
        {
            Mothership MS = Bot.Engine.EnemyMotherships[0];
            int SCORE_EXTREME_DANGER_TIME = 7;
            int SCORE_DANGER_TIME = 12;
            int EXTREME_DANGER_DETAIL_COUNT = 2;

            Delegates.FilterFunction<Pirate> EnemyHasPush = x => Bot.Engine.GetEnemyPiratesInRange(x.GetLocation(), Bot.Engine.PushRange).Count > 1;
            Delegates.DefenseFunction<Pirate> ExtremeDangerFunction = p =>
            {
                bool CapsuleInGame = Bot.Engine.EnemyLivingCapsules.Any(y => y.IsHeld());
                return new DefenseStats((p.HasCapsule() || !CapsuleInGame) && ((PirateShip)p).TurnsToReach(MS) <= SCORE_EXTREME_DANGER_TIME ? p.Distance(MS) : 0,
                    p.NumPushesForCapsuleLoss);
            };
            Delegates.DefenseFunction<Pirate> DangerFunction = p =>
            {
                bool CapsuleInGame = Bot.Engine.EnemyLivingCapsules.Any(y => y.IsHeld());
                return new DefenseStats((p.HasCapsule() || !CapsuleInGame) && EnemyHasPush(p) && (p.Distance(MS) - (EnemyHasPush(p) ? p.PushDistance : 0)) / p.MaxSpeed <= SCORE_DANGER_TIME ? p.Distance(MS) : 0,
                    p.NumPushesForCapsuleLoss);
            };
            BasicDefenseSquad<Pirate> DefensePlugin = new BasicDefenseSquad<Pirate>(MS.Location,
                    EXTREME_DANGER_DETAIL_COUNT, () => Bot.Engine.EnemyLivingPirates.Select(x => (Pirate)x).ToArray(), ExtremeDangerFunction, DangerFunction);
            SquadLogic logic = new SquadLogic(DefensePlugin);
            return logic;
        }

        public override int Deploy(Capsule capsule)
        {
            if (capsule.Id == 0)
                return AssignSizes().arg0;
            return 0;
        }

        public override int Deploy(Mothership mothership)
        {
            if (mothership.Id == 0)
                return AssignSizes().arg1;
            return 0;
        }
    }
}
