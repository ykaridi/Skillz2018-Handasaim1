using System.Linq;
using MyBot.Engine.Handlers;
using MyBot.Engine.Delegates;
using MyBot.Engine;
using MyBot.Plugins.Squads;
using Pirates;

namespace MyBot.Plugins.Squads
{
    public class BasicDefenseSquad<T> : SquadPlugin where T : SpaceObject
    {
        private Location Camp;
        private int ExtremeDangerDetailCount;
        private System.Func<T[]> PossibleDangers;
        private Delegates.DefenseFunction<T> ExtremeDangerFunction;
        private Delegates.DefenseFunction<T> DangerFunction;
        private Delegates.PushMapper PushMapper;
        /// <summary>
        /// Constructs a defense squad
        /// </summary>
        /// <param name="Camp">Camping location</param>
        /// <param name="ExtremeDangerDetailCount">Size of extreme danger minimal detail</param>
        /// <param name="PossibleDangers">A function to return possible dangers</param>
        /// <param name="ExtremeDangerFunction">A function that determines wether a danger is considered extreme</param>
        /// <param name="DangerFunction">A function that determines wether a possible danger poses a threat</param>
        public BasicDefenseSquad(Location Camp, int ExtremeDangerDetailCount, System.Func<T[]> PossibleDangers, Delegates.DefenseFunction<T> ExtremeDangerFunction, Delegates.DefenseFunction<T> DangerFunction)
        {
            this.Camp = Camp;
            this.ExtremeDangerDetailCount = ExtremeDangerDetailCount;
            this.PossibleDangers = PossibleDangers;
            this.ExtremeDangerFunction = ExtremeDangerFunction;
            this.DangerFunction = DangerFunction;
            this.PushMapper = (x, y) => Bot.Engine.DefaultPush(x, y);
        }
        /// <summary>
        /// Constructs a defense squad
        /// </summary>
        /// <param name="Camp">Camping location</param>
        /// <param name="ExtremeDangerDetailCount">Size of extreme danger minimal detail</param>
        /// <param name="PossibleDangers">A function to return possible dangers</param>
        /// <param name="ExtremeDangerFunction">A function that determines wether a danger is considered extreme</param>
        /// <param name="DangerFunction">A function that determines wether a possible danger poses a threat</param>
        /// <param name="PushMapper">A custom pushing function</param>
        public BasicDefenseSquad(Location Camp, int ExtremeDangerDetailCount, System.Func<T[]> PossibleDangers, Delegates.DefenseFunction<T> ExtremeDangerFunction, Delegates.DefenseFunction<T> DangerFunction, Delegates.PushMapper PushMapper)
            : this(Camp, ExtremeDangerDetailCount, PossibleDangers, ExtremeDangerFunction, DangerFunction)
        {
            this.PushMapper = PushMapper;
        }

        public bool DoTurn(Squad squad)
        {
            Squad Ordered = squad.OrderBy(x => -x.PushReloadTurns * 0 + x.Id).ToList();
            Squad ExtremeDetail = Ordered.Take(ExtremeDangerDetailCount).ToList();
            Squad DangerDetail = squad.FilterOutBySquad(ExtremeDetail);

            bool ExtremeResult = HandleDangers(squad, PossibleDangers(), ExtremeDangerFunction);
            bool DangerResult = (!ExtremeResult) && HandleDangers(DangerDetail, PossibleDangers(), DangerFunction);

            return (!GameEngine.UNSAFE_SQUAD_PLUGINS) && (ExtremeResult || DangerResult);
        }

        private bool HandleDangers(Squad squad, T[] Dangers, Delegates.DefenseFunction<T> Scorer)
        {
            Dangers = Dangers.Where(x => Scorer(x).rank > 0).ToArray();
            SquadPushPlugin Pusher = new SquadPushPlugin(x => Dangers.Any(y => y.UniqueId == x.UniqueId) ? Scorer(x as T).rank : 0, true,
                PushMapper);

            if (!Dangers.IsEmpty() && squad.Count > 0)
            {
                Pusher.DoTurn(squad);
                Squad CurrentSquad = squad;
                foreach (T danger in Dangers.OrderBy(x => Scorer(x).rank))
                {
                    DefenseStats stats = Scorer(danger);
                    Squad LocalSquad = CurrentSquad.OrderBy(x => x.Distance(danger)).Take(stats.amount).ToList();
                    LocalSquad.ForEach(x => x.Sail(danger));
                    CurrentSquad = CurrentSquad.FilterOutBySquad(LocalSquad);
                }
                return true;
            }

            return false;
        }
    }
}
