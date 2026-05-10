using System;
using Wobble.Graphics.Animations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Backgrounds;
using Quaver.Shared.Graphics.Form.Dropdowns.Custom;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.MapInformation;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.Search;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.Tools;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Quaver.Shared.Skinning;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Scheduling;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    public class SelectFilterPanel : Sprite
    {
        /// <summary>
        ///     The mapsets that are currently available to play
        /// </summary>
        private Bindable<List<Mapset>> AvailableMapsets { get; }

        /// <summary>
        ///     The current search the user has had
        /// </summary>
        private Bindable<string> CurrentSearchQuery { get; }

        /// <summary>
        /// </summary>
        private Bindable<bool> IsPlayTesting { get; }

        /// <summary>
        /// </summary>
        private Bindable<SelectContainerPanel> ActiveLeftPanel { get; }

        /// <summary>
        ///     Underlying button that prevents mapsets from being clicked from inside the area
        /// </summary>
        private ImageButton Button { get; } = null!;

        /// <summary>
        ///     The banner that displays the current map's background
        /// </summary>
        private FilterPanelBanner Banner { get; set; } = null!;

        /// <summary>
        ///     Displays the current map information
        /// </summary>
        private FilterPanelMapInfo MapInfo { get; set; } = null!;

        /// <summary>
        ///     Items that are aligned from right to left
        /// </summary>
        private List<Drawable> RightItems { get; set; } = null!;

        /// <summary>
        ///     The help component for search
        /// </summary>
        public FilterPanelSearchHelp? SearchHelp { get; private set; }

        /// <summary>
        ///     The dim screen behind the filter panel
        /// </summary>
        private ImageButton DimScreen { get; set; } = null!;

        /// <summary>
        ///     The textbox to search for maps
        /// </summary>
        public FilterPanelSearchBox SearchBox { get; private set; } = null!;

        /// <summary>
        ///     The textbox to search for maps (v2.0 design)
        /// </summary>
        public FilterPanelSearchBoxV2 SearchBoxV2 { get; private set; } = null!;

        /// <summary>
        ///     Wrapper for the search box (v2.0)
        /// </summary>
        private Container SearchBoxWrapper { get; set; } = null!;

        /// <summary>
        ///     Map counter (v2.0)
        /// </summary>
        private FilterPanelMapsAvailableV2 MapsCounter { get; set; } = null!;

        /// <summary>
        ///     Cached width of the maps counter to detect layout changes
        /// </summary>
        private float _cachedMapsCounterWidth;

        /// <summary>
        ///     Switch button for Mapsets (v2.0)
        /// </summary>
        private FilterPanelSwitchButton SwitchButtonMapsets { get; set; } = null!;

        /// <summary>
        ///     Switch button for Playlists (v2.0)
        /// </summary>
        private FilterPanelSwitchButton SwitchButtonPlaylists { get; set; } = null!;

        /// <summary>
        ///     Dropdown for filtering by GameMode (4K/7K) used in V2
        /// </summary>
        private FilterPanelDropdownV2 GameModeDropdown { get; set; } = null!;

        /// <summary>
        ///     Square button for additional options (V2)
        /// </summary>
        private FilterPanelSquareButton SquareButton { get; set; } = null!;



        /// <summary>
        ///     Slider for difficulty range (V2)
        /// </summary>
        private FilterPanelDifficultyControl? DifficultySlider { get; set; }

        /// <summary>
        ///     Dropdown for filtering by status (V2)
        /// </summary>
        private FilterPanelStatusDropdownV2? StatusDropdown { get; set; }

        /// <summary>
        ///    Container for the top row (always visible)
        /// </summary>
        public Container? TopRowContainer { get; private set; }

        /// <summary>
        ///     Container for the bottom row (revealed) - formerly FilterClipContainer
        /// </summary>
        public ClippingContainer? BottomRowContainer { get; private set; }

        /// <summary>
        ///     Target ClipHeight for animation
        /// </summary>
        private float TargetFilterClipHeight { get; set; }

        /// <summary>
        ///     Min difficulty filter value
        /// </summary>
        private Bindable<float> MinDifficultyFilter { get; } = new Bindable<float>(0f);

        /// <summary>
        ///     Max difficulty filter value
        /// </summary>
        private Bindable<float> MaxDifficultyFilter { get; } = new Bindable<float>(float.MaxValue);

        /// <summary>
        ///     Event invoked when the panel height changes (V2)
        /// </summary>
        public event Action<float>? HeightChanged;

        /// <summary>
        ///     Background sprite for V2 (NineSlice)
        /// </summary>
        private NineSliceSprite BackgroundV2 { get; set; } = null!;

        /// <summary>
        ///     The text that displays how many maps are available
        /// </summary>
        private FilterPanelMapsAvailable MapsAvailable { get; set; } = null!;

        /// <summary>
        ///     The dropdown to sort maps
        /// </summary>
        private FilterDropdownSorting SortDropdown { get; set; } = null!;

        /// <summary>
        ///     The dropdown to sort maps (V2)
        /// </summary>
        private FilterPanelSortingDropdownV2 SortingDropdown { get; set; } = null!;

        /// <summary>
        ///     The dropdown to group maps
        /// </summary>
        private FilterDropdownGroupBy SortGroupBy { get; set; } = null!;

        /// <summary>
        ///     The dropdown to sort by mode
        /// </summary>
        private FilterDropdownMode SortMode { get; set; } = null!;

        /// <summary>
        ///     Task used to filter mapsets
        /// </summary>
        private TaskHandler<int, int> FilterMapsetsTask { get; }

        /// <summary>
        ///     If the panel currently has input focus.
        /// </summary>
        private bool _isInputFocused;

        /// <summary>
        /// </summary>

        public SelectFilterPanel(Bindable<List<Mapset>> availableMapsets, Bindable<string> currentSearchQuery,
            Bindable<bool> isPlayTesting, Bindable<SelectContainerPanel> activeLeftPanel)
        {
            AvailableMapsets = availableMapsets;
            CurrentSearchQuery = currentSearchQuery;
            IsPlayTesting = isPlayTesting;
            ActiveLeftPanel = activeLeftPanel;

            // Check skin version for design selection
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1.0f;

            if (version == 2.0f)
            {
                InitializeLayoutV2();
            }
            else
            {
                InitializeLayoutV1();
            }

            // Common Logic
            FilterMapsetsTask = new TaskHandler<int, int>(StartFilterMapsetsTask);
            CurrentSearchQuery.ValueChanged += OnSearchQueryChanged;

            if (ConfigManager.SelectOrderMapsetsBy != null)
                ConfigManager.SelectOrderMapsetsBy.ValueChanged += OnSelectOrderMapsetsChanged;

            if (ConfigManager.SelectFilterGameModeBy != null)
                ConfigManager.SelectFilterGameModeBy.ValueChanged += OnSelectFilterGameModeChanged;

            if (ConfigManager.SelectFilterStatusBy != null)
                ConfigManager.SelectFilterStatusBy.ValueChanged += OnSelectFilterStatusChanged;

            if (ConfigManager.SelectGroupMapsetsBy != null)
                ConfigManager.SelectGroupMapsetsBy.ValueChanged += OnSelectGroupMapsetsChanged;

            if (ConfigManager.SelectSortDirection != null)
                ConfigManager.SelectSortDirection.ValueChanged += OnSelectSortDirectionChanged;

            MinDifficultyFilter.ValueChanged += (s, e) => StartFilterMapsetsTask();
            MaxDifficultyFilter.ValueChanged += (s, e) => StartFilterMapsetsTask();

            ModManager.ModsChanged += OnModsChanged;
            PlaylistManager.Selected.ValueChanged += OnPlaylistChanged;
            PlaylistManager.PlaylistMapsManaged += OnPlaylistMapsManaged;

            FilterMapsets();
        }


        /// <summary>
        ///     Initializes the layout for version 2.0 (Simplified panel)
        /// </summary>
        private void InitializeLayoutV2()
        {
            // V2.0: Simplified panel with search box
            // We use a separate NineSliceSprite for background to allow resizing without affecting children
            Image = null; // Explicitly set to null to prevent parent Sprite from drawing anything
            Tint = Color.Transparent; // Double safety against white box
            var bgTexture = SkinManager.Skin?.SongSelect?.SelectFilterPanelRight ?? UserInterface.FilterPanelRight;

            BackgroundV2 = new NineSliceSprite(bgTexture, new SliceMargins(0, 0, 6, 6))
            {
                Parent = this,
                Size = new ScalableVector2(1040, 60)
            };

            Size = new ScalableVector2(1040, 60);

            // Create Bottom Row Container (V2) - formerly FilterClipContainer
            // Container positioned at Y=60 (just below the top row) for stationary cropping
            BottomRowContainer = new ClippingContainer
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = 10,
                Y = 60, // Fixed position below top row (60px panel height)
                Size = new ScalableVector2(1040, 50), // Width covers all filter elements
                ClipHeight = 0, // Start clipped (hidden)
                FullContentHeight = 50,
                SetChildrenVisibility = true
            };

            // Slider positioned at top of container (Y=0)
            DifficultySlider = new FilterPanelDifficultyControl(MinDifficultyFilter, MaxDifficultyFilter)
            {
                Parent = BottomRowContainer, // Parented to BottomRow
                Alignment = Alignment.TopLeft,
                X = 0,
                Y = 0
            };

            // Create Top Row Container (V2) - Holds all always-visible elements
            TopRowContainer = new Container
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = 0,
                Y = 0,
                Size = new ScalableVector2(1040, 60)
            };

            var searchBoxContainer = new Sprite
            {
                Parent = TopRowContainer, // Parented to TopRow
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(542, 40),
                X = 10,
                Y = 10,
                Image = UserInterface.SearchBoxMask,
                Tint = SkinManager.Skin.SearchFilterPanelColor
            };

            // Create search icon inside container (10px from left, 16x16)
            var searchIcon = new Sprite
            {
                Parent = searchBoxContainer,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(16, 16),
                X = 10,
                Tint = SkinManager.Skin.SearchActiveTextColor,
                Image = FontAwesome.Get(FontAwesomeIcon.fa_magnifying_glass)
            };

            // Create map counter inside container (10px from right)
            MapsCounter = new FilterPanelMapsAvailableV2(AvailableMapsets)
            {
                Parent = searchBoxContainer,
                Alignment = Alignment.MidRight,
                X = -10
            };

            // Calculate search box width
            var leftOffset = 26f;
            var rightOffset = 10f + 100f; // Safe estimate for counter
            var searchBoxWidth = 542f - leftOffset - rightOffset;

            // Create a wrapper for the search box
            SearchBoxWrapper = new Container
            {
                Parent = searchBoxContainer,
                Alignment = Alignment.MidLeft,
                X = leftOffset,
                Width = searchBoxWidth,
                Height = 40
            };

            // Create search box v2.0
            SearchBoxV2 = new FilterPanelSearchBoxV2(CurrentSearchQuery, IsPlayTesting, ActiveLeftPanel,
                searchBoxWidth, "Search maps...")
            {
                Parent = SearchBoxWrapper,
                Alignment = Alignment.TopLeft,
                X = 0
            };

            // Create switch buttons
            var isPlaylists = ConfigManager.SelectGroupMapsetsBy.Value == GroupMapsetsBy.Playlists;
            SwitchButtonMapsets = new FilterPanelSwitchButton(UserInterface.SwitchButtonLeft, "Mapsets", !isPlaylists)
            {
                Parent = TopRowContainer, // Parented to TopRow
                Alignment = Alignment.TopLeft,
                X = 10 + 542 + 10,
                Y = 10
            };
            SwitchButtonMapsets.Clicked += OnSwitchButtonMapsetsClicked;

            SwitchButtonPlaylists = new FilterPanelSwitchButton(UserInterface.SwitchButtonRight, "Playlists", isPlaylists)
            {
                Parent = TopRowContainer, // Parented to TopRow
                Alignment = Alignment.TopLeft,
                X = SwitchButtonMapsets.X + SwitchButtonMapsets.Width,
                Y = 10
            };
            SwitchButtonPlaylists.Clicked += OnSwitchButtonPlaylistsClicked;

            // Status Dropdown positioned relative to SwitchButtonMapsets
            StatusDropdown = new FilterPanelStatusDropdownV2
            {
                Parent = BottomRowContainer, // Parented to BottomRow
                Alignment = Alignment.TopLeft,
                X = (SwitchButtonMapsets.X) - 10,
                Y = 0
            };

            // Calculating X based on where GameModeDropdown should be to fulfill right margin 10px
            // 1040 - 10 (margin) - 40 (expand) - 10 (gap) - 204 (dropdown) = 776
            var topRightControlsX = 1040 - 10 - 40 - 10 - 204;

            SortingDropdown = new FilterPanelSortingDropdownV2(AvailableMapsets)
            {
                Parent = BottomRowContainer, // Parented to BottomRow
                Alignment = Alignment.TopLeft,
                X = topRightControlsX - 10,
                Y = 0
            };

            // Create GameMode Dropdown (V2) - Directly in TopRow
            GameModeDropdown = new FilterPanelDropdownV2
            {
                Parent = TopRowContainer, // Parented to TopRow
                Alignment = Alignment.TopLeft,
                X = topRightControlsX,
                Y = 10
            };

            // Create Square Button - Directly in TopRow (10px margin from right)
            SquareButton = new FilterPanelSquareButton
            {
                Parent = TopRowContainer, // Parented to TopRow
                Alignment = Alignment.TopLeft,
                X = 1040 - 10 - 40,
                Y = 10
            };
            SquareButton.Clicked += OnSquareButtonClicked;

            // Dim Screen
            DimScreen = new ImageButton(WobbleAssets.WhiteBox, (sender, args) =>
            {
                if (SearchHelp != null)
                    SearchHelp.Visible = false;

                GameModeDropdown?.Dropdown?.Close();
                StatusDropdown?.Dropdown?.Close();
                SortingDropdown?.Dropdown?.Close();

                UpdateDimScreenVisibility();
            })
            {
                Parent = this,
                Tint = Color.Black,
                Alpha = 0.85f,
                Visible = false,
                IsClickable = false,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height)
            };

            // Add DimScreen at index 0 to be behind everything else in this container
            Children.Remove(DimScreen);
            Children.Insert(0, DimScreen);

            // Create Search Help Panel - Hidden by default
            SearchHelp = new FilterPanelSearchHelp
            {
                Parent = this, // Parent to main panel to draw over other elements if needed, or TopRowContainer
                Alignment = Alignment.TopLeft,
                Y = 60, // Below the top row
                X = 0,
                Width = 1040, // Match updated background width
                Visible = false
            };

            // Bind Help Icon Click
            MapsCounter.HelpIcon.Clicked += OnHelpIconClicked;

            // Bind Dropdown events for DimScreen
            GameModeDropdown.Opened += (s, e) => UpdateDimScreenVisibility();
            GameModeDropdown.Closed += (s, e) => UpdateDimScreenVisibility();

            if (StatusDropdown != null)
            {
                StatusDropdown.Opened += (s, e) => UpdateDimScreenVisibility();
                StatusDropdown.Closed += (s, e) => UpdateDimScreenVisibility();
            }

            if (SortingDropdown != null)
            {
                SortingDropdown.Opened += (s, e) => UpdateDimScreenVisibility();
                SortingDropdown.Closed += (s, e) => UpdateDimScreenVisibility();
            }
        }

        /// <summary>
        ///    Called when the help icon is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHelpIconClicked(object? sender, EventArgs e)
        {
            if (SearchHelp == null)
                return;

            SearchHelp.Visible = !SearchHelp.Visible;
            UpdateDimScreenVisibility();
        }

        /// <summary>
        ///    Updates the visibility of the dimmed background screen
        /// </summary>
        private void UpdateDimScreenVisibility()
        {
            if (DimScreen == null)
                return;

            var anyDropdownOpen = (GameModeDropdown?.Dropdown?.Opened ?? false) ||
                                  (StatusDropdown?.Dropdown?.Opened ?? false) ||
                                  (SortingDropdown?.Dropdown?.Opened ?? false);

            var helpOpen = SearchHelp?.Visible ?? false;

            DimScreen.Visible = anyDropdownOpen || helpOpen;
            DimScreen.IsClickable = DimScreen.Visible;

            // Handle InputStack focus
            if (DimScreen.Visible && !_isInputFocused)
            {
                ButtonManager.PushInputRoot(this);
                _isInputFocused = true;
            }
            else if (!DimScreen.Visible && _isInputFocused)
            {
                ButtonManager.PopInputRoot();
                _isInputFocused = false;
            }

            if (DimScreen.Visible)
            {
                // Update Dim Position to cover the whole screen relative to this container
                DimScreen.X = -AbsolutePosition.X;
                DimScreen.Y = -AbsolutePosition.Y;
            }
        }


        /// <summary>
        ///    Called when the search query has changed
        /// </summary>

        private void OnSearchQueryChanged(object? sender, BindableValueChangedEventArgs<string> args) => StartFilterMapsetsTask();

        /// <summary>
        ///     Initializes the layout for version 1.0 (Original design)
        /// </summary>
        private void InitializeLayoutV1()
        {
            // V1.0: Current design with all elements
            Size = new ScalableVector2(WindowManager.Width, 88);

            Image = SkinManager.Skin?.SongSelect?.SelectFilterPanelRight;

            if (Image?.Width == null)
            {
                Tint = ColorHelper.HexToColor("#0F2C44");
            }

            Banner = new FilterPanelBanner(this)
            {
                Parent = this,
                Alignment = Alignment.MidLeft
            };

            MapInfo = new FilterPanelMapInfo
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 25
            };

            RightItems = new List<Drawable>();

            CreateSortDropdown();
            CreateSortGroupBy();
            CreateSortModeDropdown();
            CreateSearchBox();
            CreateMapsAvailable();

            AlignRightItems();

            // Banner-dependent events
            MapManager.Selected.ValueChanged += OnMapChanged;
        }

        /// <summary>
        ///     Creates <see cref="SortDropdown"/>
        /// </summary>
        private void CreateSortDropdown()
        {
            SortDropdown = new FilterDropdownSorting(AvailableMapsets) { Parent = this, };
            RightItems.Add(SortDropdown);
        }





        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateDimScreenVisibility();
            UpdateSearchBoxLayout();

            // Safety check: If the panel is not visible, ensure it's not holding the input root.
            if (!Visible && _isInputFocused)
            {
                ButtonManager.PopInputRoot();
                _isInputFocused = false;
            }

            // Update ClipHeight and visibility for version 2.0
            if (BottomRowContainer != null)
            {
                // Snap reveal/hide elements instantly to avoid layout jitter.
                if (TargetFilterClipHeight > 0)
                {
                    BottomRowContainer.ClipHeight = 50;
                    BottomRowContainer.Visible = Height > 65; // Only show when panel has enough height.
                }
                else
                {
                    BottomRowContainer.ClipHeight = 0;
                    BottomRowContainer.Visible = false;
                }

                // Disable clipping when visible to allow dropdown list to overflow
                BottomRowContainer.EnableClipping = !BottomRowContainer.Visible;

                // Manage children visibility to prevent input blocking
                if (DifficultySlider != null)
                    DifficultySlider.Visible = BottomRowContainer.Visible;
                if (StatusDropdown != null)
                    StatusDropdown.Visible = BottomRowContainer.Visible;
                if (SortingDropdown != null)
                    SortingDropdown.Visible = BottomRowContainer.Visible;
            }
        }

        /// <summary>
        ///    Updates the search box layout dynamically based on the maps counter width (V2 only).
        /// </summary>
        private void UpdateSearchBoxLayout()
        {
            if (SkinManager.Skin?.UserInterfaceVersion != 2 || SearchBoxWrapper == null || MapsCounter == null || SearchBoxV2 == null)
                return;

            if (Math.Abs(_cachedMapsCounterWidth - MapsCounter.Width) <= 0.5f)
                return;

            _cachedMapsCounterWidth = MapsCounter.Width;
            
            var leftOffset = 26f;
            var rightOffset = 10f + _cachedMapsCounterWidth;
            var availableWidth = 542f - leftOffset - rightOffset;

            SearchBoxWrapper.Width = availableWidth;
            SearchBoxV2.Size = new ScalableVector2(availableWidth, 40);
        }

        /// <summary>
        ///     Creates <see cref="SortGroupBy"/>
        /// </summary>
        private void CreateSortGroupBy()
        {
            SortGroupBy = new FilterDropdownGroupBy(AvailableMapsets) { Parent = this };
            RightItems.Add(SortGroupBy);
        }

        /// <summary>
        ///     Creates <see cref="SortMode"/>
        /// </summary>
        private void CreateSortModeDropdown()
        {
            SortMode = new FilterDropdownMode(AvailableMapsets) { Parent = this };
            RightItems.Add(SortMode);
        }

        /// <summary>
        ///     Creates <see cref="MapsAvailable"/>
        /// </summary>
        private void CreateMapsAvailable()
        {
            MapsAvailable = new FilterPanelMapsAvailable(AvailableMapsets) { Parent = this };
            RightItems.Add(MapsAvailable);
        }

        /// <summary>
        ///     Creates <see cref="SearchBox"/>
        /// </summary>
        private void CreateSearchBox()
        {
            SearchBox = new FilterPanelSearchBox(CurrentSearchQuery, AvailableMapsets, IsPlayTesting, ActiveLeftPanel,
                    "Type to search...")
            {
                Parent = this,
                AllowCursorMovement = false,
            };

            RightItems.Add(SearchBox);
        }

        /// <summary>
        ///     Aligns the items from right to left
        /// </summary>
        private void AlignRightItems()
        {
            for (var i = 0; i < RightItems.Count; i++)
            {
                var item = RightItems[i];

                item.Parent = this;

                item.Alignment = Alignment.MidRight;

                const int padding = 25;
                var spacing = 30;

                item.X = i == 0 ? -padding : RightItems[i - 1].X - RightItems[i - 1].Width - spacing;
            }
        }

        /// <summary>
        /// </summary>
        private void StartFilterMapsetsTask()
        {
            if (FilterMapsetsTask.IsRunning)
                FilterMapsetsTask.Cancel();

            FilterMapsetsTask.Run(50);
        }

        /// <summary>
        ///     Begins the task to filter mapsets
        /// </summary>
        /// <param name="a"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private int StartFilterMapsetsTask(int a, CancellationToken token)
        {
            FilterMapsets();
            return 0;
        }

        /// <summary>
        ///     Handles filtering mapsets for the screen
        /// </summary>
        private void FilterMapsets()
        {
            var filtered = MapsetHelper.FilterMapsets(CurrentSearchQuery, MinDifficultyFilter.Value, MaxDifficultyFilter.Value);

            AddScheduledUpdate(() =>
            {
                if (IsDisposed)
                    return;

                if (ConfigManager.EnableVerboseSongSelectLogs?.Value == true)
                {
                    Logger.Debug($"Filtering mapsets by -  Query: `{CurrentSearchQuery.Value}` | Sort By: {ConfigManager.SelectOrderMapsetsBy?.Value}",
                        LogType.Runtime);
                }

                AvailableMapsets.Value = filtered;

                if (AvailableMapsets.Value.Count == 0)
                    return;

                // Check if the map is in any of the mapsets
                if (MapManager.Selected.Value != null)
                {
                    foreach (var set in AvailableMapsets.Value)
                    {
                        if (set.Maps.Any(x => x.Md5Checksum == MapManager.Selected.Value.Md5Checksum))
                            return;
                    }
                }

                if (AvailableMapsets.Value.Count > 0)
                {
                    MapManager.SelectMapFromMapset(AvailableMapsets.Value.First());

                    if (MapManager.Selected.Value != null)
                        BackgroundHelper.Load(MapManager.Selected.Value);
                }
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectOrderMapsetsChanged(object? sender, BindableValueChangedEventArgs<OrderMapsetsBy> e) => StartFilterMapsetsTask();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectFilterGameModeChanged(object? sender, BindableValueChangedEventArgs<GameMode> e) => StartFilterMapsetsTask();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectFilterStatusChanged(object? sender, BindableValueChangedEventArgs<RankedStatusFilter> e) => StartFilterMapsetsTask();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectSortDirectionChanged(object? sender, BindableValueChangedEventArgs<SortDirection> e) => StartFilterMapsetsTask();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectGroupMapsetsChanged(object? sender, BindableValueChangedEventArgs<GroupMapsetsBy> e)
        {
            SwitchButtonMapsets?.SetActive(e.Value == GroupMapsetsBy.None);
            SwitchButtonPlaylists?.SetActive(e.Value == GroupMapsetsBy.Playlists);

            StartFilterMapsetsTask();
        }

        /// <summary>
        ///     Responsible for initiating the new banner load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object? sender, BindableValueChangedEventArgs<Map> e)
        {
            ThreadScheduler.Run(() =>
            {
                lock (Banner)
                    lock (Banner.Background.Image)
                    {
                        // This has to be multi-threaded because MapManager.GetBackgroundPath
                        // parses osu! maps to get the path of BG. It doesn't exist in the osu!db
                        if (MapManager.GetBackgroundPath(e.OldValue) == MapManager.GetBackgroundPath(e.Value))
                            return;

                        Banner.FadeToBlack();
                        BackgroundHelper.Load(e.Value);
                    }
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnModsChanged(object? sender, ModsChangedEventArgs e)
        {
            if (ConfigManager.SelectOrderMapsetsBy == null)
                return;

            if (ConfigManager.SelectOrderMapsetsBy.Value != OrderMapsetsBy.Difficulty)
                return;

            var isSpeedMod = e.ChangedMods >= ModIdentifier.Speed05X && e.ChangedMods <= ModIdentifier.Speed20X ||
                             e.ChangedMods >= ModIdentifier.Speed055X && e.ChangedMods <= ModIdentifier.Speed095X ||
                             e.ChangedMods >= ModIdentifier.Speed105X && e.ChangedMods <= ModIdentifier.Speed195X;

            if (e.Type == ModChangeType.RemoveAll || e.Type == ModChangeType.RemoveSpeed || isSpeedMod)
                StartFilterMapsetsTask();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistChanged(object? sender, BindableValueChangedEventArgs<Playlist> e) => StartFilterMapsetsTask();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistMapsManaged(object? sender, PlaylistMapsManagedEventArgs e)
        {
            if (ConfigManager.SelectGroupMapsetsBy.Value != GroupMapsetsBy.Playlists)
                return;

            StartFilterMapsetsTask();
        }

        /// <summary>
        ///     Called when the Mapsets switch button is clicked.
        /// </summary>
        private void OnSwitchButtonMapsetsClicked(object? sender, EventArgs e)
        {
            if (SwitchButtonMapsets.IsActive)
                return;

            SwitchButtonMapsets.SetActive(true);
            SwitchButtonPlaylists.SetActive(false);

            // Switch to Mapsets view (using GroupMapsetsBy.None)
            if (ConfigManager.SelectGroupMapsetsBy != null)
                ConfigManager.SelectGroupMapsetsBy.Value = GroupMapsetsBy.None;
        }

        /// <summary>
        ///     Called when the Playlists switch button is clicked.
        /// </summary>
        private void OnSwitchButtonPlaylistsClicked(object? sender, EventArgs e)
        {
            if (SwitchButtonPlaylists.IsActive)
                return;

            SwitchButtonPlaylists.SetActive(true);
            SwitchButtonMapsets.SetActive(false);

            // Switch to Playlists view
            if (ConfigManager.SelectGroupMapsetsBy != null)
                ConfigManager.SelectGroupMapsetsBy.Value = GroupMapsetsBy.Playlists;
        }

        /// <summary>
        ///     Called when the Square Button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSquareButtonClicked(object? sender, EventArgs e)
        {
            if (BackgroundV2 == null)
                return;

            this.ClearAnimations();
            BackgroundV2.ClearAnimations();

            float targetHeight;
            if (SquareButton.Active)
            {
                // Expand to 110px
                targetHeight = 110;
                TargetFilterClipHeight = 50; // Full container height (10px margin + 40px slider)

                if (SearchHelp != null)
                {
                    SearchHelp.ClearAnimations();
                    // SearchHelp's internal PanelBackground has Width=782 and Y=10.
                    // We want visual gap of 10px on X: X = -782 - 10 = -792.
                    // We want visual Y to align to BackgroundV2.Y (which is 0): Y = -10 (since -10 + 10 = 0).
                    SearchHelp.Animations.Add(new Animation(AnimationProperty.X, Easing.OutQuint, SearchHelp.X, -792, 200));
                    SearchHelp.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, SearchHelp.Y, -10, 200));
                }
            }
            else
            {
                // Retract to original height (60px)
                targetHeight = 60;
                TargetFilterClipHeight = 0; // Clip slider completely

                if (SearchHelp != null)
                {
                    SearchHelp.ClearAnimations();
                    SearchHelp.Animations.Add(new Animation(AnimationProperty.X, Easing.OutQuint, SearchHelp.X, 0, 200));
                    SearchHelp.Animations.Add(new Animation(AnimationProperty.Y, Easing.OutQuint, SearchHelp.Y, 60, 200));
                }
            }

            this.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, this.Height, targetHeight, 200));
            BackgroundV2.Animations.Add(new Animation(AnimationProperty.Height, Easing.OutQuint, BackgroundV2.Height, targetHeight, 200));

            // Notify parent to resize
            HeightChanged?.Invoke(targetHeight);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            if (CurrentSearchQuery != null)
                CurrentSearchQuery.ValueChanged -= OnSearchQueryChanged;

            if (ConfigManager.SelectOrderMapsetsBy != null)
                ConfigManager.SelectOrderMapsetsBy.ValueChanged -= OnSelectOrderMapsetsChanged;

            if (ConfigManager.SelectFilterGameModeBy != null)
                ConfigManager.SelectFilterGameModeBy.ValueChanged -= OnSelectFilterGameModeChanged;

            if (ConfigManager.SelectFilterStatusBy != null)
                ConfigManager.SelectFilterStatusBy.ValueChanged -= OnSelectFilterStatusChanged;

            if (ConfigManager.SelectGroupMapsetsBy != null)
                ConfigManager.SelectGroupMapsetsBy.ValueChanged -= OnSelectGroupMapsetsChanged;

            if (ConfigManager.SelectSortDirection != null)
                ConfigManager.SelectSortDirection.ValueChanged -= OnSelectSortDirectionChanged;

            if (SwitchButtonMapsets != null)
                SwitchButtonMapsets.Clicked -= OnSwitchButtonMapsetsClicked;

            if (SwitchButtonPlaylists != null)
                SwitchButtonPlaylists.Clicked -= OnSwitchButtonPlaylistsClicked;

            if (SquareButton != null)
                SquareButton.Clicked -= OnSquareButtonClicked;

            if (MapsCounter?.HelpIcon != null)
                MapsCounter.HelpIcon.Clicked -= OnHelpIconClicked;

            MapManager.Selected.ValueChanged -= OnMapChanged;
            ModManager.ModsChanged -= OnModsChanged;
            PlaylistManager.Selected.ValueChanged -= OnPlaylistChanged;
            PlaylistManager.PlaylistMapsManaged -= OnPlaylistMapsManaged;

            FilterMapsetsTask?.Dispose();

            // Pops this panel from the input stack if it's currently focused.
            if (_isInputFocused)
            {
                ButtonManager.PopInputRoot();
                _isInputFocused = false;
            }

            base.Destroy();
        }

    }
}
