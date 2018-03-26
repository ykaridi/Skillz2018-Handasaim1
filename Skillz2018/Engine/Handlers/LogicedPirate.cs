namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// A class for representing a pirate possessing logic to play with
    /// </summary>
    public class LogicedPirate
    {
        /// <summary>
        /// Pirate object
        /// </summary>
        public readonly PirateShip pirate;
        /// <summary>
        /// Logic object
        /// </summary>
        public readonly PirateLogic logic;

        public LogicedPirate(PirateShip s, PirateLogic logic)
        {
            this.pirate = s;
            this.logic = logic;
        }

        /// <summary>
        /// Execute a turn of the pirate
        /// </summary>
        public bool DoTurn()
        {
            return logic.DoTurn(pirate);
        }
    }
}
