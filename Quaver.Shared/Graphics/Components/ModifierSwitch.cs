using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Buttons;
using Color = Microsoft.Xna.Framework.Color;

namespace Quaver.Shared.Graphics.Components
{
    public class ModifierSwitch : Button
    {
        /// <summary>
        ///    Whether the switch is currently on.
        /// </summary>
        public bool IsOn { get; private set; }

        /// <summary>
        ///    The background sprite of the switch.
        /// </summary>
        private Sprite Background { get; }

        /// <summary>
        ///    The thumb sprite of the switch.
        /// </summary>
        private Sprite Thumb { get; }

        /// <summary>
        ///    Action to perform when the switch is toggled.
        /// </summary>
        private Action<bool> OnToggle { get; }

        /// <summary>
        /// </summary>
        /// <param name="isOn"></param>
        /// <param name="onToggle"></param>
        public ModifierSwitch(bool isOn, Action<bool> onToggle)
        {
            IsOn = isOn;
            OnToggle = onToggle;

            Size = new ScalableVector2(70, 24);
            Tint = Color.Transparent;

            Background = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(70, 24),
                Image = GetSwitchGradientTexture()
            };

            Thumb = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(38, 18),
                X = IsOn ? 29 : 3,
                Image = IsOn ? SkinManager.Skin.UniversalSwitchOn : SkinManager.Skin.UniversalSwitchOff,
                Tint = IsOn ? SkinManager.Skin.SwitchOnColor : SkinManager.Skin.SwitchOffColor
            };

            Clicked += (sender, args) => Toggle();
        }

        /// <summary>
        ///    Toggles the switch.
        /// </summary>
        public void Toggle()
        {
            IsOn = !IsOn;
            OnToggle?.Invoke(IsOn);

            UpdateVisuals();
        }

        /// <summary>
        ///    Updates the visuals of the switch based on its current state.
        /// </summary>
        /// <param name="animate"></param>
        public void UpdateVisuals(bool animate = true)
        {
            Background.Image = GetSwitchGradientTexture();
            Thumb.Image = IsOn ? SkinManager.Skin.UniversalSwitchOn : SkinManager.Skin.UniversalSwitchOff;
            Thumb.Tint = IsOn ? SkinManager.Skin.SwitchOnColor : SkinManager.Skin.SwitchOffColor;

            var targetX = IsOn ? 29 : 3;

            if (animate)
            {
                Thumb.Animations.Clear();
                Thumb.Animations.Add(new Animation(AnimationProperty.X, Easing.OutQuint, Thumb.X, targetX, 200));
            }
            else
            {
                Thumb.X = targetX;
            }
        }

        /// <summary>
        ///    Generates the gradient texture for the switch background.
        /// </summary>
        /// <returns></returns>
        private Texture2D GetSwitchGradientTexture()
        {
            var mask = UserInterface.UniversalSwitchBackground;
            var width = mask.Width;
            var height = mask.Height;

            using (var image = new SixLabors.ImageSharp.Image<Rgba32>(width, height))
            {
                var mainColor = SkinManager.Skin.SwitchMainBackgroundColor;
                var stateColor = IsOn ? SkinManager.Skin.SwitchOnBackgroundColor : SkinManager.Skin.SwitchOffBackgroundColor;

                for (var x = 0; x < width; x++)
                {
                    var t = (float)x / Math.Max(width - 1, 1);
                    var color = IsOn ? Microsoft.Xna.Framework.Color.Lerp(mainColor, stateColor, t) : Microsoft.Xna.Framework.Color.Lerp(stateColor, mainColor, t);

                    for (var y = 0; y < height; y++)
                    {
                        image[x, y] = new Rgba32(color.R, color.G, color.B, color.A);
                    }
                }

                // Apply mask
                var maskBytes = GameBase.Game.Resources.Get(@"Quaver.Resources/Textures/UI/Universal/universal-switch-background.png");
                if (maskBytes != null)
                {
                    using (var maskImage = SixLabors.ImageSharp.Image.Load<Rgba32>(maskBytes))
                    {
                        for (var x = 0; x < width; x++)
                        {
                            for (var y = 0; y < height; y++)
                            {
                                var alpha = maskImage[x, y].A / 255f;
                                var current = image[x, y];
                                image[x, y] = new Rgba32(current.R, current.G, current.B, (byte)(current.A * alpha));
                            }
                        }
                    }
                }

                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);
                    ms.Position = 0;
                    return AssetLoader.LoadTexture2D(ms);
                }
            }
        }
    }
}
