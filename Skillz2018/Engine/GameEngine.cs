using System.Collections.Generic;
using System.Linq;
using Pirates;
using MyBot.Engine;
using MyBot.Engine.Handlers;
using MyBot.Geometry;
using MyBot.Engine.Delegates;


namespace MyBot.Engine
{
    public class GameEngine
    {
        public const bool UNSAFE_PIRATE_PLUGINS = false;
        public const bool UNSAFE_SQUAD_PLUGINS = true;

        public GameEngine()
        {
            random = new System.Random();
            Store = new DataStore();
        }

        public const string INDENT = " ";

        public int CAMP_REFRESH_RANGE { get; private set; } = 750;
        public bool CanStickBomb { get; private set; } = false;
        public readonly System.Random random;
        public Dictionary<int, int> HitList;
        public Dictionary<int, bool> MoveList;
        public Dictionary<int, Location> ExpectedLocationMap;
        internal DataStore Store { get; private set; }
        public PirateGame Game { get; private set; }
        public string StatusLog { get; private set; }
        public string ActionLog { get; private set; }
        public string DataStoreLog
        {
            get
            {
                return string.Join("\n", Store.Select(p => INDENT + "{" + p.Key + "," + p.Value + "}"));
            }
        }
        public void Update(PirateGame pg)
        {
            this.Game = pg;

            HitList = new Dictionary<int, int>();
            MoveList = new Dictionary<int, bool>();
            ExpectedLocationMap = new Dictionary<int, Location>();
            StatusLog = "";
            ActionLog = "";
            
            this.MyLivingPirates.ForEach(pirate =>
            {
                MarkOnList(pirate, true);
            });
            foreach (SpaceObject so in SpaceObjects)
            {
                if (so.InMap())
                    UpdateExpectedLocation(so, new Location(0, 0));
            }

            CanStickBomb = TurnsUntilStickyBomb <= 0;

            RefreshCampCheck();
            UpdateStatus();
        }
        private void UpdateStatus()
        {
            foreach (PirateShip pirate in MyPirates)
            {
                if (!pirate.Alive)
                {
                    StatusLog += INDENT + "@> Pirate #" + pirate.Id + " Is dead!\n";
                    continue;
                }
                StatusLog += INDENT + "@> Pirate #" + pirate.Id + "\n" + INDENT + " Location: " + pirate.Location.Serialize();
                if (pirate.PushReloadTurns > 0)
                    StatusLog += "\n" + INDENT + " Push reload turns: " + pirate.PushReloadTurns;
                else
                    StatusLog += "\n" + INDENT + " Pirate can push!";
                StatusLog += "\n";
            }
            StatusLog = StatusLog.Substring(0, StatusLog.Length - 1);
        }
        public void PrintDataStore()
        {
            Debug("Data Store: [\n" + DataStoreLog + "\n];\n\n");
        }
        public void PrintStatusLog()
        {
            Debug("Status: [\n" + StatusLog + "\n];\n\n");
        }
        public void PrintActionLog()
        {
            Debug("Action Log: [\n" + ActionLog + "\n];\n\n");
        }

        private void PreTurn()
        {
            
        }
        public void DoTurn(IndividualPirateHandler ph, bool RespectDataStoreAssignments = true)
        {
            PreTurn();

            LogicedPirate[] pirates = ph.AssignPirateLogic(MyLivingPirates.ToArray());
            for (int i = 0; i < pirates.Length; i++)
            {
                LogicedPirate pirate = pirates[i];
                pirate.DoTurn();
            }

            PostTurn();
        }
        public void DoTurn(SquadPirateHandler ph, bool RespectDataStoreAssignments = true)
        {
            PreTurn();

            List<LogicedPirateSquad> Squads = new List<LogicedPirateSquad>();
            Squad APirates = new Squad(MyLivingPirates);
            Squads.AddRange(ph.AssignSquads(APirates.Select(x => (PirateShip)x).ToArray()));
            foreach (LogicedPirateSquad lps in Squads)
            {
                if (lps.LogicedPirates.IsEmpty()) continue;
                lps.DoTurn();
            }

            PostTurn();
        }
        private void PostTurn()
        {
            FinalizeActionLog();
        }
        
        #region Extends
        /// <summary>
        /// My sticky bombs in play
        /// </summary>
        public StickyBomb[] MyStickyBombs
        {
            get
            {
                return AllStickyBombs.Where(x => x.Owner.Id == Self.Id).ToArray();
            }
        }
        /// <summary>
        /// Enemy sticky bombs in play
        /// </summary>
        public StickyBomb[] EnemyStickyBombs
        {
            get
            {
                return AllStickyBombs.Where(x => x.Owner.Id == Enemy.Id).ToArray();
            }
        }
        /// <summary>
        /// All sticky bombs in play (max 2)
        /// </summary>
        public StickyBomb[] AllStickyBombs
        {
            get
            {
                return Game.GetAllStickyBombs();
            }
        }
        /// <summary>
        /// The explosion range of the sticky bomb
        /// </summary>
        public int StickyBombExplosionRange
        {
            get
            {
                return Game.StickyBombExplosionRange;
            }
        }
        /// <summary>
        /// Amount of turns the sticky bomb ticks before exploding
        /// </summary>
        public int StickyBombCountdown
        {
            get
            {
                return Game.StickyBombCountdown;
            }
        }
        /// <summary>
        /// The range for a pirate to stick a bomb
        /// </summary>
        public int StickBombRange
        {
            get
            {
                return Game.StickBombRange;
            }
        }
        /// <summary>
        /// Turns to reload sticky bomb ability
        /// </summary>
        public int StickyBombReloadTurns
        {
            get
            {
                return Game.StickyBombReloadTurns;
            }
        }
        /// <summary>
        /// Turns until I can stick a bomb
        /// </summary>
        public int TurnsUntilStickyBomb
        {
            get
            {
                return Self.TurnsToStickyBomb;
            }
        }
        /// <summary>
        /// Turns until enemy can stick a bomb
        /// </summary>
        public int TurnsToEnemyStickyBomb
        {
            get
            {
                return Enemy.TurnsToStickyBomb;
            }
        }
        /// <summary>
        /// The state name of a heavy pirate
        /// </summary>
        public string HEAVY_STATE_NAME
        {
            get
            {
                return Game.STATE_NAME_HEAVY;
            }
        }
        /// <summary>
        /// The state name of a normal pirate
        /// </summary>
        public string NORMAL_STATE_NAME
        {
            get
            {
                return Game.STATE_NAME_NORMAL;
            }
        }
        /// <summary>
        /// All hostile space objects
        /// </summary>
        public SpaceObject[] HostileSpaceObjects
        {
            get
            {
                return EnemyLivingPirates.Select(x => (SpaceObject)((Pirate)x)).Concat(AllLivingAsteroids).ToArray();
            }
        }
        /// <summary>
        /// All pushable space objects
        /// </summary>
        public SpaceObject[] PushableSpaceObjects
        {
            get
            {
                return HostileSpaceObjects.Concat(AllWormholes).ToArray();
            }
        }
        /// <summary>
        /// All space objects
        /// </summary>
        public SpaceObject[] SpaceObjects
        {
            get
            {
                return (EnemyPirates.Select(x => (SpaceObject)((Pirate)x)).ToList().Concat(MyPirates.Select(x => (SpaceObject)((Pirate)x)).ToList()).Concat(AllAsteroids).ToList()).Concat(AllWormholes).ToArray();
            }
        }
        /// <summary>
        /// All currently active wormholes
        /// </summary>
        public Wormhole[] ActiveWormholes
        {
            get
            {
                return Game.GetActiveWormholes();
            }
        }
        /// <summary>
        /// All wormholes in play
        /// </summary>
        public Wormhole[] AllWormholes
        {
            get
            {
                return Game.GetAllWormholes();
            }
        }
        /// <summary>
        /// A collection of all asteroids in game
        /// </summary>
        public Asteroid[] AllAsteroids
        {
            get
            {
                return Game.GetAllAsteroids();
            }
        }
        /// <summary>
        /// A collection of all asteroids currently in play
        /// </summary>
        public Asteroid[] AllLivingAsteroids
        {
            get
            {
                return Game.GetLivingAsteroids();
            }
        }
        /// <summary>
        /// Pirates's maximum push distance
        /// </summary>
        public int PushDistance
        {
            get
            {
                return Game.PushDistance;
            }
        }
        public int HeavyPushDistance
        {
            get
            {
                return Game.HeavyPushDistance;
            }
        }
        /// <summary>
        /// Pirates's push range
        /// </summary>
        public int PushRange
        {
            get
            {
                return Game.PushRange;
            }
        }
        /// <summary>
        /// Capsule pickup range
        /// </summary>
        public int CapsulePickupRange
        {
            get
            {
                return Game.CapsulePickupRange;
            }
        }
        /// <summary>
        /// Turns until capsule spawns
        /// </summary>
        public int CapsuleSpawnTurns
        {
            get
            {
                return Game.CapsuleSpawnTurns;
            }
        }
        /// <summary>
        /// Number of columns in map
        /// </summary>
        public int Cols
        {
            get
            {
                return Game.Cols;
            }
        }
        /// <summary>
        /// Amount of points required to automatically win
        /// </summary>
        public int MaxPoints
        {
            get
            {
                return Game.MaxPoints;
            }
        }
        /// <summary>
        /// Maximum amount of turns playable
        /// </summary>
        public int MaxTurns
        {
            get
            {
                return Game.MaxTurns;
            }
        }
        /// <summary>
        /// Number of pushes required for a pirate to drop the capsule
        /// </summary>
        public int NumberOfPushesForCapsuleLoss
        {
            get
            {
                return Game.NumPushesForCapsuleLoss;
            }
        }
        /// <summary>
        /// Number of pushes required for a heavy pirate to drop the capsule
        /// </summary>
        public int NumberOfPushesForHeavyCapsuleLoss
        {
            get
            {
                return Game.HeavyNumPushesForCapsuleLoss;
            }
        }
        /// <summary>
        /// Number of turns it takes for the push ability to reload
        /// </summary>
        public int PushMaxReloadTurns
        {
            get
            {
                return Game.PushMaxReloadTurns;
            }
        }
        /// <summary>
        /// Amount of rows in the game map
        /// </summary>
        public int Rows
        {
            get
            {
                return Game.Rows;
            }
        }
        /// <summary>
        /// The middle of the map
        /// </summary>
        public Location Middle
        {
            get
            {
                return new Location(Rows / 2, Cols / 2);
            }
        }
        /// <summary>
        /// Current turn number
        /// </summary>
        public int Turn
        {
            get
            {
                return Game.Turn;
            }
        }
        /// <summary>
        /// All capsules in play
        /// </summary>
        public Capsule[] AllCapsules
        {
            get
            {
                return Game.GetAllCapsules();
            }
        }
        /// <summary>
        /// A squad of all enemy pirates
        /// </summary>
        public Squad EnemyPirates
        {
            get
            {
                return new Squad(Game.GetAllEnemyPirates().Select(x => (PirateShip)x));
            }
        }
        /// <summary>
        /// All motherships in play
        /// </summary>
        public Mothership[] AllMotherships
        {
            get
            {
                return Game.GetAllMotherships();
            }
        }
        /// <summary>
        /// A squad of all our pirates
        /// </summary>
        public Squad MyPirates
        {
            get
            {
                return new Squad(Game.GetAllMyPirates().Select(x => (PirateShip)x));
            }
        }
        /// <summary>
        /// All players in play
        /// </summary>
        public Player[] Players
        {
            get
            {
                return Game.GetAllPlayers();
            }
        }
        /// <summary>
        /// Player object of ourselves
        /// </summary>
        public Player Self
        {
            get
            {
                return Game.GetMyself();
            }
        }
        /// <summary>
        /// Player object of the enemy
        /// </summary>
        public Player Enemy
        {
            get
            {
                return Game.GetEnemy();
            }
        }
        /// <summary>
        /// All enemy capsules
        /// </summary>
        public Capsule[] EnemyCapsules
        {
            get
            {
                return Game.GetEnemyCapsules().OrderBy(x => x.Id).ToArray();
            }
        }
        /// <summary>
        /// Enemy capsules in play
        /// </summary>
        public Capsule[] EnemyLivingCapsules
        {
            get
            {
                return EnemyCapsules.Where(x => x.IsAlive()).ToArray();
            }
        }
        /// <summary>
        /// A squad of all living enemy pirates
        /// </summary>
        public Squad EnemyLivingPirates
        {
            get
            {
                return new Squad(Game.GetEnemyLivingPirates().Select(x => (PirateShip)x));
            }
        }
        /// <summary>
        /// Enemy mothership object
        /// </summary>
        public Mothership[] EnemyMotherships
        {
            get
            {
                return Game.GetEnemyMotherships();
            }
        }
        /// <summary>
        /// Maximal amount of time a turn can last
        /// </summary>
        public int MaxTurnTime
        {
            get
            {
                return Game.GetMaxTurnTime();
            }
        }
        /// <summary>
        /// All our capsules
        /// </summary>
        public Capsule[] MyCapsules
        {
            get
            {
                return Game.GetMyCapsules().OrderBy(x => x.Id).ToArray();
            }
        }
        /// <summary>
        /// Our capsules in play
        /// </summary>
        public Capsule[] MyLivingCapsules
        {
            get
            {
                return MyCapsules.Where(x => x.IsAlive()).ToArray();
            }
        }
        /// <summary>
        /// A squad of all our living pirates
        /// </summary>
        public Squad MyLivingPirates
        {
            get
            {
                return new Squad(Game.GetMyLivingPirates().Select(x => (PirateShip)x));
            }
        }
        /// <summary>
        /// Our mothership object
        /// </summary>
        public Mothership[] MyMotherships
        {
            get
            {
                return Game.GetMyMotherships();
            }
        }
        /// <summary>
        /// Amount of time remainig to play current turn
        /// </summary>
        public int TimeRemaining
        {
            get
            {
                return Game.GetTimeRemaining();
            }
        }
        /// <summary>
        /// Retrevies an enemy pirate
        /// </summary>
        /// <param name="id">Pirate's id</param>
        /// <returns>An object of the enemy pirates</returns>
        public PirateShip GetEnemyPirateById(int id)
        {
            return EnemyPirates.First(x => x.Id == id);
        }
        /// <summary>
        /// The enemy's score
        /// </summary>
        public int EnemyScore
        {
            get
            {
                return Enemy.Score;
            }
        }
        /// <summary>
        /// Our score
        /// </summary>
        public int MyScore
        {
            get
            {
                return Self.Score;
            }
        }
        /// <summary>
        /// Retrevies a friendly pirate
        /// </summary>
        /// <param name="id">Pirate's id</param>
        /// <returns>An object of the friendly pirates</returns>
        public PirateShip GetMyPirateById(int id)
        {
            return (PirateShip)Game.GetMyPirateById(id);
        }
        /// <summary>
        /// Pirates's maximum speed
        /// </summary>
        public int MaxPirateSpeed
        {
            get
            {
                return Game.PirateMaxSpeed;
            }
        }
        /// <summary>
        /// Heavy pirate's max speed
        /// </summary>
        public int MaxHeavyPirateSpeed
        {
            get
            {
                return Game.HeavyMaxSpeed;
            }
        }
        /// <summary>
        /// Prints a message to the game console
        /// </summary>
        /// <param name="arg">Messsage to print</param>
        public void Debug(object arg)
        {
            Game.Debug(arg);
        }
        #endregion Extends
        #region Camp Check
        private void RefreshCampCheck()
        {
            EnemyLivingPirates.ForEach(ps =>
            {
                string key = "[EnemyPirate=" + ps.Id + "]";
                Tuple<Location, Location, int> data = Store.GetValue<Tuple<Location, Location, int>>(key, new Tuple<Location, Location, int>(ps.Location, new Location(0, 0), 0), prefix: DataStore.CAMPER);
                Location delta = data.arg0.Subtract(ps.Location).Add(data.arg1);
                if (delta.Normal() < CAMP_REFRESH_RANGE)
                    data = new Tuple<Location, Location, int>(ps.Location, delta, data.arg2 + 1);
                else
                    data = new Tuple<Location, Location, int>(ps.Location, new Location(0, 0), 0);
                Store.SetValue(key, data, prefix: DataStore.CAMPER);
            });
        }
        private Location GetMovementDelta(Pirate ps)
        {
            string key = "[EnemyPirate=" + ps.Id + "]";
            Tuple<Location, Location, int> data = Store.GetValue<Tuple<Location, Location, int>>(key, new Tuple<Location, Location, int>(ps.Location, new Location(0, 0), 0), prefix: DataStore.CAMPER);
            return data.arg1;
        }
        /// <summary>
        /// Checks amount of time a certain pirate has been camping
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public int GetCampLength(Pirate ps)
        {
            string key = "[EnemyPirate=" + ps.Id + "]";
            Tuple<Location, Location, int> data = Store.GetValue<Tuple<Location, Location, int>>(key, new Tuple<Location, Location, int>(ps.Location, new Location(0, 0), 0), prefix: DataStore.CAMPER);
            return data.arg2;
        }
        /// <summary>
        /// Checks if a camper is registered in a certain zone
        /// </summary>
        /// <param name="loc">Circle's middle</param>
        /// <param name="radius">Radius of circle to check camping zone</param>
        /// <param name="campers">Returns the pirates camping</param>
        /// <param name="minTurns">Amount of turns required to be considered a camper</param>
        /// <returns>Boolean indicating if any pirates are camping</returns>
        public bool CheckForCamper(MapObject loc, int radius, out PirateShip[] campers, int minTurns = 3)
        {
            campers = GetEnemyPiratesInRange(loc, radius).Where(p => GetCampLength((Pirate)p) >= minTurns).ToArray();
            if (campers.IsEmpty())
                return false;
            return true;
        }
        /// <summary>
        /// Checks if a camper is registered
        /// </summary>
        /// <param name="minTurns">Amount of turns required to be considered a camper</param>
        /// <param name="campers">Returns the pirates camping</param>
        /// <returns>Boolean indicating if any pirates are camping</returns>
        public bool CheckForCamper(int minTurns, out PirateShip[] campers)
        {
            return CheckForCamper(new Location(0, 0), Rows * Rows + Cols * Cols, out campers, minTurns);
        }
        /// <summary>
        /// Checks if a camper is registered in a certain zone
        /// </summary>
        /// <param name="loc">Circle's middle</param>
        /// <param name="radius">Radius of circle to check camping zone</param>
        /// <param name="minTurns">Amount of turns required to be considered a camper</param>
        /// <returns>Boolean indicating if any pirates are camping</returns>
        public bool ExistsCamper(MapObject loc, int radius, int minTurns)
        {
            PirateShip[] tmp;
            return CheckForCamper(loc, radius, out tmp, minTurns);
        }
        /// <summary>
        /// Checks if a camper is registered
        /// </summary>
        /// <param name="minTurns">Amount of turns required to be considered a camper</param>
        /// <returns>Boolean indicating if any pirates are camping</returns>
        public bool ExistsCamper(int minTurns)
        {
            return ExistsCamper(new Location(0, 0), Rows * Rows + Cols * Cols, minTurns);
        }
        /// <summary>
        /// Counts an amount of campers in a certain zone
        /// </summary>
        /// <param name="loc">Circle's middle</param>
        /// <param name="radius">Radius of circle to check camping zone</param>
        /// <param name="minTurns">Amount of turns required to be considered a camper</param>
        /// <returns>The amount of pirates camping the location</returns>
        public int CountCampers(Location loc, int radius, int minTurns = 3)
        {
            PirateShip[] campers;
            CheckForCamper(loc, radius, out campers, minTurns: minTurns);
            return campers.Length;
        }
        #endregion Camp Check
        #region Game state approximations
        private void AppendToHitlist(SpaceObject obj)
        {
            HitList[obj.UniqueId] = GetHits(obj) + 1;
        }
        /// <summary>
        /// Checks if a pirate is expected to live the next turn (with a given error tolerance)
        /// </summary>
        /// <param name="pirate">Pirate in question</param>
        /// <param name="offset">Error offset</param>
        /// <returns>Boolean indicating if the pirate is expected to live at the end of the turn</returns>
        public bool IsAliveAfterTurn(Pirate pirate, int offset = 0)
        {
            return System.Math.Abs(GetExpectedLocation(pirate).DistanceFromBorder()) > offset;
        }
        /// <summary>
        /// Returns the number of hits on a specific pirate (Friendly hits only)
        /// </summary>
        /// <param name="pirate">Pirate in question</param>
        /// <returns>Number of friendly hits</returns>
        public int GetHits(SpaceObject obj)
        {
            int value;
            if (HitList.TryGetValue(obj.UniqueId, out value))
                return value;
            else
            {
                HitList.Add(obj.UniqueId, 0);
                return 0;
            }
        }

        /// <summary>
        /// Calculates how many pirates can attack the given location
        /// </summary>
        /// <param name="obj">Object to attack</param>
        /// <returns>Amount of pirates</returns>
        public int GetPushesAgainst(SpaceObject obj)
        {
            return GetPushesAgainst(MyLivingPirates, obj);
        }
        /// <summary>
        /// Calculates how many pirates can attack the given location
        /// </summary>
        /// <param name="obj">Object to attack</param>
        /// <param name="pirates">Subset of pirates from which to calculate</param>
        /// <returns>Amount of pirates whom can attack</returns>
        public int GetPushesAgainst(IEnumerable<PirateShip> pirates, SpaceObject obj)
        {
            return pirates.Count(x => x.CanPush(obj));
        }
        /// <summary>
        /// Calculates how many pirates can attack the given location
        /// </summary>
        /// <param name="pirates">Subset of pirates from which to calculate</param>
        /// <returns>Amount of pirates whom can attack</returns>
        public int GetPushesAgainst(IEnumerable<PirateShip> pirates)
        {
            return pirates.Count(x => x.PushReloadTurns <= 0);
        }

        /// <summary>
        /// Calculates how far you can push the given location
        /// </summary>
        /// <param name="obj">Object to attack</param>
        /// <returns>Distance pirate's can push to</returns>
        public int GetDistanceAgainst(SpaceObject obj)
        {
            return GetDistanceAgainst(MyLivingPirates, obj);
        }
        /// <summary>
        /// Calculates how far you can push the given location
        /// </summary>
        /// <param name="obj">Object to attack</param>
        /// <param name="pirates">Subset of pirates from which to calculate</param>
        /// <returns>Distance pirate's can push to</returns>
        public int GetDistanceAgainst(IEnumerable<PirateShip> pirates, SpaceObject obj)
        {
            return pirates.Sum(x => x.CanPush(obj) ? x.PushDistance : 0);
        }
        /// <summary>
        /// Calculates how far you can push the given location
        /// </summary>
        /// <param name="obj">Object to attack</param>
        /// <param name="pirates">Subset of pirates from which to calculate</param>
        /// <returns>Distance pirate's can push to</returns>
        public int GetAvilableDistance(IEnumerable<PirateShip> pirates, SpaceObject obj)
        {
            return pirates.Sum(x => x.CanPush(obj) ? x.PushDistance : 0);
        }

        /// <summary>
        /// Checks if the pirates can kill the target
        /// </summary>
        /// <param name="attackers">Attacker's pirate squad</param>
        /// <param name="target">Object to attempt killing</param>
        /// <returns>Boolean indicating if the pirates can kill the target</returns>
        public bool CanKill(Squad attackers, SpaceObject target)
        {
            return GetDistanceAgainst(attackers, target) > target.DistanceFromBorder();
        }
        /// <summary>
        /// Estimates if the pirates can kill a pirate located at a target location
        /// </summary>
        /// <param name="attackers">Attacker's pirate squad</param>
        /// <param name="target">Assumed enemy position</param>
        /// <returns>Boolean indicating if the pirates can kill the target</returns>
        public bool CanKillEstimated(Squad attackers, MapObject target)
        {
            return attackers.Sum(x => (x.PushReloadTurns <= 0 && x.InPushRange(target) && x.CanPlay) ? x.PushDistance : 0) > target.DistanceFromBorder();
        }

        private void MarkOnList(Pirate pirate, bool state = false)
        {
            MoveList[pirate.Id] = state;
        }
        private void UpdateExpectedLocation(SpaceObject obj, Location changeVector)
        {
            if (ExpectedLocationMap.ContainsKey(obj.UniqueId))
            {
                ExpectedLocationMap[obj.UniqueId] = ExpectedLocationMap[obj.UniqueId].Add(changeVector);
            }
            else
            {
                ExpectedLocationMap[obj.UniqueId] = obj.GetLocation().Add(changeVector);
            }
        }
        /// <summary>
        /// Get the expected locaton of a space object (all actions we have done)
        /// </summary>
        /// <param name="obj">Space object</param>
        /// <returns>Presumed location at end of turn</returns>
        public Location GetExpectedLocation(SpaceObject obj)
        {
            return ExpectedLocationMap[obj.UniqueId];
        }
        /// <summary>
        /// Checks whether the pirate can play a move currently
        /// </summary>
        /// <param name="pirate">Pirate whom to play with</param>
        /// <returns>Boolean indicating if pirate can play</returns>
        public bool CanPlay(Pirate pirate)
        {
            if (MoveList.ContainsKey(pirate.Id))
                return MoveList[pirate.Id];
            else
                return false;
        }
        #endregion Game state approximations
        #region Game Interactions
        /// <summary>
        /// A default pushing function
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attackers"></param>
        /// <param name="PushForCapsule"></param>
        /// <param name="WormholeSafetyMultiplier"></param>
        /// <returns></returns>
        public PushMapping DefaultPush(SpaceObject obj, Squad attackers, bool PushForCapsule = false, int WormholeSafetyMultiplier = 2)
        {
            if (obj is Pirate p)
            {
                if (CanKill(attackers, obj))
                    return PushMapping.ByDistance(attackers, obj.DistanceFromBorder(), obj.ClosestBorder());
                else if (p.HasCapsule() && PushForCapsule)
                {
                    int av = Bot.Engine.GetPushesAgainst(attackers, obj);
                    if (av >= NumberOfPushesForHeavyCapsuleLoss)
                        return PushMapping.ByNumPushes(attackers, NumberOfPushesForHeavyCapsuleLoss, obj.ClosestBorder());
                    else
                        return PushMapping.ByNumPushes(attackers, p.NumPushesForCapsuleLoss, obj.ClosestBorder());
                }
            }
            else if (obj is Wormhole)
            {
                Wormhole w = obj as Wormhole;
                if (MyMotherships.Length > 0 && MyCapsules.Length > 0)
                {
                    #region WormholeDefaultPush
                    Mothership NearestMothership = MyMotherships.Nearest(w);
                    Capsule NearestMine = MyCapsules.FirstBy(x => x.InitialLocation.Distance(w));
                    Mothership PartnerNearestMothership = MyMotherships.Nearest(w.Partner);
                    Capsule PartnerNearestMine = MyCapsules.FirstBy(x => x.InitialLocation.Distance(w.Partner));

                    int Opt1 = ScoreWormholePath(NearestMine.InitialLocation, PartnerNearestMothership, w);
                    int Opt2 = ScoreWormholePath(PartnerNearestMine.InitialLocation, NearestMothership, w.Partner);

                    if (Opt1 < Opt2 || (Opt1 == Opt2 && w.UniqueId < w.Partner.UniqueId))
                        return PushMapping.To(attackers, obj, (Location)(((Point)NearestMine.InitialLocation).InDirection(NearestMothership, -(NearestMine.PickupRange + w.WormholeRange * WormholeSafetyMultiplier))));
                    else if (Opt1 > Opt2)
                        return PushMapping.To(attackers, obj, NearestMothership);
                    #endregion WormholeDefaultPush
                }
                else
                    return new PushMapping();
            }
            return new PushMapping();
        }
        /// <summary>
        /// Sticks a sticky bomb on a space object
        /// </summary>
        /// <param name="o">Bomber</param>
        /// <param name="d">Bombee</param>
        /// <returns>A boolean indicating wether the bombing was completed successfuly</returns>
        public bool StickBomb(PirateShip o, SpaceObject d)
        {
            if (CanPlay(o) && CanStickBomb)
            {
                AppendAction("$> Pirate #" + o.Id + " Is sticking a bomb");
                ((Pirate)o).StickBomb(d);
                MarkOnList(o);
                CanStickBomb = false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Swaps between the state of two pirates
        /// </summary>
        /// <param name="o">Pirate 1</param>
        /// <param name="d">Pirate 2</param>
        /// <returns>A boolean indicating wether the swap was completed successfuly</returns>
        public bool SwapStates(PirateShip o, PirateShip d)
        {
            if (CanPlay(o))
            {
                AppendAction("$> Pirate #" + o.Id + " & Pirate #" + d.Id + " are swapping...");
                ((Pirate)o).SwapStates(d);
                MarkOnList(o);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Attempts pushing a pirate
        /// </summary>
        /// <param name="pirate">Pusher pirate</param>
        /// <param name="targetObject">Pushee target</param>
        /// <param name="location">Location to push to</param>
        /// <returns>Boolean indicating if the push was successful</returns>
        public bool Push(Pirate pirate, SpaceObject targetObject, MapObject location)
        {
            string Message = "";
            if (targetObject is Pirate)
            {
                Pirate target = (Pirate)targetObject;
                string friendlyness = ((PirateShip)target).IsOurs ? "friendly" : "enemy";
                Message = "Pirate #" + pirate.Id + " Pushing " + friendlyness + " pirate #" + target.Id;
            }
            else if (targetObject is Asteroid)
                Message = "Pirate # " + pirate.Id + " Pushing an asteroid";
            else if (targetObject is Wormhole)
                Message = "Pirate # " + pirate.Id + " Pushing a wormhole!";
            if (CanPlay(pirate) && pirate.CanPush(targetObject))
            {
                pirate.Push(targetObject, location);
                MarkOnList(pirate);
                AppendToHitlist(targetObject);
                Location delta = location.GetLocation().Subtract(targetObject.Location);
                if (delta.Normal() > pirate.PushDistance)
                    UpdateExpectedLocation(targetObject, delta.Multiply(((double)pirate.PushDistance) / delta.Normal()));
                AppendAction("$> " + Message);
                return true;
            }
            if (!CanPlay(pirate))
                AppendAction("![No More Moves]> " + Message);
            if (pirate.PushReloadTurns > 0)
                AppendAction("![Push Reloading]> " + Message);
            else if (!pirate.InPushRange(targetObject))
                AppendAction("![Not In Range]> " + Message);
            else
                AppendAction("![Can't Push]> " + Message);

            return false;
        }
        /// <summary>
        /// Attempts sailing 
        /// </summary>
        /// <param name="pirate">The pirate whom to move</param>
        /// <param name="loc">The location to sail to</param>
        /// <returns>Boolean indicating if the sail was successful</returns>
        public bool Sail(Pirate pirate, MapObject loc)
        {
            if (pirate.Distance(loc) <= 0)
            {
                AppendAction("$> Pirate #" + pirate.Id + " attempted sailing to itself");
                return false;
            }
            if (CanPlay(pirate) && loc.InMap())
            {
                pirate.Sail(loc);
                MarkOnList(pirate);
                Location delta = loc.GetLocation().Subtract(pirate.Location);
                if (delta.Normal() > pirate.MaxSpeed)
                    UpdateExpectedLocation(pirate, delta.Multiply(((double)pirate.MaxSpeed) / delta.Normal()));
                else
                    UpdateExpectedLocation(pirate, delta);
                AppendAction("$> Pirate #" + pirate.Id + " Sailing torwards " + loc.Serialize());
                return true;
            }
            if (!CanPlay(pirate))
                AppendAction("![No More Moves]> Pirate # " + pirate.Id + " Attempted sailing to " + loc.Serialize());
            else if (!loc.InMap())
                AppendAction("![Invalid sail]> Pirate # " + pirate.Id + " Attempted sailing to " + loc.Serialize());
            return false;
        }
        #endregion Game Interactions
        #region Utility Methods 
        /// <summary>
        /// Calculates the number of turns for a pirate to reach a location
        /// </summary>
        /// <param name="origin">Original location</param>
        /// <param name="dest">Destination</param>
        /// <param name="speed">Movement speed</param>
        /// <returns>Number of turns to reach destination from origin</returns>
        public int TurnsToReach(MapObject origin, MapObject dest, int speed)
        {
            return (int)System.Math.Ceiling(((double)origin.Distance(dest)) / speed);
        }
        /// <summary>
        /// Calculates a path avoiding the obstructions (selects first obstruction)
        /// </summary>
        /// <typeparam name="T">Obstruction type</typeparam>
        /// <param name="line">Original line</param>
        /// <param name="dist">Avoiding distance (dent size)</param>
        /// <param name="Obstructions">A collection of objects we wish to avoid</param>
        /// <returns>A path avoiding the closest obstacle</returns>
        public Path AvoidingPath<T>(Line line, int dist, T Obstruction) where T : MapObject
        {
            if (Obstruction == null)
                return (Path)line;
            Path[] paths = line.IndentTriangle(Obstruction, dist);
            Path path = paths.Where(x => x.Points.All(y => ((Location)y).InMap())).FirstOrDefault();
            if (path == null)
                return (Path)line;
            return path;
        }
        /// <summary>
        /// Calculates all possible sail options
        /// </summary>
        /// <param name="BaseLocation">The origin</param>
        /// <param name="radius">Movement radius</param>
        /// <param name="destination">The destination to sail to</param>
        /// <param name="nearer">Calculate only options which get us closer to the destinatio</param>
        /// <returns>A list of all possible locations to sail to</returns>
        public List<Location> GetAllSailOptions(MapObject BaseLocation, int radius, MapObject destination, bool nearer = true)
        {
            List<Location> pl = new List<Location>();
            for (int c = -radius; c <= radius; c++)
            {
                int rm = (radius - System.Math.Abs(c));
                for (int r = -rm; r <= rm; r++)
                {
                    if (!nearer || BaseLocation.Distance(destination) > BaseLocation.Add(r, c).Distance(destination))
                        pl.Add(BaseLocation.Add(r, c));
                }
            }
            return pl;
        }
        #endregion Utility Methods
        #region Wormhole teleportation
        /// <summary>
        /// Recursively scores a position (internal use for wormholes)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dest"></param>
        /// <param name="speed"></param>
        /// <param name="depth"></param>
        /// <param name="maxDepth"></param>
        /// <param name="dangerMultiplier"></param>
        /// <param name="baseCost"></param>
        /// <returns></returns>
        public Tuple<double, Location> ScorePosition(MapObject origin, MapObject dest, int speed, int depth = 1, int maxDepth = -1, double dangerMultiplier = 0.25, double baseCost = 0.1)
        {
            if (maxDepth < 1)
                maxDepth = Bot.Engine.AllWormholes.Length;

            if (depth > maxDepth)
                return new Tuple<double, Location>(origin.Distance(dest), dest.GetLocation());

            List<Tuple<double, Location>> results = Bot.Engine.AllWormholes.Select(x =>
            {
                double TurnDifference = (int)System.Math.Max(0, ((double)(Bot.Engine.EnemyLivingPirates.Select(y => y.TurnsToReach(x)).FirstOr(0) - Bot.Engine.TurnsToReach(origin, x, speed))));
                double a = 1 + baseCost + (TurnDifference > 0 ? System.Math.Pow(dangerMultiplier, TurnDifference) : 0);
                double b = ScorePosition(x.Partner, dest, depth + 1, maxDepth, speed, dangerMultiplier, baseCost).arg0 + origin.Distance(x) + x.TurnsToReactivate * speed;
                return new Tuple<double, Location>(a * b + 1, x.Location);
            }).ToList();
        
            results.Add(new Tuple<double, Location>(origin.Distance(dest), dest.GetLocation()));
            return results.FirstBy(x => x.arg0);
        }
        /// <summary>
        /// Scores a path of wormholes (calculates required distance)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="wormholes"></param>
        /// <returns></returns>
        public int ScoreWormholePath(MapObject origin, MapObject destination, IEnumerable<Wormhole> wormholes)
        {
            return ScoreWormholePath(origin, destination, wormholes.ToArray());
        }
        /// <summary>
        /// Scores a path of wormholes (calculates required distance)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="wormholes"></param>
        /// <returns></returns>
        public int ScoreWormholePath(MapObject origin, MapObject destination, params Wormhole[] wormholes)
        {
            int Distance = origin.Distance(wormholes.First());
            for (int i = 0; i < wormholes.Count() - 1; i++)
            {
                Distance += wormholes[i].Partner.Distance(wormholes[i + 1]);
            }
            Distance += destination.Distance(wormholes.Last().Partner);
            return Distance;
        }
        #endregion Wormhole teleportation
        #region Custom 
        /// <summary>
        /// Retreives enemy pirates in a certain distance from the location
        /// </summary>
        /// <param name="loc">Location to find pirates near to</param>
        /// <param name="range">Maximum distance from the given location</param>
        /// <returns>All enemy pirates within range from loc</returns>
        public Squad GetEnemyPiratesInRange(MapObject loc, int range)
        {
            return EnemyLivingPirates.Filter(x => x.InRange(loc, range));
        }
        /// <summary>
        /// Retreives enemy pirates in a certain distance from the location filtered by a predetermined filter
        /// </summary>
        /// <param name="loc">Location to find pirates near to</param>
        /// <param name="range">Maximum distance from the given location</param>
        /// <param name="Filter">Filter which to filter by</param>
        /// <returns>All enemy pirates within range from loc satisfying the filter predicate</returns>
        public Squad GetEnemyPiratesInRange(MapObject loc, int range, Delegates.Delegates.FilterFunction<PirateShip> Filter)
        {
            return EnemyLivingPirates.Filter(x => x.InRange(loc, range) && Filter(x));
        }
        /// <summary>
        /// Retreives friendly pirates in a certain distance from the location
        /// </summary>
        /// <param name="loc">Location to find pirates near to</param>
        /// <param name="range">Maximum distance from the given location</param>
        /// <returns>All friendly pirates within range from loc</returns>
        public Squad GetMyShipsInRange(MapObject loc, int range)
        {
            return MyLivingPirates.Filter(x => x.InRange(loc, range));
        }
        public void AppendAction(string action)
        {
            ActionLog += INDENT + action + "\n";
        }
        private void FinalizeActionLog()
        {
            if (ActionLog.Length == 0)
                return;
            ActionLog = ActionLog.Substring(0, ActionLog.Length - 1);
        }
        #endregion Custom
    }
    public static class EngineExtensions
    {
        /// <summary>
        /// Casts a collection of pirates to a squad
        /// </summary>
        /// <param name="pirates">Pirate collection</param>
        /// <returns>Squad object</returns>
        public static Squad Upgrade(this IEnumerable<Pirate> pirates)
        {
            return new Squad(pirates.Select(p => (PirateShip)p));
        }
        /// <summary>
        /// Returns whether the capsule is held by some pirate
        /// </summary>
        /// <param name="cap">Capsule in question</param>
        /// <returns>Boolean indicating if the capsule is held</returns>
        public static bool IsHeld(this Capsule cap)
        {
            return cap.Holder != null;
        }

        /// <summary>
        /// Calculates the most effective push to the border
        /// </summary>
        /// <param name="obj">The game object to push</param>
        /// <returns>The best location to push the pirate to</returns>
        public static Location ClosestBorder(this MapObject obj)
        {
            Location org = obj.GetLocation();
            Location[] locs = new Location[] { new Location(org.Row, -1), new Location(org.Row, Bot.Engine.Cols + 1), new Location(-1, org.Col), new Location(Bot.Engine.Rows + 1, org.Col) };

            return locs.FirstBy(x => x.Distance(org));
        }
        /// <summary>
        /// Calculates the distance from the border
        /// </summary>
        /// <param name="obj">The game object to calculates it's distance</param>
        /// <returns>The distance of the object from the nearest border</returns>
        public static int DistanceFromBorder(this MapObject obj)
        {
            if (!obj.InMap())
                return 0;
            return System.Math.Min(System.Math.Min(Bot.Engine.Rows - obj.GetLocation().Row, obj.GetLocation().Row), System.Math.Min(Bot.Engine.Cols - obj.GetLocation().Col, obj.GetLocation().Col));
        }
        /// <summary>
        /// Checks if a location is between two other locations
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="bound1"></param>
        /// <param name="bound2"></param>
        /// <returns></returns>
        public static bool IsBetween(this MapObject loc, MapObject bound1, MapObject bound2)
        {
            return loc.GetLocation().Row.IsBetween(bound1.GetLocation().Row, bound2.GetLocation().Row) && loc.GetLocation().Col.IsBetween(bound1.GetLocation().Col, bound2.GetLocation().Col);
        }
        /// <summary>
        /// Converts a location to string
        /// </summary>
        /// <param name="loc">Location</param>
        /// <returns>(Row,Col)</returns>
        public static string Serialize(this MapObject loc)
        {
            return "(" + loc.GetLocation().Row + "," + loc.GetLocation().Col + ")";
        }
        /// <summary>
        /// Adds two locations
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns>loc + (X,Y)</returns>
        public static Location Add(this MapObject loc, int X, int Y)
        {
            return new Location(loc.GetLocation().Row + X, loc.GetLocation().Col + Y);
        }
        /// <summary>
        /// Adds two locations
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="loc2"></param>
        /// <returns>loc + (X,Y)</returns>
        public static Location Add(this MapObject loc, MapObject loc2)
        {
            return loc.GetLocation().Add(loc2.GetLocation());
        }
        /// <summary>
        /// Multiples a location by a constant
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="multiplier"></param>
        /// <returns>loc*multiplier</returns>
        public static Location Multiply(this MapObject loc, int multiplier)
        {
            return new Location(loc.GetLocation().Row * multiplier, loc.GetLocation().Col * multiplier);
        }
        /// <summary>
        /// Divides a location by a constant
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="divisor"></param>
        /// <returns>loc/divisor</returns>
        public static Location Divide(this MapObject loc, int divisor)
        {
            return new Location(loc.GetLocation().Row / divisor, loc.GetLocation().Col / divisor);
        }
        /// <summary>
        /// Returns the normal of the specified location vector
        /// </summary>
        /// <param name="loc">Location vector</param>
        /// <returns>|loc|</returns>
        public static double Normal(this MapObject loc)
        {
            return System.Math.Sqrt(loc.GetLocation().Col.Power(2) + loc.GetLocation().Row.Power(2));
        }
        public static Location Normalized(this MapObject loc)
        {
            return loc.Divide(System.Math.Max((int)loc.Normal(), 1));
        }

        public static bool IsHalted(this Asteroid ast)
        {
            return ast.Direction.Equals(new Location(0, 0));
        }
        public static int EffectiveSpeed(this Asteroid ast)
        {
            if (ast.IsHalted())
                return 0;
            else
                return ast.Speed;
        }
    }
}
