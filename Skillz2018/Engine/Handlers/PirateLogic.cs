namespace MyBot.Engine.Handlers
{
    /// <summary>
    /// A class representing a pirate's logic
    /// This is a basic class used in our framework
    /// Plugins are attached to the logic and run Queue-Fashioned
    /// </summary>
    public class PirateLogic
    {
        /// <summary>
        /// The logic's plugins
        /// </summary>
        private PiratePlugin[] Plugins
        {
            get; set;
        } = new PiratePlugin[0];

        public PirateLogic() { }
        public PirateLogic(params PiratePlugin[] Plugins)
        {
            this.Plugins = Plugins;
        }

        /// <summary>
        /// Plays a single turn of the logic on a specified pirate
        /// </summary>
        /// <param name="pirate">Pirate to play turn with</param>
        /// <returns>Boolean indicating wether the pirate has played</returns>
        public bool DoTurn(PirateShip pirate)
        {
            foreach (PiratePlugin plugin in Plugins)
            {
                if (plugin.DoTurn(pirate))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attaches a plugin to the current logic (Queue-Fashioned, which means it gets the least priority)
        /// </summary>
        /// <param name="plugin">Plugin to attach</param>
        /// <returns>New PirateLogic object (object is immutable)</returns>
        public PirateLogic AttachPlugin(PiratePlugin plugin)
        {
            PiratePlugin[] plugins = new PiratePlugin[Plugins.Length + 1];
            for (int i = 0; i < Plugins.Length; i++)
            {
                plugins[i] = Plugins[i];
            }
            plugins[Plugins.Length] = plugin;
            return new PirateLogic(plugins);
        }
    }
}
