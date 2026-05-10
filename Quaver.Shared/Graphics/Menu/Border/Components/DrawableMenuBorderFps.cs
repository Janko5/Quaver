using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Form;
using Wobble.Managers;
using Wobble.Assets;

namespace Quaver.Shared.Graphics.Menu.Border.Components
{
    public class DrawableMenuBorderFps : Sprite, IMenuBorderItem
    {
        public bool UseCustomPaddingY { get; } = false;
        public int CustomPaddingY { get; } = 0;
        public bool UseCustomPaddingX { get; } = true;
        public int CustomPaddingX { get; set; }

        private SpriteTextPlus TextFpsCount { get; }
        private SpriteTextPlus TextFpsLabel { get; }
        private SpriteTextPlus TextUpsCount { get; }
        private SpriteTextPlus TextUpsLabel { get; }

        private int _lastFps = -1;
        private int _lastUps = -1;
        private int OriginalPaddingX { get; }

        public DrawableMenuBorderFps(int paddingX)
        {
            OriginalPaddingX = paddingX;
            SetChildrenAlpha = true;

            Image = SkinManager.Skin.MenuBorder.FpsBackground;
            Size = new ScalableVector2(90, 50);
            Tint = SkinManager.Skin.MenuBorder.SquareButtonNotActiveColor;

            var font = FontManager.GetWobbleFont(Fonts.InterBold);
            TextFpsLabel = new SpriteTextPlus(font, "FPS", 17, cache: false)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                X = -10,
                Y = 5,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentColor
            };

            TextFpsCount = new SpriteTextPlus(font, "0", 17, cache: false)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = 5,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentSecondColor
            };

            TextUpsLabel = new SpriteTextPlus(font, "UPS", 17, cache: false)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                X = -10,
                Y = 28,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentColor
            };

            TextUpsCount = new SpriteTextPlus(font, "0", 17, cache: false)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = 28,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentSecondColor
            };

            ConfigManager.FpsCounter.ValueChanged += OnFpsCounterChanged;

            // Apply initial state AFTER children are added to ensure SetChildrenAlpha triggers properly
            Alpha = ConfigManager.FpsCounter.Value ? 1f : 0f;
            Width = ConfigManager.FpsCounter.Value ? 90 : 0;
            CustomPaddingX = ConfigManager.FpsCounter.Value ? OriginalPaddingX : 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!ConfigManager.FpsCounter.Value)
                return;

            var gameFps = ((QuaverGame)GameBase.Game).Fps;
            if (gameFps != null)
            {
                if (_lastFps != gameFps.FrameRate)
                {
                    _lastFps = gameFps.FrameRate;
                    TextFpsCount.Text = $"{_lastFps}";
                }

                if (_lastUps != gameFps.UpdateRate)
                {
                    _lastUps = gameFps.UpdateRate;
                    TextUpsCount.Text = $"{_lastUps}";
                }

                TextFpsCount.X = TextFpsLabel.X - TextFpsLabel.Width - 4;
                TextUpsCount.X = TextUpsLabel.X - TextUpsLabel.Width - 4;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (ConfigManager.FpsCounter.Value)
            {
                // Force hide the global FPS counter since this panel replaces it for MenuBorder v2
                var globalFps = ((QuaverGame)GameBase.Game).Fps;
                if (globalFps != null && globalFps.Visible)
                {
                    globalFps.Visible = false;
                }
            }

            base.Draw(gameTime);
        }

        private void OnFpsCounterChanged(object? sender, BindableValueChangedEventArgs<bool> e)
        {
            Alpha = ConfigManager.FpsCounter.Value ? 1f : 0f;
            Width = ConfigManager.FpsCounter.Value ? 90 : 0;
            CustomPaddingX = ConfigManager.FpsCounter.Value ? OriginalPaddingX : 0;

            if (Parent is MenuBorder border)
                border.AlignRightItems();

            // Note: Re-enabling the global fps counter is handled by QuaverGame.ShowFpsCounter 
            // when the config value changes. If the config goes to false, it is hidden everywhere.
            // If it goes to true, QuaverGame shows it, but our Draw loop will immediately hide it 
            // again as long as this MenuBorder is active.
        }

        public override void Destroy()
        {
            ConfigManager.FpsCounter.ValueChanged -= OnFpsCounterChanged;

            // Restore global counter visibility if the config isn't turned off
            if (ConfigManager.FpsCounter.Value)
            {
                var globalFps = ((QuaverGame)GameBase.Game).Fps;
                if (globalFps != null)
                    globalFps.Visible = true;
            }

            base.Destroy();
        }
    }
}
