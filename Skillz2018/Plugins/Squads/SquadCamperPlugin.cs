using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Engine;
using MyBot.Plugins.Squads;
using Pirates;

namespace MyBot.Plugins.Squads
{
    public class SquadCamperPlugin : SquadPlugin
    {
        Location Camp;
        /// <summary>
        /// Constructs a camping squad
        /// </summary>
        /// <param name="Camp">The location to camp at</param>
        public SquadCamperPlugin(MapObject Camp)
        {
            this.Camp = Camp.GetLocation();
        }

        public bool DoTurn(Squad squad)
        {
            bool Moved = false;
            squad.ForEach(x => Moved = x.Sail(Camp) ? true : Moved);
            return Moved;
        }
    }
}
