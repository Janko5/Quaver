using System;
using System.Collections.Generic;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Server.Client.Events.Scores;
using Quaver.Server.Client.Structures;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using System.Threading;
using Wobble.Logging;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Rankings
{
    /// <summary>
    ///     Fetches scores for <see cref="LeaderboardType.Mods"/> using API v2.
    /// </summary>
    public class ScoreFetcherMods : IScoreFetcher
    {
        public FetchedScoreStore Fetch(Map map, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!OnlineManager.Connected || OnlineManager.Client == null || OnlineManager.Self == null)
                    return new FetchedScoreStore(new List<Score>());

                var client = OnlineManager.Client;

                var mods = ModHelper.GetModsFromRate(ModHelper.GetRateFromMods(ModManager.Mods));

                if (mods == ModIdentifier.None)
                    mods = 0;

                mods = ModManager.Mods - (long)mods;

                // Parallel Fetching
                var mapInfoTask = System.Threading.Tasks.Task.Run(() => client.RetrieveMapInfoV2(map.Md5Checksum));
                var onlineScoresTask = System.Threading.Tasks.Task.Run(() => client.RetrieveScoreboardV2(map.Md5Checksum, OnlineScoreboard.Mods, mods));
                var pbResponseTask = System.Threading.Tasks.Task.Run(() => client.RetrievePersonalBestV2(map.Md5Checksum, OnlineManager.Self.OnlineUser.Id, OnlineScoreboard.Mods, mods));

                System.Threading.Tasks.Task.WaitAll(mapInfoTask, onlineScoresTask, pbResponseTask);

                var mapInfo = mapInfoTask.Result;
                if (mapInfo?.Map != null)
                {
                    map.RankedStatus = (RankedStatus)mapInfo.Map.RankedStatus;
                    map.OnlineOffset = mapInfo.Map.OnlineOffset;
                    map.DateLastUpdated = mapInfo.Map.DateLastUpdated ?? DateTime.MinValue;
                    map.NeedsOnlineUpdate = false;
                }
                else
                {
                    map.RankedStatus = RankedStatus.NotSubmitted;
                }

                var onlineScores = onlineScoresTask.Result;
                var scores = new List<Score>();
                if (onlineScores?.Scores != null)
                {
                    foreach (var scoreV2 in onlineScores.Scores)
                        scores.Add(Score.FromScoreV2(scoreV2, map.Md5Checksum));
                }

                var pbResponse = pbResponseTask.Result;
                var pb = pbResponse?.Score != null ? Score.FromScoreV2(pbResponse.Score, map.Md5Checksum) : null;

                // Update OnlineGrade from PB if available
                if (pb != null)
                {
                    var onlineGrade = GradeHelper.GetGradeFromAccuracy((float)pb.Accuracy);
                    if (GradeHelper.GetGradeImportanceIndex(onlineGrade) > GradeHelper.GetGradeImportanceIndex(map.OnlineGrade))
                        map.OnlineGrade = onlineGrade;
                }

                MapDatabaseCache.UpdateMap(map);
                client.TriggerRetrievedOnlineScores(new RetrievedOnlineScoresEventArgs(map.MapId, map.Md5Checksum,
                    mapInfo: mapInfo, scoresV2: onlineScores, personalBestV2: pbResponse));

                return new FetchedScoreStore(scores, pb);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                return new FetchedScoreStore(new List<Score>());
            }
        }
    }
}