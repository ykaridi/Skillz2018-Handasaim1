using System.Collections.Generic;
using System.Linq;
using Pirates;
using MyBot.Engine;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Geometry;
using MyBot.Plugins.Pirates;
using MyBot.Plugins.Squads;
using MyBot.Strategies;

namespace MyBot.Strategies
{
    class PVPBot : SquadStrategyTemplate
    {
        private Bomber bomber = new Bomber(x =>
        {
            double score = Bomber.ScoreLocation(x);
            if (score >= 2)
                return score;
            else
                return 0;
        }, x => 0);

        public int WAIT_UNTIL_DITCH_BACKUP = 85;

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

        private Delegates.PushMapper DefensePushMapper = (obj, attackers) => (obj is Pirate && ((Pirate)obj).HasCapsule()) ? Bot.Engine.DefaultPush(obj, attackers, true) : Bot.Engine.DefaultPush(obj, attackers);

        public override void PreTurn(PirateShip[] pirates)
        {
            AntiBomber antiBomber = new AntiBomber();
            antiBomber.DoTurn(new Squad(pirates));
        }

        public override PirateLogic AttackBaseLogic()
        {
            AsteroidDodger asteroidDodger = new AsteroidDodger((ast, p) =>
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
                }
            );
            EmergencyCapsulePusherPlugin emergencyCapsulePusher = new EmergencyCapsulePusherPlugin();
            PortalOptimizer portalOptimizer = new PortalOptimizer();
            
            return new PirateLogic(bomber, asteroidDodger, emergencyCapsulePusher, portalOptimizer);
        }

        public override SquadLogic CapsuleChaserLogic(Capsule Target)
        {
            return new SquadLogic().AttachPlugin(new BoostedCapsuleSquadPlugin(Target, AVOIDING_DISTANCE, Bot.Engine.NumberOfPushesForHeavyCapsuleLoss, CAMPER_DURATION, IGNORE_DEEP_CAMPS, BACKWARD_MULTIPLIER));
        }
        public override SquadLogic CapsuleCamperLogic(Capsule Target)
        {
            return new SquadLogic().AttachPlugin(new BasicDefenseSquad<Wormhole>(Target.InitialLocation, 0, () => Bot.Engine.AllWormholes, x => new DefenseStats(0, 0), x => new DefenseStats(x.Distance(Target.InitialLocation), WormholeDeployer(x)), (obj, attackers) => Bot.Engine.DefaultPush(obj, attackers, false)))
                .AttachPlugin(new SquadCamperPlugin(Target.InitialLocation));
        }

        public override PirateLogic DefenseBaseLogic()
        {
            AsteroidDodger asteroidDodger = new AsteroidDodger((ast, p) =>
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
                }
            );
            EmergencyCapsulePusherPlugin emergencyCapsulePusher = new EmergencyCapsulePusherPlugin();
            return new PirateLogic(bomber, asteroidDodger, emergencyCapsulePusher);
        }

        public override SquadLogic DefenseLogic(Mothership MS)
        {
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

            BasicDefenseSquad<SpaceObject> DefensePlugin = new BasicDefenseSquad<SpaceObject>(MS.Location,
                EXTREME_DANGER_DETAIL_COUNT, () => Bot.Engine.EnemyLivingPirates.Select(x => (SpaceObject)((Pirate)x)).Concat(Bot.Engine.AllWormholes).ToArray(), ExtremeDangerFunction, DangerFunction,
                DefensePushMapper);
            SquadCamperPlugin camper = new SquadCamperPlugin(MS);
            return new SquadLogic(DefensePlugin, camper);
        }

        public override int Deploy(Capsule capsule)
        {
            int total = AssignSizes().arg0;

            int Mines = Bot.Engine.MyCapsules.Length;
            Mines = System.Math.Min(Mines, total);
            Capsule[] Capsules = Bot.Engine.MyCapsules.OrderBy(x => x.InitialLocation.Distance(Bot.Engine.MyMotherships.Nearest(x))).ToArray();

            int BaseSize = total / Mines;
            int Remainder = System.Math.Max((total - BaseSize * Mines) % Mines, 0);

            for (int i = 0; i < Mines; i++)
            {
                if (capsule.Id == Capsules[i].Id)
                    return BaseSize + (i < Remainder ? 1 : 0);
            }
            return 0;
        }

        public override int Deploy(Mothership mothership)
        {
            int total = AssignSizes().arg1;

            int EnemyMotherships = Bot.Engine.EnemyMotherships.Length;
            if (EnemyMotherships <= 0)
                return 0;

            EnemyMotherships = System.Math.Min(System.Math.Max(1, total / 2), EnemyMotherships);
            Mothership[] Motherships = Bot.Engine.EnemyMotherships.OrderBy(x => x.Distance(Bot.Engine.EnemyCapsules.Select(y => y.InitialLocation).Nearest(x))).ToArray();

            int BaseSize = total / EnemyMotherships;
            int Remainder = System.Math.Max((total - BaseSize * EnemyMotherships) % EnemyMotherships, 0);

            for (int i = 0; i < EnemyMotherships; i++)
            {
                if (mothership.Id == Motherships[i].Id)
                    return BaseSize + (i < Remainder ? 1 : 0);
            }
            return 0;
        }

        public override Tuple<int, int> AssignSizes()
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
