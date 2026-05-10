using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Form;
using Wobble.Managers;
using Wobble;
using Wobble.Window;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Graphics.Overlays.Volume
{
    public class VolumeControlSlider : Container
    {
        /// <summary>
        /// </summary>
        public readonly BindableInt BindedValue;

        /// <summary>
        /// </summary>
        public Slider Slider { get; private set; } = null!;

        private NineSliceSprite SliderBackground { get; set; } = null!;
        private NineSliceSprite SliderActiveBackground { get; set; } = null!;
        private VolumeThumbContainer ThumbContainer { get; set; } = null!;


        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="value"></param>
        public VolumeControlSlider(float width, BindableInt value)
        {
            BindedValue = value;

            CreateSlider(width);

            Size = new ScalableVector2(width, 26);

            BindedValue.ValueChanged += OnValueChanged;
            BindedValue.TriggerChangeEvent();

            SkinManager.SkinLoaded += OnSkinLoaded;
        }

        private void OnSkinLoaded(object? sender, SkinReloadedEventArgs e)
        {
            SliderBackground.Tint = SkinManager.Skin.VolumeController.VolumeSliderBackgroundColor;
            SliderActiveBackground.Tint = SkinManager.Skin.VolumeController.VolumeSliderActiveColor;
            
            // Update selection state
            Deselect();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            SkinManager.SkinLoaded -= OnSkinLoaded;
            // ReSharper disable once DelegateSubtraction
            BindedValue.ValueChanged -= OnValueChanged;
            base.Destroy();
        }

        /// <summary>
        /// </summary>
        public void Select()
        {
            SliderActiveBackground.Tint = SkinManager.Skin.VolumeController.VolumeHoverColor;
        }

        /// <summary>
        /// </summary>
        public void Deselect()
        {
            SliderActiveBackground.Tint = SkinManager.Skin.VolumeController.VolumeSliderActiveColor;
        }

        /// <summary>
        /// </summary>
        private void CreateSlider(float width)
        {
            var ballTexture = UserInterface.BlankBox;

            // Base Background
            SliderBackground = new NineSliceSprite(UserInterface.VolumeSliderElement, new SliceMargins(6, 6, 0, 0))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 0,
                Height = 26,
                Width = width,
                Tint = SkinManager.Skin.VolumeController.VolumeSliderBackgroundColor
            };

            ThumbContainer = new VolumeThumbContainer
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 0,
                Height = 26,
                Width = 0,
            };

            // Active Background
            SliderActiveBackground = new NineSliceSprite(UserInterface.VolumeSliderElement, new SliceMargins(6, 6, 0, 0))
            {
                Parent = ThumbContainer,
                Alignment = Alignment.MidLeft,
                X = 0,
                Height = 26,
                Width = 26,
                Tint = SkinManager.Skin.VolumeController.VolumeSliderActiveColor,
                UsePreviousSpriteBatchOptions = true,
            };

            // Transparent Native Slider on top to capture input
            Slider = new Slider(BindedValue, new Vector2(width, 26), ballTexture)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 0,
                Image = UserInterface.BlankBox,
                Tint = Color.Transparent,
                IsSystemLayer = true
            };

            Slider.ActiveColor.Image = UserInterface.BlankBox;
            Slider.ActiveColor.Tint = Color.Transparent;
            Slider.ProgressBall.Tint = Color.Transparent; // Explicitly ensure the ball is transparent in case BlankBox draws white
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnValueChanged(object? sender, BindableValueChangedEventArgs<int> e)
        {
            if (SliderActiveBackground == null || ThumbContainer == null || BindedValue.MaxValue <= 0)
                return;

            float percentage = MathHelper.Clamp((float)e.Value / BindedValue.MaxValue, 0f, 1f);
            float naturalWidth = SliderBackground.Width * percentage;

            ThumbContainer.Visible = naturalWidth > 0;

            if (ThumbContainer.Visible)
            {
                ThumbContainer.Width = naturalWidth;
                SliderActiveBackground.Width = Math.Max(26f, naturalWidth);
            }
        }

        /// <summary>
        ///     Container used to crop the thumb when it's too small.
        /// </summary>
        private class VolumeThumbContainer : Sprite
        {
            /// <summary>
            /// </summary>
            public VolumeThumbContainer()
            {
                Image = UserInterface.BlankBox;
                Tint = Color.Transparent;

                SpriteBatchOptions = new SpriteBatchOptions
                {
                    SortMode = SpriteSortMode.Deferred,
                    BlendState = BlendState.NonPremultiplied,
                    RasterizerState = new RasterizerState
                    {
                        ScissorTestEnable = true,
                        CullMode = CullMode.None,
                    },
                };
            }

            /// <inheritdoc />
            public override void Draw(GameTime gameTime)
            {
                if (!Visible)
                    return;

                var currentRect = GameBase.Game.GraphicsDevice.ScissorRectangle;

                var widthScale = (float)GameBase.Game.Graphics.PreferredBackBufferWidth / WindowManager.Width;
                var heightScale = (float)GameBase.Game.Graphics.PreferredBackBufferHeight / WindowManager.Height;

                var rect = new Rectangle()
                {
                    X = (int)(ScreenRectangle.X * widthScale),
                    Y = (int)(ScreenRectangle.Y * heightScale),
                    Width = (int)(ScreenRectangle.Width * widthScale),
                    Height = (int)(ScreenRectangle.Height * heightScale),
                };

                // GraphicsDevice.ScissorRectangle must be within the backbuffer
                var viewport = GameBase.Game.GraphicsDevice.Viewport;
                rect.X = MathHelper.Clamp(rect.X, 0, viewport.Width);
                rect.Y = MathHelper.Clamp(rect.Y, 0, viewport.Height);
                rect.Width = MathHelper.Clamp(rect.Width, 0, viewport.Width - rect.X);
                rect.Height = MathHelper.Clamp(rect.Height, 0, viewport.Height - rect.Y);

                GameBase.Game.GraphicsDevice.ScissorRectangle = rect;

                base.Draw(gameTime);

                // EXPLICIT BATCH FLUSH: Ensures the scissor rectangle is applied to the deferred batch
                // before we restore the previous scissor state.
                GameBase.Game.TryEndBatch();

                GameBase.Game.GraphicsDevice.ScissorRectangle = currentRect;
            }
        }
    }
}
