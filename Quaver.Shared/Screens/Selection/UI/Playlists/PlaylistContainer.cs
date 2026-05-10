using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites.Text;
using Wobble.Input;
using Wobble;
using Wobble.Managers;
using Wobble.Window;
using Quaver.Shared.Skinning;
using Quaver.Shared;

namespace Quaver.Shared.Screens.Selection.UI.Playlists
{
    public class PlaylistContainer : SongSelectContainer<Playlist>
    {
        /// <summary>
        /// </summary>
        public override SelectScrollContainerType Type { get; } = SelectScrollContainerType.Playlists;

        /// <summary>
        /// </summary>
        public Bindable<SelectScrollContainerType> ActiveScrollContainer { get; }

        /// <summary>
        ///     The amount of time that has elapsed since the user requested to initialize the mapsets
        /// </summary>
        private double TimeElapsedUntilInitializationRequest { get; set; } = ReinitializeTime;

        /// <summary>
        ///     The time it takes until the mapsets will reinitialize
        /// </summary>
        private const int ReinitializeTime = 250;

        /// <summary>
        ///     If the mapsets have reinitialized
        /// </summary>
        private bool HasReinitialized { get; set; }

        /// <summary>
        ///     Shows if there are no playlists
        /// </summary>
        private SpriteTextPlus NoPlaylistText { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="activeScrollContainer"></param>
        public PlaylistContainer(Bindable<SelectScrollContainerType> activeScrollContainer) : base(PlaylistManager.Playlists, 12)
        {
            ActiveScrollContainer = activeScrollContainer;
            NoPlaylistText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "No playlists created!", 30)
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Tint = Color.White,
                Visible = PlaylistManager.Playlists.Count == 0
            };

            AutoScaleHeight = false;

            // Define the scissor rasterizer state for clipping
            ScissorRasterizer = new RasterizerState
            {
                ScissorTestEnable = true,
                CullMode = CullMode.None
            };
        }

        /// <summary>
        ///    The rasterizer state used for clipping the container.
        /// </summary>
        private RasterizerState ScissorRasterizer { get; }

        /// <inheritdoc />
        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            var game = GameBase.Game;
            if (game?.GraphicsDevice == null)
                return;

            // Calculate the screen rectangle for scissoring
            var screenRect = ScreenRectangle;

            // Create a scissor rectangle based on the Height
            var scissorRect = new Rectangle(
                (int)(screenRect.X * WindowManager.ScreenScale.X),
                (int)(screenRect.Y * WindowManager.ScreenScale.Y),
                (int)(screenRect.Width * WindowManager.ScreenScale.X),
                (int)(screenRect.Height * WindowManager.ScreenScale.Y)
            );

            // Clamp to screen bounds
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

            try { spriteBatch.End(); } catch { }

            game.GraphicsDevice.ScissorRectangle = oldScissorRect;

            // Restore with a clean rasterizer without scissor test,
            // to avoid leaking scissor state to subsequent sibling draws
            // (oldRasterizerState could have ScissorTestEnable=true from a previous ScrollContainer).
            GameBase.DefaultSpriteBatchOptions.Begin();
            GameBase.DefaultSpriteBatchInUse = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            TimeElapsedUntilInitializationRequest += gameTime.ElapsedGameTime.TotalMilliseconds;
            InitializePlaylists(false);

            // Close Right Click Options if scrolling
            var deltaY = CurrentY - PreviousY;
            if (Math.Abs(deltaY) > 1)
            {
                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.ActiveRightClickOptions?.Close();
            }

            base.Update(gameTime);
        }

        /// <summary>
        ///     Returns the height of a playlist slot including spacing/padding.
        ///     V2: 100px height + 10px spacing = 110px.
        ///     V1: 97px height (legacy).
        /// </summary>
        private int GetPlaylistSlotHeight() => SkinManager.Skin?.UserInterfaceVersion == 2 ? 110 : DrawableMapset.MapsetHeight;

        /// <inheritdoc />
        public override void RecalculateContainerHeight(bool usePoolCount = false)
        {
            if (AvailableItems == null)
            {
                base.RecalculateContainerHeight(usePoolCount);
                return;
            }

            var count = usePoolCount ? Pool.Count : AvailableItems.Count;
            var totalHeight = GetPlaylistSlotHeight() * count + PaddingTop + PaddingBottom;

            // V2 optimization: Ensure the content ends exactly at the last playlist panel's edge.
            if (SkinManager.Skin?.UserInterfaceVersion == 2 && count > 0)
            {
                totalHeight = 110 * (count - 1) + 100 + PaddingTop;
            }

            if (totalHeight > Height)
                ContentContainer.Height = totalHeight;
            else
                ContentContainer.Height = Height;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override float GetSelectedPosition() => (-SelectedIndex.Value + 4) * GetPlaylistSlotHeight();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override PoolableSprite<Playlist> CreateObject(Playlist item, int index) => new DrawablePlaylist(this, item, index);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void HandleInput(GameTime gameTime)
        {
            if (ActiveScrollContainer.Value != SelectScrollContainerType.Playlists)
                return;

            // Move to the next mapset
            if (KeyboardManager.IsUniqueKeyPress(Keys.Right) || KeyboardManager.IsUniqueKeyPress(Keys.Down))
            {
                if (SelectedIndex.Value + 1 >= AvailableItems.Count)
                    return;

                PlaylistManager.Selected.Value = AvailableItems[SelectedIndex.Value + 1];
                SelectedIndex.Value++;

                ScrollToSelected();
            }
            // Move to the previous mapset
            else if (KeyboardManager.IsUniqueKeyPress(Keys.Left) || KeyboardManager.IsUniqueKeyPress(Keys.Up))
            {
                if (SelectedIndex.Value - 1 < 0)
                    return;

                PlaylistManager.Selected.Value = AvailableItems[SelectedIndex.Value - 1];

                SelectedIndex.Value--;
                ScrollToSelected();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        protected override void SetSelectedIndex()
        {
            SelectedIndex.Value = AvailableItems.FindIndex(x => x == PlaylistManager.Selected.Value);

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
        /// </summary>
        /// <returns></returns>
        public void InitializePlaylists(bool restart)
        {
            if (restart)
            {
                TimeElapsedUntilInitializationRequest = 0;
                HasReinitialized = false;
                return;
            }

            if (TimeElapsedUntilInitializationRequest < ReinitializeTime || HasReinitialized)
                return;

            lock (Pool)
            {
                DestroyAndClearPool();

                AvailableItems = PlaylistManager.Playlists;

                SetSelectedIndex();

                // Reset the starting index so we can be aware of the mapsets that are needed
                PoolStartingIndex = DesiredPoolStartingIndex(SelectedIndex.Value);

                // Recreate the object pool
                CreatePool();
                PositionAndContainPoolObjects();

                // Snap to it immediately
                SnapToSelected();

                NoPlaylistText.Visible = AvailableItems.Count == 0;
            }

            HasReinitialized = true;
            FireInitializedEvent();
        }
    }
}