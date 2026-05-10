using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Database.Profiles;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Helpers;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Skinning;
using Quaver.Shared.Screens.Menu.UI.Visualizer;
using Quaver.Shared.Screens.Visualizer;
using Quaver.Shared.Screens.Selection.Components;
using Quaver.Shared.Screens.Selection.UI;
using Quaver.Shared.Screens.Selection.UI.Background;
using Quaver.Shared.Screens.Selection.UI.Borders.Footer;
using Quaver.Shared.Screens.Selection.UI.FilterPanel;
using Quaver.Shared.Screens.Selection.UI.Leaderboard;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Quaver.Shared.Screens.Selection.UI.Modifiers;
using Quaver.Shared.Screens.Selection.UI.Playlists;
using Quaver.Shared.Screens.Selection.UI.Playlists.Dialogs.Create;
using Quaver.Shared.Screens.Selection.UI.Preview;
using Quaver.Shared.Database.Judgements;
using Quaver.API.Maps.Processors.Scoring;
using Quaver.Shared.Screens.Selection.UI.Profile;
using Quaver.Shared.Screens.Tests.UI.Borders;
using Quaver.Shared.Modifiers.Mods;
using Quaver.Shared.Graphics.Components;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Screens;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection
{
    public class SelectionScreenView : ScreenView
    {
        /// <summary>
        /// </summary>
        private SelectionScreen SelectScreen => (SelectionScreen)Screen;

        /// <summary>
        ///     Plays the audio for the song select screen
        /// </summary>
        private SelectJukebox Jukebox { get; set; }

        /// <summary>
        /// </summary>
        private BackgroundImage Background { get; set; }

        /// <summary>
        /// </summary>
        private MenuBorder Header { get; set; }

        /// <summary>
        /// </summary>
        private MenuBorder Footer { get; set; }

        private Drawable Visualizer { get; set; }

        /// <summary>
        /// </summary>
        private SelectFilterPanel FilterPanel { get; set; }

        /// <summary>
        /// </summary>
        private LeaderboardContainer LeaderboardContainer { get; set; }

        /// <summary>
        /// </summary>
        private ModifierSelectorContainer ModifierSelector { get; set; }

        /// <summary>
        /// </summary>
        public MapsetScrollContainer MapsetContainer { get; private set; }

        /// <summary>
        /// </summary>
        public MapScrollContainer MapContainer { get; private set; }

        /// <summary>
        /// </summary>
        private PlaylistContainer PlaylistContainer { get; set; }

         /// <summary>
         /// </summary>
         private SelectMapPreviewContainer MapPreviewContainer { get; set; }

         /// <summary>
         /// </summary>
         private PreviewClippingContainer MapPreviewWrapper { get; set; }

        /// <summary>
        /// </summary>
        private Sprite? TabsPanel { get; set; }

        /// <summary>
        /// </summary>
        private TabButton? ModsTabButton { get; set; }

        /// <summary>
        /// </summary>

        /// <summary>
        ///     The position of the active panel on the left
        /// </summary>
        private const int ScreenPaddingX = 35;  // 10px gap from list + 15px scrollbar + 10px from screen edge

        /// <summary>
        ///     The left-side padding for the leaderboard/modifiers panel. V2 uses 20px, V1 uses ScreenPaddingX.
        /// </summary>
        private static int LeftPanelPaddingX => (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f ? 20 : ScreenPaddingX;

        /// <summary>
        ///     The amount of y-axis space between <see cref="FilterPanel"/> and the left panel
        /// </summary>
        private const int LeftPanelSpacingY = 20;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="screen"></param>
        public SelectionScreenView(SelectionScreen screen) : base(screen)
        {
            CreateJukebox();
            CreateBackground();
            CreateHeader();
            CreateFooter();
            CreateAudioVisualizer();
            CreateFilterPanel();
            CreateMapsetContainer();
            CreateMapContainer();
            CreateMapPreviewContainer();
            CreatePlaylistContainer();

            CreateLeaderboardContainer();
            CreateModifierSelectorContainer();
            CreateTabsPanel();
            ReorderContainerLayerDepth();


            SelectScreen.ActiveLeftPanel.ValueChanged += OnActiveLeftPanelChanged;
            SelectScreen.AvailableMapsets.ValueChanged += OnAvailableMapsetsChanged;
            SelectScreen.ActiveScrollContainer.ValueChanged += OnActiveScrollContainerChanged;
            MapsetContainer.ContainerInitialized += OnMapsetContainerInitialized;
            SelectScreen.ScreenExiting += OnExiting;
            ConfigManager.SelectGroupMapsetsBy.ValueChanged += OnGroupingChanged;
            PlaylistManager.PlaylistCreated += OnPlaylistCreated;
            PlaylistManager.PlaylistDeleted += OnPlaylistDeleted;
            PlaylistManager.PlaylistSynced += OnPlaylistSynced;
            PlaylistContainer.ContainerInitialized += OnPlaylistContainerInitialized;
            if (FilterPanel.SearchBox != null)
                FilterPanel.SearchBox.OnStoppedTyping += OnSearchingStopped;
            if (FilterPanel.SearchBoxV2 != null)
                FilterPanel.SearchBoxV2.OnStoppedTyping += OnSearchingStopped;

            // Trigger a scroll container change, to bring in the correct container
            SelectScreen.ActiveScrollContainer.TriggerChange();
            SelectScreen.ActiveLeftPanel.TriggerChange();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime) => Container?.Update(gameTime);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            GameBase.Game.GraphicsDevice.Clear(ColorHelper.HexToColor("#2F2F2F"));
            Container?.Draw(gameTime);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            Container?.Destroy();
            TabsPanel?.Destroy();

            // ReSharper disable twice DelegateSubtraction
            SelectScreen.ActiveLeftPanel.ValueChanged -= OnActiveLeftPanelChanged;
            MapsetContainer.ContainerInitialized -= OnMapsetContainerInitialized;
            ConfigManager.SelectGroupMapsetsBy.ValueChanged -= OnGroupingChanged;
            PlaylistManager.PlaylistCreated -= OnPlaylistCreated;
            PlaylistManager.PlaylistDeleted -= OnPlaylistDeleted;
            PlaylistManager.PlaylistSynced -= OnPlaylistSynced;
            PlaylistContainer.ContainerInitialized -= OnPlaylistContainerInitialized;
            SelectScreen.ScreenExiting -= OnExiting;
            SelectScreen.AvailableMapsets.ValueChanged -= OnAvailableMapsetsChanged;
            SelectScreen.ActiveScrollContainer.ValueChanged -= OnActiveScrollContainerChanged;
            ModManager.ModsChanged -= OnModsChanged;

            if (JudgementWindowsDatabaseCache.Selected != null)
                JudgementWindowsDatabaseCache.Selected.ValueChanged -= OnJudgementWindowsChanged;

            if (FilterPanel != null)
                FilterPanel.HeightChanged -= OnFilterPanelHeightChanged;

            if (FilterPanel?.SearchBox != null)
                FilterPanel.SearchBox.OnStoppedTyping -= OnSearchingStopped;
            if (FilterPanel?.SearchBoxV2 != null)
                FilterPanel.SearchBoxV2.OnStoppedTyping -= OnSearchingStopped;
        }

        /// <summary>
        ///     Creates <see cref="Jukebox"/>
        /// </summary>
        private void CreateJukebox() => Jukebox = new SelectJukebox(SelectScreen) { Parent = Container };

        /// <summary>
        ///     Creates <see cref="Background"/>
        /// </summary>
        private void CreateBackground()
            => Background = new SongSelectBackground() { Parent = Container };

        /// <summary>
        ///     Creates <see cref="Header"/>
        /// </summary>
        private void CreateHeader() => Header = new MenuHeaderMain() { Parent = Container };

        /// <summary>
        ///     Creates <see cref="Footer"/>
        /// </summary>
        private void CreateFooter() => Footer = new SelectMenuFooter(SelectScreen)
        {
            Parent = Container,
            Alignment = Alignment.BotLeft
        };

        /// <summary>
        ///     Creates <see cref="Visualizer"/>
        /// </summary>
        private void CreateAudioVisualizer()
        {
            var visualizerType = SkinManager.Skin.MusicVisualizer.MusicVisualizerType;

            if (visualizerType == 0)
                return;

            if (visualizerType == 2)
            {
                Visualizer = new DotMatrixAudioVisualizer(700, (int)Footer.Height, 4, 2)
                {
                    Parent = Footer,
                    Alignment = Alignment.BotLeft,
                    X = (WindowManager.Width - 700) / 2,
                    Y = 0,
                };
            }
            else if (visualizerType == 3)
            {
                Visualizer = new WaveAudioVisualizer(700, (int)Footer.Height, 8)
                {
                    Parent = Footer,
                    Alignment = Alignment.BotLeft,
                    X = (WindowManager.Width - 700) / 2,
                    Y = 0,
                };
            }
            else
            {
                var barVisualizer = new MenuAudioVisualizer(700, (int)Footer.Height, 100, 3, 4)
                {
                    Parent = Footer,
                    Alignment = Alignment.BotLeft,
                    X = (WindowManager.Width - 700) / 2,
                    Y = 0,
                };

                // Respect skin alpha if MusicVisualizer section is present
                if (SkinManager.Skin?.MusicVisualizer == null || SkinManager.Skin.Config == null || !SkinManager.Skin.Config.Sections.ContainsSection("MusicVisualizer"))
                    barVisualizer.Bars.ForEach(x => x.Alpha = 1.0f);

                Visualizer = barVisualizer;
            }
        }

        /// <summary>
        ///     Creates <see cref="FilterPanel"/>
        /// </summary>
        private void CreateFilterPanel()
        {
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;

            FilterPanel = new SelectFilterPanel(SelectScreen.AvailableMapsets,
                SelectScreen.CurrentSearchQuery, SelectScreen.IsPlayTestingInPreview, SelectScreen.ActiveLeftPanel)
            {
                Parent = Container,
                Alignment = version == 2.0f ? Alignment.TopRight : Alignment.TopLeft,
                Y = version == 2.0f
                    ? Header.Height + 10  // V2.0: 10px below header
                    : Header.Height + Header.ForegroundLine.Height - 2  // V1.0: current position
            };

            FilterPanel.HeightChanged += OnFilterPanelHeightChanged;
        }

        /// <summary>
        ///     Creates <see cref="LeaderboardContainer"/>
        /// </summary>
        private void CreateLeaderboardContainer()
        {
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;
            LeaderboardContainer = new LeaderboardContainer
            {
                Parent = Container,
                Alignment = Alignment.TopLeft,
                X = LeftPanelPaddingX,
                Y = version == 2.0f ? Header.Height + 80 : FilterPanel.Y + FilterPanel.Height + LeftPanelSpacingY
            };

            LeaderboardContainer.X = -LeaderboardContainer.Width - LeftPanelPaddingX;
            LeaderboardContainer.MoveToX(LeftPanelPaddingX, Easing.OutQuint, 500);
        }

        /// <summary>
        ///     Creates <see cref="ModifierSelector"/>
        /// </summary>
        private void CreateModifierSelectorContainer()
        {
            ModifierSelector = new ModifierSelectorContainer(SelectScreen.ActiveLeftPanel)
            {
                Parent = Container,
                Y = LeaderboardContainer.Y
            };

            ModifierSelector.X = -ModifierSelector.Width - ScreenPaddingX;
        }

        /// <summary>
        ///     Creates <see cref="MapPreviewContainer"/>
        /// </summary>
        private void CreateMapPreviewContainer()
        {
             var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;
             var containerY = version == 2.0f ? Header.Height + 70 : FilterPanel.Y + FilterPanel.Height;
             var containerHeight = version == 2.0f ? (int)(WindowManager.Height - containerY - Footer.Height) : (int)(WindowManager.Height - MenuBorder.HEIGHT * 2 - FilterPanel.Height);

             MapPreviewWrapper = new PreviewClippingContainer
             {
                 Parent = Container,
                 Y = containerY,
                 Size = new ScalableVector2((SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f ? 725 : 564, containerHeight)
             };

             MapPreviewContainer = new SelectMapPreviewContainer(SelectScreen.IsPlayTestingInPreview, SelectScreen.ActiveLeftPanel, containerHeight)
             {
                 Parent = MapPreviewWrapper,
                 Y = 0
             };

             MapPreviewContainer.X = -MapPreviewContainer.Width - ScreenPaddingX;
        }

        /// <summary>
        ///     Creates <see cref="TabsPanel"/>
        /// </summary>
        private void CreateTabsPanel()
        {
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;
            if (version != 2.0f)
                return;

            TabsPanel = new Sprite
            {
                Parent = Container,
                Image = SkinManager.Skin?.SongSelect?.TabsPanel ?? UserInterface.TabsPanel,
                Size = new ScalableVector2(725, 60),
                Alignment = Alignment.TopLeft,
                X = 20,
                Y = Header.Height + 10
            };

            var lbButton = new TabButton("Leaderboard", () => SelectScreen.ActiveLeftPanel.Value = SelectContainerPanel.Leaderboard,
                () => SelectScreen.ActiveLeftPanel.Value == SelectContainerPanel.Leaderboard)
            {
                Parent = TabsPanel,
                X = 10f,
                Y = (TabsPanel.Height - TabButton.StandardHeight) / 2
            };

            ModsTabButton = new TabButton("Modifiers", () => SelectScreen.ActiveLeftPanel.Value = SelectContainerPanel.Modifiers,
                () => SelectScreen.ActiveLeftPanel.Value == SelectContainerPanel.Modifiers, null, true)
            {
                Parent = TabsPanel,
                X = lbButton.X + lbButton.Width + 10f,
                Y = lbButton.Y
            };

            var previewButton = new TabButton("", () => SelectScreen.ActiveLeftPanel.Value = SelectContainerPanel.MapPreview,
                () => SelectScreen.ActiveLeftPanel.Value == SelectContainerPanel.MapPreview,
                TextureManager.Load("Quaver.Resources/Textures/UI/SongSelect/LeftPanel/icon-view-map.png"))
            {
                Parent = TabsPanel,
                X = TabsPanel.Width - TabButton.StandardHeight - 10f,
                Y = lbButton.Y
            };

            // Set initial judgement window icon
            if (JudgementWindowsDatabaseCache.Selected != null)
                ModsTabButton.UpdateValueIcon(GetJudgementWindowTexture(JudgementWindowsDatabaseCache.Selected.Value.Name));

            // Update icon when judgement window changes
            if (JudgementWindowsDatabaseCache.Selected != null)
                JudgementWindowsDatabaseCache.Selected.ValueChanged += OnJudgementWindowsChanged;

            // Set initial mods
            ModsTabButton.UpdateModIcons(ModManager.CurrentModifiersList);

            // Update mods when they change
            ModManager.ModsChanged += OnModsChanged;
        }

        /// <summary>
        ///     Maps the judgement window name to the texture path.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetJudgementWindowTexture(string name)
        {
            var suffix = "Custom";

            if (name.Contains("Chill"))
                suffix = "CHILL";
            else if (name.Contains("Extreme"))
                suffix = "EXT";
            else if (name.Contains("Impossible"))
                suffix = "IMP";
            else if (name.Contains("Lenient"))
                suffix = "LEN";
            else if (name.Contains("Peaceful"))
                suffix = "PEAC";
            else if (name.Contains("Standard"))
                suffix = "STD";
            else if (name.Contains("Strict"))
                suffix = "STR";
            else if (name.Contains("Tough"))
                suffix = "TOU";

            return $@"Quaver.Resources/Textures/UI/Mods/JW-{suffix}.png";
        }

        private void OnJudgementWindowsChanged(object sender, BindableValueChangedEventArgs<JudgementWindows> args)
        {
            ModsTabButton?.ScheduleUpdate(() => ModsTabButton?.UpdateValueIcon(GetJudgementWindowTexture(args.Value.Name)));
        }

        private void OnModsChanged(object sender, ModsChangedEventArgs args)
        {
            ModsTabButton?.ScheduleUpdate(() => ModsTabButton?.UpdateModIcons(ModManager.CurrentModifiersList));
        }



        /// <summary>
        ///     Creates <see cref="MapsetContainer"/>
        /// </summary>
        private void CreateMapsetContainer()
        {
            // Calculate container Y and height for proper spacing
            var containerY = FilterPanel.Y + FilterPanel.Height + 10;  // 10px gap from filter panel
            var containerHeight = WindowManager.Height - containerY - Footer.Height - 10;  // 10px gap from footer

            MapsetContainer = new MapsetScrollContainer(SelectScreen.AvailableMapsets, SelectScreen.ActiveScrollContainer)
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                Y = containerY,
                Height = containerHeight
            };

            // Update scrollbar to match new container height
            MapsetContainer.UpdateScrollbarHeight();

            MapsetContainer.X = MapsetContainer.Width + ScreenPaddingX;
        }

        /// <summary>
        ///     Creates <see cref="MapContainer"/>
        /// </summary>
        private void CreateMapContainer()
        {
            MapContainer = new MapScrollContainer(SelectScreen.AvailableMapsets, MapsetContainer,
                MapManager.Selected.Value?.Mapset?.Maps, SelectScreen.ActiveScrollContainer)
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                Y = MapsetContainer.Y,
                Height = MapsetContainer.Height  // Same height as MapsetContainer
            };

            MapContainer.X = MapContainer.Width + ScreenPaddingX;
        }

        /// <summary>
        /// </summary>
        private void CreatePlaylistContainer()
        {
            PlaylistContainer = new PlaylistContainer(SelectScreen.ActiveScrollContainer)
            {
                Parent = Container,
                Alignment = Alignment.TopRight,
                Y = MapsetContainer.Y,
                Height = MapsetContainer.Height  // Same height as MapsetContainer
            };

            PlaylistContainer.X = PlaylistContainer.Width + ScreenPaddingX;
            PlaylistContainer.InitializePlaylists(false);
        }

        /// <summary>
        ///     Handles animations when the active left panel has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnActiveLeftPanelChanged(object sender, BindableValueChangedEventArgs<SelectContainerPanel> e)
        {
            LeaderboardContainer.ClearAnimations();
            ModifierSelector.ClearAnimations();

            const int animTime = 400;
            const Easing easing = Easing.OutQuint;
            var leftPadding = LeftPanelPaddingX;
            var inactivePos = -LeaderboardContainer.Width - leftPadding;

            switch (e.Value)
            {
                case SelectContainerPanel.Leaderboard:
                    LeaderboardContainer.MoveToX(leftPadding, easing, animTime);
                    MapPreviewContainer.MoveToX(inactivePos, easing, animTime);
                    ModifierSelector.MoveToX(inactivePos, easing, animTime);

                    LeaderboardContainer.FetchScores();
                    break;
                case SelectContainerPanel.Modifiers:
                    LeaderboardContainer.MoveToX(inactivePos, easing, animTime);
                    MapPreviewContainer.MoveToX(inactivePos, easing, animTime);
                    ModifierSelector.MoveToX(leftPadding, easing, animTime);
                    break;
                case SelectContainerPanel.MapPreview:
                    LeaderboardContainer.MoveToX(inactivePos, easing, animTime);
                    ModifierSelector.MoveToX(inactivePos, easing, animTime);

                    var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;
                    var targetX = (float)leftPadding;

                    if (version == 2.0f && TabsPanel != null)
                        targetX = TabsPanel.X + (TabsPanel.Width - MapPreviewContainer.Width) / 2;

                    MapPreviewContainer.MoveToX(targetX, easing, animTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Called when new available mapsets are changed.
        ///     This will effectively perform an animation to bring forward the mapset container
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAvailableMapsetsChanged(object sender, BindableValueChangedEventArgs<List<Mapset>> e)
        {
            if (SelectScreen.ActiveScrollContainer.Value == SelectScrollContainerType.Playlists)
                return;

            SelectScreen.ActiveScrollContainer.Value = SelectScrollContainerType.Mapsets;

            MapsetContainer.ClearAnimations();

            const int animTime = 500;
            MapsetContainer.MoveToX(MapsetContainer.Width + ScreenPaddingX, Easing.OutQuint, animTime);
        }

        /// <summary>
        ///     Handles bringing the correct scroll container forward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnActiveScrollContainerChanged(object sender, BindableValueChangedEventArgs<SelectScrollContainerType> e)
        {
            var inactivePosition = MapsetContainer.Width + ScreenPaddingX;
            const int activePosition = -ScreenPaddingX;
            const int animTime = 450;
            const Easing easing = Easing.OutQuint;

            MapContainer.ClearAnimations();
            PlaylistContainer.ClearAnimations();
            MapsetContainer.ClearAnimations();
            switch (e.Value)
            {
                case SelectScrollContainerType.Mapsets:
                    // Skip MapsetContainer animation if coming from Playlists - the pool was destroyed
                    // and OnMapsetContainerInitialized will handle the animation after reinitialization.
                    // This prevents duplicate/competing animations for a smoother transition.
                    if (e.OldValue != SelectScrollContainerType.Playlists)
                    {
                        MapsetContainer.MoveToX(activePosition, easing, animTime);
                    }
                    MapContainer.MoveToX(inactivePosition, easing, animTime);
                    PlaylistContainer.MoveToX(inactivePosition, easing, animTime);
                    break;
                case SelectScrollContainerType.Maps:
                    MapContainer.MoveToX(activePosition, easing, animTime);
                    MapsetContainer.MoveToX(inactivePosition, easing, animTime);
                    PlaylistContainer.MoveToX(inactivePosition, easing, animTime);
                    break;
                case SelectScrollContainerType.Playlists:
                    PlaylistContainer.MoveToX(activePosition, easing, animTime);

                    MapsetContainer.MoveToX(inactivePosition, easing, animTime);
                    MapContainer.MoveToX(inactivePosition, easing, animTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Makes sure the Menu headers/footers display on top of the scroll container
        ///     and that the correct buttons are clickable based on depth
        /// </summary>
        private void ReorderContainerLayerDepth()
        {
            // Ensure FilterPanel is drawn after Leaderboard (and everything else usually)
            // by re-adding it to the container (moves to end of list)
            FilterPanel.Parent = Container;

            // Ensure Header, Footer and Visualizer are topmost
            Header.Parent = Container;
            Footer.Parent = Container;
            
            if (Visualizer != null)
                Visualizer.Parent = Container;
        }

        /// <summary>
        ///     Animations perform when the screen exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExiting(object sender, ScreenExitingEventArgs e)
        {
            LeaderboardContainer.ClearAnimations();
            ModifierSelector.ClearAnimations();
            MapContainer.ClearAnimations();
            MapsetContainer.ClearAnimations();
            PlaylistContainer.ClearAnimations();
            MapPreviewContainer.ClearAnimations();

            const Easing easing = Easing.OutQuint;
            const int time = 400;

            var leftPadding = LeftPanelPaddingX;
            LeaderboardContainer.MoveToX(-LeaderboardContainer.Width - leftPadding, easing, time);
            ModifierSelector.MoveToX(-ModifierSelector.Width - leftPadding, easing, time);
            MapPreviewContainer.MoveToX(-MapPreviewContainer.Width - leftPadding, easing, time);

            MapContainer.MoveToX(MapContainer.Width + ScreenPaddingX, easing, time);
            MapsetContainer.MoveToX(MapsetContainer.Width + ScreenPaddingX, easing, time);
            PlaylistContainer.MoveToX(PlaylistContainer.Width + ScreenPaddingX, easing, time);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapsetContainerInitialized(object sender, SelectContainerInitializedEventArgs e)
        {
            if (SelectScreen.ActiveScrollContainer.Value != SelectScrollContainerType.Mapsets)
                return;

            MapsetContainer.ClearAnimations();
            MapsetContainer.MoveToX(-ScreenPaddingX, Easing.OutQuint, 600);
        }

        /// <summary>
        ///     Called when the user changes their grouping setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnGroupingChanged(object sender, BindableValueChangedEventArgs<GroupMapsetsBy> e)
        {
            switch (e.Value)
            {
                case GroupMapsetsBy.None:
                    // We want to completely destroy the pool to prevent the mapsets from
                    // coming in and displaying prematurely
                    if (SelectScreen.ActiveScrollContainer.Value == SelectScrollContainerType.Playlists)
                    {
                        SelectScreen.AvailableMapsets.Value = new List<Mapset>();
                        MapsetContainer.Schedule(() => MapsetContainer.DestroyPool());
                    }

                    SelectScreen.ActiveScrollContainer.Value = SelectScrollContainerType.Mapsets;
                    break;
                case GroupMapsetsBy.Playlists:
                    SelectScreen.ActiveScrollContainer.Value = SelectScrollContainerType.Playlists;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Called when a new playlist has been created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistCreated(object sender, PlaylistCreatedEventArgs e) => ReInitializePlaylists();

        /// <summary>
        ///     Called when a playlist has been deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistDeleted(object sender, PlaylistDeletedEventArgs e) => ReInitializePlaylists();

        /// <summary>
        ///     Called when a playlist has been synced to an online map pool
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistSynced(object sender, PlaylistSyncedEventArgs e)
        {
            PlaylistManager.Selected.Value = e.Playlist;
            ReInitializePlaylists();
        }

        /// <summary>
        ///     Handles reinitializing the playlist container with animations
        /// </summary>
        private void ReInitializePlaylists()
        {
            switch (SelectScreen.ActiveScrollContainer.Value)
            {
                case SelectScrollContainerType.Mapsets:
                    PlaylistContainer.Schedule(() => PlaylistContainer.DestroyPool());
                    MapsetContainer.ClearAnimations();
                    MapsetContainer.MoveToX(MapsetContainer.Width + ScreenPaddingX, Easing.OutQuint, 450);

                    // Add a delay before updating these values to account for animations
                    ThreadScheduler.RunAfter(() =>
                    {
                        ConfigManager.SelectGroupMapsetsBy.Value = GroupMapsetsBy.Playlists;
                        SelectScreen.ActiveScrollContainer.Value = SelectScrollContainerType.Playlists;
                    }, 250);
                    break;
                case SelectScrollContainerType.Playlists:
                    PlaylistContainer.ClearAnimations();
                    PlaylistContainer.MoveToX(PlaylistContainer.Width + ScreenPaddingX, Easing.OutQuint, 450);
                    break;
            }

            PlaylistContainer.InitializePlaylists(true);
        }

        /// <summary>
        ///     Called when the playlist container has been initialized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistContainerInitialized(object sender, SelectContainerInitializedEventArgs e)
        {
            if (SelectScreen.ActiveScrollContainer.Value != SelectScrollContainerType.Playlists)
                return;

            PlaylistContainer.MoveToX(-ScreenPaddingX, Easing.OutQuint, 600);
        }

        /// <summary>
        ///     Called when the user stops typing to search. Used for immediately animating
        /// </summary>
        /// <param name="obj"></param>
        private void OnSearchingStopped(string obj)
        {
            MapsetContainer.ClearAnimations();
            MapContainer.ClearAnimations();

            const Easing easing = Easing.OutQuint;
            const int time = 400;

            MapContainer.MoveToX(MapContainer.Width + ScreenPaddingX, easing, time);
            MapsetContainer.MoveToX(MapsetContainer.Width + ScreenPaddingX, easing, time);
        }
        /// <summary>
        ///     Handles the filter panel resizing logic
        /// </summary>
        /// <param name="targetPanelHeight"></param>
        private void OnFilterPanelHeightChanged(float targetPanelHeight)
        {
            // Calculate new Y and Height for the containers
            // V2: FilterPanel.Y + Height + 10px spacing
            var containerY = FilterPanel.Y + targetPanelHeight + 10;

            // Height = ScreenHeight - Y - Footer - 10px spacing
            var containerHeight = WindowManager.Height - containerY - Footer.Height - 10;

            // Animate MapsetContainer
            if (MapsetContainer != null)
            {
                MapsetContainer.Animations.Clear();
                MapsetContainer.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, MapsetContainer.Y, containerY, 200));
                MapsetContainer.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, MapsetContainer.Height, containerHeight, 200));

                // Force update scrollbar height immediately (it might animate better if synced in Update, but good enough)
                // Actually, since we animate property, we can't easily sync explicitly in compiled code without loop.
                // We'll trust Container Update loop or just set it at the end? 
                // Since we can't hook into "OnComplete", we'll just set it.
                // Wait, if I set it once, it won't animate.
                // Scrollbar background (NineSlice) height should ideally animate.
                // But ScrollbarBackground is protected in that class.
            }

            // Animate MapContainer
            if (MapContainer != null)
            {
                MapContainer.Animations.Clear();
                MapContainer.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, MapContainer.Y, containerY, 200));
                MapContainer.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, MapContainer.Height, containerHeight, 200));
            }

            // Animate PlaylistContainer
            if (PlaylistContainer != null)
            {
                PlaylistContainer.Animations.Clear();
                PlaylistContainer.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, PlaylistContainer.Y, containerY, 200));
                PlaylistContainer.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, PlaylistContainer.Height, containerHeight, 200));
            }

             // Animate MapPreviewContainer
             var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;
             if (version != 2.0f && MapPreviewWrapper != null)
             {
                 // MapPreviewWrapper should reach edges (no 10px gaps) to keep SeekBar full-height
                 var previewY = FilterPanel.Y + targetPanelHeight;
                 var previewHeight = WindowManager.Height - previewY - Footer.Height;

                 MapPreviewWrapper.Animations.Clear();
                 MapPreviewWrapper.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, MapPreviewWrapper.Y, previewY, 200));
                 MapPreviewWrapper.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, MapPreviewWrapper.Height, previewHeight, 200));

                 if (MapPreviewContainer != null)
                 {
                     MapPreviewContainer.Animations.Clear();
                     MapPreviewContainer.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, MapPreviewContainer.Height, previewHeight, 200));
                 }
             }
        }


         /// <summary>
         ///     A container that clips children using Wobble's native SpriteBatch management.
         /// </summary>
         private class PreviewClippingContainer : Container
         {
             public SpriteBatchOptions Options { get; }

             public PreviewClippingContainer()
             {
                 Options = new SpriteBatchOptions
                 {
                     SortMode = SpriteSortMode.Deferred,
                     BlendState = BlendState.NonPremultiplied,
                     RasterizerState = new RasterizerState { ScissorTestEnable = true },
                 };
             }



             public override void Draw(GameTime gameTime)
             {
                 if (!Visible)
                     return;

                 // 1. End the current default batch.
                 GameBase.Game.TryEndBatch();

                 // 2. Start our scissored batch.
                 Options.Begin();

                 // 3. LIE to the children (Sprites).
                 // If they see DefaultSpriteBatchInUse == true, they will draw into our scissored batch
                 // instead of starting a new unclipped default one.
                 var oldInUse = GameBase.DefaultSpriteBatchInUse;
                 GameBase.DefaultSpriteBatchInUse = true;

                 // 4. Calculate and set the ScissorRectangle.
                 var widthScale = GameBase.Game.Graphics.PreferredBackBufferWidth / WindowManager.Width;
                 var heightScale = GameBase.Game.Graphics.PreferredBackBufferHeight / WindowManager.Height;

                 // Boundary calculation: 
                 // Top: Max of (Container Start) and (Tabs Bottom 130 + 10px gap = 140).
                 // This ensures it never draws over tabs.
                 var topGlobal = Math.Max(ScreenRectangle.Y, 140);
                 var bottomGlobal = ScreenRectangle.Y + ScreenRectangle.Height;

                 var rect = new Rectangle
                 {
                     X = (int)(ScreenRectangle.X * widthScale),
                     Y = (int)(topGlobal * heightScale),
                     Width = (int)(ScreenRectangle.Width * widthScale),
                     Height = (int)((bottomGlobal - topGlobal) * heightScale),
                 };

                 // Clamp to screen
                 var viewport = GameBase.Game.GraphicsDevice.Viewport;
                 var intersection = Rectangle.Intersect(rect, new Rectangle(0, 0, viewport.Width, viewport.Height));

                 if (intersection.Width > 0 && intersection.Height > 0)
                     GameBase.Game.GraphicsDevice.ScissorRectangle = intersection;

                 // 5. Draw children (MapPreviewContainer -> LoadedGameplayScreen -> Playfield)
                 base.Draw(gameTime);

                 // 6. End our scissored batch and restore the engine state.
                 GameBase.Game.TryEndBatch();
                 GameBase.DefaultSpriteBatchInUse = oldInUse;

                 if (oldInUse)
                     GameBase.DefaultSpriteBatchOptions.Begin();
             }
         }
     }
 }
