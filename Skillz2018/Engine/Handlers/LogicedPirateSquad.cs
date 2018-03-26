using System.Linq;

namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// A class representing a pirate squad possesing logic to play with
    /// </summary>
    public class LogicedPirateSquad
    {
        /// <summary>
        /// The logiced pirates in the squad
        /// </summary>
        public readonly LogicedPirate[] LogicedPirates;
        public readonly SquadLogic Logic;
        public LogicedPirateSquad(Squad s, SquadLogic logic)
        {
            this.LogicedPirates = s.Select(x => x.LogicPirate(new PirateLogic())).ToArray();
            this.Logic = logic;
        }
        public LogicedPirateSquad(LogicedPirate[] pirates, SquadLogic logic)
        {
            this.LogicedPirates = pirates;
            this.Logic = logic;
        }
        public LogicedPirateSquad(LogicedPirate[] pirates)
        {
            this.LogicedPirates = pirates;
            this.Logic = new SquadLogic();
        }

        /// <summary>
        /// Exceutes the strategy for each pirate in the squad
        /// </summary>
        public void DoTurn()
        {
            Logic.DoTurn(new Squad(LogicedPirates.Where(x => {
                bool result = !x.DoTurn();
                return result || GameEngine.UNSAFE_PIRATE_PLUGINS;
                }).Select(x => x.pirate)));
        }
    }
}
