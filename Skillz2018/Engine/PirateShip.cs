using Pirates;
using MyBot.Engine;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Geometry;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MyBot.Engine
{
    public class PirateShip : MapObject
    {
        #region Scanning Functions
        public Squad GetPiratesInPushRange()
        {
            return GetPiratesInRange(PushRange);
        }
        public Squad GetPiratesInRange(int range)
        {
            return Bot.Engine.GetEnemyPiratesInRange(Location, range);
        }
        #endregion Scanning Functions

        private readonly Pirate pirate;
        public PirateShip(Pirate pirate)
        {
            this.pirate = pirate;
        }
        public static implicit operator Pirate(PirateShip me)
        {
            return me.pirate;
        }
        public static explicit operator PirateShip(Pirate me)
        {
            return new PirateShip(me);
        }

        #region Extends
        public StickyBomb[] StickyBombs
        {
            get
            {
                return pirate.StickyBombs;
            }
        }
        public int UniqueId
        {
            get
            {
                return pirate.UniqueId;
            }
        }
        public bool HasCapsule
        {
            get
            {
                return pirate.HasCapsule();
            }
        }
        public bool Alive
        {
            get
            {
                return pirate.IsAlive();
            }
        }
        public Capsule Capsule
        {
            get
            {
                return pirate.Capsule;
            }
        }
        public int NumberOfPushesForCapsuleLoss
        {
            get
            {
                return pirate.NumPushesForCapsuleLoss;
            }
        }
        public int PushRange
        {
            get
            {
                return pirate.PushRange;
            }
        }
        public int PushDistance
        {
            get
            {
                return pirate.PushDistance;
            }
        }
        public int PushReloadTurns
        {
            get
            {
                return pirate.PushReloadTurns;
            }
        }
        public int SpawnTurns
        {
            get
            {
                return SpawnTurns;
            }
        }
        public int TurnsToRevive
        {
            get
            {
                return pirate.TurnsToRevive;
            }
        }
        public int Id
        {
            get
            {
                return pirate.Id;
            }
        }
        public bool IsAliveAfterTurn
        {
            get
            {
                return Bot.Engine.IsAliveAfterTurn(pirate);
            }
        }
        public Location Location
        {
            get
            {
                return pirate.Location;
            }
        }
        public bool IsOurs
        {
            get
            {
                return Owner.Id == Bot.Engine.Self.Id;
            }
        }
        public Player Owner
        {
            get
            {
                return pirate.Owner;
            }
        }
        public Location InitialLocation
        {
            get
            {
                return pirate.InitialLocation;
            }
        }
        public int MaxSpeed
        {
            get
            {
                return pirate.MaxSpeed;
            }
        }

        public bool CanPlay
        {
            get
            {
                return Bot.Engine.CanPlay(pirate);
            }
        }
        public bool IsHeavy
        {
            get
            {
                return pirate.StateName.Equals(Bot.Engine.HEAVY_STATE_NAME);
            }
        }
        public bool IsNormal
        {
            get
            {
                return pirate.StateName.Equals(Bot.Engine.NORMAL_STATE_NAME);
            }
        }

        public bool CanStickBomb(SpaceObject target)
        {
            return pirate.InStickBombRange(target);
        }
        public bool StickBomb(SpaceObject target)
        {
            return Bot.Engine.StickBomb(this, target);
        }
        public bool SwapStates(PirateShip other)
        {
            return Bot.Engine.SwapStates(this, other);
        }

        private bool Sail(MapObject loc)
        {
            return Bot.Engine.Sail(pirate, loc);
        }
        public bool Sail(MapObject loc, int AvoidingDistance = 0, bool EnableWarps = false)
        {
            return Sail(loc, x => AvoidingDistance, EnableWarps);
        }
        public bool Sail(MapObject loc, System.Func<SpaceObject, int> AvoidingDistanceSelector, bool EnableWarps = false)
        {
            Line line = new Line(pirate, loc);
            if (EnableWarps && Bot.Engine.AllWormholes.Length > 0)
                line = new Line(pirate, Bot.Engine.ScorePosition(pirate, loc, MaxSpeed).arg1);
            Path path = (Path)line;
            Tuple<SpaceObject, int>[] Obstacles = Bot.Engine.HostileSpaceObjects.Select(x => new Tuple<SpaceObject, int>(x, AvoidingDistanceSelector(x))).Where(x => x.arg1 > 0)
                .Where(x => line.IsProjectionOnLine(x.arg0.GetLocation()) && (!(x.arg0 is Pirate) || !(x.arg0 as Pirate).InPushRange(this))).ToArray();
            if (!Obstacles.IsEmpty())
            {
                Tuple<SpaceObject, int> obstruction = Obstacles.FirstBy(x => Distance(x.arg0));
                path = Bot.Engine.AvoidingPath(line, obstruction.arg1, obstruction.arg0);
                Bot.Engine.AppendAction("$> Pirate #" + Id + " is attempting evasion with path " + path.ToString());
            }
            IEnumerable<Tuple<int, Location>> PossibleDests = Enumerable.Range(2, MaxSpeed).Select(x => new Tuple<int, Location>(x, (Location)path.GetNextPoint(this, x)))
                .Where(x => !Bot.Engine.AllWormholes.Any(y => y.TurnsToReactivate <= 1 && y.InRange(x.arg1, y.WormholeRange)));
            Location dest;
            if (EnableWarps)
                dest = (Location)path.GetNextPoint(this, MaxSpeed);
            else if (PossibleDests.IsEmpty())
                dest = GetLocation();
            else
                dest = PossibleDests.FirstBy(x => -x.arg0).arg1;

            return Sail(dest);
        }
        
        public override string ToString()
        {
            return (IsOurs ? "Friendly" : "Enemy") + " Pirate #" + Id.ToString();
        }
        public override Location GetLocation()
        {
            return Location;
        }
        public bool InPushRange(MapObject obj)
        {
            return pirate.InPushRange(obj);
        }
        #endregion Extends
        #region Custom
        public Location ExpectedLocation
        {
            get
            {
                return Bot.Engine.GetExpectedLocation(pirate);
            }
        }
        public int TurnsToReach(MapObject obj)
        {
            return Bot.Engine.TurnsToReach(this, obj, MaxSpeed);
        }
        public bool Push(PirateShip target, MapObject obj)
        {
            return Bot.Engine.Push(pirate, (Pirate)target, obj);
        }
        public bool Push(SpaceObject target, MapObject obj)
        {
            return Bot.Engine.Push(pirate, target, obj);
        }
        public bool CanPush(SpaceObject target)
        {
            return CanPlay && pirate.CanPush(target);
        }

        public Squad GetEnemyShipsInPushRange()
        {
            return Bot.Engine.GetEnemyPiratesInRange(this, PushRange);
        }
        public Squad GetMyShipsInPushRange()
        {
            return Bot.Engine.GetMyShipsInRange(this, PushRange);
        }
        public LogicedPirate LogicPirate(PirateLogic logic)
        {
            return new LogicedPirate(this, logic);
        }
        #endregion Custom
    }
}
