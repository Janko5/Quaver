namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Rankings
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using Wobble.Logging;
    using Quaver.Server.Client.Events.Scores;
    using Quaver.Server.Client.Structures;
    using Quaver.Shared.Database.Maps;
    using Quaver.Shared.Database.Scores;
    using Quaver.Shared.Online;

    /// <summary>
    ///     Fetches scores for clan leaderboard.
    /// </summary>
    public class ScoreFetcherClan : IScoreFetcher
    {
        public FetchedScoreStore Fetch(Map map, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!OnlineManager.Connected || OnlineManager.Client == null)
                    return new FetchedScoreStore(new List<Score>());

                var client = OnlineManager.Client;

                // Parallel Fetching
                var mapInfoTask = System.Threading.Tasks.Task.Run(() => client.RetrieveMapInfoV2(map.Md5Checksum));
                var onlineScoresTask = System.Threading.Tasks.Task.Run(() => client.GetClanScoreboardV2(map.Md5Checksum));

                System.Threading.Tasks.Task.WaitAll(mapInfoTask, onlineScoresTask);

                var mapInfo = mapInfoTask.Result;
                var onlineScores = onlineScoresTask.Result;
                
                var scores = new List<Score>();

                if (onlineScores?.Scores != null)
                {
                    foreach (var score in onlineScores.Scores)
                        scores.Add(Score.FromClanScore(score, map.Md5Checksum));
                }

                // 3. Sync map metadata to local DB and notify UI
                client.TriggerRetrievedOnlineScores(new RetrievedOnlineScoresEventArgs(map.MapId, map.Md5Checksum,
                    mapInfo: mapInfo, clanScoresV2: onlineScores));

                return new FetchedScoreStore(scores, null);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                return new FetchedScoreStore(new List<Score>());
            }
        }
    }
}