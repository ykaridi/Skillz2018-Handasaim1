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
    class BasicSquadStrategy : SquadPirateHandler
    {
        public LogicedPirateSquad[] AssignSquads(PirateShip[] pirates)
        {
            int Count = Bot.Engine.MyPirates.Count;
            int AttackSize = Count / 2 + Count % 2;

            Squad AttackSquad = new Squad(pirates).Filter(x => x.Id < Count);
            Squad DefenseSquad = new Squad(pirates).Filter(x => x.Id >= Count);

            PirateLogic BaseLogic = this.BaseLogic();
            SquadLogic AttackLogic = this.AttackLogic();
            SquadLogic DefenseLogic = this.DefenseLogic();

            LogicedPirateSquad LogicedAttackSquad = new LogicedPirateSquad(AttackSquad.Select(x => x.LogicPirate(BaseLogic)).ToArray(),
                AttackLogic);
            LogicedPirateSquad LogicedDefenseSquad = new LogicedPirateSquad(AttackSquad.Select(x => x.LogicPirate(BaseLogic)).ToArray(),
                DefenseLogic);

            return new LogicedPirateSquad[] { LogicedAttackSquad, LogicedDefenseSquad };
        }

        public SquadLogic AttackLogic()
        {
            BoostedCapsuleSquadPlugin collector = new BoostedCapsuleSquadPlugin(Bot.Engine.MyCapsules[0],
                Bot.Engine.PushRange);
            SquadLogic logic = new SquadLogic(collector);
            return logic;
        }
        public SquadLogic DefenseLogic()
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
        public PirateLogic BaseLogic()
        {
            AsteroidDodger dodger = new AsteroidDodger();
            PirateLogic logic = new PirateLogic(dodger);
            return logic;
        }
    }
}
