using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.API.Maps.Processors.Rating;
using Quaver.Server.Client;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Selection.UI.Leaderboard.Components;
using Quaver.Shared.Screens.Selection.UI.Leaderboard.Rankings;
using Quaver.Shared.Skinning;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Scheduling;
using Logger = Wobble.Logging.Logger;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard
{
    public class LeaderboardContainer : Sprite, ILoadable
    {
        /// <summary>
        ///     Displays "LEADERBOARD"
        /// </summary>
        private SpriteTextPlus Header { get; set; }

        /// <summary>
        ///     Whether the leaderboard is using the V2 layout.
        /// </summary>
        public bool IsV2 => SkinManager.Skin?.UserInterfaceVersion >= 2f;

        /// <summary>
        ///     Allows the user to select between different leaderboard types
        /// </summary>
        private LeaderboardTypeDropdown TypeDropdown { get; set; }

        /// <summary>
        ///     The background for <see cref="ScoresContainer"/>
        /// </summary>
        public Sprite ScoresContainerBackground { get; private set; }

        /// <summary>
        ///     Displays the scores of the leaderboard
        /// </summary>
        public LeaderboardScoresContainer ScoresContainer { get; set; }

        /// <summary>
        ///     A header above the user's personal best score
        /// </summary>
        private SpriteTextPlus PersonalBestHeader { get; set; }

        /// <summary>
        ///     Displays the user's personal best score for the leaderboard section
        /// </summary>
        private LeaderboardPersonalBestScore PersonalBestScore { get; set; }

        /// <summary>
        ///     Task that's ran when fetching for leaderboard scores
        /// </summary>
        public TaskHandler<Map, FetchedScoreStore> FetchScoreTask { get; }

        /// <summary>
        ///     Trophy symbol that shows what the user's PB rank is
        /// </summary>
        private Sprite PersonalBestTrophy { get; set; }

        /// <summary>
        ///     Displays the user's personal best rank
        /// </summary>
        private SpriteTextPlus PersonalBestRank { get; set; }

        /// <summary>
        /// </summary>
        public LeaderboardContainer()
        {
            Size = IsV2 ? new ScalableVector2(725, 860) : new ScalableVector2(564, 838);
            Alpha = 0f;
            AutoScaleHeight = true;

            FetchScoreTask = new TaskHandler<Map, FetchedScoreStore>(FetchScores);
            FetchScoreTask.OnCompleted += OnFetchedScores;

            CreateHeaderText();
            CreateRankingDropdown();
            CreateScoresContainer();
            CreatePersonalBestHeader();
            CreatePersonalBestScore();
            CreateTrophy();
            CreatePersonalBestRank();

            if (!IsV2)
                ListHelper.Swap(Children, Children.IndexOf(TypeDropdown), Children.IndexOf(ScoresContainerBackground));

            MapManager.Selected.ValueChanged += OnMapChanged;

            if (ConfigManager.LeaderboardSection != null)
                ConfigManager.LeaderboardSection.ValueChanged += OnLeaderboardSectionChanged;

            if (ConfigManager.DisplayFailedLocalScores != null)
                ConfigManager.DisplayFailedLocalScores.ValueChanged += OnDisplayFailedLocalScoresChanged;

            ModManager.ModsChanged += OnModsChanged;
            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
            ScoreDatabaseCache.ScoreDeleted += OnScoreDeleted;
            ScoreDatabaseCache.LocalMapScoresDeleted += OnMapLocalScoresDeleted;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            FetchScoreTask?.Dispose();

            // ReSharper disable once DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;

            if (ConfigManager.LeaderboardSection != null)
            {
                // ReSharper disable once DelegateSubtraction
                ConfigManager.LeaderboardSection.ValueChanged -= OnLeaderboardSectionChanged;
            }

            if (ConfigManager.DisplayFailedLocalScores != null)
            {
                // ReSharper disable once DelegateSubtraction
                ConfigManager.DisplayFailedLocalScores.ValueChanged -= OnDisplayFailedLocalScoresChanged;
            }

            ModManager.ModsChanged -= OnModsChanged;

            // ReSharper disable once DelegateSubtraction
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;
            ScoreDatabaseCache.ScoreDeleted -= OnScoreDeleted;
            ScoreDatabaseCache.LocalMapScoresDeleted -= OnMapLocalScoresDeleted;

            base.Destroy();
        }

        /// <summary>
        ///    Creates <see cref="Header"/>
        /// </summary>
        private void CreateHeaderText()
        {
            Header = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "LEADERBOARD", 30)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Tint = SkinManager.Skin.SongSelect.LeaderboardTitleColor,
                Visible = !IsV2
            };
        }

        /// <summary>
        ///     Creates <see cref="TypeDropdown"/>
        /// </summary>
        private void CreateRankingDropdown()
        {
            TypeDropdown = new LeaderboardTypeDropdown(IsV2)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = 0
            };
        }

        /// <summary>
        ///     Creates <see cref="ScoresContainer"/>
        /// </summary>
        private void CreateScoresContainer()
        {
            ScoresContainerBackground = new Sprite()
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = IsV2 ? 0 : 0,
                Y = IsV2 ? 0 : Header.Y + Header.Height + 8,
                Size = IsV2 ? new ScalableVector2(725, 700) : new ScalableVector2(Width, 664),
                Image = SkinManager.Skin?.SongSelect?.LeaderboardPanel ?? UserInterface.LeaderboardPanel,
                AutoScaleHeight = true
            };

            ScoresContainer = new LeaderboardScoresContainer(this)
            {
                Parent = ScoresContainerBackground,
                Alignment = IsV2 ? Alignment.TopLeft : Alignment.MidCenter,
                X = IsV2 ? 10 : 0,
                Y = IsV2 ? 60 : 0
            };

            // V2: reparent dropdown into panel
            if (IsV2)
            {
                TypeDropdown.Parent = ScoresContainerBackground;
                TypeDropdown.Alignment = Alignment.TopRight;
                TypeDropdown.X = -10;
                TypeDropdown.Y = 10;
            }
        }

        /// <summary>
        ///     Creates <see cref="PersonalBestHeader"/>
        /// </summary>
        private void CreatePersonalBestHeader()
        {
            PersonalBestHeader = new SpriteTextPlus(Header.Font, "PERSONAL BEST", Header.FontSize)
            {
                Parent = this,
                Y = ScoresContainerBackground.Y + ScoresContainerBackground.Height + 28,
                Tint = SkinManager.Skin.SongSelect.PersonalBestTitleColor,
                Visible = !IsV2
            };
        }

        /// <summary>
        /// </summary>
        private void CreateTrophy()
        {
            PersonalBestTrophy = new Sprite
            {
                Parent = this,
                Y = IsV2 ? PersonalBestScore.Y + 5 : PersonalBestHeader.Y + 4,
                Alignment = Alignment.TopLeft,
                X = IsV2 ? 645 : 484,
                Size = new ScalableVector2(22, 20),
                Image = UserInterface.SongSelectTrophy,
                Alpha = 0
            };
        }

        /// <summary>
        /// </summary>
        private void CreatePersonalBestRank()
        {
            PersonalBestRank = new SpriteTextPlus(Header.Font, "#50", Header.FontSize - 2)
            {
                Parent = this,
                Y = IsV2 ? PersonalBestTrophy.Y - 3 : PersonalBestTrophy.Y - 2,
                Alignment = Alignment.TopLeft,
                X = IsV2 ? 673 : 484,
                Alpha = 0,
                Tint = SkinManager.Skin.SongSelect.PersonalBestRankColor
            };
        }

        /// <summary>
        ///     Creates <see cref="PersonalBestScore"/>
        /// </summary>
        private void CreatePersonalBestScore()
        {
            PersonalBestScore = new LeaderboardPersonalBestScore(this)
            {
                Parent = this,
                X = IsV2 ? 0 : 0,
                Y = IsV2 ? ScoresContainerBackground.Y + ScoresContainerBackground.Height + 10 : PersonalBestHeader.Y + PersonalBestHeader.Height + 6
            };
        }

        /// <summary>
        ///     Initiates a task to fetch scores for the selected map
        /// </summary>
        public void FetchScores()
        {
            var map = MapManager.Selected.Value;
            if (map != null)
                map.NeedsOnlineUpdate = false;

            if (map != null)
                FetchScoreTask.Run(map, 250);
            
            StartLoading();
        }

        /// <summary>
        ///     Fetches scores for the passed in map
        /// </summary>
        /// <returns></returns>
        private FetchedScoreStore FetchScores(Map map, CancellationToken token)
        {
            if (map == null)
                return new FetchedScoreStore(new List<Score>());

            var sw = System.Diagnostics.Stopwatch.StartNew();
            FetchedScoreStore scores;

            switch (ConfigManager.LeaderboardSection.Value)
            {
                case LeaderboardType.Local:
                    scores = new ScoreFetcherLocal().Fetch(map, token);
                    break;
                case LeaderboardType.Global:
                    scores = new ScoreFetcherGlobal().Fetch(map, token);
                    break;
                case LeaderboardType.Mods:
                    scores = new ScoreFetcherMods().Fetch(map, token);
                    break;
                case LeaderboardType.Country:
                    scores = new ScoreFetcherCountry().Fetch(map, token);
                    break;
                case LeaderboardType.Rate:
                    scores = new ScoreFetcherRate().Fetch(map, token);
                    break;
                case LeaderboardType.Friends:
                    scores = new ScoreFetcherFriends().Fetch(map, token);
                    break;
                case LeaderboardType.All:
                    scores = new ScoreFetcherAll().Fetch(map, token);
                    break;
                case LeaderboardType.Clan:
                    scores = new ScoreFetcherClan().Fetch(map, token);
                    break;
                default:
                    scores = new FetchedScoreStore();
                    break;
            }

            // Set scores to use during gameplay
            if (OnlineManager.CurrentGame != null)
                return scores;

            MapManager.Selected.Value.Scores.Value = scores.Scores;
            ScoresHelper.SetRatingProcessors(MapManager.Selected.Value.Scores.Value);

            sw.Stop();

            return scores;
        }

        /// <summary>
        ///     Called when having successfully fetched scores
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFetchedScores(object? sender, TaskCompleteEventArgs<Map, FetchedScoreStore> e)
        {
            if (e.Input != MapManager.Selected.Value)
            {
                Logger.Important($"Discarding stale leaderboard scores for map: {e.Input}. Current selection: {MapManager.Selected.Value}", LogType.Runtime);
                return;
            }

            Logger.Debug($"Fetched {e.Result.Scores?.Count} {ConfigManager.LeaderboardSection?.Value} scores for map: {e.Input} | " +
                         $"Has PB: {e.Result.PersonalBest != null}", LogType.Runtime);

            StopLoading();

            foreach (var x in new List<Drawable>(Children))
            {
                if (x is IFetchedScoreHandler handler)
                    handler.HandleFetchedScores(e.Input, e.Result);
            }

            ScoresContainer.HandleFetchedScores(e.Input, e.Result);

            PersonalBestTrophy.ClearAnimations();
            PersonalBestRank.ClearAnimations();

            // Handle personal best rank
            if (!IsV2 && ConfigManager.LeaderboardSection != null && ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
            {
                var rank = e.Result.Scores?.FindIndex(x => x.Name == e.Result.PersonalBest?.Name);

                if (rank == -1)
                    return;

                var count = e.Result.Scores?.Count ?? 0;
                PersonalBestRank.Text = $"#{rank + 1} of #{count}";

                // Right-align the group [trophy | 4px | rank text] to the panel's right edge
                PersonalBestRank.X = Width - PersonalBestRank.Width;
                PersonalBestTrophy.X = PersonalBestRank.X - PersonalBestTrophy.Width - 4;

                const int animTime = 150;
                PersonalBestTrophy.FadeTo(1, Easing.Linear, animTime);
                PersonalBestRank.FadeTo(1, Easing.Linear, animTime);
            }
        }

        /// <summary>
        ///     Called when the selected map has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object? sender, BindableValueChangedEventArgs<Map> e)
        {
            FetchScores();
        }

        /// <summary>
        ///     Called when the user changes the selected leaderboard section
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLeaderboardSectionChanged(object? sender, BindableValueChangedEventArgs<LeaderboardType> e) => FetchScores();

        /// <summary>
        ///     Called when the user changes the option to display failed local scores
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisplayFailedLocalScoresChanged(object? sender, BindableValueChangedEventArgs<bool> e)
        {
            if (ConfigManager.LeaderboardSection == null || ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
                return;

            FetchScores();
        }

        /// <summary>
        ///     Called when the user selects new mods while their leaderboard section is selected mods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object? sender, ModsChangedEventArgs e)
        {
            if (ConfigManager.LeaderboardSection == null ||
                ConfigManager.LeaderboardSection.Value != LeaderboardType.Mods && ConfigManager.LeaderboardSection.Value != LeaderboardType.Rate)
                return;

            FetchScores();
        }

        /// <summary>
        /// </summary>
        public void StartLoading()
        {
            foreach (var x in new List<Drawable>(Children))
            {
                if (x is ILoadable loadable)
                    loadable.StartLoading();
            }

            ScoresContainer.StartLoading();

            const int animTime = 150;

            PersonalBestTrophy.ClearAnimations();
            PersonalBestTrophy.FadeTo(0, Easing.Linear, animTime);

            PersonalBestRank.ClearAnimations();
            PersonalBestRank.FadeTo(0, Easing.Linear, animTime);
        }

        /// <summary>
        /// </summary>
        public void StopLoading()
        {
            foreach (var x in new List<Drawable>(Children))
            {
                if (x is ILoadable loadable)
                    loadable.StopLoading();
            }

            ScoresContainer.StopLoading();
        }

        /// <summary>
        ///     Whenever the user connects to the server in song select, it will automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectionStatusChanged(object? sender, BindableValueChangedEventArgs<ConnectionStatus> e)
        {
            if (e.Value != ConnectionStatus.Connected || ConfigManager.LeaderboardSection.Value == LeaderboardType.Local)
                return;

            ScheduleUpdate(FetchScores);
        }

        /// <summary>
        ///     Called when the user deletes a local score.
        ///     Refreshes the leaderboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnScoreDeleted(object? sender, ScoreDeletedEventArgs e)
        {
            if (ConfigManager.LeaderboardSection == null || ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
                return;

            FetchScores();
        }

        /// <summary>
        ///     Called when the user deletes a map's scores
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapLocalScoresDeleted(object? sender, LocalScoresDeletedEventArgs e)
        {
            if (ConfigManager.LeaderboardSection == null || ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
                return;

            FetchScores();
        }
    }
}
