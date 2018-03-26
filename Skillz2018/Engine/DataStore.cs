using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;
using MyBot.Engine.Handlers;

namespace MyBot.Engine
{
    /// <summary>
    /// A class used for storing data across turns
    /// </summary>
    public class DataStore : Dictionary<string, object>
    {
        public const string WAIT = "!";
        public const string CAMPER = "@[Camper]";
        public DataStore() : base()
        {
            
        }

        /// <summary>
        /// Flushes old entries
        /// </summary>
        public void Flush()
        {
            string[] keys = Keys.ToArray();
            foreach (string key in keys)
            {
                if (!key.StartsWith(WAIT)) this.Remove(key);
            }
        }
        /// <summary>
        /// Updates entries to the next turn
        /// </summary>
        public void NextTurn()
        {
            string[] keys = Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.StartsWith(WAIT))
                {
                    object value = this[key];
                    this.Remove(key);
                    this[key.Substring(WAIT.Length)] = value;
                }
            }
        }

        /// <summary>
        /// Attempts retreiving an entry from the dictionary
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Key associated with the entry</param>
        /// <param name="def">Default value</param>
        /// <param name="prefix">Entry prefix</param>
        /// <param name="ttl">Time To Live</param>
        /// <returns></returns>
        public T GetValue<T>(string key, T def, string prefix = "$", int ttl = 0)
        {
            string nk = WAIT.Multiply(ttl) + prefix + key;
            if (ContainsKey(nk)) return (T)this[nk];
            else return def;
        }

        /// <summary>
        /// Attempts storing an entry in the dictionary
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Key associated with the entry</param>
        /// <param name="value">Value associated with the entry</param>
        /// <param name="prefix">Entry prefix</param>
        /// <param name="ttl">Time To Live</param>
        public void SetValue<T>(string key, T value, string prefix = "$", int ttl = 1)
        {
            string nk = WAIT.Multiply(ttl) + prefix + key;
            this[nk] = value;
        }
    }
}
