using System.Collections.Generic;
using System.Linq;
using Pirates;
using System.Collections;

namespace MyBot.Engine
{
    public class Squad : IReadOnlyCollection<PirateShip>
    {
        PirateShip[] Pirates;
        public Squad(params PirateShip[] Pirates)
        {
            this.Pirates = Pirates;
        }
        public Squad(IEnumerable<PirateShip> Pirates)
        {
            this.Pirates = Pirates.ToArray();
        }

        public static implicit operator Squad(PirateShip[] Pirates)
        {
            return new Squad(Pirates);
        }
        public static implicit operator Squad(List<PirateShip> Pirates)
        {
            return new Squad(Pirates);
        }

        public override string ToString()
        {
            return "Squad <" + this.Select(x => x.ToString()).ToArray().Join(", ") + ">";
        }

        public PirateShip this[int index] => Pirates[index];

        public int Count => Pirates.Length;

        public IEnumerator<PirateShip> GetEnumerator()
        {
            foreach(PirateShip p in Pirates)
            {
                yield return p;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (PirateShip p in Pirates)
            {
                yield return p;
            }
        }

        public bool HasCapsule
        {
            get
            {
                return this.Any(x => x.HasCapsule);
            }
        }

        public Squad Filter(System.Func<PirateShip, bool> predicate)
        {
            return new Squad(this.Where(predicate));
        }
        public void ForEach(Delegates.Delegates.PirateForeach action)
        {
            foreach (PirateShip p in this)
                action(p);
        }

        public Squad FilterById(params int[] id)
        {
            return FilterById(id.AsEnumerable());
        }
        public Squad FilterById(IEnumerable<int> ids)
        {
            return Filter(x => ids.Contains(x.Id));
        }
        public Squad FilterOutById(params int[] id)
        {
            return FilterOutById(id.AsEnumerable());
        }
        public Squad FilterOutById(IEnumerable<int> ids)
        {
            return Filter(x => !ids.Contains(x.Id));
        }
        public Squad FilterOutBySquad(Squad squad)
        {
            return FilterOutById(squad.Select(x => x.Id));
        }
        public bool ContainsPirate(PirateShip pirate)
        {
            return this.Any(x => x.UniqueId == pirate.UniqueId);
        }
        public bool ContainsPirate(Pirate pirate)
        {
            return this.Any(x => x.UniqueId == pirate.UniqueId);
        }
        public Squad AddPirates(params PirateShip[] pirates)
        {
            return new Squad(this.Concat(pirates));
        }
        public Squad AddPirates(IEnumerable<PirateShip> pirates)
        {
            return new Squad(this.Concat(pirates));
        }
        public Squad LivingPirates()
        {
            return new Squad(this.Where(x => x.Alive));
        }

        public Location Middle
        {
            get
            {
                if (Count == 0)
                    return new Location(Bot.Engine.Rows / 2, Bot.Engine.Cols / 2);
                return new Location(this.Select(x => x.Location.Row).Sum() / Count, this.Select(x => x.Location.Col).Sum() / Count);
            }
        }
        public double Spread
        {
            get
            {
                Location mid = Middle;
                return System.Math.Sqrt((this.Select(x => x.Distance(mid).Power(2)).Sum()));
            }
        }
    }
}
