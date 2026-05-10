using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Scheduling;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public class MapsetScrollContainer : SongSelectContainer<Mapset>
    {
        public override SelectScrollContainerType Type { get; } = SelectScrollContainerType.Mapsets;

        /// <summary>
        /// </summary>
        private Bindable<List<Mapset>> AvailableMapsets { get; }

        /// <summary>
        ///     The currently active container in song select
        /// </summary>
        public Bindable<SelectScrollContainerType> ActiveScrollContainer { get; }

        /// <summary>
        ///     The duration of the smart scroll animation
        /// </summary>
        private const double ScrollDuration = 800;

        /// <summary>
        ///     If the mapsets have reinitialized
        /// </summary>
        private bool HasReinitialized { get; set; }

        /// <summary>
        ///     The mapset that is in the middle of the screen
        /// </summary>
        private DrawableMapset? MiddleMapset
        {
            get
            {
                if (Pool.Count == 0)
                    return null;

                return Pool[Pool.Count / 2] as DrawableMapset;
            }
        }

        private Quaver.Shared.Screens.Selection.UI.Playlists.DrawablePlaylist? PlaylistHeaderPanel { get; set; }

        /// <summary>
        ///     Whether frequently changing text is being cached or not
        /// </summary>
        private bool _isCached = true;
        public bool IsCached
        {
            get => _isCached;
            private set
            {
                if (value == _isCached)
                    return;

                SetCaching(value);
                _isCached = value;
            }
        }

        /// <summary>
        ///     Caching is disabled when the scroll speed exceeds this value.
        ///     This value should be low enough to disable caching when selecting a random mapset, absolute scrolling, mashing PgUp/PgDn,
        ///     but should be high enough to avoid disabling caching when up/down are mashed.
        ///
        ///     Units: change in y-position / milliseconds since last update
        /// </summary>
        public double CacheThreshold { get; } = 100;

        /// <summary>
        ///     Tracks which mapsets are currently expanded to show their difficulties.
        ///     Uses Mapset reference identity so that mapsets sharing the same Directory
        ///     (e.g. from BPM/NPS separation) are tracked independently.
        /// </summary>
        private HashSet<Mapset> ExpandedMapsets { get; } = new HashSet<Mapset>();

        /// <summary>
        ///     Tracks which mapset is currently collapsing (animating closed).
        ///     Used to prevent immediate layout snapping/culling issues.
        /// </summary>
        private Mapset? CollapsingMapset { get; set; }

        /// <summary>
        ///     Cached index of the currently expanded mapset.
        ///     Used to optimize scroll calculations.
        /// </summary>
        private int CachedExpandedMapsetIndex { get; set; } = -1;

        /// <summary>
        ///     Cached index of the currently collapsing mapset.
        /// </summary>
        private int CachedCollapsingMapsetIndex { get; set; } = -1;

        /// <summary>
        ///     The dynamically animated layout height for the collapsing mapset.
        ///     Managed independently of the ephemeral pool items.
        /// </summary>
        private float CachedCollapsingMapsetHeight { get; set; }

        /// <summary>
        ///     Cached middle object index for incremental pool shifting.
        ///     Avoids O(N) scan each frame by walking ±1 from the previous value.
        /// </summary>
        private int _cachedMiddleObjectIndex;

        /// <summary>
        ///     Dirty flag: set to true when pool item positions need updating (e.g. scroll, expansion, initialization).
        /// </summary>
        private bool _needsReposition = true;

        /// <summary>
        ///     Cached result of GetMapsetSlotHeight()
        /// </summary>
        private int _cachedSlotHeight = -1;

        /// <summary>
        ///    The last version of the song select screen detected.
        ///    Used as a fallback during transient uninitialized skin states.
        /// </summary>
        private int _lastDetectedVersion = -1;

        /// <summary>
        ///     The amount of time that has elapsed since the user requested to initialize the mapsets
        /// </summary>
        private double TimeElapsedUntilInitializationRequest { get; set; } = ReinitializeTime;

        /// <summary>
        ///     The time it takes until the mapsets will reinitialize
        /// </summary>
        private const int ReinitializeTime = 250;

        /// <summary>
        ///     Holds Mapsets that have been created but are no longer needed
        /// </summary>
        private Stack<DrawableMapset> RecycledMapsets { get; } = new Stack<DrawableMapset>();

        /// <summary>
        ///     Holds tasks that need to be processed every frame during mapset discovery
        /// </summary>
        private Queue<Action> InitializationTasks { get; } = new Queue<Action>();

        /// <summary>
        ///     The height of a mapset panel
        /// </summary>
        public const int MapsetPanelHeight = 97;

        /// <summary>
        ///     The height of a difficulty panel when expanded inline
        /// </summary>
        public const int DifficultyPanelHeight = 60;

        /// <summary>
        ///     Returns the height of a mapset slot including spacing/padding.
        ///     V2: 100px height + 10px spacing = 110px.
        ///     V1: 97px height (legacy).
        /// </summary>
        private int GetMapsetSlotHeight()
        {
            if (_cachedSlotHeight != -1)
                return _cachedSlotHeight;

            var version = GetSongSelectVersion();
            _cachedSlotHeight = (int)(version == 2 ? MapsetHelper.V2MapsetSlotHeight : MapsetHelper.V1MapsetHeight);
            return _cachedSlotHeight;
        }

        /// <summary>
        ///     Safely gets the song select version.
        ///     If the skin is temporarily null (e.g. during screen transitions), it returns the last known version.
        /// </summary>
        private int GetSongSelectVersion()
        {
            var version = SkinManager.Skin?.UserInterfaceVersion;

            // Version Lock: If we have already detected V2, we locked it for this screen instance.
            // This prevents falling back to V1 during transient null skin states in view transitions.
            if (_lastDetectedVersion == 2)
                return 2;

            if (version == null)
                return _lastDetectedVersion != -1 ? _lastDetectedVersion : 1;

            _lastDetectedVersion = (int)version.Value;
            return _lastDetectedVersion;
        }

        /// <summary>
        ///    The rasterizer state used for clipping the container.
        /// </summary>
        private RasterizerState ScissorRasterizer { get; }

        public MapsetScrollContainer(Bindable<List<Mapset>> availableMapsets, Bindable<SelectScrollContainerType> activeScrollContainer)
            : base(availableMapsets.Value, 12)
        {
            AvailableMapsets = availableMapsets;
            ActiveScrollContainer = activeScrollContainer;

            AutoScaleHeight = false;

            ScissorRasterizer = new RasterizerState
            {
                ScissorTestEnable = true,
                CullMode = CullMode.None
            };

            MapManager.Selected.ValueChanged += OnMapChanged;
            MapManager.MapsetDeleted += OnMapsetDeleted;
            AvailableMapsets.ValueChanged += OnAvailableMapsetsChanged;
            SelectionScreen.RandomMapsetSelected += OnRandomMapsetSelected;

            SkinManager.SkinLoaded += OnSkinLoaded;
        }

        private void OnSkinLoaded(object? sender, SkinReloadedEventArgs e) => _cachedSlotHeight = -1;

        /// <inheritdoc />
        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            var game = GameBase.Game;
            if (game?.GraphicsDevice == null)
                return;

            var screenRect = ScreenRectangle;

            var scissorRect = new Rectangle(
                (int)(screenRect.X * WindowManager.ScreenScale.X),
                (int)(screenRect.Y * WindowManager.ScreenScale.Y),
                (int)(screenRect.Width * WindowManager.ScreenScale.X),
                (int)(screenRect.Height * WindowManager.ScreenScale.Y)
            );

            var viewport = game.GraphicsDevice.Viewport;
            scissorRect.X = MathHelper.Clamp(scissorRect.X, 0, viewport.Width);
            scissorRect.Y = MathHelper.Clamp(scissorRect.Y, 0, viewport.Height);
            scissorRect.Width = MathHelper.Clamp(scissorRect.Width, 0, viewport.Width - scissorRect.X);
            scissorRect.Height = MathHelper.Clamp(scissorRect.Height, 0, viewport.Height - scissorRect.Y);

            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            var spriteBatch = GameBase.Game.SpriteBatch;
            var oldScissorRect = game.GraphicsDevice.ScissorRectangle;
            var oldRasterizerState = game.GraphicsDevice.RasterizerState;

            if (oldRasterizerState.ScissorTestEnable)
                scissorRect = Rectangle.Intersect(scissorRect, oldScissorRect);

            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            try { spriteBatch.End(); } catch { }

            game.GraphicsDevice.ScissorRectangle = scissorRect;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                ScissorRasterizer,
                null,
                WindowManager.Scale);

            base.Draw(gameTime);

            spriteBatch.End();

            game.GraphicsDevice.ScissorRectangle = oldScissorRect;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                oldRasterizerState,
                null,
                WindowManager.Scale);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            TimeElapsedUntilInitializationRequest += gameTime.ElapsedGameTime.TotalMilliseconds;
            InitializeMapsets(false);

            // Deferred stabilization block removed - logic moved to InitializationTasks handler below

            // Disable caching of frequently changing text if scrolling fast enough
            // Caching stays disabled until scrolling stops to prevent repeated recaching when scroll speed fluctuates
            var deltaY = CurrentY - PreviousY;
            var elapsed = gameTime.ElapsedGameTime.TotalMilliseconds;
            var speed = elapsed <= 0 ? 0 : Math.Abs(deltaY) / elapsed;
            IsCached = !(speed > CacheThreshold || (!IsCached && speed != 0));

            // Close Right Click Options if scrolling
            if (Math.Abs(deltaY) > 1)
            {
                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.ActiveRightClickOptions?.Close();
            }

            var isCollapsing = CollapsingMapset != null;

            DrawableMapset expandingPoolItem = null;
            if (CachedExpandedMapsetIndex != -1 && AvailableItems != null && CachedExpandedMapsetIndex < AvailableItems.Count)
            {
                var expandedMapset = AvailableItems[CachedExpandedMapsetIndex];
                for (var i = 0; i < Pool.Count; i++)
                {
                    if (Pool[i] is DrawableMapset dm && dm.Item == expandedMapset)
                    {
                        expandingPoolItem = dm;
                        break;
                    }
                }
            }
            var isExpanding = expandingPoolItem != null && expandingPoolItem.IsAnimatingExpand;

            if (isCollapsing)
            {
                var dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                var mapset = CollapsingMapset;
                var fullHeight = mapset != null ? CalculateStaticExpandedHeight(mapset) : 0;

                // 300f is the CollapseAnimationDuration constant from DrawableMapset
                var step = fullHeight * (dt / 300f);
                CachedCollapsingMapsetHeight -= step;

                // Check if the collapsing animation has finished
                if (CachedCollapsingMapsetHeight <= 0)
                {
                    CachedCollapsingMapsetHeight = 0;
                    CollapsingMapset = null;
                    UpdateCachedExpandedMapsetIndex();

                    // Force a recalculation when finished to ensure perfectly clean rest state
                    RecalculateContainerHeight();
                }
            }

            // Both collapsing and expanding require continuous layout recalcs to push elements down smoothly
            if (isCollapsing || isExpanding)
            {
                _needsReposition = true;

                // Update layout continuously during animation, but only if we detect a change
                var prevHeight = ContentContainer.Height;
                RecalculateContainerHeight();

                if (Math.Abs(ContentContainer.Height - prevHeight) > 0.1f)
                {
                    // Clamp scroll position to prevent empty space at bottom
                    // If content shrinks, we might be scrolled further down than allowed.
                    var minY = Height - ContentContainer.Height;
                    if (minY > 0) minY = 0;

                    if (ContentContainer.Y < minY)
                    {
                        ContentContainer.Y = minY;

                        TargetY = minY;
                        PreviousTargetY = minY;
                        ContentContainer.Animations.Clear();
                    }

                    // Force pool shifting to adapt to new Height/Y immediatelly
                    HandlePoolShifting();
                }
            }

            // Process ONE initialization task per frame to avoid freezing
            if (InitializationTasks.Count > 0)
            {
                var task = InitializationTasks.Dequeue();
                task.Invoke();

                // If this was the last task (the pool is fully assembled)
                if (InitializationTasks.Count == 0)
                {
                    // Final stabilization of layout
                    _cachedSlotHeight = -1;
                    RecalculateContainerHeight();
                    UpdateScrollbarHeight();
                    
                    PositionAndContainPoolObjects();
                    SnapToSelected();

                    // Auto-expand the selected mapset on initialization
                    var selected = AvailableItems.FirstOrDefault(x => x.Maps.Contains(MapManager.Selected.Value));
                    if (selected != null)
                        ExpandMapset(selected);

                    _needsReposition = true;

                    ScrollToSelected();
                    FireInitializedEvent();
                }
            }

            if (Quaver.Shared.Config.ConfigManager.SelectGroupMapsetsBy.Value == Quaver.Shared.Config.GroupMapsetsBy.Playlists &&
                Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value != null)
            {
                if (PlaylistHeaderPanel == null)
                {
                    PlaylistHeaderPanel = new Quaver.Shared.Screens.Selection.UI.Playlists.DrawablePlaylist(this, Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value);
                    PlaylistHeaderPanel.Parent = ContentContainer;
                    _needsReposition = true;
                }
                else if (PlaylistHeaderPanel.Item != Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value)
                {
                    PlaylistHeaderPanel.UpdateContent(Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value, 0);
                    _needsReposition = true;
                }

                PlaylistHeaderPanel.Y = 0;
                PlaylistHeaderPanel.Visible = true;
                PlaylistHeaderPanel.Alpha = 1;
            }
            else
            {
                if (PlaylistHeaderPanel != null)
                {
                    PlaylistHeaderPanel.Parent = null;
                    PlaylistHeaderPanel.Destroy();
                    PlaylistHeaderPanel = null;
                    _needsReposition = true;
                }
            }

            base.Update(gameTime);

            // Reposition pool items only when layout has changed (expand, collapse, pool shift, init)
            if (_needsReposition)
            {
                RepositionPoolItems();
                _needsReposition = false;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable twice DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;
            MapManager.MapsetDeleted -= OnMapsetDeleted;

            if (AvailableMapsets != null)
                AvailableMapsets.ValueChanged -= OnAvailableMapsetsChanged;

            SelectionScreen.RandomMapsetSelected -= OnRandomMapsetSelected;
            SkinManager.SkinLoaded -= OnSkinLoaded;

            InitializationTasks.Clear();
            while (RecycledMapsets.Count > 0)
            {
                RecycledMapsets.Pop().Destroy();
            }

            base.Destroy();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <returns></returns>
        protected override float GetSelectedPosition() => (-SelectedIndex.Value + 4) * GetMapsetSlotHeight();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override PoolableSprite<Mapset> CreateObject(Mapset item, int index) => new DrawableMapset(this, item, index);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        protected override void HandleInput(GameTime gameTime)
        {
            if (ActiveScrollContainer.Value != SelectScrollContainerType.Mapsets)
                return;

            // Pressing up and down while the current mapset is not visible to scroll to the one
            // that is in the middle
            if ((KeyboardManager.IsUniqueKeyPress(Keys.Left) || KeyboardManager.IsUniqueKeyPress(Keys.Right)) && CanScrollToMiddleMapset())
            {
                MapManager.SelectMapFromMapset(MiddleMapset!.Item);
                SelectedIndex.Value = AvailableMapsets.Value.IndexOf(MiddleMapset!.Item);

                ScrollToSelected();
            }
            // Move to the next mapset
            else if (KeyboardManager.IsUniqueKeyPress(Keys.Right) || KeyboardManager.IsUniqueKeyPress(Keys.Down))
            {
                if (SelectedIndex.Value + 1 >= AvailableMapsets.Value.Count)
                    return;

                MapManager.SelectMapFromMapset(AvailableMapsets.Value[SelectedIndex.Value + 1]);
                SelectedIndex.Value++;

                ScrollToSelected();
                ExpandMapset(AvailableMapsets.Value[SelectedIndex.Value]);
            }
            // Move to the previous mapset
            else if (KeyboardManager.IsUniqueKeyPress(Keys.Left) || KeyboardManager.IsUniqueKeyPress(Keys.Up))
            {
                if (SelectedIndex.Value - 1 < 0)
                    return;

                MapManager.SelectMapFromMapset(AvailableMapsets.Value[SelectedIndex.Value - 1]);

                SelectedIndex.Value--;
                ScrollToSelected();
                ExpandMapset(AvailableMapsets.Value[SelectedIndex.Value]);
            }
            // Move to the next difficulty of a mapset
            else if (KeyboardManager.IsCtrlDown()
                && KeyboardManager.IsUniqueKeyPress(Keys.PageDown))
            {
                var currentMapset = AvailableMapsets.Value.ElementAtOrDefault(SelectedIndex.Value);
                if (currentMapset == null || currentMapset.Maps.Count != 1)
                    return;

                InputEnabled = false;
                var val = SelectedIndex.Value;
                var found = false;

                for (var i = val + 1; i != val; i++)
                {
                    if (i >= AvailableMapsets.Value.Count)
                    {
                        i = 0;
                        if (i == val) break;
                    }

                    var mapset = AvailableMapsets.Value[i];

                    if (mapset.Maps.First().Mapset != MapManager.Selected.Value.Mapset)
                        continue;

                    if (mapset.Maps.First() == MapManager.Selected.Value)
                        continue;

                    SelectedIndex.Value = i;
                    MapManager.SelectMapFromMapset(AvailableMapsets.Value[i]);
                    ScrollToSelected();
                    found = true;
                    break;
                }

                if (!found)
                    InputEnabled = true;
            }
            // Move to the previous difficulty of a mapset
            else if (KeyboardManager.IsCtrlDown()
                && KeyboardManager.IsUniqueKeyPress(Keys.PageUp))
            {
                var currentMapset = AvailableMapsets.Value.ElementAtOrDefault(SelectedIndex.Value);
                if (currentMapset == null || currentMapset.Maps.Count != 1)
                    return;

                InputEnabled = false;
                var val = SelectedIndex.Value;
                var found = false;

                for (var i = val - 1; i != val; i--)
                {
                    if (i < 0)
                    {
                        i = AvailableMapsets.Value.Count - 1;
                        if (i == val) break;
                    }

                    var mapset = AvailableMapsets.Value[i];

                    if (mapset.Maps.First().Mapset != MapManager.Selected.Value.Mapset)
                        continue;

                    if (mapset.Maps.First() == MapManager.Selected.Value)
                        continue;

                    SelectedIndex.Value = i;
                    MapManager.SelectMapFromMapset(AvailableMapsets.Value[i]);
                    ScrollToSelected();
                    found = true;
                    break;
                }

                if (!found)
                    InputEnabled = true;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private void InitializeMapsets(bool restart)
        {
            // CRITICAL: Synchronize AvailableItems FIRST, before any early returns
            // This ensures the list is always up-to-date even when debouncing
            AvailableItems = AvailableMapsets.Value;

            if (restart)
            {
                // Clear stale expansion state to prevent ghost difficulty panels
                // after view switches (e.g. Playlists → Mapsets). Without this,
                // recycled DrawableMapsets re-expand with corrupted DifficultyContainers.
                ExpandedMapsets.Clear();
                CachedExpandedMapsetIndex = -1;
                CachedCollapsingMapsetIndex = -1;
                CollapsingMapset = null;
                CachedCollapsingMapsetHeight = 0;
                _cachedMiddleObjectIndex = 0;
                _cachedSlotHeight = -1; // Reset height cache to ensure proper V1/V2 detection

                // Proactively expand the selected mapset if it exists in the new list
                var selected = AvailableItems.FirstOrDefault(x => x.Maps.Contains(MapManager.Selected.Value));
                if (selected != null)
                {
                    ExpandedMapsets.Add(selected);
                    CachedExpandedMapsetIndex = AvailableItems.IndexOf(selected);
                }

                RecalculateContainerHeight();
                
                // Do NOT snap yet - wait for InitializationTasks to finish in Update

                TimeElapsedUntilInitializationRequest = 0;
                HasReinitialized = false;
                return;
            }

            if (TimeElapsedUntilInitializationRequest < ReinitializeTime || HasReinitialized)
                return;

            lock (Pool)
            {
                UpdateCachedExpandedMapsetIndex();

                SetSelectedIndex();

                // Reset the starting index so we can be aware of the mapsets that are needed
                PoolStartingIndex = DesiredPoolStartingIndex(SelectedIndex.Value);

                var targetPoolCount = Math.Min(PoolSize, AvailableItems.Count);

                // Instantly remove excess elements
                while (Pool.Count > targetPoolCount)
                {
                    var extra = (DrawableMapset)Pool.Last();
                    extra.ClearAnimations();
                    if (extra.IsExpanded) extra.Collapse(); // Visual cleanup
                    RemoveContainedDrawable(extra);
                    Pool.RemoveAt(Pool.Count - 1);
                    RecycledMapsets.Push(extra);
                }

                // Clear any outdated initialization tasks
                InitializationTasks.Clear();

                // Queue up the creation and updating of the items
                for (var i = 0; i < targetPoolCount; i++)
                {
                    var indexOffset = i;
                    InitializationTasks.Enqueue(() =>
                    {
                        var dataIndex = PoolStartingIndex + indexOffset;

                        // Make sure we don't go out of bounds if items changed while queued
                        if (dataIndex >= AvailableItems.Count) return;

                        DrawableMapset drawable;

                        // Create or recycle
                        // If we are currently expanding the pool size (up to targetPoolCount)
                        if (indexOffset >= Pool.Count)
                        {
                            if (RecycledMapsets.Count > 0)
                            {
                                drawable = RecycledMapsets.Pop();
                            }
                            else
                            {
                                drawable = (DrawableMapset)CreateObject(AvailableItems[dataIndex], dataIndex);
                                drawable.DestroyIfParentIsNull = false;
                            }

                            Pool.Add(drawable);
                            AddContainedDrawable(drawable);
                            RecalculateContainerHeight();
                        }
                        else
                        {
                            drawable = (DrawableMapset)Pool[indexOffset];
                        }

                        drawable.UpdateContent(AvailableItems[dataIndex], dataIndex);
                    });
                }
            }

            HasReinitialized = true;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object? sender, BindableValueChangedEventArgs<Map> e) => ScrollToSelected();

        /// <summary>
        ///     Called when the list of available maps has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAvailableMapsetsChanged(object? sender, BindableValueChangedEventArgs<List<Mapset>> e) => InitializeMapsets(true);

        /// <summary>
        ///     Properly sets the selected index when a random map was selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRandomMapsetSelected(object? sender, RandomMapsetSelectedEventArgs e)
        {
            SelectedIndex.Value = e.Index;
            ExpandMapset(AvailableItems[e.Index]);
            ScrollToSelected(800);
        }

        /// <summary>
        ///     Sets the appropriate index of the selected mapset
        /// </summary>
        protected override void SetSelectedIndex()
        {
            SelectedIndex.Value = AvailableItems.FindIndex(x => x.Maps.Contains(MapManager.Selected.Value));

            var oldValue = SelectedIndex.Value;

            if (SelectedIndex.Value == -1)
                SelectedIndex.Value = 0;

            // Manually trigger the change event in the case that the selected index is still the same value.
            // Other components that rely on the bindable will want to update their state in case
            // the mapset has changed in any way.
            if (oldValue == SelectedIndex.Value)
                SelectedIndex.TriggerChangeEvent();
        }

        /// <summary>
        ///     When a mapset has been deleted, reset the selected index of the container to the newly selected mapset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapsetDeleted(object? sender, MapsetDeletedEventArgs e)
        {
            if (e.Index == -1)
                SelectedIndex.Value = 0;

            if (e.Index - 1 < 0)
                return;

            SelectedIndex.Value = e.Index - 1;
        }

        /// <summary>
        ///     Tries to find the currently selected mapset if it is in the pool
        /// </summary>
        /// <returns></returns>
        private DrawableMapset? GetCurrentlySelectedMapset()
        {
            for (var i = 0; i < Pool.Count; i++)
            {
                var item = (DrawableMapset)Pool[i];

                if (item.Item.Maps.Contains(MapManager.Selected.Value))
                    return item;
            }

            return null;
        }

        /// <summary>
        ///     Returns if the user is eligible to scroll to the middle mapset.
        ///
        ///     - The mapset must not already be selected
        ///     - The currently selected mapset must be out of view
        /// </summary>
        /// <returns></returns>
        private bool CanScrollToMiddleMapset()
        {
            var mapsetNotSelected = MiddleMapset != null && MiddleMapset.Item.Maps.First() != MapManager.Selected.Value;
            return mapsetNotSelected && GetCurrentlySelectedMapset() == null;
        }

        private void SetCaching(bool cache) => Pool.ForEach(mapset => ((DrawableMapset)mapset).DrawableContainer.IsCached = cache);

        /// <summary>
        ///     Checks if a mapset is currently expanded to show its difficulties
        /// </summary>
        /// <param name="mapset">The mapset to check</param>
        /// <returns>True if expanded, false otherwise</returns>
        public bool IsMapsetExpanded(Mapset mapset)
        {
            if (mapset == null)
                return false;

            return ExpandedMapsets.Contains(mapset);
        }

        /// <summary>
        ///     Expands a mapset to show its difficulties inline
        /// </summary>
        /// <param name="mapset">The mapset to expand</param>
        public void ExpandMapset(Mapset mapset)
        {
            if (mapset == null || IsMapsetExpanded(mapset))
                return;

            if (CollapsingMapset == mapset)
            {
                CollapsingMapset = null;
                CachedCollapsingMapsetIndex = -1;
            }

            // Enforce single expansion: Collapse all other expanded mapsets
            var currentlyExpanded = ExpandedMapsets.ToList();
            foreach (var other in currentlyExpanded)
            {
                if (other == mapset)
                    continue;

                CollapseMapset(other, true);
            }

            ExpandedMapsets.Add(mapset);
            CachedExpandedMapsetIndex = AvailableItems.IndexOf(mapset);

            // Find the DrawableMapset in the pool and expand it
            foreach (var poolItem in Pool)
            {
                if (poolItem is DrawableMapset drawableMapset && drawableMapset.Item == mapset)
                {
                    drawableMapset.Expand();
                    break;
                }
            }

            // Mark layout dirty and reposition immediately
            _needsReposition = true;
            RepositionPoolItems();
            _needsReposition = false;
            RecalculateContainerHeight();
            ScrollToSelected(ScrollDuration);
        }

        /// <summary>
        ///     Collapses a mapset to hide its difficulties
        /// </summary>
        /// <param name="mapset">The mapset to collapse</param>
        /// <param name="force">If true, forces collapse bypassing count checks</param>
        public void CollapseMapset(Mapset mapset, bool force = false)
        {
            if (mapset == null || !IsMapsetExpanded(mapset))
                return;

            if (!force && ExpandedMapsets.Count <= 1)
                return;

            ExpandedMapsets.Remove(mapset);

            // Set as collapsing so we maintain layout awareness during animation
            CollapsingMapset = mapset;
            UpdateCachedExpandedMapsetIndex();

            // Find the DrawableMapset in the pool and collapse it to get accurate starting height
            var poolItem = Pool.OfType<DrawableMapset>().FirstOrDefault(x => x.Item == mapset);
            if (poolItem != null)
            {
                poolItem.Collapse();
                CachedCollapsingMapsetHeight = poolItem.CurrentAnimatedHeight;
            }
            else
            {
                CachedCollapsingMapsetHeight = CalculateStaticExpandedHeight(mapset);
            }

            // Mark layout dirty and reposition immediately
            _needsReposition = true;
            RepositionPoolItems();
            _needsReposition = false;
            RecalculateContainerHeight();
        }

        /// <summary>
        ///     Toggles the expanded state of a mapset
        /// </summary>
        /// <param name="mapset">The mapset to toggle</param>
        public void ToggleMapsetExpanded(Mapset mapset)
        {
            if (mapset == null)
                return;

            if (IsMapsetExpanded(mapset))
                CollapseMapset(mapset);
            else
                ExpandMapset(mapset);
        }

        /// <summary>
        ///     Calculates the Y position of a mapset at a given index.
        /// </summary>
        /// <param name="index">The index of the mapset</param>
        /// <param name="forceStatic">If true, uses final static heights instead of animating ones</param>
        /// <returns>The relative Y position</returns>
        public float GetMapsetY(int index, bool forceStatic = false)
        {
            float y = index * GetMapsetSlotHeight() + PaddingTop;

            // Add height of expanded mapset if it is ABOVE this index
            // Since we enforce single expansion, checking CachedExpandedMapsetIndex is sufficient
            if (CachedExpandedMapsetIndex != -1 && CachedExpandedMapsetIndex < index)
            {
                var expandedMapset = AvailableItems[CachedExpandedMapsetIndex];
                y += GetExpandedHeightForMapset(expandedMapset, forceStatic);
            }

            // Also add height of the collapsing mapset if it is ABOVE this index
            if (CachedCollapsingMapsetIndex != -1 && CachedCollapsingMapsetIndex < index && CachedCollapsingMapsetIndex != CachedExpandedMapsetIndex)
            {
                var collapsingMapset = AvailableItems[CachedCollapsingMapsetIndex];
                y += GetExpandedHeightForMapset(collapsingMapset, forceStatic);
            }

            return y;
        }

        /// <summary>
        ///     Scrolls to a specific mapset using exact centering logic.
        ///     - If expanded, the selected difficulty panel is centered.
        ///     - If collapsed, the mapset panel itself is centered.
        /// </summary>
        /// <param name="index">The index of the mapset</param>
        /// <param name="duration">Animation duration</param>
        private void ScrollToMapsetSmart(int index, double duration)
        {
            if (index < 0 || index >= AvailableItems.Count)
                return;

            var mapset = AvailableItems[index];
            var mapsetY = GetMapsetY(index, true);

            float targetScrollY;

            // Check if mapset is visually expanding/expanded and contains the selected map
            if (IsMapsetExpanded(mapset) && MapManager.Selected.Value != null && mapset.Maps.Contains(MapManager.Selected.Value))
            {
                var selectedMap = MapManager.Selected.Value;
                var mapIndex = mapset.Maps.IndexOf(selectedMap);

                var version = SkinManager.Skin?.UserInterfaceVersion ?? 1;
                var baseHeight = MapsetHelper.GetMapsetHeight(null, false, (int)version);
                var topSpacing = version == 2 ? 10f : 0f;
                const int panelHeight = 60;
                const int panelSpacing = 11;

                // Center point of the exact active difficulty panel relative to mapset
                // We use baseHeight instead of GetMapsetSlotHeight() to avoid including inter-slot spacing.
                var difficultyOffsetY = baseHeight + topSpacing
                                        + (mapIndex * (panelHeight + panelSpacing))
                                        + (panelHeight / 2f);

                var absoluteDifficultyCenterY = mapsetY + difficultyOffsetY;

                // Align this absolute point to the dead center of the screen
                targetScrollY = -(absoluteDifficultyCenterY - (Height / 2f));
            }
            else
            {
                // Align the mapset panel itself to the dead center of the screen
                var mapsetCenterY = mapsetY + (GetMapsetSlotHeight() / 2f);
                targetScrollY = -(mapsetCenterY - (Height / 2f));
            }

            ScrollTo(targetScrollY, duration > 0 ? (int)duration : 0);
        }

        /// <summary>
        ///     Scrolls to the currently selected mapset with smart positioning.
        /// </summary>
        /// <param name="duration"></param>
        public void ScrollToSelected(double? duration = null)
        {
            if (SelectedIndex.Value < 0)
                return;

            ScrollToMapsetSmart(SelectedIndex.Value, duration ?? ScrollDuration);
        }

        /// <summary>
        ///     Snap to selected without animation (for initialization)
        /// </summary>
        public override void SnapToSelected()
        {
            base.SnapToSelected();
            ScrollToSelected(0);
        }

        /// <summary>
        ///     Gets the dynamic/static height for a mapset.
        /// </summary>
        /// <param name="mapset">The mapset to get height for</param>
        /// <param name="forceStatic">If true, returns the final static height instead of the animating one</param>
        /// <returns>The height for the mapset</returns>
        private float GetExpandedHeightForMapset(Mapset mapset, bool forceStatic = false)
        {
            if (mapset == null || mapset.Maps == null)
                return 0;

            if (mapset == CollapsingMapset)
                return forceStatic ? 0 : CachedCollapsingMapsetHeight;

            if (!IsMapsetExpanded(mapset))
                return 0;

            if (!forceStatic)
            {
                var poolItem = Pool.OfType<DrawableMapset>().FirstOrDefault(x => x.Item == mapset);
                if (poolItem != null && poolItem.IsAnimatingExpand)
                    return poolItem.CurrentAnimatedHeight;
            }

            return CalculateStaticExpandedHeight(mapset);
        }

        /// <summary>
        ///     Computes the static full height of an expanded mapset.
        /// </summary>
        private float CalculateStaticExpandedHeight(Mapset mapset)
        {
            if (mapset?.Maps == null) return 0;

            var version = GetSongSelectVersion();
            return MapsetHelper.GetDifficultyListHeight(mapset.Maps.Count, version);
        }

        /// <summary>
        ///     Calculates the cumulative expanded height of all mapsets before a certain index.
        /// </summary>
        /// <param name="beforeIndex">The index to calculate up to</param>
        /// <param name="forceStatic">If true, uses final static heights instead of animating ones</param>
        /// <returns>The cumulative expanded height</returns>
        private float GetCumulativeExpandedHeight(int beforeIndex, bool forceStatic = false)
        {
            if (AvailableItems == null)
                return 0;

            float total = 0;

            if (CachedExpandedMapsetIndex != -1 && CachedExpandedMapsetIndex < beforeIndex)
            {
                var expandedMapset = AvailableItems[CachedExpandedMapsetIndex];
                total += GetExpandedHeightForMapset(expandedMapset, forceStatic);
            }

            if (CachedCollapsingMapsetIndex != -1 && CachedCollapsingMapsetIndex < beforeIndex && CachedCollapsingMapsetIndex != CachedExpandedMapsetIndex)
            {
                var collapsingMapset = AvailableItems[CachedCollapsingMapsetIndex];
                total += GetExpandedHeightForMapset(collapsingMapset, forceStatic);
            }

            return total;
        }

        /// <summary>
        ///     Repositions all items in the pool to account for expanded mapsets.
        ///     Uses smooth animation for position changes.
        /// </summary>
        public void RepositionPoolItems()
        {
            var paddingTop = PaddingTop;
            if (Quaver.Shared.Config.ConfigManager.SelectGroupMapsetsBy.Value == Quaver.Shared.Config.GroupMapsetsBy.Playlists &&
                Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value != null)
            {
                paddingTop = (GetSongSelectVersion() == 2 ? 110 : DrawableMapset.MapsetHeight);
            }

            for (var i = 0; i < Pool.Count; i++)
            {
                var poolItem = Pool[i];
                var itemIndex = PoolStartingIndex + i;

                // Base position + cumulative expanded height from mapsets above
                var baseY = itemIndex * GetMapsetSlotHeight() + paddingTop;
                var expandedOffset = GetCumulativeExpandedHeight(itemIndex);
                var targetY = baseY + expandedOffset;

                // Only animate if position has changed significantly
                if (Math.Abs(poolItem.Y - targetY) > 1f)
                {
                    // Directly set Y for smooth animation driven by Update loop
                    // The animation is driven by DrawableMapset.Update changing CurrentAnimatedHeight
                    poolItem.Y = targetY;
                }
            }
        }

        /// <summary>
        ///     Override to calculate container height including expanded mapsets
        /// </summary>
        public override void RecalculateContainerHeight(bool usePoolCount = false)
        {
            if (AvailableItems == null)
            {
                base.RecalculateContainerHeight(usePoolCount);
                return;
            }

            var count = usePoolCount ? Pool.Count : AvailableItems.Count;
            var baseHeight = GetMapsetSlotHeight() * count;

            // Add expanded height for mapset
            // Using the cached indices avoids iterating the entire list O(N) -> O(1).
            float totalExpandedHeight = 0;

            // Check if we have a valid cached index locally in the list
            if (CachedExpandedMapsetIndex != -1 && CachedExpandedMapsetIndex < AvailableItems.Count)
            {
                var mapset = AvailableItems[CachedExpandedMapsetIndex];
                // We use static height for container bounds to ensure ScrollTo doesn't jump/clamp
                // prematurely while the list is still expanding.
                totalExpandedHeight += GetExpandedHeightForMapset(mapset, true);
            }

            if (CachedCollapsingMapsetIndex != -1 && CachedCollapsingMapsetIndex < AvailableItems.Count && CachedCollapsingMapsetIndex != CachedExpandedMapsetIndex)
            {
                var collapsingMapset = AvailableItems[CachedCollapsingMapsetIndex];
                // Collapsing mapsets contribute 0 static height because they are disappearing.
                totalExpandedHeight += GetExpandedHeightForMapset(collapsingMapset, true);
            }

            var version = GetSongSelectVersion();
            var paddingBottom = version == 2 ? 0 : PaddingBottom;

            var paddingTop = PaddingTop;
            if (Quaver.Shared.Config.ConfigManager.SelectGroupMapsetsBy.Value == Quaver.Shared.Config.GroupMapsetsBy.Playlists &&
                Quaver.Shared.Database.Playlists.PlaylistManager.Selected.Value != null)
            {
                paddingTop = (version == 2 ? 110 : DrawableMapset.MapsetHeight);
            }

            var totalHeight = baseHeight + totalExpandedHeight + paddingTop + paddingBottom;

            // V2 optimization: Ensure the content ends exactly at the last mapset panel's edge.
            // MapsetSlotHeight (110) includes a 10px gap which is only needed BETWEEN items.
            if (AvailableItems != null && AvailableItems.Count > 0 && paddingBottom <= 0)
            {
                if (version == 2)
                    totalHeight -= (MapsetHelper.V2MapsetSlotHeight - MapsetHelper.V2MapsetHeight);
            }

            if (totalHeight > Height)
                ContentContainer.Height = totalHeight;
            else
                ContentContainer.Height = Height;
        }

        /// <summary>
        ///     Updates the cached expanded mapset index.
        /// </summary>
        private void UpdateCachedExpandedMapsetIndex()
        {
            var expandedMapset = ExpandedMapsets.FirstOrDefault();

            if (expandedMapset != null && AvailableItems != null)
                CachedExpandedMapsetIndex = AvailableItems.IndexOf(expandedMapset);
            else
                CachedExpandedMapsetIndex = -1;

            if (CollapsingMapset != null && AvailableItems != null)
                CachedCollapsingMapsetIndex = AvailableItems.IndexOf(CollapsingMapset);
            else
                CachedCollapsingMapsetIndex = -1;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Handles shifting the object pool by accounting for variable item heights.
        /// </summary>
        protected override void HandlePoolShifting()
        {
            if (AvailableItems == null || AvailableItems.Count == 0 || Pool.Count != Math.Min(PoolSize, AvailableItems.Count))
                return;

            // Find the object currently in the middle of the container.
            // Uses cached index from previous frame and walks ±1 incrementally (O(1) amortized)
            // instead of scanning from 0 each time (O(N)).
            var centerY = -ContentContainer.Y + Height / 2;
            var idx = Math.Clamp(_cachedMiddleObjectIndex, 0, AvailableItems.Count - 1);

            // Walk forward if centerY is past the current item's range
            while (idx < AvailableItems.Count - 1)
            {
                var endY = GetMapsetY(idx) + GetMapsetSlotHeight() + GetExpandedHeightForMapset(AvailableItems[idx]);
                if (centerY <= endY) break;
                idx++;
            }

            // Walk backward if centerY is before the current item
            while (idx > 0)
            {
                var startY = GetMapsetY(idx);
                if (centerY >= startY) break;
                idx--;
            }

            _cachedMiddleObjectIndex = idx;
            var middleObjectIndex = idx;

            // Compute the corresponding PoolStartingIndex.
            var desiredPoolStartingIndex = DesiredPoolStartingIndex(middleObjectIndex);

            // If our PoolStartingIndex is already corrected, then there's nothing to do.
            if (PoolStartingIndex == desiredPoolStartingIndex)
                return;

            // Compute the overlap: the number of pooled objects that can be re-used from the previous position.
            var difference = Math.Abs(PoolStartingIndex - desiredPoolStartingIndex);
            var overlap = Math.Max(Pool.Count - difference, 0);
            var refresh = Pool.Count - overlap;

            if (PoolStartingIndex > desiredPoolStartingIndex)
            {
                // The container has been scrolled back. The re-usable objects are in the beginning of the buffer.
                for (var i = 0; i < refresh; i++)
                {
                    var objectIndex = desiredPoolStartingIndex + Pool.Count - 1 - overlap - i;

                    var drawable = Pool.Last();
                    drawable.Y = GetMapsetY(objectIndex);
                    drawable.UpdateContent(AvailableItems[objectIndex], objectIndex);

                    // Circularly shift the list back one.
                    Pool.RemoveAt(Pool.Count - 1);
                    Pool.Insert(0, drawable);
                }
            }
            else
            {
                // The container has been scrolled forward. The re-usable objects are in the end of the buffer.
                for (var i = 0; i < refresh; i++)
                {
                    var objectIndex = desiredPoolStartingIndex + overlap + i;

                    var drawable = Pool.First();
                    drawable.Y = GetMapsetY(objectIndex);
                    drawable.UpdateContent(AvailableItems[objectIndex], objectIndex);

                    // Circularly shift the list forward one.
                    Pool.RemoveAt(0);
                    Pool.Add(drawable);
                }
            }

            PoolStartingIndex = desiredPoolStartingIndex;
            _needsReposition = true;
        }
    }
}
