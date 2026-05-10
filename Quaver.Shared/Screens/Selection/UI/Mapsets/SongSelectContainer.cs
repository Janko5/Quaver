using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Window;
using Quaver.Shared.Graphics.Form.Dropdowns.RightClick;
using Quaver.Shared.Skinning;


namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public abstract class SongSelectContainer<T> : PoolableScrollContainer<T>
    {
        /// <summary>
        /// </summary>
        public abstract SelectScrollContainerType Type { get; }

        /// <summary>
        ///     The index of the selected available item
        /// </summary>
        public BindableInt SelectedIndex { get; set; }

        /// <summary>
        ///     The height of the scroll container
        /// </summary>
        public static int HEIGHT { get; } = 880;

        /// <summary>
        /// </summary>
        protected Drawable ScrollbarBackground { get; set; }

        /// <summary>
        ///     Custom scrollbar thumb using 9-slice sprite
        /// </summary>
        protected NineSliceSprite CustomScrollbarThumb { get; set; }

        private bool _isCustomDragging;
        private float _customDragOffset;
        private float _lastScrollbarHeight = -1;
        private float _lastContainerHeight = -1;
        private float _lastContentHeight = -1;
        private float _lastContentY = float.NaN;
        private float _lastComputedThumbHeight = -1;

        /// <summary>
        ///     Event invoked when the mapset container has had its maps initialized
        /// </summary>
        public event EventHandler<SelectContainerInitializedEventArgs> ContainerInitialized;

        /// <summary>
        ///     The area that is clickable for buttons within the container
        /// </summary>
        public Container ClickableArea { get; }

        /// <summary>
        /// </summary>
        /// <param name="availableItems"></param>
        /// <param name="poolSize"></param>
        public SongSelectContainer(List<T> availableItems, int poolSize) : base(availableItems, poolSize, 0,
            new ScalableVector2(DrawableMapset.WIDTH, HEIGHT), new ScalableVector2(DrawableMapset.WIDTH, 0))
        {
            AutoScaleHeight = true;

            if (PoolSize != int.MaxValue)
                PoolSize = (int)(poolSize * WindowManager.BaseToVirtualRatio);

            PaddingBottom = 10;

            InputEnabled = true;
            EasingType = Easing.OutQuint;
            TimeToCompleteScroll = 1200;
            ScrollSpeed = 320;

            Alpha = 0;
            CreateScrollbar();

            AllowScrollbarDragging = true;

            ClickableArea = new Container()
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Width = Width,
                Height = HEIGHT,
                AutoScaleHeight = true
            };

            SelectedIndex = new BindableInt(-1, 0, int.MaxValue);

            // ReSharper disable once VirtualMemberCallInConstructor
            SetSelectedIndex();

            PoolStartingIndex = DesiredPoolStartingIndex(SelectedIndex.Value);
            CreatePool();
            PositionAndContainPoolObjects();
            SnapToSelected();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            InputEnabled = MouseManager.CurrentState.Position.X >= ScreenRectangle.X
                           && ScreenRectangle.Y <= MouseManager.CurrentState.Position.Y
                           && MouseManager.CurrentState.Position.Y <= ScreenRectangle.Bottom;

            if (DialogManager.Dialogs.Count == 0
                && !KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt)
                && !KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt))
            {
                HandleInput(gameTime);
            }

            base.Update(gameTime);

            // Custom Scrollbar dragging logic
            if (CustomScrollbarThumb != null && ScrollbarBackground != null)
            {
                var heightChanged = Math.Abs(_lastContainerHeight - Height) > 0.001f;

                if (heightChanged)
                {
                    ScrollbarBackground.Height = Height;
                    _lastContainerHeight = Height;
                }

                if (ContentContainer.Height > 0 && (Math.Abs(_lastContentHeight - ContentContainer.Height) > 0.001f || heightChanged))
                {
                    var thumbHeight = MathHelper.Clamp((Height / ContentContainer.Height) * Height, 30, Height);
                    if (Math.Abs(_lastComputedThumbHeight - thumbHeight) > 0.001f)
                    {
                        CustomScrollbarThumb.Height = thumbHeight;
                        _lastComputedThumbHeight = thumbHeight;
                    }

                    _lastContentHeight = ContentContainer.Height;
                }

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

                // Calculate Y position within the background container
                // We use ContentContainer.Y for smoothness, but check TargetY for final snapping
                var maxScrollYPos = ContentContainer.Height - Height;
                var currentPercent = maxScrollYPos > 0 ? MathHelper.Clamp(Math.Abs(ContentContainer.Y) / maxScrollYPos, 0, 1) : 0;
                
                var thumbScrollableRange = ScrollbarBackground.Height - CustomScrollbarThumb.Height;

                var targetThumbY = (float)Math.Round(currentPercent * thumbScrollableRange);
                if (float.IsNaN(_lastContentY) || Math.Abs(_lastContentY - ContentContainer.Y) > 0.001f ||
                    Math.Abs(_lastScrollbarHeight - ScrollbarBackground.Height) > 0.001f)
                {
                    CustomScrollbarThumb.Y = targetThumbY;
                    _lastContentY = ContentContainer.Y;
                    _lastScrollbarHeight = ScrollbarBackground.Height;
                }
            }

            // Sync ClickableArea dimensions with container (fixes click detection after resolution change)
            if (ClickableArea != null && Math.Abs(ClickableArea.Height - Height) > 1f)
                ClickableArea.Height = Height;
        }

        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            ContainerInitialized = null;
            SelectedIndex?.Dispose();
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
            // X = 35 = 10px gap from list + 15px scrollbar + 10px from screen edge when active
            ScrollbarBackground = new NineSliceSprite(UserInterface.UniversalScrollBackground, margins)
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = 25,  // 10px gap from list + 15px scrollbar width = right edge at 10px from screen
                Size = new ScalableVector2(15, Height),  // Same height as mapset list
                Tint = bgColor
            };

            // Scrollbar thumb: using 9-slice for rounded corners (same 15px width)
            CustomScrollbarThumb = new NineSliceSprite(UserInterface.UniversalScrollBackground, margins)
            {
                Parent = ScrollbarBackground,
                Alignment = Alignment.TopCenter,
                Width = 15,  // Same width as background
                Tint = thumbColor
            };
        }


        /// <summary>
        ///     Updates the scrollbar height to match the container height.
        ///     Call this after changing the container Height property.
        /// </summary>
        public void UpdateScrollbarHeight()
        {
            if (ScrollbarBackground != null)
                ScrollbarBackground.Height = Height;
        }

        /// <summary>
        ///     Scrolls the container to the selected position
        /// </summary>
        public virtual void ScrollToSelected(int time = 1800)
        {
            // Scroll the the place where the map is.
            var targetScroll = GetSelectedPosition();
            ScrollTo(targetScroll, time);
        }

        /// <summary>
        ///     Snaps the scroll container to the initial mapset.
        /// </summary>
        public virtual void SnapToSelected()
        {
            ContentContainer.Y = SelectedIndex.Value < 7 ? 0 : GetSelectedPosition();

            ContentContainer.Animations.Clear();
            
            // Force pool shift in the next frame by making Y and PreviousY differ.
            // Using a large negative value ensures a different state even for Y=0.
            PreviousContentContainerY = -999999f; 
            
            TargetY = ContentContainer.Y;
            PreviousTargetY = ContentContainer.Y;
            HandlePoolShifting();
        }

        /// <summary>
        ///     Destroys all of the objects in the pool and clears the list
        /// </summary>
        protected void DestroyAndClearPool()
        {
            lock (Pool)
            {
                Pool.ForEach(x => x.Destroy());
                Pool.Clear();
            }
        }

        /// <summary>
        ///     Makes sure all of the objects in the pool are positioned properly
        ///     and contained in the container
        /// </summary>
        protected void PositionAndContainPoolObjects()
        {
            for (var i = 0; i < Pool.Count; i++)
            {
                Pool[i].Y = (PoolStartingIndex + i) * Pool[i].HEIGHT + PaddingTop;
                AddContainedDrawable(Pool[i]);
            }
        }

        /// <summary>
        ///     Fires the <see cref="ContainerInitialized"/> event
        /// </summary>
        protected void FireInitializedEvent() => ContainerInitialized?.Invoke(this, new SelectContainerInitializedEventArgs());

        /// <summary>
        ///     Gets the position of the selected item
        /// </summary>
        /// <returns></returns>
        protected abstract float GetSelectedPosition();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected abstract override PoolableSprite<T> CreateObject(T item, int index);

        /// <summary>
        ///     Handles input for the scroll container
        /// </summary>
        /// <param name="gameTime"></param>
        protected abstract void HandleInput(GameTime gameTime);

        /// <summary>
        ///     Sets the appropriate index of the selected mapset
        /// </summary>
        protected abstract void SetSelectedIndex();
    }
}
