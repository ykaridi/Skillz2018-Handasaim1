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
    class EtgarimWeek3 : SquadPirateHandler
    {
        public int AVOIDING_DISTANCE = 3 * Bot.Engine.PushRange;
        public int CAPSULE_DANGER_DISTANCE = Bot.Engine.PushDistance + Bot.Engine.MaxPirateSpeed - Bot.Engine.PushRange;
        public int DEFENDER_DANGER_RADIUS = Bot.Engine.PushRange * 7;
        public int CAMPER_DURATION = 50;
        public int SCORE_EXTREME_DANGER_TIME = 7;
        public int SCORE_DANGER_TIME = 12;
        public int PORTAL_DANGER_TIME = 15;
        public int EXTREME_DANGER_DETAIL_COUNT = Bot.Engine.NumberOfPushesForCapsuleLoss;
        public double BACKWARD_MULTIPLIER = 1.5;

        public System.Func<PirateShip, int> PirateDeployer = x => Bot.Engine.NumberOfPushesForHeavyCapsuleLoss;
        public System.Func<Wormhole, int> WormholeDeployer = x => 1;

        public bool IGNORE_DEEP_CAMPS = false;

        private PirateLogic AttackBaseLogic;
        private PirateLogic DefenseBaseLogic;

        private Delegates.PushMapper DefensePushMapper = (obj, attackers) => (obj is Pirate && ((Pirate)obj).HasCapsule()) ? Bot.Engine.DefaultPush(obj, attackers, true) : Bot.Engine.DefaultPush(obj, attackers);

        public LogicedPirateSquad[] AssignSquads(PirateShip[] pirates)
        {
            // Private cases
            if (Bot.Engine.Enemy.BotName == "25767")
                DefensePushMapper = (obj, attackers) => {
                    PushMapping pm = (obj is Pirate && ((Pirate)obj).HasCapsule()) ? Bot.Engine.DefaultPush(obj, attackers, true) : Bot.Engine.DefaultPush(obj, attackers);
                    return new PushMapping(pm.attackers, new Location(Bot.Engine.Rows - pm.dest.Row, Bot.Engine.Cols - pm.dest.Col));
                };


            // Initialize BaseLogic
            AttackBaseLogic = new PirateLogic().AttachPlugin(new Week3Custom.EtgarimWeek3Plugin()).AttachPlugin(new AsteroidDodger((ast, p) =>
            {
                /*
                Location nm = Bot.Engine.MyMotherships.Nearest(p).Location;
                Line l = new Line(ast, nm);
                return l.IsProjectionOnLine(p) ? AsteroidDodger.TangentPush(ast, p) : nm;
                */
                Squad PossibleTargets = Bot.Engine.EnemyLivingPirates.Filter(enemy =>
                {
                    Line l = new Line(enemy, ast);
                    bool ShouldPush = !l.IsProjectionOnLine(p) || l.Distance(p) > ast.Size * 1.5;
                    return ShouldPush;
                });
                if (!PossibleTargets.IsEmpty())
                    return PossibleTargets.FirstBy(x => x.Distance(ast)).Location;
                else
                    return AsteroidDodger.TangentPush(ast, p);
            }
                ))
                .AttachPlugin(new EmergencyCapsulePusherPlugin()).AttachPlugin(new PortalOptimizer());
            DefenseBaseLogic = new PirateLogic().AttachPlugin(new Week3Custom.EtgarimWeek3Plugin()).AttachPlugin(new AsteroidDodger((ast, p) =>
            {
                Squad PossibleTargets = Bot.Engine.EnemyLivingPirates.Filter(enemy =>
                {
                    Line l = new Line(enemy, ast);
                    bool ShouldPush = !l.IsProjectionOnLine(p) || l.Distance(p) > ast.Size * 1.5;
                    return ShouldPush;
                });
                if (!PossibleTargets.IsEmpty())
                    return PossibleTargets.FirstBy(x => x.Distance(ast)).Location;
                else
                    return AsteroidDodger.TangentPush(ast, p);
            }))
                .AttachPlugin(new EmergencyCapsulePusherPlugin());

            // Not good practice
            pirates = Bot.Engine.MyPirates.ToArray();

            Tuple<int, int> Sizes = AssignSizes();
            int AttackSize = Sizes.arg0;
            int DefenseSize = Sizes.arg1;

            Squad AttackSquad = new Squad(pirates.Where(x => x.Id < AttackSize));
            Squad DefenseSquad = new Squad(pirates.Where(x => x.Id >= AttackSize));

            LogicedPirateSquad[] AttackerPirates = LogicAttackers(AttackSquad);
            LogicedPirateSquad[] DefenderPirates = LogicDefenders(DefenseSquad);

            Bot.Engine.Debug("Testing pirates...");
            // Not good practice
            return AttackerPirates.Concat(DefenderPirates).Select(x =>
            {
                Bot.Engine.Debug(new Squad(x.LogicedPirates.Select(y => y.pirate)));
                return new LogicedPirateSquad(x.LogicedPirates.Where(y => y.pirate.Alive).ToArray(), x.Logic);
            }).ToArray();
        }

        public LogicedPirateSquad[] LogicAttackers(Squad squad)
        {
            #region Distrbution
            if (squad.Count <= 0)
                return new LogicedPirateSquad[0];

            int Mines = Bot.Engine.MyCapsules.Length;
            //Mines = System.Math.Min(System.Math.Max(1, squad.Count / 2 + squad.Count % 2), Mines);
            Mines = System.Math.Min(Mines, squad.Count);
            Capsule[] Capsules = Bot.Engine.MyCapsules.OrderBy(x => x.InitialLocation.Distance(Bot.Engine.MyMotherships.Nearest(x))).ToArray();

            int BaseSize = squad.Count / Mines;
            int Remainder = System.Math.Max((squad.Count - BaseSize * Mines) % Mines, 0);
            #endregion Distrbution

            LogicedPirateSquad[] Squads = new LogicedPirateSquad[0];
            for (int idx = 0; idx < Mines; idx++)
            {
                Squad CurrentSquad = new Squad(squad.Skip(BaseSize * idx + System.Math.Min(idx, Remainder)).Take(BaseSize + (idx < Remainder ? 1 : 0)));
                if (CurrentSquad.Count <= 0)
                    continue;

                Location Middle = CurrentSquad.Select(x => x.InitialLocation).Middle();
                Capsule Target = Capsules[idx];

                SquadLogic CapsuleChasersLogic = new SquadLogic().AttachPlugin(new BoostedCapsuleSquadPlugin(Target, AVOIDING_DISTANCE, Bot.Engine.NumberOfPushesForHeavyCapsuleLoss, CAMPER_DURATION, IGNORE_DEEP_CAMPS, BACKWARD_MULTIPLIER));
                SquadLogic CapsuleCampersLogic = new SquadLogic().AttachPlugin(new BasicDefenseSquad<Wormhole>(Target.InitialLocation, 0, () => Bot.Engine.AllWormholes, x => new DefenseStats(0,0), x => new DefenseStats(x.Distance(Target.InitialLocation), WormholeDeployer(x)), (obj, attackers) => Bot.Engine.DefaultPush(obj, attackers, false)))
                    .AttachPlugin(new SquadCamperPlugin(Target.InitialLocation));

                LogicedPirateSquad[] LogicedCurrentSquads;

                if (Target.IsHeld() && CurrentSquad.ContainsPirate(Target.Holder) && CurrentSquad.LivingPirates().Count > 2)
                {
                    Squad CapsuleChasers = new Squad(CurrentSquad.LivingPirates().FilterOutById(Target.Holder.Id).OrderBy(x => System.Math.Max(x.PushReloadTurns, x.TurnsToReach(Target))).Take(Target.Holder.NumPushesForCapsuleLoss - 1)).AddPirates((PirateShip) Target.Holder);
                    Squad CapsuleCampers = CurrentSquad.FilterOutBySquad(CapsuleChasers);

                    LogicedCurrentSquads = new LogicedPirateSquad[] { new LogicedPirateSquad(CapsuleCampers.Select(x => x.LogicPirate(AttackBaseLogic)).ToArray(), CapsuleCampersLogic),
                                                                        new LogicedPirateSquad(CapsuleChasers.Select(x => x.LogicPirate(AttackBaseLogic)).ToArray(), CapsuleChasersLogic) };
                }
                else
                {
                    LogicedCurrentSquads = new LogicedPirateSquad[] { new LogicedPirateSquad(CurrentSquad.Select(x => x.LogicPirate(AttackBaseLogic)).ToArray(), CapsuleChasersLogic) };
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
            if (EnemyMotherships <= 0)
                return new LogicedPirateSquad[0];
            EnemyMotherships = System.Math.Min(System.Math.Max(1, squad.Count / 2), EnemyMotherships);
            Mothership[] Motherships = Bot.Engine.EnemyMotherships.OrderBy(x => x.Distance(Bot.Engine.EnemyCapsules.Select(y => y.InitialLocation).Nearest(x))).ToArray();

            int BaseSize = squad.Count / EnemyMotherships;
            int Remainder = System.Math.Max((squad.Count - BaseSize * EnemyMotherships) % EnemyMotherships, 0);
            #endregion Distrbution

            LogicedPirateSquad[] Squads = new LogicedPirateSquad[0];
            for (int idx = 0; idx < EnemyMotherships; idx++)
            {
                Squad CurrentSquad = new Squad(squad.Skip(BaseSize * idx + System.Math.Min(idx, Remainder)).Take(BaseSize + (idx < Remainder ? 1 : 0)));
                if (CurrentSquad.Count <= 0)
                    continue;

                Mothership MS = Bot.Engine.EnemyMotherships[idx];

                #region Scoring Functions
                Delegates.FilterFunction<MapObject> EnemyHasPush = x => Bot.Engine.GetEnemyPiratesInRange(x.GetLocation(), Bot.Engine.PushRange).Count > 1;
                Delegates.DefenseFunction<SpaceObject> ExtremeDangerFunction = x =>
                {
                    bool CapsuleInGame = Bot.Engine.EnemyLivingCapsules.Any(y => y.IsHeld());
                    if (x is Pirate p)
                        return new DefenseStats((p.HasCapsule() || !CapsuleInGame) && ((PirateShip)p).TurnsToReach(MS) <= SCORE_EXTREME_DANGER_TIME ? p.Distance(MS) : 0, PirateDeployer((PirateShip)p));
                    else if (x is Wormhole w)
                        return new DefenseStats(w.Distance(MS) / Bot.Engine.MaxPirateSpeed <= SCORE_EXTREME_DANGER_TIME ? w.Distance(MS) + Bot.Engine.MaxPirateSpeed * SCORE_EXTREME_DANGER_TIME : 0, WormholeDeployer(w));
                    else
                        return new DefenseStats(0, 0);
                };
                Delegates.DefenseFunction<SpaceObject> DangerFunction = x =>
                {
                    bool CapsuleInGame = Bot.Engine.EnemyLivingCapsules.Any(y => y.IsHeld());
                    if (x is Pirate p)
                        return new DefenseStats((p.HasCapsule() || !CapsuleInGame) && EnemyHasPush(p) && (p.Distance(MS) - (EnemyHasPush(p) ? p.PushDistance : 0)) / p.MaxSpeed <= SCORE_DANGER_TIME ? p.Distance(MS) : 0, PirateDeployer((PirateShip)p));
                    else if (x is Wormhole w)
                        return new DefenseStats(w.Distance(MS) / Bot.Engine.MaxPirateSpeed <= SCORE_DANGER_TIME ? w.Distance(MS) + Bot.Engine.MaxPirateSpeed * SCORE_DANGER_TIME : 0, WormholeDeployer(w));
                    else
                        return new DefenseStats(0, 0);
                };
                #endregion Scoring Functions

                SquadPlugin DefensePlugin = new BasicDefenseSquad<SpaceObject>(MS.Location,
                    EXTREME_DANGER_DETAIL_COUNT, () => Bot.Engine.EnemyLivingPirates.Select(x => (SpaceObject)((Pirate)x)).Concat(Bot.Engine.AllWormholes).ToArray(), ExtremeDangerFunction, DangerFunction,
                    DefensePushMapper);
                LogicedPirateSquad LogicedCurrentSquad = new LogicedPirateSquad(CurrentSquad.Select(x => x.LogicPirate(DefenseBaseLogic)).ToArray(), new SquadLogic(DefensePlugin, new SquadCamperPlugin(MS)));

                Squads = Squads.Concat(new LogicedPirateSquad[] { LogicedCurrentSquad }).ToArray();
            }

            return Squads;
        }

        public Tuple<int, int> AssignSizes()
        {
            int Total = Bot.Engine.MyPirates.Count; 
            int AttackSize = 0;
            int DefenseSize = 0;
            if (Bot.Engine.MyCapsules.Length <= 0 && Bot.Engine.EnemyCapsules.Length <= 0)
            {

            }
            else if (Bot.Engine.MyCapsules.Length <= 0 || Bot.Engine.MyMotherships.Length <= 0)
            {
                AttackSize = 0;
                DefenseSize = Total;
            }
            else if (Bot.Engine.EnemyCapsules.Length <= 0 || Bot.Engine.EnemyMotherships.Length <= 0)
            {
                AttackSize = Total;
                DefenseSize = 0;
            }
            else
            {
                if (Bot.Engine.EnemyMotherships.Length == 1)
                    AttackSize = System.Math.Max(2, Total - 4);
                else
                    AttackSize = Total / 2 + (Total % 2);

                if (DefenseSize == 0)
                    DefenseSize = Total - AttackSize;
                else if (AttackSize == 0)
                    AttackSize = Total - DefenseSize;
            }
            AttackSize = System.Math.Min(Total, AttackSize);
            DefenseSize = System.Math.Min(Total, DefenseSize);
            return new Tuple<int, int>(AttackSize, DefenseSize);
        }
    }
}
