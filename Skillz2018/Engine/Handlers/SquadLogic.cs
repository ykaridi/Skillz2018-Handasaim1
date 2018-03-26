namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// A class representing a squads's logic
    /// This is a basic class used in our framework
    /// Plugins are attached to the logic and run Queue-Fashioned
    /// </summary>
    public class SquadLogic
    {
        /// <summary>
        /// The logic's plugins
        /// </summary>
        private SquadPlugin[] Plugins
        {
            get; set;
        } = new SquadPlugin[0];

        public SquadLogic() { }
        public SquadLogic(params SquadPlugin[] Plugins)
        {
            this.Plugins = Plugins;
        }

        /// <summary>
        /// Plays a single turn of the logic on a specified squad
        /// </summary>
        /// <param name="squad">Squad to play turn with</param>
        /// <returns>Boolean indicating wether the squad has finished playing</returns>
        public bool DoTurn(Squad squad)
        {
            foreach (SquadPlugin plugin in Plugins)
            {
                if (plugin.DoTurn(squad))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attaches a plugin to the current logic (Queue-Fashioned, which means it gets the least priority)
        /// </summary>
        /// <param name="plugin">Plugin to attach</param>
        /// <returns>New SquadLogic object (object is immutable)</returns>
        public SquadLogic AttachPlugin(SquadPlugin plugin)
        {
            SquadPlugin[] plugins = new SquadPlugin[Plugins.Length + 1];
            for (int i = 0; i < Plugins.Length; i++)
            {
                plugins[i] = Plugins[i];
            }
            plugins[Plugins.Length] = plugin;
            return new SquadLogic(plugins);
        }
    }
}