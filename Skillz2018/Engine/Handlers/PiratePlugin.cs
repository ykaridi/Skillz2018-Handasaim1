using MyBot.Engine;

namespace MyBot.Engine.Handlers
{
    public interface PiratePlugin
    {
        bool DoTurn(PirateShip ship);
    }
}
