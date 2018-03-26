using Pirates;
using MyBot.Engine;
using System.Linq;
using MyBot.Engine.Delegates;
using MyBot.Engine.Handlers;


namespace MyBot.Plugins.Pirates
{
    public class Bomber : PiratePlugin
    {
        private Delegates.ScoringFunction<SpaceObject> bombingScorer;
        private Delegates.ScoringFunction<PirateShip> selfBombingScorer;
        public Bomber(Delegates.ScoringFunction<SpaceObject> bombingScorer,
            Delegates.ScoringFunction<PirateShip> selfBombingScorer)
        {
            this.bombingScorer = bombingScorer;
            this.selfBombingScorer = selfBombingScorer;
        }

        public Bomber()
        {
            this.bombingScorer = x => 0;
            this.selfBombingScorer = x => 0;
        }
        delegate void ScoringAction(Location loc, int score);
        public bool DoTurn(PirateShip ship)
        {
            Delegates.ScoringFunction<SpaceObject> scorer = x =>
                x.UniqueId == ship.UniqueId ? selfBombingScorer((PirateShip) ((Pirate) x)) : bombingScorer(x);
            if (ship.StickyBombs.Length > 0)
            {
                /*
                int remaining = ship.StickyBombs.Select(x => x.Countdown + 1).Min();
                ship.Sail(MaximalScore(ship, ship.MaxSpeed * remaining).arg0);
                */
                Location direction = Bot.Engine.MyLivingPirates.Where(x => x.StickyBombs.Length <= 0).Select(x => x.Location.Subtract(ship.Location).Multiply(-1)).Concat(
                    Bot.Engine.EnemyLivingPirates.Where(x => x.StickyBombs.Length <= 0).Select(x => x.Location.Subtract(ship.Location))).Middle();
                ship.Sail(ship.Location.Add(direction));
                return true;
            }
            else
            {
                SpaceObject[] bombable = Bot.Engine.PushableSpaceObjects.Concat(new SpaceObject[] {ship})
                    .Where(x => ship.CanStickBomb(x)).ToArray();
                SpaceObject max = bombable.FirstBy(x => -scorer(x));
                if (scorer(max) > 0 && Bot.Engine.CanStickBomb)
                {
                    ship.StickBomb(max);
                    return true;
                }
                return false;
            }
        }

        private static double[,] ScoreRange(MapObject obj, int range)
        {
            Location loc = obj.GetLocation();
            double[,] scores = new double[range * 2 + 1, range * 2 + 1];

            ScoringAction currentAction = (x, s) =>
            {
                if (x.InRange(obj, range))
                {
                    int nr = range + (loc.Row - x.Row);
                    int nc = range + (loc.Col - x.Col);
                    Location mid = new Location(nr, nc);

                    for (int ra = -Bot.Engine.StickyBombExplosionRange; ra <= Bot.Engine.StickyBombExplosionRange; ra++)
                    {
                        for (int ca = -Bot.Engine.StickyBombExplosionRange;
                            ca <= Bot.Engine.StickyBombExplosionRange;
                            ca++)
                        {
                            if ((nr + ra).IsBetween(0, range * 2) && (nc + ca).IsBetween(0, range * 2) && new Location(nr + ra, nc + ca).InRange(mid, Bot.Engine.StickyBombExplosionRange))
                                scores[nr + ra, nc + ca] -= s;
                        }
                    }
                }
            };

            Bot.Engine.MyLivingPirates.ForEach(x => currentAction(x.Location, -1));
            Bot.Engine.EnemyLivingPirates.ForEach(x => currentAction(x.Location, 1));

            return scores;
        }

        public static double ScoreLocation(MapObject obj)
        {
            Location loc = obj.GetLocation();
            double score = 0;

            ScoringAction currentAction = (x, s) =>
            {
                if (x.InRange(obj, Bot.Engine.StickyBombExplosionRange))
                {
                    score += s;
                }
            };

            Bot.Engine.MyLivingPirates.ForEach(x => currentAction(x.Location, -1));
            Bot.Engine.EnemyLivingPirates.ForEach(x => currentAction(x.Location, 1));

            return score;
        }

        private static Tuple<Location, double> MaximalScore(MapObject obj, int range)
        {
            Location loc = obj.GetLocation();
            double[,] scores = ScoreRange(obj, range);
            return Enumerable.Range(-range, range * 2 + 1)
                .SelectMany(x => Enumerable.Range(-range, range * 2 + 1).Select(y => new Tuple<Location, double>(loc.Add(x, y), scores[x + range, y + range])))
                .Where(x => x.arg0.InRange(loc, range))
                .FirstBy(x => -x.arg1);
        }
    }
}
