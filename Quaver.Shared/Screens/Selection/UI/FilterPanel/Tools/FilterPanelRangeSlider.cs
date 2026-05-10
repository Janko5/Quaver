using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Input;
using Wobble.Window;
using Wobble;
using Wobble.Graphics.Shaders;
using System.Collections.Generic;

using Quaver.Shared.Skinning;
using Quaver.Shared;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Tools
{
    public class FilterPanelRangeSlider : Sprite
    {
        /// <summary>
        ///     The minimum value of the slider
        /// </summary>
        public Bindable<float> MinValue { get; }

        /// <summary>
        ///     The maximum value of the slider
        /// </summary>
        public Bindable<float> MaxValue { get; }

        /// <summary>
        ///     The absolute minimum possible value
        /// </summary>
        public float MinRange { get; }

        /// <summary>
        ///     The absolute maximum possible value
        /// </summary>
        public float MaxRange { get; }

        /// <summary>
        ///     The background track of the slider
        /// </summary>
        private NineSliceSprite Track { get; }

        /// <summary>
        ///     The filled bar between the two thumbs
        /// </summary>
        private NineSliceSprite RangeBar { get; }

        /// <summary>
        ///     The minimum value thumb
        /// </summary>
        private Sprite ThumbMin { get; }

        /// <summary>
        ///     The maximum value thumb
        /// </summary>
        private Sprite ThumbMax { get; }

        /// <summary>
        ///     The colored bar used when UseDifficultyColorsInDifficultySlider is enabled
        /// </summary>
        private ClippedSprite DifficultyColorBar { get; }

        /// <summary>
        ///     The container for masking the difficulty color bar
        /// </summary>
        private NineSliceMaskContainer MaskedDifficultyBar { get; }

        /// <summary>
        ///     If the user is currently dragging the min thumb
        /// </summary>
        private bool IsDraggingMin { get; set; }

        /// <summary>
        ///     If the user is currently dragging the max thumb
        /// </summary>
        private bool IsDraggingMax { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        /// <param name="width"></param>
        public FilterPanelRangeSlider(Bindable<float> minValue, Bindable<float> maxValue, float minRange, float maxRange, float width)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            MinRange = minRange;
            MaxRange = maxRange;
            Width = width;
            Height = 30;

            // Ensure we propagate alpha to children and don't draw ourselves
            SetChildrenAlpha = true;
            Tint = Color.Transparent;

            // CRITICAL: Inherit SpriteBatch options from parent (ClippingContainer)
            // This ensures scissor rect clipping is preserved during collapse animation
            UsePreviousSpriteBatchOptions = true;

            // Track (Background)
            Track = new NineSliceSprite(UserInterface.SliderElement, new SliceMargins(6))
            {
                Parent = this,
                Size = new ScalableVector2(width, 30),
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.DifficultySliderNotSelectedBackgroundColor,
                UsePreviousSpriteBatchOptions = true
            };

            // Range Bar (Filled area)
            RangeBar = new NineSliceSprite(UserInterface.SliderElement, new SliceMargins(6))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.DifficultySliderSelectedBackgroundColor,
                Height = 30,
                UsePreviousSpriteBatchOptions = true
            };

            // Difficulty Color Bar (Alternative filled area)
            // It will be inside the mask container, so its parent is set there.
            DifficultyColorBar = new ClippedSprite
            {
                Alignment = Alignment.TopLeft,
                Pivot = Vector2.Zero,
                Height = 30,
                Visible = true // Always visible inside the mask
            };

            // Mask Container
            MaskedDifficultyBar = new NineSliceMaskContainer(UserInterface.SliderElement, new SliceMargins(6))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Height = 30,
                UsePreviousSpriteBatchOptions = true,
                Visible = false
            };

            // Add the gradient to the mask
            MaskedDifficultyBar.AddContainedSprite(DifficultyColorBar);

            // Generate texture if graphics device is available
            if (GameBase.Game?.GraphicsDevice != null)
            {
                DifficultyColorBar.Image = GenerateDifficultyTexture();
            }

            // Thumbs
            // User said "używają slider-element.png w ich oryginalnej rozdzielczości". 
            // I should check the resolution. Assuming it's suitable for a thumb.
            var thumbTexture = UserInterface.SliderElement;

            ThumbMin = new Sprite
            {
                Parent = this,
                Image = thumbTexture,
                Size = new ScalableVector2(thumbTexture.Width, thumbTexture.Height),
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.DifficultySliderThumbColor,
                UsePreviousSpriteBatchOptions = false
            };

            ThumbMax = new Sprite
            {
                Parent = this,
                Image = thumbTexture,
                Size = new ScalableVector2(thumbTexture.Width, thumbTexture.Height),
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.DifficultySliderThumbColor,
                UsePreviousSpriteBatchOptions = false
            };

            // Initial Positioning
            UpdateVisuals();

            // Bind Events
            MinValue.ValueChanged += (s, e) => UpdateVisuals();
            MaxValue.ValueChanged += (s, e) => UpdateVisuals();
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);

            HandleInput();
        }

        private void HandleInput()
        {
            var mouse = MouseManager.CurrentState;
            var relativeMouseX = mouse.X - ScreenRectangle.X;

            // Check for drag start
            if (MouseManager.IsUniquePress(MouseButton.Left))
            {
                if (ThumbMin.IsHovered())
                    IsDraggingMin = true;
                else if (ThumbMax.IsHovered())
                    IsDraggingMax = true;
                else if (Track.IsHovered())
                {
                    // Click jump behavior - move closest thumb
                    var distMin = Math.Abs(ThumbMin.ScreenRectangle.X + ThumbMin.Width / 2 - mouse.X);
                    var distMax = Math.Abs(ThumbMax.ScreenRectangle.X + ThumbMax.Width / 2 - mouse.X);

                    if (distMin < distMax)
                        IsDraggingMin = true;
                    else
                        IsDraggingMax = true;
                }
            }

            // Check for drag end
            if (mouse.LeftButton == ButtonState.Released)
            {
                IsDraggingMin = false;
                IsDraggingMax = false;
            }

            // Handle Dragging
            if (IsDraggingMin || IsDraggingMax)
            {
                // Calculate value from position
                // Position 0 = MinRange, Position Width = MaxRange
                // But we should account for Thumb Width to keep it inside? User didn't specify.
                // Usually sliders map center to values, or left edge.
                // Let's map pixels 0 to Width to MinRange to MaxRange.

                var percent = MathHelper.Clamp(relativeMouseX / Width, 0, 1);
                var value = MinRange + (MaxRange - MinRange) * percent;

                // Round to 2 decimal places for neatness? User said "00.00".
                value = (float)Math.Round(value, 2);

                if (IsDraggingMin)
                {
                    // Clamp to MaxValue if not infinite
                    if (MaxValue.Value <= MaxRange && value > MaxValue.Value)
                        value = MaxValue.Value;

                    // Clamp visual range (0 - 99.99)
                    if (value > MaxRange) value = MaxRange;

                    MinValue.Value = value;
                }
                else if (IsDraggingMax)
                {
                    // Check for infinity snap (top 1% of the slider or explicitly past the end)
                    if (percent >= 0.99f)
                    {
                        if (MaxValue.Value != float.MaxValue)
                            MaxValue.Value = float.MaxValue;
                    }
                    else
                    {
                        // Clamp to MinValue
                        if (value < MinValue.Value)
                            value = MinValue.Value;

                        // Ensure we aren't setting a value > MaxRange if we aren't at the infinity snap point
                        if (value > MaxRange)
                            value = MaxRange;

                        MaxValue.Value = value;
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            if (Width <= 0) return;

            var valRange = MaxRange - MinRange;
            if (valRange == 0) return;

            // Calculate percentages
            var minPercent = (MinValue.Value - MinRange) / valRange;
            var maxPercent = (MaxValue.Value > MaxRange) ? 1.0f : (MaxValue.Value - MinRange) / valRange;

            // Update Thumb Positions - Map percentage to available track space (Width - ThumbWidth)
            // This ensures the thumbs stay entirely within the track boundaries.
            ThumbMin.X = (Width - ThumbMin.Width) * minPercent;
            ThumbMax.X = (Width - ThumbMax.Width) * maxPercent;

            // Update Range Bar - Align to the centers of the thumbs
            var barX = ThumbMin.X + (ThumbMin.Width / 2f);
            var barWidth = (ThumbMax.X + (ThumbMax.Width / 2f)) - barX;

            if (SkinManager.Skin.UseDifficultyColorsInDifficultySlider && DifficultyColorBar.Image != null)
            {
                RangeBar.Visible = false;
                MaskedDifficultyBar.Visible = true;

                MaskedDifficultyBar.X = barX;
                MaskedDifficultyBar.Width = barWidth;

                // DifficultyColorBar is child of MaskedDifficultyBar
                DifficultyColorBar.X = 0;
                DifficultyColorBar.Width = barWidth;

                // Calculate source rectangle to show correct part of gradient
                var texWidth = DifficultyColorBar.Image.Width;
                var sourceX = (int)(minPercent * texWidth);
                var sourceWidth = (int)((maxPercent - minPercent) * texWidth);

                // Ensure valid source rect
                if (sourceWidth < 1) sourceWidth = 1;
                if (sourceX + sourceWidth > texWidth) sourceWidth = texWidth - sourceX;

                DifficultyColorBar.SourceRectangle = new Rectangle(sourceX, 0, sourceWidth, 1);
            }
            else
            {
                RangeBar.Visible = true;
                MaskedDifficultyBar.Visible = false;

                RangeBar.X = barX;
                RangeBar.Width = barWidth;
            }
        }

        /// <summary>
        ///     Generates a texture representing the difficulty colors across the slider's range.
        /// </summary>
        /// <returns></returns>
        private Texture2D GenerateDifficultyTexture()
        {
            var width = 4096;
            var texture = new Texture2D(GameBase.Game.GraphicsDevice, width, 1);
            var data = new Color[width];
            var diffRange = MaxRange - MinRange;

            for (var i = 0; i < width; i++)
            {
                var percent = (float)i / width;
                var difficulty = MinRange + diffRange * percent;
                data[i] = ColorHelper.DifficultyToColor(difficulty);
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        ///     A sprite that supports source rectangle clipping.
        /// </summary>
        private class ClippedSprite : Sprite
        {
            public Rectangle? SourceRectangle { get; set; }

            public override void DrawToSpriteBatch()
            {
                if (!Visible) return;
                GameBase.Game.SpriteBatch.Draw(Image, RenderRectangle, SourceRectangle, _color, SpriteOverallRotation, Origin, SpriteEffect, 0f);
            }
        }

        private class NineSliceMaskContainer : NineSliceSprite
        {
            public DepthStencilState MaskDepthStencilState { get; }
            public DepthStencilState ContainedDepthStencilState { get; }
            private Matrix Matrix { get; }
            public AlphaTestEffect MaskAlphaTestEffect { get; }

            public NineSliceMaskContainer(Texture2D image, SliceMargins margins) : base(image, margins)
            {
                MaskDepthStencilState = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,
                    StencilPass = StencilOperation.Replace,
                    ReferenceStencil = 1,
                    DepthBufferEnable = false,
                };

                ContainedDepthStencilState = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.LessEqual,
                    StencilPass = StencilOperation.Keep,
                    ReferenceStencil = 1,
                    DepthBufferEnable = false,
                };

                Matrix = Matrix.CreateOrthographicOffCenter(0, WindowManager.Width, WindowManager.Height, 0, 0, 1);

                MaskAlphaTestEffect = new AlphaTestEffect(GameBase.Game.GraphicsDevice)
                {
                    Projection = Matrix,
                };

                SpriteBatchOptions = new SpriteBatchOptions
                {
                    DepthStencilState = MaskDepthStencilState,
                    Shader = new Shader(MaskAlphaTestEffect, new Dictionary<string, object>())
                };
            }

            public override void Update(GameTime gameTime)
            {
                MaskAlphaTestEffect.Alpha = Alpha;
                base.Update(gameTime);
            }

            public void AddContainedSprite(Drawable drawable)
            {
                drawable.Parent = this;

                for (var i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];

                    if (i == 0)
                    {
                        child.SpriteBatchOptions = new SpriteBatchOptions
                        {
                            DepthStencilState = ContainedDepthStencilState,
                            Shader = new Shader(CreateAlphaTestEffect(drawable), new Dictionary<string, object>())
                        };
                    }
                    else
                    {
                        child.UsePreviousSpriteBatchOptions = true;
                    }
                }
            }

            private AlphaTestEffect CreateAlphaTestEffect(Drawable drawable)
            {
                var alpha = 0f;
                // Wobble Sprite has Alpha property
                if (drawable is Sprite sprite) alpha = sprite.Alpha;

                return new AlphaTestEffect(GameBase.Game.GraphicsDevice)
                {
                    Projection = Matrix,
                    Alpha = alpha,
                };
            }
        }
    }
}
