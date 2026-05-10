using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Quaver.Shared.Skinning;
using TagLib.Ape;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Scheduling;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Components
{
    public class LeaderboardScoresContainer : PoolableScrollContainer<Score>, ILoadable, IFetchedScoreHandler
    {
        /// <summary>
        ///     The parent leaderboard container
        /// </summary>
        private LeaderboardContainer Container { get; }

        /// <summary>
        /// </summary>
        private Drawable ScrollbarBackground { get; set; } = null!;

        /// <summary>
        ///     Custom scrollbar thumb using 9-slice sprite
        /// </summary>
        private NineSliceSprite CustomScrollbarThumb { get; set; } = null!;

        private bool _isCustomDragging;
        private float _customDragOffset;

        /// <summary>
        ///     Loading wheel displayed when scores are loading
        /// </summary>
        private LoadingWheel LoadingWheel { get; set; } = null!;

        /// <summary>
        ///     Gives the user a status update
        /// </summary>
        private SpriteTextPlus StatusText { get; set; } = null!;

        /// <summary>
        ///     Container to hold status text and loading wheel, placed exactly at the 4th mask.
        /// </summary>
        private Container StatusContainer { get; set; } = null!;

        /// <summary>
        ///     If the scores have finished loading
        /// </summary>
        private bool FinishedLoading { get; set; }

        /// <summary>
        ///     The button to update the map to the latest version
        /// </summary>
        private IconButton UpdateButton { get; set; } = null!;

        private bool IsV2 => (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) >= 2f;

        /// <summary>
        ///     If the scrollbar is required to be shown (based on real scores, V2 exclusive)
        /// </summary>
        public bool RequiresScroll
        {
            get
            {
                if (!IsV2 || AvailableItems == null)
                    return false;

                var realCount = 0;
                for (var i = 0; i < AvailableItems.Count; i++)
                {
                    if (AvailableItems[i].IsEmptyScore)
                        break;
                    realCount++;
                }

                return realCount > 9;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        public LeaderboardScoresContainer(LeaderboardContainer container) : base(new List<Score>(), 12, 0,
            (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) >= 2f ? new ScalableVector2(705, container.ScoresContainerBackground.Height - 70) : new ScalableVector2(container.ScoresContainerBackground.Width - 4, container.ScoresContainerBackground.Height - 4),
            (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) >= 2f ? new ScalableVector2(705, container.ScoresContainerBackground.Height - 70) : new ScalableVector2(container.ScoresContainerBackground.Width - 4, container.ScoresContainerBackground.Height - 4))
        {
            Container = container;
            Alpha = 0;

            if (PoolSize != int.MaxValue)
                PoolSize = (int)(PoolSize * WindowManager.BaseToVirtualRatio) + 2;

            InputEnabled = true;
            EasingType = Easing.OutQuint;
            TimeToCompleteScroll = 1200;
            ScrollSpeed = 220;
            IsMinScrollYEnabled = true;

            CreateScrollbar();
            CreateStatusContainer();
            CreateLoadingWheel();
            CreateStatusText();
            CreateUpdateButton();

            Container.FetchScoreTask.OnCompleted += OnScoresRetrieved;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            bool wasScrollVisible = ScrollbarBackground.Visible;

            if (Pool != null)
                ScrollbarBackground.Visible = RequiresScroll;
            else
                ScrollbarBackground.Visible = false;

            if (wasScrollVisible != ScrollbarBackground.Visible && Pool != null)
            {
                Pool.ForEach(x =>
                {
                    if (x is DrawableLeaderboardScore score)
                    {
                        score.Size = new Wobble.Graphics.ScalableVector2(score.GetScoreWidth(), score.HEIGHT);
                        if (score.ChildContainer != null)
                            score.ChildContainer.Size = score.Size;
                    }
                });
            }

            InputEnabled = GraphicsHelper.RectangleContains(ScreenRectangle, MouseManager.CurrentState.Position)
                           && DialogManager.Dialogs.Count == 0
                           && !KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt)
                           && !KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt);

            if (FinishedLoading && Pool != null && Pool.Count > 0 && Pool.First().Parent != ContentContainer)
            {
                Pool.ForEach(x =>
                {
                    AddContainedDrawable(x);

                    var score = x as DrawableLeaderboardScore;
                    score?.AddScheduledUpdate(() => score.ChildContainer.FadeIn());
                });
            }

            // Only make the update perform hover if absolutely necessary
            UpdateButton.IsPerformingFadeAnimations = MapManager.Selected.Value != null
                                                      && MapManager.Selected.Value.NeedsOnlineUpdate &&
                                                      LoadingWheel.Alpha < 0.1f;

            // Sync custom scrollbar thumb with scroll position
            if (CustomScrollbarThumb != null && ScrollbarBackground != null && ScrollbarBackground.Visible)
            {
                // Sync background height with container height
                ScrollbarBackground.Height = Height;

                // Calculate thumb height manually to ensure perfect precision with our calculations
                if (ContentContainer.Height > 0)
                    CustomScrollbarThumb.Height = MathHelper.Clamp((Height / ContentContainer.Height) * Height, 30, Height);

                if (!_isCustomDragging && MouseManager.IsUniquePress(MouseButton.Left) && CustomScrollbarThumb.IsHovered() && DialogManager.Dialogs.Count == 0)
                {
                    _isCustomDragging = true;
                    _customDragOffset = CustomScrollbarThumb.ScreenRectangle.Y - MouseManager.CurrentState.Position.Y;
                }
                else
                {
                    _isCustomDragging = _isCustomDragging && MouseManager.CurrentState.LeftButton == ButtonState.Pressed;
                }

                if (_isCustomDragging)
                {
                    var scrollableRange = ScrollbarBackground.ScreenRectangle.Height - CustomScrollbarThumb.ScreenRectangle.Height;
                    if (scrollableRange > 0)
                    {
                        var localMouseY = MouseManager.CurrentState.Position.Y + _customDragOffset - ScrollbarBackground.ScreenRectangle.Y;
                        var percent = MathHelper.Clamp(localMouseY / scrollableRange, 0, 1);
                        var maxScrollY = ContentContainer.Height - Height;
                        TargetY = -maxScrollY * percent;
                        ContentContainer.Animations.Clear();
                        ContentContainer.Y = TargetY;
                    }
                }

                var maxScrollYPos = ContentContainer.Height - Height;
                var currentPercent = maxScrollYPos > 0 ? MathHelper.Clamp(Math.Abs(ContentContainer.Y) / maxScrollYPos, 0, 1) : 0;
                var thumbScrollableRange = ScrollbarBackground.Height - CustomScrollbarThumb.Height;

                CustomScrollbarThumb.Y = (float)Math.Round(currentPercent * thumbScrollableRange);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            Container.FetchScoreTask.OnCompleted -= OnScoresRetrieved;
            base.Destroy();
        }

        /// <summary>
        ///     Creates the scrollbar sprite and aligns it properly
        /// </summary>
        private void CreateScrollbar()
        {
            // Hide the default scrollbar created by base class (but keep it for height calculation)
            Scrollbar.Alpha = 0;

            var skin = SkinManager.Skin;
            var bgColor = skin?.ScrollbarBackgroundColor ?? SkinManager.Skin.ScrollbarBackgroundColor;
            var thumbColor = skin?.ScrollbarThumbColor ?? SkinManager.Skin.ScrollbarThumbColor;
            var margins = skin?.ScrollbarTopBottomMargins ?? new SliceMargins(0, 0, 8, 8);

            // Scrollbar background: using 9-slice for rounded corners (15px width)
            // X = 10px gap from leaderboard
            ScrollbarBackground = new NineSliceSprite(UserInterface.UniversalScrollBackground, margins)
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = IsV2 ? 0 : 25,  // 10px gap from leaderboard + 15px scrollbar width in V1
                Size = new ScalableVector2(15, Height),
                Tint = bgColor,
                Visible = false
            };

            // Scrollbar thumb: using 9-slice for rounded corners
            CustomScrollbarThumb = new NineSliceSprite(UserInterface.UniversalScrollBackground, margins)
            {
                Parent = ScrollbarBackground,
                Alignment = Alignment.TopCenter,
                Width = 15,
                Tint = thumbColor
            };
        }


        /// <summary>
        ///     Creates the status container
        /// </summary>
        private void CreateStatusContainer()
        {
            var baseHeight = IsV2 ? 70 : 66;
            StatusContainer = new Container
            {
                Parent = this,
                Alignment = Alignment.TopCenter,
                Size = new ScalableVector2(Width, baseHeight),
                Y = 3 * DrawableLeaderboardScore.ScoreHeight
            };
        }

        /// <summary>
        ///     Creates the loading wheel for the screen
        /// </summary>
        private void CreateLoadingWheel()
        {
            LoadingWheel = new LoadingWheel
            {
                Parent = StatusContainer,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(50, 50),
                Alpha = 0
            };
        }

        protected override PoolableSprite<Score> CreateObject(Score item, int index) => new DrawableLeaderboardScore(this, item, index, false);

        /// <summary>
        ///     Fades the wheel in to make it appear as if it is loading
        /// </summary>
        public void StartLoading()
        {
            ScheduleUpdate(() =>
            {
                if (IsDisposed)
                    return;

                LoadingWheel.Animations.RemoveAll(x => x.Properties != AnimationProperty.Rotation);
                LoadingWheel.FadeTo(1, Easing.Linear, 250);
                FadeStatusTextOut();
                ClearPool();
                ContentContainer.Height = Height;

                SnapToTop();

                AvailableItems ??= new List<Score>();
                AvailableItems.Clear();

                var maxItems = (int)Math.Ceiling(Height / DrawableLeaderboardScore.ScoreHeight);
                for (var i = 0; i < maxItems; i++)
                    AvailableItems.Add(new Score { IsEmptyScore = true });

                try
                {
                    CreatePool(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }

                ScrollbarBackground.Visible = false;
                FinishedLoading = true;

                UpdateButton.ClearAnimations();
                UpdateButton.IsClickable = false;
                UpdateButton.FadeTo(0, Easing.Linear, 250);
            });
        }

        /// <summary>
        ///     Fades the wheel out to 0.
        /// </summary>
        public void StopLoading()
        {
            LoadingWheel.Animations.RemoveAll(x => x.Properties != AnimationProperty.Rotation);
            LoadingWheel.FadeTo(0, Easing.Linear, 250);
            FadeStatusTextOut();
            FinishedLoading = false;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="store"></param>
        public void HandleFetchedScores(Map map, FetchedScoreStore store)
        {
            if (map == null)
            {
                StatusText.Text = "There is currently no map selected!";
                FadeStatusTextIn();
                return;
            }

            var isConnected = OnlineManager.Connected;
            var isDonator = OnlineManager.IsDonator;

            // User isn't online
            if (RequiresOnline(ConfigManager.LeaderboardSection.Value) && !isConnected)
            {
                StatusText.Text = "You must be online to access this leaderboard!";
                FadeStatusTextIn();
                return;
            }

            // User isn't a donator
            if (RequiresOnline(ConfigManager.LeaderboardSection.Value) && RequiresDonator() && !isDonator)
            {
                StatusText.Text = "You must be a donator to access this leaderboard!";
                FadeStatusTextIn();
                return;
            }

            // No scores are available
            if (store.Scores.Count == 0)
            {
                // The user's map is not up-to-date, so prompt them of this.
                if (map.NeedsOnlineUpdate)
                {
                    StatusText.Text = "Your map is outdated. Please update it!";
                    UpdateButton.ClearAnimations();
                    UpdateButton.IsClickable = true;
                    UpdateButton.FadeTo(1, Easing.Linear, 250);
                }
                else
                {
                    UpdateButton.ClearAnimations();
                    UpdateButton.IsClickable = false;
                    UpdateButton.FadeTo(0, Easing.Linear, 250);

                    // The map isn't ranked, but the user is a donator, so they can access leaderboards on all maps
                    if (map.RankedStatus != RankedStatus.Ranked && isDonator && ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
                        StatusText.Text = "Scores on this map will be unranked!";
                    else if (ConfigManager.LeaderboardSection.Value != LeaderboardType.Local)
                    {
                        switch (map.RankedStatus)
                        {
                            case RankedStatus.NotSubmitted:
                                StatusText.Text = "This map is not submitted online!";
                                break;
                            case RankedStatus.Unranked:
                                StatusText.Text = "This map is not ranked!";
                                break;
                            case RankedStatus.Ranked:
                                StatusText.Text = "No scores available. Be the first!";
                                break;
                            case RankedStatus.DanCourse:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(map.RankedStatus), map.RankedStatus, null);
                        }
                    }
                    else
                        StatusText.Text = "No scores available. Be the first!";
                }

                FadeStatusTextIn();
                return;
            }
        }

        public override void RecalculateContainerHeight(bool usePoolCount = false)
        {
            base.RecalculateContainerHeight(usePoolCount);

            if (Pool == null)
                return;

            var gap = SkinManager.Skin.SongSelect.LeaderboardScoresGap;
            if (ContentContainer.Height > gap)
                ContentContainer.Height -= gap;

            // If the last score is empty, cut off the content container so it doesn't scroll.
            if (Pool.LastOrDefault()?.Item.IsEmptyScore ?? false)
                ContentContainer.Height = Height;
        }

        public static bool RequiresOnline(LeaderboardType type) =>
            type switch
            {
                LeaderboardType.Global => true,
                LeaderboardType.Friends => true,
                LeaderboardType.Mods => true,
                LeaderboardType.Country => true,
                LeaderboardType.Local => false,
                _ => false
            };

        /// <summary>
        ///     If the leaderboard requires donator privileges
        /// </summary>
        /// <returns></returns>
        private bool RequiresDonator() => RequiresOnline(ConfigManager.LeaderboardSection.Value)
                                          && (ConfigManager.LeaderboardSection.Value == LeaderboardType.Country
                                          || ConfigManager.LeaderboardSection.Value == LeaderboardType.Friends
                                          || ConfigManager.LeaderboardSection.Value == LeaderboardType.Global);

        /// <summary>
        ///     Creates <see cref="StatusText"/>
        /// </summary>
        private void CreateStatusText()
        {
            StatusText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 20)
            {
                Parent = StatusContainer,
                Alignment = Alignment.MidCenter,
                Tint = SkinManager.Skin.SongSelect.LeaderboardStatusTextColor
            };
        }

        /// <summary>
        ///     Creates <see cref="UpdateButton"/>
        /// </summary>
        private void CreateUpdateButton()
        {
            var baseHeight = IsV2 ? 70 : 66;
            const int buttonHeight = 40;
            var centerOffset = (baseHeight - buttonHeight) / 2f;

            UpdateButton = new IconButton(UserInterface.BlankBox, (o, e) =>
                {
                    ThreadScheduler.Run(() => MapManager.UpdateMapToLatestVersion(MapManager.Selected.Value));
                    StartLoading();
                })
            {
                Parent = this,
                Alignment = Alignment.TopCenter,
                Size = new ScalableVector2(220, buttonHeight),
                Y = 4 * DrawableLeaderboardScore.ScoreHeight + centerOffset,
                Alpha = 0,
                IsPerformingFadeAnimations = false,
                IsClickable = false,
                Image = UserInterface.UpdateButton
            };
        }

        private void FadeStatusTextIn()
        {
            StatusText.ClearAnimations();
            StatusText.FadeTo(1, Easing.Linear, 250);
        }

        private void FadeStatusTextOut()
        {
            StatusText.ClearAnimations();
            StatusText.FadeTo(0, Easing.Linear, 250);
        }

        /// <summary>
        ///     Called upon retrieving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnScoresRetrieved(object? sender, TaskCompleteEventArgs<Map, FetchedScoreStore> e)
        {
            ScheduleUpdate(() =>
            {
                if (IsDisposed)
                    return;

                ClearPool();
                SnapToTop();

                AvailableItems = e.Result.Scores;

                if (AvailableItems == null)
                    return;

                const int MaxShownItems = 50;

                // We don't have enough scores in the leaderboard, so fill it with empty scores, so the leaderboard
                // still preserves the table look
                if (AvailableItems.Count < MaxShownItems)
                {
                    var count = MaxShownItems - AvailableItems.Count;

                    for (var i = 0; i < count; i++)
                        AvailableItems.Add(new Score { IsEmptyScore = true });
                }

                try
                {
                    CreatePool(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }

                FinishedLoading = true;
            });
        }

        /// <summary>
        ///     Snaps to the top of the container
        /// </summary>
        private void SnapToTop()
        {
            // Snap to the top of the container
            ContentContainer.Animations.Clear();
            ContentContainer.Y = 0;
            PreviousContentContainerY = ContentContainer.Y;
            TargetY = PreviousContentContainerY;
            PreviousTargetY = PreviousContentContainerY;

            PoolStartingIndex = 0;
        }

        private void ClearPool()
        {
            if (Pool is null)
                return;

            try
            {
                foreach (var x in new List<Drawable>(Pool))
                    x.Destroy();

                Pool.Clear();
            }
            catch (Exception e)
            {
                Wobble.Logging.Logger.Error(e, LogType.Runtime);
            }
        }
    }
}
