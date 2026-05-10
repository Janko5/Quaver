using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.API.Enums;
using Quaver.Server.Client.Events.Scores;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public sealed class DrawableMapset : PoolableSprite<Mapset>
    {
        public static int MapsetHeight { get; set; } = 97;

        public static class DifficultyPanelDimensions
        {
            public const int V1Width = 1080;
            public const int V1Expansion = 47;
            public const int V2Width = 900;
            public const int V2SelectedWidth = 925;
            public static int V2Expansion => V2SelectedWidth - V2Width;
        }

        /// <summary>
        ///     Dynamic base height based on version.
        /// </summary>
        public static int BaseHeight => (int)MapsetHelper.GetMapsetHeight(null, false, SkinManager.Skin?.UserInterfaceVersion ?? 1);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override int HEIGHT { get; } = MapsetHeight;

        /// <summary>
        /// </summary>
        public static int WIDTH { get; } = 1188;

        /// <summary>
        ///     Contains the actual mapset
        /// </summary>
        public DrawableMapsetContainer DrawableContainer { get; }

        /// <summary>
        ///     If this mapset is currently selected
        /// </summary>
        public bool IsSelected { get; private set; }

        /// <summary>
        ///     Whether the mapset is expanded to show its difficulties
        /// </summary>
        public bool IsExpanded { get; private set; }

        /// <summary>
        ///     Container that holds the inline difficulty panels when expanded (with clipping for animation)
        /// </summary>
        private ClippingContainer DifficultyContainer { get; set; } = null!;

        /// <summary>
        ///     The current animated height of the difficulty list (used for smooth expand/collapse)
        /// </summary>
        public float CurrentAnimatedHeight { get; private set; }

        /// <summary>
        ///     The target height for the difficulty list animation
        /// </summary>
        private float TargetDifficultyHeight { get; set; }

        /// <summary>
        ///     Whether the expand animation is currently playing
        /// </summary>
        public bool IsAnimatingExpand { get; private set; }

        /// <summary>
        ///     Whether the collapse animation is currently playing
        /// </summary>
        public bool IsAnimatingCollapse { get; private set; }

        /// <summary>
        ///     Animation constants
        /// </summary>
        private const float ExpandAnimationDuration = 300f;
        private const float CollapseAnimationDuration = 300f;
        private const float BottomPadding = 10f;

        /// <summary>
        ///     Helper to get the parent scroll container casted to the correct type
        /// </summary>
        private MapsetScrollContainer ScrollContainer => (MapsetScrollContainer)Container;

        /// <summary>
        ///     List of drawable difficulty items when expanded
        /// </summary>
        private List<DrawableMapContainer> DifficultyItems { get; } = new List<DrawableMapContainer>();

        /// <summary>
        ///     Stores references for each map's difficulty panel for in-place updates (all icons and texts)
        ///     Key: Map, Value: all elements that need repositioning when modifiers change
        /// </summary>
        private Dictionary<Map, DifficultyPanelRefs> DifficultyStatsRefs { get; } = new Dictionary<Map, DifficultyPanelRefs>();

        /// <summary>
        ///     Holds all UI element references for a difficulty panel
        /// </summary>
        private class DifficultyPanelRefs
        {
            public Sprite NpsIcon { get; set; } = null!;
            public float NpsIconOriginalX { get; set; }
            public SpriteTextPlus NpsText { get; set; } = null!;
            public float NpsTextOriginalX { get; set; }
            public Sprite LnIcon { get; set; } = null!;
            public float LnIconOriginalX { get; set; }
            public SpriteTextPlus LnText { get; set; } = null!;
            public float LnTextOriginalX { get; set; }
            public Sprite? BpmIcon { get; set; }
            public float BpmIconOriginalX { get; set; }
            public SpriteTextPlus? BpmText { get; set; }
            public float BpmTextOriginalX { get; set; }
            public Sprite? LengthIcon { get; set; }
            public float LengthIconOriginalX { get; set; }
            public SpriteTextPlus? LengthText { get; set; }
            public float LengthTextOriginalX { get; set; }
            public NineSliceSprite RatingOverlay { get; set; } = null!;
            public float RatingOverlayOriginalX { get; set; }
            public Sprite RatingIcon { get; set; } = null!;
            public SpriteTextPlus RatingText { get; set; } = null!;
            public MarqueeSpriteText DifficultyNameText { get; set; } = null!;
        }

        private float _maxNpsWidth;
        private float _maxLnWidth;
        private float _maxBpmWidth;
        private float _maxLengthWidth;


        /// <summary>
        ///     Dictionary to cache string measurements for the current initialization run
        /// </summary>
        private readonly Dictionary<string, float> _measurementCache = new();

        private static float MeasureString(WobbleFontStore font, string text, Dictionary<string, float> localCache)
        {
            if (localCache.TryGetValue(text, out var cachedWidth))
                return cachedWidth;

            // Use the shared cache in MapsetHelper to avoid redundant measurements across tiles.
            // We pass font name and size to ensure cache hits are valid for the current font context.
            var width = MapsetHelper.MeasureTextShared(font, text, Assets.Fonts.InterBold.ToString(), (int)font.FontSize);
            localCache[text] = width;
            return width;
        }

        /// <summary>
        ///     Holds difficulty panels that have been created but are no longer needed
        /// </summary>
        private Stack<ImageButton> RecycledDifficultyPanels { get; } = new Stack<ImageButton>();

        /// <summary>
        ///     Holds tasks that need to be processed every frame during difficulty list construction
        /// </summary>
        private Queue<Action> ConstructionTasks { get; } = new Queue<Action>();


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public DrawableMapset(PoolableScrollContainer<Mapset> container, Mapset item, int index) : base(container, item, index)
        {
            Size = new ScalableVector2(WIDTH, HEIGHT);

            DrawableContainer = new DrawableMapsetContainer(this)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                UsePreviousSpriteBatchOptions = true
            };

            Alpha = 0;
            UpdateContent(item, index);

            MapManager.Selected.ValueChanged += OnMapChanged;

            if (OnlineManager.Client != null)
                OnlineManager.Client.OnRetrievedOnlineScores += OnRetrievedOnlineScores;

            MapsetInfoRetriever.MapsetInfoRetrieved += OnMapsetInfoRetrieved;

            ModManager.ModsChanged += OnModsChanged;

            UsePreviousSpriteBatchOptions = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;

            if (OnlineManager.Client != null)
                OnlineManager.Client.OnRetrievedOnlineScores -= OnRetrievedOnlineScores;

            MapsetInfoRetriever.MapsetInfoRetrieved -= OnMapsetInfoRetrieved;

            ModManager.ModsChanged -= OnModsChanged;

            base.Destroy();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Updates the expand/collapse animation state
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Handle expand animation
            if (IsAnimatingExpand)
            {
                var dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                // Linear step: total distance * (dt / duration)
                var distance = TargetDifficultyHeight;
                var step = distance * (dt / ExpandAnimationDuration);

                CurrentAnimatedHeight += step;

                if (CurrentAnimatedHeight >= TargetDifficultyHeight)
                    CurrentAnimatedHeight = TargetDifficultyHeight;

                if (DifficultyContainer != null)
                    DifficultyContainer.ClipHeight = CurrentAnimatedHeight;

                // Update sprite height for proper repositioning
                Height = BaseHeight + CurrentAnimatedHeight;

                // Check if animation is complete
                if (Math.Abs(CurrentAnimatedHeight - TargetDifficultyHeight) < 1f)
                {
                    CurrentAnimatedHeight = TargetDifficultyHeight;
                    if (DifficultyContainer != null)
                        DifficultyContainer.ClipHeight = TargetDifficultyHeight;
                    Height = BaseHeight + TargetDifficultyHeight;
                    IsAnimatingExpand = false;
                }
                // Ensure content is constructed fast enough to be visible during animation
                // Process up to 10 tasks per frame while animating to prevent the "void" appearance
                for (var i = 0; i < 10 && ConstructionTasks.Count > 0; i++)
                {
                    var task = ConstructionTasks.Dequeue();
                    task.Invoke();
                }
            }

            // Handle collapse animation
            if (IsAnimatingCollapse)
            {
                var dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                // We need to know the starting height to compute the linear step accurately,
                // but since it's collapsing to 0, TargetDifficultyHeight is 0 and CurrentAnimatedHeight is decreasing.
                // It's safer to use the original target height (which is the actual full height) 
                // or compute it based on the container's FullContentHeight.
                var distance = DifficultyContainer?.FullContentHeight ?? CurrentAnimatedHeight;
                if (distance == 0) distance = BaseHeight; // Fallback

                var step = distance * (dt / CollapseAnimationDuration);

                CurrentAnimatedHeight -= step;

                if (CurrentAnimatedHeight <= 0)
                    CurrentAnimatedHeight = 0;

                if (DifficultyContainer != null)
                    DifficultyContainer.ClipHeight = CurrentAnimatedHeight;

                // Update sprite height for proper repositioning
                Height = BaseHeight + CurrentAnimatedHeight;

                // Check if animation is complete
                if (CurrentAnimatedHeight < 1f)
                {
                    // Animation complete - clean up
                    CurrentAnimatedHeight = 0;
                    IsAnimatingCollapse = false;

                    if (DifficultyContainer != null)
                    {
                        DifficultyContainer.ClipHeight = 0;
                        DifficultyContainer.Height = 0;
                        DifficultyContainer.Visible = false;

                        // OPTIMIZATION: Avoid .ToList() allocation by iterating backwards
                        for (var i = DifficultyContainer.Children.Count - 1; i >= 0; i--)
                        {
                            if (DifficultyContainer.Children[i] is ImageButton child)
                            {
                                child.Parent = null;
                                child.Visible = false;
                                RecycledDifficultyPanels.Push(child);
                            }
                            else
                            {
                                DifficultyContainer.Children[i].Destroy();
                            }
                        }
                    }

                    Height = BaseHeight;
                    DifficultyItems.Clear();
                    DifficultyStatsRefs.Clear();
                }
            }

            // Process one construction task per frame to avoid lag
            if (ConstructionTasks.Count > 0)
            {
                var task = ConstructionTasks.Dequeue();
                task.Invoke();
            }
        }


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public override void UpdateContent(Mapset item, int index)
        {
            var isNewItem = Item != item;
            Item = item;
            Index = index;

            // Synchronous visual reset before ScheduleUpdate to prevent 1-frame artifacts
            // where old difficulty panels are rendered at the new mapset's position.
            if (isNewItem)
            {
                IsAnimatingExpand = false;
                IsAnimatingCollapse = false;
                CurrentAnimatedHeight = 0;
                TargetDifficultyHeight = 0;
                Height = BaseHeight;
                ConstructionTasks.Clear();

                if (DifficultyContainer != null)
                {
                    // Optimization: Snap layout state if height is roughly at rest (BaseHeight).
                    // This prevents small floating-point jitter from keeping animations "alive".
                    if (Math.Abs(Height - BaseHeight) < 0.5f)
                    {
                        Height = BaseHeight;
                        IsAnimatingCollapse = false;
                        DifficultyContainer.Visible = false;
                    }
                    DifficultyContainer.ClipHeight = 0;
                }
            }

            IsSelected = Item.Maps.Contains(MapManager.Selected.Value);

            ScheduleUpdate(() =>
            {
                if (isNewItem)
                {
                    // Deferred cleanup: recycle/destroy children (requires ScheduleUpdate
                    // because it manipulates the Children collection).
                    IsExpanded = false;

                    if (DifficultyContainer != null)
                    {
                        DifficultyContainer.Height = 0;

                        // OPTIMIZATION: Avoid .ToList() by iterating backwards
                        for (var i = DifficultyContainer.Children.Count - 1; i >= 0; i--)
                        {
                            var child = DifficultyContainer.Children[i];
                            if (child is ImageButton btn)
                            {
                                btn.Visible = false;
                                DifficultyContainer.Children.Remove(btn);
                                RecycledDifficultyPanels.Push(btn);
                            }
                            else
                            {
                                child.Destroy();
                            }
                        }
                    }

                    DifficultyItems.Clear();
                    DifficultyStatsRefs.Clear();
                }

                // Make sure the mapset is properly selected/deselected when updating the content
                if (IsSelected)
                    Select();
                else
                    Deselect();

                // Update all the values in the mapset
                DrawableContainer.UpdateContent(Item, Index);

                // Sync expanded state with container (recycling fix)
                // IMPORTANT: Handle this INSTANTLY without animation to prevent scrolling jumps
                if (ScrollContainer.IsMapsetExpanded(Item))
                {
                    if (!IsExpanded)
                        Expand();

                    // Flush remaining construction tasks so all difficulty panels are created
                    // and TargetDifficultyHeight is finalized before we force-finish.
                    while (ConstructionTasks.Count > 0)
                        ConstructionTasks.Dequeue().Invoke();

                    // Force-finish: snap to final expanded state without animation.
                    // Check TargetDifficultyHeight > 0 instead of IsAnimatingExpand because
                    // the animation start flag may not be set yet if tasks just got flushed.
                    if (IsAnimatingExpand || TargetDifficultyHeight > 0)
                    {
                        CurrentAnimatedHeight = TargetDifficultyHeight;
                        if (DifficultyContainer != null)
                            DifficultyContainer.ClipHeight = TargetDifficultyHeight;
                        Height = BaseHeight + TargetDifficultyHeight;
                        IsAnimatingExpand = false;
                    }
                }
                else
                {
                    if (IsExpanded)
                        Collapse();
                }
            });
        }

        /// <summary>
        ///     Selects the mapset and performs an animation
        /// </summary>
        public void Select()
        {
            IsSelected = true;
            DrawableContainer?.Select();
        }

        /// <summary>
        ///     Deselects the mapset and performs an animation
        /// </summary>
        public void Deselect()
        {
            IsSelected = false;
            DrawableContainer?.Deselect();
        }

        /// <summary>
        ///     Difficulty name is only visible on mapsets for playlists
        /// </summary>
        public bool IsPlaylistMapset() => DrawableContainer.DifficultyName.Visible;

        /// <summary>
        ///     Handles opening/closing the mapset when the map has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapChanged(object? sender, BindableValueChangedEventArgs<Map> e)
        {
            if (!Item.Maps.Contains(e.Value))
            {
                Deselect();
                // We DON'T want to automatically collapse just because a map changed,
                // ONLY if the mapset itself changed in MapsetScrollContainer, which handles its own logic.
                // MapsetScrollContainer already triggers IsExpanded = false logic via UpdateContent.
                return;
            }

            if (!IsSelected)
                Select();

            // Refresh expanded panels to update selection highlighting without recreating them
            if (IsExpanded)
            {
                UpdateDifficultySelection();
            }
        }

        private void OnMapsetInfoRetrieved(object? sender, EventArgs args) => UpdateContent(Item, Index);

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRetrievedOnlineScores(object? sender, RetrievedOnlineScoresEventArgs e) => UpdateContent(Item, Index);

        /// <summary>
        ///     Called when mods change - update difficulty stats in-place if expanded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object? sender, ModsChangedEventArgs e)
        {
            UpdateContent(Item, Index);

            // Update difficulty stats in-place AFTER UpdateContent (Rating, BPM, Length)
            // Use ScheduleUpdate to ensure it runs after UpdateContent's ScheduleUpdate
            if (IsExpanded)
            {
                ScheduleUpdate(() =>
                {
                    // Double-check still expanded after UpdateContent ran
                    if (IsExpanded && DifficultyStatsRefs.Count > 0)
                        UpdateDifficultyStats();
                });
            }
        }

        /// <summary>
        ///     Expands the mapset to show inline difficulties
        /// </summary>
        public void Expand()
        {
            if (IsExpanded || Item?.Maps == null)
                return;

            IsExpanded = true;

            // Create the container for difficulties if it doesn't exist
            DifficultyContainer ??= new ClippingContainer
            {
                Parent = this,
                Size = new ScalableVector2(WIDTH, 0),
                UsePreviousSpriteBatchOptions = true
            };

            // Positon the difficulty container directly after the base mapset panel.
            // Spacing (if any) is now handled internally by the difficulty list layout 
            // to match MapsetHelper's height calculations.
            DifficultyContainer.Y = BaseHeight;
            InitializeDifficultyList();
        }

        /// <summary>
        ///     Common logic to initialize difficulty list (shared between V1 and V2 for now as they share same column logic)
        /// </summary>
        private void InitializeDifficultyList()
        {
            _measurementCache.Clear();

            var font = FontManager.GetWobbleFont(Assets.Fonts.InterBold);
            font.FontSize = 20;

            // Clear any outdated construction tasks
            ConstructionTasks.Clear();

            // Instead of destroying, recycle existing items
            // OPTIMIZATION: Iterate backwards to avoid .ToList() allocation
            for (var i = DifficultyContainer.Children.Count - 1; i >= 0; i--)
            {
                var child = DifficultyContainer.Children[i];
                if (child is ImageButton btn)
                {
                    btn.Visible = false;
                    DifficultyContainer.Children.Remove(btn);
                    RecycledDifficultyPanels.Push(btn);
                }
                else
                {
                    child.Destroy();
                }
            }

            // Clear existing stat references before rebuilding
            DifficultyStatsRefs.Clear();

            DifficultyItems.Clear();

            // Reset widths
            _maxNpsWidth = 0;
            _maxLnWidth = 0;
            _maxBpmWidth = 0;
            _maxLengthWidth = 0;

            _measurementCache.Clear();

            // Pre-calculate TargetDifficultyHeight synchronously using single source of truth
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1;
            TargetDifficultyHeight = MapsetHelper.GetDifficultyListHeight(Item.Maps.Count, version);
            CurrentAnimatedHeight = 0;

            var rate = (float)Math.Max(0.5, Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods));
            
            // OPTIMIZATION: Cache count and first element to avoid repeated LINQ/property calls
            var mapsCount = Item.Maps.Count;
            if (mapsCount == 0) return;

            var firstMap = Item.Maps[0];
            var firstAudio = firstMap.AudioPath;
            var hasVariousAudio = false;

            for (var i = 1; i < mapsCount; i++)
            {
                if (Item.Maps[i].AudioPath != firstAudio)
                {
                    hasVariousAudio = true;
                    break;
                }
            }

            // STAGE 1: Measure all text widths synchronously so _maxNpsWidth etc. are
            // ready before STAGE 2 is enqueued. Previously these were separate ConstructionTasks
            // (one per map), which caused a bug on the first expand: _maxNpsWidth was still 0
            // when STAGE 2 executed because all tasks were dequeued one per frame.
            foreach (var map in Item.Maps)
            {
                var diff = map.DifficultyFromMods(ModManager.Mods);

                // NPS
                var npsVal = Math.Floor(map.NotesPerSecond * 100) / 100;
                var npsStr = npsVal.ToString("F2") + " NPS";
                var nWidth = MeasureString(font, npsStr, _measurementCache);
                if (nWidth > _maxNpsWidth) _maxNpsWidth = nWidth;

                // LN
                var lnStr = ((int)map.LNPercentage).ToString() + "%";
                var lWidth = MeasureString(font, lnStr, _measurementCache);
                if (lWidth > _maxLnWidth) _maxLnWidth = lWidth;

                // BPM and Length (only for VARIOUS mapsets)
                if (hasVariousAudio)
                {
                    var bpm = (int)(map.Bpm * rate);
                    var bpmStr = bpm.ToString() + " BPM";
                    var bpmWidth = MeasureString(font, bpmStr, _measurementCache);
                    if (bpmWidth > _maxBpmWidth) _maxBpmWidth = bpmWidth;

                    var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
                    var lengthStr = length.Hours > 0 ? length.ToString(@"hh\:mm\:ss") : length.ToString(@"mm\:ss");
                    var lengthWidth = MeasureString(font, lengthStr, _measurementCache);
                    if (lengthWidth > _maxLengthWidth) _maxLengthWidth = lengthWidth;
                }



            }

            // STAGE 2: Panel Construction task (happens after all measurements are done)
            ConstructionTasks.Enqueue(() =>
            {
                var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
                int panelWidth = DifficultyPanelDimensions.V1Width;
                var xAdjustment = isV2 ? (DifficultyPanelDimensions.V2Width - panelWidth) : 0;

                CalculateDifficultyStatsLayout(panelWidth, hasVariousAudio, _maxNpsWidth, _maxLnWidth, _maxBpmWidth, _maxLengthWidth,
                    out float npsGroupLeft, out float lnGroupLeft, out float bpmGroupLeft, out float lengthGroupLeft);

                var npsTexture = UserInterface.DifficultyPanelNPSIcon;
                var lnTexture = UserInterface.DifficultyPanelLNPercentIcon;
                var ratingTexture = UserInterface.DifficultyPanelRatingIcon;
                var clockTexture = UserInterface.MapsetLengthIcon;
                var bpmTexture = UserInterface.MapsetBpmIcon;

                var version = SkinManager.Skin?.UserInterfaceVersion ?? 1;
                var panelSource = SkinManager.Skin?.SongSelect?.DifficultyPanelSource?.ToLower();

                if (string.IsNullOrEmpty(panelSource))
                    panelSource = version >= 2f ? "difficulty" : "mapset";

                Texture2D selectedTexture, deselectedTexture, hoveredTexture;

                if (panelSource == "difficulty")
                {
                    selectedTexture = SkinManager.Skin?.SongSelect?.DifficultySelected ?? UserInterface.DifficultySelected;
                    deselectedTexture = SkinManager.Skin?.SongSelect?.DifficultyDeselected ?? UserInterface.DifficultyDeselected;
                    hoveredTexture = SkinManager.Skin?.SongSelect?.DifficultyHovered ?? UserInterface.DifficultyHovered;
                }
                else
                {
                    selectedTexture = SkinManager.Skin?.SongSelect?.MapsetSelected ?? UserInterface.SelectedMapset;
                    deselectedTexture = SkinManager.Skin?.SongSelect?.MapsetDeselected ?? UserInterface.DeselectedMapset;
                    hoveredTexture = SkinManager.Skin?.SongSelect?.MapsetHovered ?? UserInterface.MapsetHovered;
                }


                var yOffset = version == 2 ? MapsetHelper.V2MapsetSlotHeight - MapsetHelper.V2MapsetHeight : 0f;
                const int panelHeight = 60;
                const int panelSpacing = 11;

                foreach (var map in Item.Maps)
                {
                    var currentMap = map;
                    var difficulty = map.DifficultyFromMods(ModManager.Mods);
                    var diffColor = ColorHelper.DifficultyToColor((float)difficulty);
                    var isSelected = map == MapManager.Selected.Value;

                    var currentWidth = isV2
                        ? isSelected ? DifficultyPanelDimensions.V2SelectedWidth : DifficultyPanelDimensions.V2Width
                        : isSelected ? panelWidth + DifficultyPanelDimensions.V1Expansion : panelWidth;

                    // QUEUE PANEL CREATION TASK
                    var currentY = yOffset;
                    ConstructionTasks.Enqueue(() =>
                    {
                        ImageButton diffButton;
                        if (RecycledDifficultyPanels.Count > 0)
                        {
                            diffButton = RecycledDifficultyPanels.Pop();
                            diffButton.Visible = true;
                            diffButton.Image = isSelected ? selectedTexture : deselectedTexture;

                            // Clear stale event handlers before reuse to prevent hover flickering.
                            // Old handlers still reference destroyed child sprites (e.g. hoverOverlay).
                            diffButton.ClearAllEventHandlers();
                            
                            // OPTIMIZATION: Avoid .ToList() by iterating backwards
                            for (var j = diffButton.Children.Count - 1; j >= 0; j--)
                                diffButton.Children[j].Destroy();
                        }
                        else
                        {
                            diffButton = new ImageButton(isSelected ? selectedTexture : deselectedTexture);
                        }

                        diffButton.Parent = DifficultyContainer;
                        diffButton.Size = new ScalableVector2(currentWidth, panelHeight);
                        diffButton.Y = currentY;
                        diffButton.X = WIDTH - currentWidth;
                        diffButton.Alpha = 1f;
                        diffButton.UsePreviousSpriteBatchOptions = true;

                        CreateDifficultyPanelContent(diffButton, currentMap, difficulty, diffColor, isSelected,
                                                     npsGroupLeft, lnGroupLeft, bpmGroupLeft, lengthGroupLeft,
                                                     xAdjustment, hasVariousAudio, font, 20,
                                                     npsTexture, lnTexture, clockTexture, bpmTexture, ratingTexture, hoveredTexture, deselectedTexture, isV2);
                    });

                    yOffset += panelHeight + panelSpacing;
                }

                // Final Step: Start animation and set container height.
                ConstructionTasks.Enqueue(() =>
                {
                    var version = SkinManager.Skin?.UserInterfaceVersion ?? 1;
                    var targetHeight = MapsetHelper.GetDifficultyListHeight(Item.Maps.Count, (int)version);
                    
                    DifficultyContainer.Height = targetHeight;
                    DifficultyContainer.FullContentHeight = targetHeight;
                    DifficultyContainer.Visible = true;

                    TargetDifficultyHeight = targetHeight;
                    CurrentAnimatedHeight = 0;
                    DifficultyContainer.ClipHeight = 0;
                    IsAnimatingExpand = true;
                    IsAnimatingCollapse = false;
                });
            });
        }

        private void CreateDifficultyPanelContent(ImageButton diffButton, Map map, double difficulty, Color diffColor, bool isSelected,
                                                   float npsGroupLeft, float lnGroupLeft, float bpmGroupLeft, float lengthGroupLeft,
                                                   float xAdjustment, bool hasVariousAudio, WobbleFontStore font, int fontSize,
                                                   Texture2D npsTexture, Texture2D lnTexture, Texture2D clockTexture, Texture2D bpmTexture, Texture2D ratingTexture, Texture2D hoveredTexture, Texture2D deselectedTexture,
                                                   bool isV2)
        {

            diffButton.RightClicked += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game.CurrentScreen?.ActivateRightClickOptions(new DifficultyRightClickOptions(map, diffButton));
            };

            diffButton.Clicked += (sender, args) =>
            {
                if (MapManager.Selected.Value == map)
                {
                    var game = (QuaverGame)GameBase.Game;
                    (game.CurrentScreen as SelectionScreen)?.ExitToGameplay();
                }
                else
                    MapManager.Selected.Value = map;
            };

            var leftPadding = (float)(SkinManager.Skin?.SongSelect?.DifficultyPanelMarginLeft ?? 26);
            if (map.OnlineGrade != Grade.None && SkinManager.Skin?.Grades != null)
            {
                const int gradeHeight = 40;
                var gradeImage = SkinManager.Skin.Grades[map.OnlineGrade];
                var gradeAspectRatio = (float)gradeImage.Width / gradeImage.Height;
                var gradeSprite = new Sprite
                {
                    Parent = diffButton,
                    Image = gradeImage,
                    Alignment = Alignment.MidLeft,
                    X = leftPadding,
                    Size = new ScalableVector2(gradeHeight * gradeAspectRatio, gradeHeight),
                    UsePreviousSpriteBatchOptions = true
                };
                leftPadding += gradeSprite.Width + 10;
            }

            // Create Rating Panel Overlay (Pill style)
            var bgTexture = SkinManager.Skin?.InfoBackground ?? UserInterface.BlankBox;
            var bgColor = SkinManager.Skin.SongSelect.DifficultyOverlayColor;

            const int ratingMargin = 10;
            const int ratingIconTextGap = 10;

            var ratingIcon = new Sprite
            {
                Image = ratingTexture,
                UsePreviousSpriteBatchOptions = true
            };
            ratingIcon.Size = new ScalableVector2(18 * ((float)ratingTexture.Width / ratingTexture.Height), 18);

            var ratingText = new SpriteTextPlus(font, StringHelper.RatingToString(difficulty), fontSize)
            {
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            var ratingContentWidth = ratingIcon.Width + ratingIconTextGap + ratingText.Width;
            var ratingOverlayWidth = ratingContentWidth + (ratingMargin * 2);

            var ratingOverlay = new NineSliceSprite(bgTexture, new SliceMargins(15, 0))
            {
                Parent = diffButton,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(ratingOverlayWidth, 30),
                X = leftPadding,
                UsePreviousSpriteBatchOptions = true,
                Tint = bgColor
            };

            ratingIcon.Parent = ratingOverlay;
            ratingIcon.Alignment = Alignment.MidLeft;
            ratingIcon.X = ratingMargin;
            ratingIcon.Tint = diffColor;

            ratingText.Parent = ratingOverlay;
            ratingText.Alignment = Alignment.MidLeft;
            ratingText.X = ratingIcon.X + ratingIcon.Width + ratingIconTextGap;
            ratingText.Tint = diffColor;

            leftPadding += ratingOverlayWidth + 10;

            var shift = isSelected ? (isV2 ? DifficultyPanelDimensions.V2Expansion : DifficultyPanelDimensions.V1Expansion) : 0;
            var leftmostStatX = hasVariousAudio ? lengthGroupLeft : lnGroupLeft;
            var availableNameWidth = leftmostStatX + xAdjustment + shift - leftPadding - 10;
            var nameWidth = Math.Max(0, Math.Min(420, availableNameWidth));
            var nameText = new MarqueeSpriteText(font, StringHelper.GetFormatDifficultyName(map.DifficultyName, map.Mode, map.HasScratchKey, Item.HasMultipleKeymodes), 22, nameWidth)
            {
                Parent = diffButton,
                Alignment = Alignment.MidLeft,
                X = leftPadding,
                UsePreviousSpriteBatchOptions = true,
                IsActive = isSelected
            };
            nameText.TextSprite.Tint = diffColor;

            // Hover: swap button Image to hovered texture directly (no overlay)
            diffButton.Hovered += (sender, args) =>
            {
                if (MapManager.Selected.Value != map)
                    diffButton.Image = hoveredTexture;
                nameText.IsActive = true;
            };

            diffButton.LeftHover += (sender, args) =>
            {
                if (MapManager.Selected.Value != map)
                    diffButton.Image = deselectedTexture;
                if (MapManager.Selected.Value != map) nameText.IsActive = false;
            };

            // NPS, LN, BPM, Length, Rating
            const int iconTextSpacing = 10;

            // NPS
            var nps = Math.Floor(map.NotesPerSecond * 100) / 100;
            var npsIcon = new Sprite
            {
                Parent = diffButton,
                Image = npsTexture,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(18 * ((float)npsTexture.Width / npsTexture.Height), 18),
                X = npsGroupLeft + xAdjustment,
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.DifficultyInfoIconsColor
            };
            var npsText = new SpriteTextPlus(font, nps.ToString("F2") + " NPS", fontSize)
            {
                Parent = diffButton,
                Alignment = Alignment.MidLeft,
                X = npsIcon.X + npsIcon.Width + iconTextSpacing,
                Tint = SkinManager.Skin.SongSelect.DifficultyInfoValuesColor,
                UsePreviousSpriteBatchOptions = true
            };

            // LN%
            var lnPercent = ((int)map.LNPercentage).ToString() + "%";
            if (ModManager.Mods.HasFlag(ModIdentifier.NoLongNotes)) lnPercent = "0%";
            else if (ModManager.Mods.HasFlag(ModIdentifier.FullLN)) lnPercent = "100%";
            else if (ModManager.Mods.HasFlag(ModIdentifier.Inverse)) lnPercent = $"{100 - (int)map.LNPercentage}%";

            var lnIcon = new Sprite
            {
                Parent = diffButton,
                Image = lnTexture,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(18 * ((float)lnTexture.Width / lnTexture.Height), 18),
                X = lnGroupLeft + xAdjustment,
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.DifficultyInfoIconsColor
            };
            var lnText = new SpriteTextPlus(font, lnPercent, fontSize)
            {
                Parent = diffButton,
                Alignment = Alignment.MidLeft,
                X = lnIcon.X + lnIcon.Width + iconTextSpacing,
                Tint = SkinManager.Skin.SongSelect.DifficultyInfoValuesColor,
                UsePreviousSpriteBatchOptions = true
            };




            Sprite? bpmSprite = null;
            SpriteTextPlus? bpmTextSprite = null;

            if (hasVariousAudio)
            {
                var rate = (float)Math.Max(0.5, Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods));
                var bpmValue = (int)(map.Bpm * rate);
                bpmSprite = new Sprite
                {
                    Parent = diffButton,
                    Image = bpmTexture,
                    Alignment = Alignment.MidLeft,
                    Size = new ScalableVector2(18 * ((float)bpmTexture.Width / bpmTexture.Height), 18),
                    X = bpmGroupLeft + xAdjustment,
                    UsePreviousSpriteBatchOptions = true,
                    Tint = SkinManager.Skin.SongSelect.DifficultyInfoIconsColor
                };
                bpmTextSprite = new SpriteTextPlus(font, bpmValue.ToString() + " BPM", fontSize)
                {
                    Parent = diffButton,
                    Alignment = Alignment.MidLeft,
                    X = bpmSprite.X + bpmSprite.Width + iconTextSpacing,
                    Tint = SkinManager.Skin.SongSelect.DifficultyInfoValuesColor,
                    UsePreviousSpriteBatchOptions = true
                };

                var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
                var lengthSprite = new Sprite
                {
                    Parent = diffButton,
                    Image = clockTexture,
                    Alignment = Alignment.MidLeft,
                    Size = new ScalableVector2(18 * ((float)clockTexture.Width / clockTexture.Height), 18),
                    X = lengthGroupLeft + xAdjustment,
                    UsePreviousSpriteBatchOptions = true,
                    Tint = SkinManager.Skin.SongSelect.DifficultyInfoIconsColor
                };
                var lengthTextSprite = new SpriteTextPlus(font, FormatDuration(length), fontSize)
                {
                    Parent = diffButton,
                    Alignment = Alignment.MidLeft,
                    X = lengthSprite.X + lengthSprite.Width + iconTextSpacing,
                    Tint = SkinManager.Skin.SongSelect.DifficultyInfoValuesColor,
                    UsePreviousSpriteBatchOptions = true
                };

                DifficultyStatsRefs[map] = new DifficultyPanelRefs
                {
                    DifficultyNameText = nameText,
                    NpsIcon = npsIcon,
                    NpsIconOriginalX = npsGroupLeft + xAdjustment,
                    NpsText = npsText,
                    NpsTextOriginalX = npsIcon.X + npsIcon.Width + iconTextSpacing,
                    LnIcon = lnIcon,
                    LnIconOriginalX = lnGroupLeft + xAdjustment,
                    LnText = lnText,
                    LnTextOriginalX = lnIcon.X + lnIcon.Width + iconTextSpacing,
                    BpmIcon = bpmSprite,
                    BpmIconOriginalX = bpmGroupLeft + xAdjustment,
                    BpmText = bpmTextSprite,
                    BpmTextOriginalX = bpmSprite.X + bpmSprite.Width + iconTextSpacing,
                    LengthIcon = lengthSprite,
                    LengthIconOriginalX = lengthGroupLeft + xAdjustment,
                    LengthText = lengthTextSprite,
                    LengthTextOriginalX = lengthSprite.X + lengthSprite.Width + iconTextSpacing,
                    RatingOverlay = ratingOverlay,
                    RatingOverlayOriginalX = leftPadding - ratingOverlayWidth - 10,
                    RatingIcon = ratingIcon,
                    RatingText = ratingText
                };
            }
            else
            {
                DifficultyStatsRefs[map] = new DifficultyPanelRefs
                {
                    DifficultyNameText = nameText,
                    NpsIcon = npsIcon,
                    NpsIconOriginalX = npsGroupLeft + xAdjustment,
                    NpsText = npsText,
                    NpsTextOriginalX = npsIcon.X + npsIcon.Width + iconTextSpacing,
                    LnIcon = lnIcon,
                    LnIconOriginalX = lnGroupLeft + xAdjustment,
                    LnText = lnText,
                    LnTextOriginalX = lnIcon.X + lnIcon.Width + iconTextSpacing,
                    RatingOverlay = ratingOverlay,
                    RatingOverlayOriginalX = leftPadding - ratingOverlayWidth - 10,
                    RatingIcon = ratingIcon,
                    RatingText = ratingText
                };
            }

            // Apply initial shift if selected
            if (isSelected)
            {
                var refs = DifficultyStatsRefs[map];
                shift = isV2 ? DifficultyPanelDimensions.V2Expansion : DifficultyPanelDimensions.V1Expansion;

                refs.NpsIcon.X += shift;
                refs.NpsText.X += shift;
                refs.LnIcon.X += shift;
                refs.LnText.X += shift;

                if (refs.BpmIcon != null) refs.BpmIcon.X += shift;
                if (refs.BpmText != null) refs.BpmText.X += shift;
                if (refs.LengthIcon != null) refs.LengthIcon.X += shift;
                if (refs.LengthText != null) refs.LengthText.X += shift;
            }
        }

        /// <summary>
        ///     Updates the visual state of difficulty buttons (selection highlighting)
        /// </summary>
        private void UpdateDifficultySelection()
        {
            if (DifficultyContainer == null || Item?.Maps == null || Item.Maps.Count == 0 || DifficultyContainer.Children.Count == 0)
                return;

            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;
            var panelWidth = isV2 ? DifficultyPanelDimensions.V2Width : DifficultyPanelDimensions.V1Width;
            var expansionAmount = isV2 ? DifficultyPanelDimensions.V2Expansion : DifficultyPanelDimensions.V1Expansion;

            // Determine textures based on skin config ONCE before loop
            var version = SkinManager.Skin?.UserInterfaceVersion ?? 1;
            var panelSource = SkinManager.Skin?.SongSelect?.DifficultyPanelSource?.ToLower();

            if (string.IsNullOrEmpty(panelSource))
                panelSource = version >= 2f ? "difficulty" : "mapset";
            Texture2D selectedTexture, deselectedTexture, hoveredTexture;

            if (panelSource == "difficulty")
            {
                selectedTexture = SkinManager.Skin?.SongSelect?.DifficultySelected ?? UserInterface.DifficultySelected;
                deselectedTexture = SkinManager.Skin?.SongSelect?.DifficultyDeselected ?? UserInterface.DifficultyDeselected;
                hoveredTexture = SkinManager.Skin?.SongSelect?.DifficultyHovered ?? UserInterface.DifficultyHovered;
            }
            else
            {
                selectedTexture = SkinManager.Skin?.SongSelect?.MapsetSelected ?? UserInterface.SelectedMapset;
                deselectedTexture = SkinManager.Skin?.SongSelect?.MapsetDeselected ?? UserInterface.DeselectedMapset;
                hoveredTexture = SkinManager.Skin?.SongSelect?.MapsetHovered ?? UserInterface.MapsetHovered;
            }

            for (var i = 0; i < Item.Maps.Count; i++)
            {
                var map = Item.Maps[i];
                if (DifficultyContainer.Children.Count <= i) break;

                if (DifficultyContainer.Children[i] is ImageButton button)
                {
                    var isSelected = map == MapManager.Selected.Value;
                    UpdateSingleDifficultyButton(button, map, isSelected, panelWidth, expansionAmount, selectedTexture, deselectedTexture, hoveredTexture);
                }
            }
        }

        private void UpdateSingleDifficultyButton(ImageButton button, Map map, bool isSelected, int panelWidth, int expansionAmount,
                                                  Texture2D selectedTexture, Texture2D deselectedTexture, Texture2D hoveredTexture)
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;

            button.Image = isSelected ? selectedTexture : deselectedTexture;
            button.Alpha = 1f;

            var targetWidth = isSelected ? panelWidth + expansionAmount : panelWidth;
            var targetX = WIDTH - targetWidth;

            button.ChangeWidthTo(targetWidth, Easing.OutQuint, 200);
            button.MoveToX(targetX, Easing.OutQuint, 200);

            // If hovered and not selected, keep the hovered texture
            if (!isSelected && button.IsHovered)
                button.Image = hoveredTexture;

            // Animate Content Positions
            if (DifficultyStatsRefs.TryGetValue(map, out var refs))
            {
                var shift = isSelected ? (float)expansionAmount : 0;
                var easing = Easing.OutQuint;
                var duration = 200;

                refs.NpsIcon.MoveToX(refs.NpsIconOriginalX + shift, easing, duration);
                refs.NpsText.MoveToX(refs.NpsTextOriginalX + shift, easing, duration);
                refs.LnIcon.MoveToX(refs.LnIconOriginalX + shift, easing, duration);
                refs.LnText.MoveToX(refs.LnTextOriginalX + shift, easing, duration);

                refs.BpmIcon?.MoveToX(refs.BpmIconOriginalX + shift, easing, duration);
                refs.BpmText?.MoveToX(refs.BpmTextOriginalX + shift, easing, duration);
                refs.LengthIcon?.MoveToX(refs.LengthIconOriginalX + shift, easing, duration);
                refs.LengthText?.MoveToX(refs.LengthTextOriginalX + shift, easing, duration);

                refs.DifficultyNameText.IsActive = isSelected || button.IsHovered;
            }
        }

        /// <summary>
        ///     Updates difficulty stats in-place when mods change (Rating, NPS, BPM, Length) 
        ///     AND recalculates column positions to maintain alignment
        /// </summary>
        private void UpdateDifficultyStats()
        {
            _measurementCache.Clear();

            var font = FontManager.GetWobbleFont(Assets.Fonts.InterBold);
            font.FontSize = 20;

            if (!IsExpanded || Item?.Maps == null || DifficultyStatsRefs.Count == 0)
                return;

            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;
            var panelWidth = DifficultyPanelDimensions.V1Width;
            var expansionAmount = isV2 ? DifficultyPanelDimensions.V2Expansion : DifficultyPanelDimensions.V1Expansion;
            var xAdjustment = isV2 ? DifficultyPanelDimensions.V2Width - panelWidth : 0;

            UpdateDifficultyStatsCommon(panelWidth, xAdjustment, (float)expansionAmount);
        }

        private void CalculateDifficultyStatsLayout(float panelWidth, bool hasVariousAudio, float maxNpsWidth, float maxLnWidth, float maxBpmWidth, float maxLengthWidth,
            out float npsGroupLeft, out float lnGroupLeft, out float bpmGroupLeft, out float lengthGroupLeft)
        {
            var rightPadding = SkinManager.Skin?.SongSelect?.DifficultyPanelMarginRight ?? 26;
            const int itemGroupSpacing = 10;
            const int iconTextSpacing = 10;

            var npsTexture = UserInterface.DifficultyPanelNPSIcon;
            var lnTexture = UserInterface.DifficultyPanelLNPercentIcon;
            var ratingTexture = UserInterface.DifficultyPanelRatingIcon;
            var clockTexture = UserInterface.MapsetLengthIcon;
            var bpmTexture = UserInterface.MapsetBpmIcon;

            float npsIconWidth = 18 * ((float)npsTexture.Width / npsTexture.Height);
            float lnIconWidth = 18 * ((float)lnTexture.Width / lnTexture.Height);
            float bpmIconWidth = hasVariousAudio ? 18 * ((float)bpmTexture.Width / bpmTexture.Height) : 0;
            float lengthIconWidth = hasVariousAudio ? 18 * ((float)clockTexture.Width / clockTexture.Height) : 0;
            float ratingIconWidth = 18 * ((float)ratingTexture.Width / ratingTexture.Height);

            float npsColumnWidth = maxNpsWidth + iconTextSpacing + npsIconWidth + 2;
            float lnColumnWidth = maxLnWidth + iconTextSpacing + lnIconWidth + 2;
            float bpmColumnWidth = hasVariousAudio ? maxBpmWidth + iconTextSpacing + bpmIconWidth + 2 : 0;
            float lengthColumnWidth = hasVariousAudio ? maxLengthWidth + iconTextSpacing + lengthIconWidth + 2 : 0;


            npsGroupLeft = panelWidth - rightPadding - npsColumnWidth;
            lnGroupLeft = npsGroupLeft - itemGroupSpacing - lnColumnWidth;
            bpmGroupLeft = hasVariousAudio ? lnGroupLeft - itemGroupSpacing - bpmColumnWidth : 0;
            lengthGroupLeft = hasVariousAudio ? bpmGroupLeft - itemGroupSpacing - lengthColumnWidth : 0;

        }

        private void UpdateDifficultyStatsCommon(int panelWidth, int xAdjustment, float expansionAmount)
        {
            const int fontSize = 20;

            var font = FontManager.GetWobbleFont(Assets.Fonts.InterBold);
            font.FontSize = fontSize;

            var rate = (float)Math.Max(0.5, Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods));
            var firstAudio = Item.Maps.First().AudioPath;
            var hasVariousAudio = Item.Maps.Any(m => m.AudioPath != firstAudio);

            // Step 1: Measure new maximums
            float maxNpsWidth = 0;
            float maxLnWidth = 0;
            float maxBpmWidth = 0;
            float maxLengthWidth = 0;

            foreach (var map in Item.Maps)
            {
                var diff = map.DifficultyFromMods(ModManager.Mods);
                var nps = Math.Floor(map.NotesPerSecond * 100) / 100;
                var nWidth = MeasureString(font, nps.ToString("F2") + " NPS", _measurementCache);
                if (nWidth > maxNpsWidth) maxNpsWidth = nWidth;

                var lnStr = ((int)map.LNPercentage).ToString() + "%";
                if (ModManager.Mods.HasFlag(ModIdentifier.NoLongNotes)) lnStr = "0%";
                else if (ModManager.Mods.HasFlag(ModIdentifier.FullLN)) lnStr = "100%";
                else if (ModManager.Mods.HasFlag(ModIdentifier.Inverse)) lnStr = $"{100 - (int)map.LNPercentage}%";
                var lWidth = MeasureString(font, lnStr, _measurementCache);
                if (lWidth > maxLnWidth) maxLnWidth = lWidth;

                if (hasVariousAudio)
                {
                    var bpm = (int)(map.Bpm * rate);
                    var bWidth = MeasureString(font, bpm.ToString() + " BPM", _measurementCache);
                    if (bWidth > maxBpmWidth) maxBpmWidth = bWidth;

                    var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
                    var lengthStr = length.Hours > 0 ? length.ToString(@"hh\:mm\:ss") : length.ToString(@"mm\:ss");
                    var lenWidth = MeasureString(font, lengthStr, _measurementCache);
                    if (lenWidth > maxLengthWidth) maxLengthWidth = lenWidth;
                }


            }

            CalculateDifficultyStatsLayout(panelWidth, hasVariousAudio, maxNpsWidth, maxLnWidth, maxBpmWidth, maxLengthWidth,
                out float npsGroupLeft, out float lnGroupLeft, out float bpmGroupLeft, out float lengthGroupLeft);
            
            const int iconTextSpacing = 10;

            // Step 3: Update each panel
            foreach (var map in Item.Maps)
            {
                if (!DifficultyStatsRefs.TryGetValue(map, out var refs)) continue;

                var isSelected = map == MapManager.Selected.Value;
                var shift = isSelected ? expansionAmount : 0;
                var diff = map.DifficultyFromMods(ModManager.Mods);
                var diffColor = ColorHelper.DifficultyToColor((float)diff);

                // Update Rating
                refs.RatingText.Text = StringHelper.RatingToString(diff);
                refs.RatingText.Tint = diffColor;
                refs.RatingIcon.Tint = diffColor;
                refs.DifficultyNameText.TextSprite.Tint = diffColor;
 
                var ratingContentWidth = refs.RatingIcon.Width + 10 + refs.RatingText.Width;
                var ratingOverlayWidth = ratingContentWidth + 20;
                refs.RatingOverlay.Size = new ScalableVector2(ratingOverlayWidth, 30);
 
                var leftPadding = (float)(SkinManager.Skin?.SongSelect?.DifficultyPanelMarginLeft ?? 26);
                if (map.OnlineGrade != Grade.None && SkinManager.Skin?.Grades != null)
                {
                    var gradeImage = SkinManager.Skin.Grades[map.OnlineGrade];
                    leftPadding += (40 * ((float)gradeImage.Width / gradeImage.Height)) + 10;
                }
 
                refs.RatingOverlayOriginalX = leftPadding;
                refs.RatingOverlay.X = refs.RatingOverlayOriginalX;
                leftPadding += ratingOverlayWidth + 10;

                // Update NPS
                var nps = Math.Floor(map.NotesPerSecond * 100) / 100;
                refs.NpsText.Text = nps.ToString("F2") + " NPS";
                refs.NpsIconOriginalX = npsGroupLeft + xAdjustment;
                refs.NpsIcon.X = refs.NpsIconOriginalX + shift;
                refs.NpsTextOriginalX = refs.NpsIconOriginalX + refs.NpsIcon.Width + iconTextSpacing;
                refs.NpsText.X = refs.NpsTextOriginalX + shift;

                // Update LN%
                var lnStr = ((int)map.LNPercentage).ToString() + "%";
                if (ModManager.Mods.HasFlag(ModIdentifier.NoLongNotes)) lnStr = "0%";
                else if (ModManager.Mods.HasFlag(ModIdentifier.FullLN)) lnStr = "100%";
                else if (ModManager.Mods.HasFlag(ModIdentifier.Inverse)) lnStr = $"{100 - (int)map.LNPercentage}%";
                refs.LnText.Text = lnStr;
                refs.LnIconOriginalX = lnGroupLeft + xAdjustment;
                refs.LnIcon.X = refs.LnIconOriginalX + shift;
                refs.LnTextOriginalX = refs.LnIconOriginalX + refs.LnIcon.Width + iconTextSpacing;
                refs.LnText.X = refs.LnTextOriginalX + shift;

                // Update BPM / Length
                if (hasVariousAudio && refs.BpmIcon != null && refs.BpmText != null && refs.LengthIcon != null && refs.LengthText != null)
                {
                    var bpm = (int)(map.Bpm * rate);
                    refs.BpmText.Text = bpm.ToString() + " BPM";
                    refs.BpmIconOriginalX = bpmGroupLeft + xAdjustment;
                    refs.BpmIcon.X = refs.BpmIconOriginalX + shift;
                    refs.BpmTextOriginalX = refs.BpmIconOriginalX + refs.BpmIcon.Width + iconTextSpacing;
                    refs.BpmText.X = refs.BpmTextOriginalX + shift;

                    var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
                    refs.LengthText.Text = FormatDuration(length);
                    refs.LengthIconOriginalX = lengthGroupLeft + xAdjustment;
                    refs.LengthIcon.X = refs.LengthIconOriginalX + shift;
                    refs.LengthTextOriginalX = refs.LengthIconOriginalX + refs.LengthIcon.Width + iconTextSpacing;
                    refs.LengthText.X = refs.LengthTextOriginalX + shift;
                }

                var leftmostStatX = hasVariousAudio ? lengthGroupLeft : lnGroupLeft;
                var availableNameWidth = leftmostStatX + xAdjustment + shift - leftPadding - 10;
                refs.DifficultyNameText.Size = new ScalableVector2(Math.Max(0, Math.Min(420, availableNameWidth)), refs.DifficultyNameText.Size.Y.Value);
                refs.DifficultyNameText.X = leftPadding;
            }
        }

        public void Collapse()
        {
            if (!IsExpanded || IsAnimatingCollapse)
                return;

            IsExpanded = false;
            IsAnimatingCollapse = true;
            IsAnimatingExpand = false;
            TargetDifficultyHeight = 0;
        }

        private static string FormatDuration(TimeSpan ts)
            => ts.Hours > 0 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");

        public void ToggleExpanded()
        {
            if (IsExpanded)
                Collapse();
            else
                Expand();
        }
    }
}
