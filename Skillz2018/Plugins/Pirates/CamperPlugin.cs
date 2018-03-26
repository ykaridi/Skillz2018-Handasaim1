using Pirates;
using MyBot.Engine;
using MyBot.Engine.Handlers;

namespace MyBot.Plugins.Pirates
{
    public class CamperPlugin : PiratePlugin
    {
        Location camp;
        int sensitivity;
        public CamperPlugin(MapObject camp, int sensitivity = 0)
        {
            this.camp = camp.GetLocation();
            this.sensitivity = sensitivity;
        }

        public bool DoTurn(PirateShip ship)
        {
            if (ship.Distance(camp) > sensitivity)
            {
                return ship.Sail(camp);
            }
            return false;
        }
    }
}
