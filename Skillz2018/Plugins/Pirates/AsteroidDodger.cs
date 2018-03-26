using Pirates;
using MyBot.Engine;
using System.Linq;
using MyBot.Engine.Delegates;
using MyBot.Engine.Handlers;

namespace MyBot.Plugins.Pirates
{
    public class AsteroidDodger : PiratePlugin
    {
        public static Location TangentPush(Asteroid ast, PirateShip ship)
        {
            return ast.GetLocation().Add(ship.GetLocation().Subtract(ast.GetLocation()).Multiply(-1));
        }

        private int DangerRange;
        private int HaltingRange;
        private System.Func<Asteroid, PirateShip, Location> PushLocator = (ast, ship) => TangentPush(ast, ship);
        private Delegates.FilterFunction<Asteroid> Filter = (ast) => true;

        public AsteroidDodger(int DangerRange = 3, int HaltingRange = 1)
        {
            this.DangerRange = DangerRange;
            this.HaltingRange = HaltingRange;
        }
        public AsteroidDodger(System.Func<Asteroid, PirateShip, Location> PushLocator, int DangerRange = 3, int HaltingRange = 1) : this(DangerRange, HaltingRange)
        {
            this.PushLocator = PushLocator;
        }
        public AsteroidDodger(System.Func<Asteroid, PirateShip, Location> PushLocator, Delegates.FilterFunction<Asteroid> Filter, int DangerRange = 3, int HaltingRange = 1) : this(PushLocator, DangerRange, HaltingRange)
        {
            this.Filter = Filter;
        }

        public bool DoTurn(PirateShip ship)
        {
            // Check if halt is needed
            Asteroid[] AsteroidsInRange = Bot.Engine.AllLivingAsteroids.Where(x => Bot.Engine.GetHits(x) == 0 && Filter(x) && x.Distance(ship) - x.Size < (ship.MaxSpeed + x.EffectiveSpeed()) * DangerRange).ToArray();
            if (ship.PushReloadTurns > 0 || AsteroidsInRange.IsEmpty())
                return false;
            Asteroid NearestAsteroid = AsteroidsInRange.Nearest(ship);
            if (ship.CanPush(NearestAsteroid))
                return ship.Push(NearestAsteroid, PushLocator(NearestAsteroid, ship));
            else if (!NearestAsteroid.IsHalted())
            {
                int EffectiveDistance = NearestAsteroid.Distance(ship) - NearestAsteroid.Size;
                if (EffectiveDistance < HaltingRange * (ship.MaxSpeed + NearestAsteroid.EffectiveSpeed()))
                {
                    if (EffectiveDistance < HaltingRange * NearestAsteroid.EffectiveSpeed())
                        return ship.Sail(NearestAsteroid.Add(NearestAsteroid.Direction.Normalized().Multiply(NearestAsteroid.EffectiveSpeed() + ship.PushRange)));
                    else
                        return true;
                }
                else
                    return false;
            }
            return false;
        }
    }
}
