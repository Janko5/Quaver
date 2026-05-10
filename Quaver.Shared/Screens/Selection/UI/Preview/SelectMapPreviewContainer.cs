using System;
using System.Collections.Generic;
using System.Threading;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.API.Replays;
using Quaver.Shared.Assets;
using Quaver.Shared.Audio;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Graphs;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Gameplay;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.HitObjects;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Audio.Tracks;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Scheduling;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Preview
{
    public class SelectMapPreviewContainer : Sprite
    {
        /// <summary>
        /// </summary>
        internal Bindable<bool> IsPlayTesting { get; }

        /// <summary>
        /// </summary>
        private Bindable<SelectContainerPanel> ActiveLeftPanel { get; }

        /// <summary>
        /// </summary>
        private LoadingWheel? Wheel { get; set; }

        /// <summary>
        /// </summary>
        private TaskHandler<Map, GameplayScreen> LoadGameplayScreenTask { get; }

        /// <summary>
        ///     The gameplay screen instance that is currently loaded.
        /// </summary>
        protected GameplayScreen? LoadedGameplayScreen { get; private set; }

        /// <summary>
        ///     Tells the user to press tab to toggle autoplay
        /// </summary>
        private SpriteTextPlus? TestPlayPrompt { get; set; }

        /// <summary>
        ///     If true, it will never display the autoplay toggle more than once
        /// </summary>
        private bool ShownTestPlayPrompt { get; set; }

        /// <summary>
        ///     The audio track in the previous frame, so the replay can be seeked back if it changes
        /// </summary>
        private IAudioTrack? TrackInPreviousFrame { get; set; }

        /// <summary>
        ///     The custom audio track for this container
        /// </summary>
        private IAudioTrack? Track { get; set; }

        /// <summary>
        ///     The Qua that'll be used if one is passed in through the constructor
        /// </summary>
        protected Qua? Qua { get; }

        /// <summary>
        /// </summary>
        private DifficultySeekBar? SeekBar { get; set; }





        /// <summary>
        ///     If true, a difficulty seek bar will be created and displayed
        /// </summary>
        protected bool HasSeekBar { get; set; } = true;

        /// <summary>
        ///     If true, the first load will have a 0 delay.
        /// </summary>
        private bool IsFirstLoad { get; set; } = true;

        /// <summary>
        ///     The amount of delay before the task will run
        /// </summary>
        protected int DelayTime { get; set; } = 250;

        /// <summary>
        /// </summary>
        public SelectMapPreviewContainer(Bindable<bool> isPlayTesting, Bindable<SelectContainerPanel> activeLeftPanel, int height,
            IAudioTrack? track = null, Qua? qua = null)
        {
            IsPlayTesting = isPlayTesting;
            ActiveLeftPanel = activeLeftPanel;
            Qua = qua;
            Track = track;
            Size = new ScalableVector2(564, height);
            Alpha = 0f;





            LoadGameplayScreenTask = new TaskHandler<Map, GameplayScreen>(LoadGameplayScreen);
            LoadGameplayScreenTask.OnCompleted += OnLoadedGameplayScreen;

            CreateLoadingWheel();
            CreateTestPlayPrompt();

            MapManager.Selected.ValueChanged += OnMapChanged;
            ActiveLeftPanel.ValueChanged += OnLeftPanelChanged;
            SkinManager.SkinLoaded += OnSkinLoaded;
            ModManager.ModsChanged += OnModsChanged;

            if (Track != null)
                Track.Seeked += OnTrackSeeked;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateGameplayScreen(gameTime);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable twice DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;
            ActiveLeftPanel.ValueChanged -= OnLeftPanelChanged;
            SkinManager.SkinLoaded -= OnSkinLoaded;

            ModManager.ModsChanged -= OnModsChanged;

            if (Track != null)
                Track.Seeked -= OnTrackSeeked;

            LoadGameplayScreenTask?.Dispose();
            LoadedGameplayScreen?.Destroy();
            TestPlayPrompt?.Destroy();

            base.Destroy();
        }

        /// <summary>
        /// </summary>
        private void CreateLoadingWheel() => Wheel = new LoadingWheel
        {
            Parent = this,
            Alignment = Alignment.MidCenter,
            Size = new ScalableVector2(60, 60)
        };

        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private GameplayScreen LoadGameplayScreen(Map map, CancellationToken token)
        {
            return HandleLoadGameplayScreen(map, token);
        }

        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private GameplayScreen HandleLoadGameplayScreen(Map map, CancellationToken token)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long loadQuaTime = 0;

            try
            {
                var qua = Qua ?? map.LoadQua();
                loadQuaTime = sw.ElapsedMilliseconds;
                token.ThrowIfCancellationRequested();

                if (qua == Qua)
                    qua = qua.DeepClone();
                token.ThrowIfCancellationRequested();

                map.Qua = qua;
                map.Qua.ApplyMods(ModManager.Mods);

                var autoplay = Replay.GeneratePerfectReplayKeys(new Replay(qua.Mode, "Autoplay", 0, map.Md5Checksum), qua);
                token.ThrowIfCancellationRequested();

                var gameplay = new GameplayScreen(qua, map.Md5Checksum, new List<Score>(), 
                    replay: autoplay, isPlayTesting: true, playTestTime: 0, 
                    isCalibratingOffset: false, isSongSelectPreview: true);

                if (token.IsCancellationRequested)
                {
                    gameplay.Destroy();
                    token.ThrowIfCancellationRequested();
                }

                gameplay.HandleReplaySeeking();

                sw.Stop();
                Logger.Debug($"[MapPreview] Load took: {sw.ElapsedMilliseconds}ms (LoadQua: {loadQuaTime}ms, Construction: {sw.ElapsedMilliseconds - loadQuaTime}ms) | Map: {map}", LogType.Runtime);

                return gameplay;
            }
            catch (OperationCanceledException)
            {
                return null!;
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                return null!;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoadedGameplayScreen(object? sender, TaskCompleteEventArgs<Map, GameplayScreen> e)
        {
            if (e.Result == null)
                return;

            if (MapManager.Selected.Value != e.Input)
                return;

            e.Input.Qua = e.Result.Map;

            LoadedGameplayScreen = e.Result;

            AddScheduledUpdate(() =>
            {
                var playfield = (GameplayPlayfieldKeys)LoadedGameplayScreen.Ruleset.Playfield;

                playfield.Stage.HealthBar.Visible = false;
                playfield.Stage.HitBubbles.Visible = false;

                Wheel?.ClearAnimations();
                Wheel?.FadeTo(0, Easing.Linear, 250);

                playfield.Stage.FadeIn();
                playfield.Container.Parent = this;
                playfield.Container.UsePreviousSpriteBatchOptions = true;
                playfield.Container.Size = Size;
                playfield.Container.X = 0;



                playfield.ForegroundContainer.X = 0;
                playfield.BackgroundContainer.X = 0;
                playfield.Stage.HitLightingObjects.ForEach(x => x.StopHolding());

                var scroll = ConfigManager.ScrollDirections[LoadedGameplayScreen.Map.Mode];

                var skin = SkinManager.Skin.Keys[e.Result.Map.Mode];

                const int filterPanelHeight = 88;

                // Multiplier for preview to move the top half of the elements downwards by 11/15, as that is the amount that is covered by UI.
                const float previewMultiplier = 11 / 15f;

                var isSongSelect = LoadedGameplayScreen.IsSongSelectPreview;
                var screenY = ScreenRectangle.Y;

                switch (scroll.Value)
                {
                    case ScrollDirection.Down:
                    case ScrollDirection.Split:
                        playfield.Container.Alignment = Alignment.BotLeft;
                        playfield.Container.Y = isSongSelect ? -MenuBorder.HEIGHT - screenY : 0;


                        if (playfield.Stage.HitError.Y < 0)
                            playfield.Stage.HitError.Y *= previewMultiplier;

                        if (playfield.Stage.HitBubbles.Y < 0)
                            playfield.Stage.HitBubbles.Y *= previewMultiplier;

                        if (playfield.Stage.JudgementHitBursts[0].OriginalPosY < 0)
                            for (var i = 0; i < playfield.Stage.JudgementHitBursts.Count; i++)
                                playfield.Stage.JudgementHitBursts[i].OriginalPosY *= previewMultiplier;

                        if (playfield.Stage.ComboDisplay.OriginalPosY < 0)
                            playfield.Stage.ComboDisplay.OriginalPosY *= previewMultiplier;

                        playfield.Stage.ComboDisplay.Y = playfield.Stage.ComboDisplay.OriginalPosY;
                        break;
                    case ScrollDirection.Up:
                        playfield.Container.Alignment = Alignment.TopLeft;
                        playfield.Container.Y = 0;

                        if (isSongSelect)
                        {
                            playfield.Stage.HitError.Y -= filterPanelHeight + MenuBorder.HEIGHT + 10;

                            for (var i = 0; i < playfield.Stage.JudgementHitBursts.Count; i++)
                                playfield.Stage.JudgementHitBursts[i].OriginalPosY -= filterPanelHeight + MenuBorder.HEIGHT + 10;

                            playfield.Stage.ComboDisplay.OriginalPosY -= filterPanelHeight + MenuBorder.HEIGHT + 10;
                            playfield.Stage.HitBubbles.Y -= filterPanelHeight + MenuBorder.HEIGHT + 10;
                        }

                        if (playfield.Stage.HitError.Y < 0)
                            playfield.Stage.HitError.Y *= previewMultiplier;

                        if (playfield.Stage.JudgementHitBursts[0].OriginalPosY < 0)
                            for (var i = 0; i < playfield.Stage.JudgementHitBursts.Count; i++)
                                playfield.Stage.JudgementHitBursts[i].OriginalPosY *= previewMultiplier;

                        if (playfield.Stage.ComboDisplay.OriginalPosY < 0)
                            playfield.Stage.ComboDisplay.OriginalPosY *= previewMultiplier;

                        playfield.Stage.ComboDisplay.Y = playfield.Stage.ComboDisplay.OriginalPosY;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scroll), scroll.Value, null);
                }


                ShowTestPlayPrompt();
                CreateSeekBar(e.Input.Qua, playfield);
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object? sender, BindableValueChangedEventArgs<Map> e)
        {
            if (e.OldValue != null)
                e.OldValue.Qua = null!;

            RunLoadTask();
        }

        /// <summary>
        /// </summary>
        private void UpdateGameplayScreen(GameTime gameTime)
        {
            if (LoadedGameplayScreen != null && LoadedGameplayScreen.IsDisposed)
                return;

            if (MapManager.Selected.Value?.Qua == null)
                return;

            try
            {
                // Handle seeking when the track reloads
                if (AudioEngine.Track != TrackInPreviousFrame)
                {
                    if (SeekBar != null)
                        SeekBar.Track = AudioEngine.Track;

                    Track = AudioEngine.Track;
                    TrackInPreviousFrame = Track;

                    LoadedGameplayScreen?.HandleReplaySeeking();
                }

                if (ActiveLeftPanel.Value != SelectContainerPanel.MapPreview)
                {
                    IsPlayTesting.Value = false;

                    if (LoadedGameplayScreen != null)
                    {
                        var hiddenTrack = Track ?? AudioEngine.Track;
                        LoadedGameplayScreen.IsPaused = hiddenTrack.IsPaused || hiddenTrack.IsStopped;
                    }

                    return;
                }

                if (ActiveLeftPanel.Value == SelectContainerPanel.MapPreview)
                    LoadedGameplayScreen?.HandleAutoplayTabInput(gameTime);

                LoadedGameplayScreen?.Update(gameTime);
                IsPlayTesting.Value = !LoadedGameplayScreen?.InReplayMode ?? false;

                if (LoadedGameplayScreen != null)
                {
                    var track = Track ?? AudioEngine.Track;
                    LoadedGameplayScreen.IsPaused = track.IsPaused || track.IsStopped;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLeftPanelChanged(object? sender, BindableValueChangedEventArgs<SelectContainerPanel> e)
        {
            if (e.Value != SelectContainerPanel.MapPreview)
                return;

            ShowTestPlayPrompt();

            if (LoadedGameplayScreen == null)
                RunLoadTask();
        }

        /// <summary>
        /// </summary>
        protected void RunLoadTask()
        {
            CleanupLoadedResources();

            Wheel?.ClearAnimations();
            Wheel?.FadeTo(1, Easing.Linear, 150);

            var delay = IsFirstLoad ? 0 : DelayTime;
            LoadGameplayScreenTask.Run(MapManager.Selected.Value, delay);

            IsFirstLoad = false;
        }

        /// <summary>
        ///     Safely cleans up any loaded resources on the UI thread.
        /// </summary>
        private void CleanupLoadedResources()
        {
            if (LoadedGameplayScreen == null)
                return;

            if (TestPlayPrompt != null)
                TestPlayPrompt.Parent = null;

            if (LoadedGameplayScreen.Ruleset?.Playfield?.Container != null)
                LoadedGameplayScreen.Ruleset.Playfield.Container.Parent = null;

            LoadedGameplayScreen?.Destroy();
            LoadedGameplayScreen = null;

            SeekBar?.Destroy();
            SeekBar = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSkinLoaded(object? sender, SkinReloadedEventArgs e) => RunLoadTask();

        /// <summary>
        /// </summary>
        private void CreateTestPlayPrompt()
        {
            TestPlayPrompt = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold),
                "Press [TAB] to toggle play testing", 22)
            {
                Alignment = Alignment.TopCenter,
                Y = 175,
                DestroyIfParentIsNull = false
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object? sender, ModsChangedEventArgs e)
        {
            if (e.ChangedMods.HasFlag(ModIdentifier.None)) // why is ModIdentifier.None not 0
                return;

            if ((e.ChangedMods & ModIdentifier.SpeedMods) != 0 && LoadedGameplayScreen != null)
            {
                var screen = LoadedGameplayScreen;
                ScheduleUpdate(() =>
                {
                    if (screen != null && screen.Ruleset != null && screen.Ruleset.Playfield != null)
                    {
                        CreateSeekBar(screen.Map, (GameplayPlayfieldKeys)screen.Ruleset.Playfield, false);
                    }
                });
            }

            var reloadTriggers = e.ChangedMods
                                 & ~ModIdentifier.SpeedMods
                                 & ~ModIdentifier.Autoplay
                                 & ~ModIdentifier.Coop
                                 & ~ModIdentifier.Randomize;

            if (reloadTriggers != 0) //  once again why is ModIdentifier.None not 0
                RunLoadTask();
        }

        /// <summary>
        ///     Handles creating and initializing the seek bar that displays the map's difficulty
        /// </summary>
        private void CreateSeekBar(Qua? qua, GameplayPlayfieldKeys? playfield, bool animate = true)
        {
            if (!HasSeekBar)
                return;

            var oldSeekBar = SeekBar;

            if (playfield == null || qua == null)
            {
                oldSeekBar?.Destroy();
                return;
            }

            var stageRightWidth = playfield.Stage.StageRight == null ? 0 : (int)MathHelper.Clamp(playfield.Stage.StageRight.Width, 0, 8);

            SeekBar = new DifficultySeekBar(qua, ModManager.Mods, new ScalableVector2(56, Height), 200)
            {
                Alignment = Alignment.BotLeft,
                X = (Width + playfield.Width - 36) / 2,
                Tint = ColorHelper.HexToColor("#181818"),
                SetChildrenAlpha = true,
            };

            if (animate)
                SeekBar.Alpha = 0;

            _ = new Sprite
            {
                Parent = SeekBar,
                Size = new ScalableVector2(2, SeekBar.Height),
                Tint = ColorHelper.HexToColor("#808080")
            };

            _ = new Sprite
            {
                Parent = SeekBar,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(2, SeekBar.Height),
                Tint = ColorHelper.HexToColor("#808080")
            };

            SeekBar.AudioSeeked += (o, args) => RefreshScreen();

            if (qua != MapManager.Selected.Value.Qua)
            {
                oldSeekBar?.Destroy();
                SeekBar.Destroy();
                return;
            }

            var newSeekBar = SeekBar;

            AddScheduledUpdate(() =>
            {
                oldSeekBar?.Destroy();

                if (newSeekBar == null || newSeekBar.IsDisposed || IsDisposed)
                    return;

                if (playfield == null || playfield.Container == null || playfield.Container.IsDisposed)
                    return;

                newSeekBar.Parent = this;

                if (animate)
                {
                    newSeekBar.FadeTo(1, Easing.Linear, 300);

                    playfield.Container.X = -38;

                    if (qua.HasScratchKey)
                        newSeekBar.X += SkinManager.Skin.Keys[qua.Mode].ScratchLaneSize / 4f;

                    playfield.Container.X += 2;
                }

                if (TestPlayPrompt != null)
                    TestPlayPrompt.X = -newSeekBar.Width / 2f + 2;
            });
        }

        /// <summary>
        /// </summary>
        private void ShowTestPlayPrompt()
        {
            if (ShownTestPlayPrompt || ActiveLeftPanel.Value != SelectContainerPanel.MapPreview)
                return;

            if (LoadedGameplayScreen == null)
                return;

            if (TestPlayPrompt != null)
            {
                TestPlayPrompt.DestroyIfParentIsNull = false;
                TestPlayPrompt.Parent = this;
                TestPlayPrompt.Alpha = 0;

                if (!ShownTestPlayPrompt)
                {
                    TestPlayPrompt.FadeTo(1, Easing.Linear, 300);
                    TestPlayPrompt.Wait(3000);
                    TestPlayPrompt.FadeTo(0, Easing.Linear, 300);

                    ShownTestPlayPrompt = true;
                }
            }
        }

        /// <summary>
        /// </summary>
        protected void RefreshScreen()
        {
            if (LoadedGameplayScreen == null)
                return;

            if (LoadedGameplayScreen.InReplayMode)
                LoadedGameplayScreen.HandleReplaySeeking();
            else
            {
                var hitobjectManager = (HitObjectManagerKeys)LoadedGameplayScreen.Ruleset.HitObjectManager;
                hitobjectManager.HandleSkip();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTrackSeeked(object? sender, TrackSeekedEventArgs e) => RefreshScreen();
    }
}
