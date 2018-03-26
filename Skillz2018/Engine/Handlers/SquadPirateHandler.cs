namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// An interface for representing a squaded pirate strategy
    /// </summary>
    public interface SquadPirateHandler
    {
        LogicedPirateSquad[] AssignSquads(PirateShip[] pirates);
    }
}
