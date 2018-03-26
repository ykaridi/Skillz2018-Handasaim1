using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Engine;
using Pirates;
using MyBot.Geometry;

namespace MyBot.Plugins.Squads
{
    public class BoostedCapsuleSquadPlugin : SquadPlugin
    {
        Capsule capsule;
        int CampDuration;
        int AvoidingDistance;
        int MaxPushes;
        double BackwardMultiplier;
        bool IgnoreDeeperCamps;

        /// <summary>
        /// Constructs a boosted capsule scorer squad
        /// </summary>
        /// <param name="capsule">Capsule to score</param>
        /// <param name="AvoidingDistance">Avoiding distance to keep from threats when attempting evasion</param>
        /// <param name="MaxPushes">Maximimal number of pushes to perform on the capsule carrier</param>
        /// <param name="CampDuration">Amount of time a pirate is considered a camper (used to deeper bunks)</param>
        /// <param name="IgnoreDeeperCamps">Flag wether to ignore deep camps (checks if we can pass all the way from current obstruction to mothership)</param>
        /// <param name="BackwardMultiplier">Multiplier to evade dangers "behind" us</param>
        public BoostedCapsuleSquadPlugin(Capsule capsule, int AvoidingDistance, int MaxPushes = 2, int CampDuration = 0, bool IgnoreDeeperCamps = false, double BackwardMultiplier = 2)
        {
            this.capsule = capsule;
            this.AvoidingDistance = AvoidingDistance;
            this.CampDuration = CampDuration;
            this.IgnoreDeeperCamps = IgnoreDeeperCamps;
            this.MaxPushes = MaxPushes;
            this.BackwardMultiplier = BackwardMultiplier;
        }

        public bool DoTurn(Squad squad)
        {
            if (squad.HasCapsule)
            {
                PirateShip CapsuleHolder = squad.First(x => x.HasCapsule);
                Mothership ClosestMothership = Bot.Engine.MyMotherships.Nearest(CapsuleHolder);
                Squad Boosters = squad.Filter(x => x.UniqueId != CapsuleHolder.UniqueId && x.PushReloadTurns <= 0 && (true || x.Distance(ClosestMothership) - CapsuleHolder.Distance(ClosestMothership) < x.PushRange))
                    .OrderBy(x => x.Distance(CapsuleHolder)).Take(System.Math.Max(MaxPushes, CapsuleHolder.NumberOfPushesForCapsuleLoss - 1)).ToList();
                int BoostingDistance = Bot.Engine.GetDistanceAgainst(Boosters, CapsuleHolder);
                bool ShouldPush;
                {
                    Squad EnemyPirates = Bot.Engine.EnemyLivingPirates.Filter(x => x.Distance(CapsuleHolder) <= x.PushRange + x.MaxSpeed);
                    PirateShip[] Campers;
                    Bot.Engine.CheckForCamper(CampDuration, out Campers);
                    bool ExistsDeeperCamp = Campers.Any(x =>
                    {
                        bool IsFurther = !EnemyPirates.ContainsPirate(x);
                        bool IsCloserToMothership = EnemyPirates.All(y => x.Distance(ClosestMothership) < y.Distance(ClosestMothership));
                        Squad LocalBunk = Bot.Engine.EnemyLivingPirates.Filter(y => y.Distance(CapsuleHolder) <= y.PushRange + y.MaxSpeed && Bot.Engine.GetCampLength(y) >= CampDuration);
                        bool MeetsBunkerRequirement = IsBunker(LocalBunk, LocalBunk.Middle, CapsuleHolder.NumberOfPushesForCapsuleLoss);

                        return IsFurther && IsCloserToMothership && MeetsBunkerRequirement;
                    });

                    bool IsSafeAfterBoost;
                    {
                        Location NewLocation = (Location)(((Point)CapsuleHolder).InDirection(ClosestMothership, BoostingDistance + CapsuleHolder.MaxSpeed));
                        Squad LocalBunk = Bot.Engine.EnemyLivingPirates.Filter(y => y.Distance(CapsuleHolder) <= y.PushRange + y.MaxSpeed && Bot.Engine.GetCampLength(y) >= CampDuration);
                        IsSafeAfterBoost = !IsBunker(LocalBunk, NewLocation, CapsuleHolder.NumberOfPushesForCapsuleLoss);
                    }

                    ShouldPush = CapsuleHolder.Distance(ClosestMothership) <= ClosestMothership.UnloadRange + CapsuleHolder.MaxSpeed + BoostingDistance - 1 ||
                        (IsBunker(EnemyPirates, CapsuleHolder) && ((!ExistsDeeperCamp && IsSafeAfterBoost) || IgnoreDeeperCamps));
                }

                if (ShouldPush)
                {
                    Boosters = Boosters.Where(x => x.CanPush(CapsuleHolder)).ToList();
                    Boosters.ForEach(x => x.Push(CapsuleHolder, ClosestMothership));
                    CapsuleHolder.Sail(ClosestMothership);
                } else
                {
                    CapsuleHolder.Sail(ClosestMothership, x =>
                    {
                        if (x is Pirate pirate)
                        {
                            return IsBunker(x, CapsuleHolder.NumberOfPushesForCapsuleLoss) ? AvoidingDistance : 0;
                        }
                        return 0;
                    }, true);
                    foreach (PirateShip p in Boosters)
                    {
                        p.Sail(CapsuleHolder.ExpectedLocation);
                    }
                }
                foreach (PirateShip p in squad.FilterOutBySquad(Boosters.AddPirates(CapsuleHolder)))
                    p.Sail(capsule.InitialLocation);
                return true;
            } else if (capsule.IsAlive())
            {
                foreach (PirateShip p in squad)
                    p.Sail(capsule, EnableWarps: true);
                return true;
            } else
            {
                foreach (PirateShip p in squad)
                    p.Sail(capsule.InitialLocation, EnableWarps: true);
                return true;
            }
            return false;
        }
        
        private bool IsBunker(Squad EnemyPirates, MapObject loc, int PushTolerance)
        {
            return EnemyPirates.Count > 0 && (EnemyPirates.Count >= PushTolerance
                            || Bot.Engine.CanKillEstimated(EnemyPirates, loc));
        }
        private bool IsBunker(Squad EnemyPirates, PirateShip target)
        {
            return EnemyPirates.Count > 0 && (EnemyPirates.Count >= target.NumberOfPushesForCapsuleLoss
                            || Bot.Engine.CanKill(EnemyPirates, target));
        }
        private bool IsBunker(MapObject location, int PushTolerance)
        {
            Squad EnemyPirates = Bot.Engine.EnemyLivingPirates.Filter(x => x.Distance(location) <= x.PushRange + x.MaxSpeed);
            return IsBunker(EnemyPirates, location, PushTolerance);
        }
    }
}
