namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// An interface for representing an individual pirate strategy
    /// </summary>
    public interface IndividualPirateHandler
    {
        LogicedPirate[] AssignPirateLogic(PirateShip[] pirates);
    }
}
