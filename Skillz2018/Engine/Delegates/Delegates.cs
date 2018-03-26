using Pirates;
using System.Linq;

namespace MyBot.Engine.Delegates
{
    public static class Delegates
    {
        public delegate void PirateForeach(PirateShip p);
        public delegate double ScoringFunction<T>(T obj);
        public delegate bool FilterFunction<T>(T obj);
        public delegate DefenseStats DefenseFunction<T>(T obj);
        public delegate PushMapping PushMapper(SpaceObject target, Squad attackers);
    }
    public class PushMapping
    {
        public readonly Squad attackers;
        public readonly Location dest;
        public static PushMapping ByDistance(Squad attackers, int distance, MapObject dest, bool OnlyIfPossible = true)
        {
            int acc = 0;
            Squad na = attackers.OrderByDescending(x => x.PushDistance).TakeWhile(x =>
            {
                bool res = acc < distance;
                acc += x.PushDistance;
                return res;
            }).ToList();
            if (acc >= distance || !OnlyIfPossible)
                return new PushMapping(na, dest);
            else
                return new PushMapping();
        }
        public static PushMapping To(Squad attackers, MapObject origin, MapObject dest)
        {
            return PushMapping.ByDistance(attackers, origin.Distance(dest), dest, false);
        }
        public static PushMapping ByNumPushes(Squad attackers, int num, MapObject dest)
        {
            Squad na = attackers.OrderBy(x => x.PushDistance).Take(num).ToList();
            if (na.Count >= num)
                return new PushMapping(na, dest);
            else
                return new PushMapping();
        } 
        public PushMapping(Squad attackers, MapObject dest)
        {
            this.attackers = attackers; 
            this.dest = dest.GetLocation();
        }
        public PushMapping()
        {
            this.attackers = new Squad();
            this.dest = new Location(0, 0);
        }
    }
    public class DefenseStats
    {
        public readonly int rank;
        public readonly int amount;
        public DefenseStats(int rank, int amount)
        {
            this.rank = rank;
            this.amount = amount;
        }
    }
}