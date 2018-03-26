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
    abstract class SquadStrategyTemplate : SquadPirateHandler
    {
        public virtual void PreTurn(PirateShip[] pirates)
        {

        }

        public virtual void PostTurn()
        {

        }

        public LogicedPirateSquad[] AssignSquads(PirateShip[] pirates)
        {
            // Not good practice
            pirates = Bot.Engine.MyPirates.ToArray();

            Tuple<int, int> Sizes = AssignSizes();
            int AttackSize = Sizes.arg0;
            int DefenseSize = Sizes.arg1;

            Squad AttackSquad = new Squad(pirates.Where(x => x.Id < AttackSize));
            Squad DefenseSquad = new Squad(pirates.Where(x => x.Id >= AttackSize));

            LogicedPirateSquad[] AttackerPirates = LogicAttackers(AttackSquad);
            LogicedPirateSquad[] DefenderPirates = LogicDefenders(DefenseSquad);

            PreTurn(pirates);
            // Not good practice
            return AttackerPirates.Concat(DefenderPirates).Select(x =>
            {
                return new LogicedPirateSquad(x.LogicedPirates.Where(y => y.pirate.Alive).ToArray(), x.Logic);
            }).ToArray();
        }
        public LogicedPirateSquad[] LogicAttackers(Squad squad)
        {
            #region Distrbution
            if (squad.Count <= 0)
                return new LogicedPirateSquad[0];

            int Mines = Bot.Engine.MyCapsules.Length;
            #endregion Distrbution
            PirateLogic BaseLogic = AttackBaseLogic();

            LogicedPirateSquad[] Squads = new LogicedPirateSquad[0];
            for (int idx = 0; idx < Mines; idx++)
            {
                Capsule Target = Bot.Engine.MyCapsules[idx];

                Squad CurrentSquad = squad.Take(Deploy(Target)).ToList();

                squad = squad.FilterOutBySquad(CurrentSquad);

                if (CurrentSquad.Count <= 0)
                    continue;

                SquadLogic ChaserLogic = CapsuleChaserLogic(Target);
                SquadLogic CamperLogic = CapsuleCamperLogic(Target);

                LogicedPirateSquad[] LogicedCurrentSquads;

                if (Target.IsHeld() && CurrentSquad.ContainsPirate(Target.Holder) && CurrentSquad.LivingPirates().Count > 2)
                {
                    Squad CapsuleChasers = new Squad(CurrentSquad.LivingPirates().FilterOutById(Target.Holder.Id).OrderBy(x => System.Math.Max(x.PushReloadTurns, x.TurnsToReach(Target))).Take(Target.Holder.NumPushesForCapsuleLoss - 1)).AddPirates((PirateShip)Target.Holder);
                    Squad CapsuleCampers = CurrentSquad.FilterOutBySquad(CapsuleChasers);

                    LogicedCurrentSquads = new LogicedPirateSquad[] { new LogicedPirateSquad(CapsuleCampers.Select(x => x.LogicPirate(BaseLogic)).ToArray(), CamperLogic),
                                                                        new LogicedPirateSquad(CapsuleChasers.Select(x => x.LogicPirate(BaseLogic)).ToArray(), ChaserLogic) };
                }
                else
                {
                    LogicedCurrentSquads = new LogicedPirateSquad[] { new LogicedPirateSquad(CurrentSquad.Select(x => x.LogicPirate(BaseLogic)).ToArray(), ChaserLogic) };
                }

                Squads = Squads.Concat(LogicedCurrentSquads).ToArray();
            }
            return Squads;
        }
        public LogicedPirateSquad[] LogicDefenders(Squad squad)
        {
            #region Distrbution
            if (squad.Count <= 0)
                return new LogicedPirateSquad[0];

            int EnemyMotherships = Bot.Engine.EnemyMotherships.Length;
            #endregion Distrbution

            PirateLogic BaseLogic = DefenseBaseLogic();

            LogicedPirateSquad[] Squads = new LogicedPirateSquad[0];
            for (int idx = 0; idx < EnemyMotherships; idx++)
            {
                Mothership Target = Bot.Engine.EnemyMotherships[idx];
                Squad CurrentSquad = squad.Take(Deploy(Target)).ToList();

                squad = squad.FilterOutBySquad(CurrentSquad);

                if (CurrentSquad.Count <= 0)
                    continue;

                LogicedPirateSquad LogicedCurrentSquad = new LogicedPirateSquad(CurrentSquad.Select(x => x.LogicPirate(BaseLogic)).ToArray(),
                    DefenseLogic(Target));

                Squads = Squads.Concat(new LogicedPirateSquad[] { LogicedCurrentSquad }).ToArray();
            }

            return Squads;
        }

        public abstract PirateLogic AttackBaseLogic();
        public abstract PirateLogic DefenseBaseLogic();

        public abstract int Deploy(Capsule capsule);
        public abstract int Deploy(Mothership mothership);

        public abstract SquadLogic CapsuleChaserLogic(Capsule capsule);
        public abstract SquadLogic CapsuleCamperLogic(Capsule capsule);
        public abstract SquadLogic DefenseLogic(Mothership mothership);

        public abstract Tuple<int, int> AssignSizes();
    }
}
