using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Tools
{
    public class FilterPanelDifficultyControl : Sprite
    {
        public Bindable<float> MinDifficulty { get; }
        public Bindable<float> MaxDifficulty { get; }

        private NineSliceSprite LeftPanel { get; }
        private SpriteTextPlus LeftText { get; }

        private NineSliceSprite RightPanel { get; }
        private SpriteTextPlus RightText { get; }

        private FilterPanelRangeSlider Slider { get; }

        public FilterPanelDifficultyControl(Bindable<float> minDiff, Bindable<float> maxDiff)
        {
            MinDifficulty = minDiff;
            MaxDifficulty = maxDiff;

            // Dimensions
            const float textPanelWidth = 74;
            const float sliderWidth = 374;
            const float gap = 10;
            const float height = 40; // Assuming 40 to match search box

            Size = new ScalableVector2(542, height);

            // Ensure we propagate alpha to children and don't draw ourselves
            SetChildrenAlpha = true;
            SetChildrenVisibility = true;
            Tint = Color.Transparent;

            // CRITICAL: Inherit SpriteBatch options from parent (ClippingContainer)
            // This ensures scissor rect clipping is preserved during collapse animation
            UsePreviousSpriteBatchOptions = true;

            // Left Text Panel
            LeftPanel = new NineSliceSprite(UserInterface.SearchBoxMask, new SliceMargins(6)) // Uses proper text-box texture
            {
                Parent = this,
                Size = new ScalableVector2(textPanelWidth, height),
                Alignment = Alignment.MidLeft,
                X = 0,
                Tint = SkinManager.Skin.DifficultySliderValuesBackgroundColor,
                UsePreviousSpriteBatchOptions = true
            };

            LeftText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00.00", 20, false)
            {
                Parent = LeftPanel,
                Alignment = Alignment.MidCenter,
                Tint = ColorHelper.HexToColor("#D9E3F4"),
                UsePreviousSpriteBatchOptions = true
            };

            // Slider
            Slider = new FilterPanelRangeSlider(MinDifficulty, MaxDifficulty, 0f, 60.00f, sliderWidth)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = LeftPanel.Width + gap,
                Height = height, // Slider container height
                UsePreviousSpriteBatchOptions = true
            };

            // Right Text Panel
            RightPanel = new NineSliceSprite(UserInterface.SearchBoxMask, new SliceMargins(6))
            {
                Parent = this,
                Size = new ScalableVector2(textPanelWidth, height),
                Alignment = Alignment.MidLeft,
                X = Slider.X + Slider.Width + gap,
                Tint = SkinManager.Skin.DifficultySliderValuesBackgroundColor,
                UsePreviousSpriteBatchOptions = true
            };

            RightText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "60.00", 20, false)
            {
                Parent = RightPanel,
                Alignment = Alignment.MidCenter,
                Tint = ColorHelper.HexToColor("#D9E3F4"),
                UsePreviousSpriteBatchOptions = true
            };

            // Bind updates
            MinDifficulty.ValueChanged += (s, e) => UpdateText();
            MaxDifficulty.ValueChanged += (s, e) => UpdateText();
            UpdateText();
        }

        private void UpdateText()
        {
            LeftText.Text = MinDifficulty.Value.ToString("00.00");
            RightText.Text = MaxDifficulty.Value > 60.00f ? "∞" : MaxDifficulty.Value.ToString("00.00");

            LeftText.Alpha = 1f;
            RightText.Alpha = 1f;

            LeftText.Tint = ColorHelper.DifficultyToColor(MinDifficulty.Value);
            // If infinity, use a high value for color (e.g. 100) or just the value if safe. 
            // DifficultyToColor typically clamps or handles high values.
            // Let's pass the raw value for MaxDifficulty unless it's infinity, then use something that gives the max color (purple/black).
            // Actually, we can just pass the value. If it's float.MaxValue, ColorHelper might need a check, 
            // but usually it's based on ranges. Let's use 100f for infinity to be safe and consistent with the "end" of the scale.
            RightText.Tint = ColorHelper.DifficultyToColor(MaxDifficulty.Value > 60.00f ? 100f : MaxDifficulty.Value);
        }

    }
}
