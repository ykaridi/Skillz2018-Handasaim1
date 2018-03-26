using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Engine;
using Pirates;

namespace MyBot.Plugins.Squads
{
    public class SquadPushPlugin : SquadPlugin
    {
        Delegates.ScoringFunction<SpaceObject> Scorer;
        Delegates.PushMapper PushMapper;
        bool Ascending;
        
        /// <summary>
        /// Constructs a simple squad push plugin
        /// </summary>
        /// <param name="OnlyWhenKill">Push only when killing is possible</param>
        /// <param name="OnlyCapsules">Push only capsule carrying pirates</param>
        /// <param name="CrystalEmergency">Ignore OnlyWhenKill if a capsule is about to be scored</param>
        /// <param name="PrioritizeCapsules">Prioiritize capsule carriers</param>
        public SquadPushPlugin(bool OnlyWhenKill = true, bool OnlyCapsules = false, bool CrystalEmergency = true, bool PrioritizeCapsules = true)
        {
            Scorer = obj =>
            {
                if (obj is Pirate && Bot.Engine.IsAliveAfterTurn((Pirate)obj, 0) && (!OnlyCapsules || ((Pirate)obj).HasCapsule()))
                    return ((Pirate)obj).HasCapsule() ? 2 : 1;
                else
                    return 0;
            };
            PushMapper = (obj, attackers) =>
            {
                if (!(obj is Pirate))
                    return new PushMapping();
                if (CrystalEmergency && !Bot.Engine.CanKill(attackers, obj))
                    return PushMapping.ByNumPushes(attackers, (obj as Pirate).NumPushesForCapsuleLoss, obj.ClosestBorder());
                return PushMapping.ByDistance(attackers, obj.DistanceFromBorder(), obj.ClosestBorder());
            };
            this.Ascending = false;
        }
        /// <summary>
        /// Constructs an advanced squad push plugin
        /// </summary>
        /// <param name="Scorer">A selector to score each pushable object (priority). a score beneath or equal to 0 is considered non threat</param>
        /// <param name="Ascending">Flag to set if objects are sorted from low to high</param>
        /// <param name="PushMapper">A function to describe the pushing tactic of the pirates</param>
        public SquadPushPlugin(Delegates.ScoringFunction<SpaceObject> Scorer, bool Ascending, Delegates.PushMapper PushMapper)
        {
            this.Scorer = Scorer;
            this.PushMapper = (obj, attackers) =>
            {
                if (Scorer(obj) <= 0)
                    return new PushMapping();
                else
                    return PushMapper(obj, attackers);
            };
            this.Ascending = Ascending;
        }
        public bool DoTurn(Squad squad)
        {
            foreach (SpaceObject obj in Bot.Engine.PushableSpaceObjects.Transform(true, x => (Ascending ? x.OrderByDescending(y => Scorer(y)) : x.OrderBy(y => Scorer(y))).ToArray()))
            {
                PushMapping result = PushMapper(obj, squad.Where(x => x.CanPush(obj)).ToList());
                foreach (PirateShip p in result.attackers)
                    p.Push(obj, result.dest);
            }
            return false;
        }
    }
}
