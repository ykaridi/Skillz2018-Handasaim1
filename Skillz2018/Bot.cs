using Pirates;
using MyBot.Engine;
using MyBot.Engine.Handlers;
using MyBot.Strategies;

namespace MyBot
{
    public class Bot : IPirateBot
    {
        internal static GameEngine Engine;

        public Bot()
        {
            Engine = new GameEngine();
        }

        public void DoTurn(PirateGame game)
        {
            try
            {
                int turn = game.Turn;
                SquadPirateHandler handler;

                /* Update Game */
                Engine.Store.NextTurn();
                Engine.Update(game);

                /* Strategy Change Check */
                handler = new PVPBot();

                /* Display logs */
                if (turn > 1)
                    Engine.PrintStatusLog();

                /* Play Strategy Selection */
                Engine.DoTurn(handler);

                /* Display logs */
                if (turn > 1)
                    Engine.PrintActionLog();

                Engine.Store.Flush();
                game.Debug("Turn took: " + (game.GetMaxTurnTime() - game.GetTimeRemaining()) + "ms / " + game.GetMaxTurnTime() + "ms");
            }
            catch (System.Exception e)
            {
                /* Error Handling */
                game.Debug("Error!");
                game.Debug(e.Message);
                game.Debug(e.StackTrace);
            }
        }
    }
}