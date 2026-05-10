using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Skinning;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;

namespace Quaver.Shared.Helpers
{
    public static class GameModeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapModes"></param>
        /// <param name="sprite">
        /// Reference to the sprite that will be set to the game mode texture.
        /// </param>
        /// <param name="text">
        /// Reference to the text that will be set to the game mode text. Will be "" if skin has a custom game mode texture.
        /// </param>
        public static void SetGameModeTexture(IEnumerable<GameMode> mapModes, Sprite sprite, SpriteTextPlus text)
        {
            var modes = mapModes.Distinct().OrderBy(m => ModeHelper.ToKeyCount(m)).ToList();

            if (modes.Count == 0)
            {
                sprite.Image = SkinManager.Skin?.SongSelect?.GameModeNone ?? UserInterface.GameModeNonePanel;
                text.Text = "";
                return;
            }

            // 1. Single Mode Specific Icons
            if (modes.Count == 1)
            {
                Texture2D texture = null;
                var skin = SkinManager.Skin?.SongSelect;

                switch (modes[0])
                {
                    case GameMode.Keys1: texture = skin?.GameMode1K ?? UserInterface.Keys1Panel; break;
                    case GameMode.Keys2: texture = skin?.GameMode2K ?? UserInterface.Keys2Panel; break;
                    case GameMode.Keys3: texture = skin?.GameMode3K ?? UserInterface.Keys3Panel; break;
                    case GameMode.Keys4: texture = skin?.GameMode4K ?? UserInterface.Keys4Panel; break;
                    case GameMode.Keys5: texture = skin?.GameMode5K ?? UserInterface.Keys5Panel; break;
                    case GameMode.Keys6: texture = skin?.GameMode6K ?? UserInterface.Keys6Panel; break;
                    case GameMode.Keys7: texture = skin?.GameMode7K ?? UserInterface.Keys7Panel; break;
                    case GameMode.Keys8: texture = skin?.GameMode8K ?? UserInterface.Keys8Panel; break;
                    case GameMode.Keys9: texture = skin?.GameMode9K ?? UserInterface.Keys9Panel; break;
                    case GameMode.Keys10: texture = skin?.GameMode10K ?? UserInterface.Keys10Panel; break;
                }

                if (texture != null)
                {
                    sprite.Image = texture;
                    // Reset tint in case it was modified previously
                    sprite.Tint = Color.White;
                    text.Text = "";
                    text.Tint = Color.White;
                    return;
                }
            }

            // 3. Multi-Mode Gradient (or Fallback)
            if (modes.Count > 1)
            {
                // Only use gradient for 3 or fewer modes. >3 is VARIOUS (flat color)
                if (modes.Count <= 3)
                {
                    var gradient = GetGradientTexture(modes);
                    if (gradient != null)
                    {
                        sprite.Image = gradient;
                        sprite.Tint = Color.White;
                        text.Tint = SkinManager.Skin.SongSelect.GameModeTextColor;
                    }
                    else
                    {
                        // Generic Fallback
                        sprite.Image = UserInterface.ModePanel;
                        sprite.Tint = Color.Gray;
                        text.Tint = Color.White;
                    }
                }
                else
                {
                    // VARIOUS (> 3 modes) - Use mixed mode icon
                    var texture = SkinManager.Skin?.SongSelect?.GameModeMixed ?? UserInterface.GameModeMixedPanel;

                    if (texture != null)
                    {
                        sprite.Image = texture;
                        sprite.Tint = Color.White;
                    }
                    else
                    {
                        sprite.Image = UserInterface.ModePanel;
                        sprite.Tint = new Color(165, 164, 164);
                    }

                    text.Tint = Color.White;
                }
            }
            else
            {
                // 4. Single Mode Fallback (Tinted Panel)
                Color color = Color.Gray;

                if (modes.Count == 1)
                {
                    // Redundant switch if we duplicate logic, but useful if we want to support tinting for modes without icons
                    color = GetGameModeColor(modes[0]);
                }

                sprite.Image = UserInterface.ModePanel;
                sprite.Tint = color;
                text.Tint = Color.White;
            }

            // 5. Text Generation
            string modesText = "";

            if (modes.Count <= 3)
            {
                for (int i = 0; i < modes.Count; i++)
                {
                    if (i != 0) modesText += "/";

                    modesText += modes.Count <= 3 ? ModeHelper.ToShortHand(modes[i]) : ModeHelper.ToKeyCount(modes[i]);
                }
            }

            text.Text = modesText;
        }

        public static Color GetGameModeColor(GameMode mode)
        {
            var skin = SkinManager.Skin?.SongSelect;
            switch (mode)
            {
                case GameMode.Keys1: return skin?.GameMode1KColor ?? new Color(0, 210, 200);
                case GameMode.Keys2: return skin?.GameMode2KColor ?? new Color(51, 189, 232);
                case GameMode.Keys3: return skin?.GameMode3KColor ?? new Color(0, 176, 255);
                case GameMode.Keys4: return skin?.GameMode4KColor ?? new Color(5, 135, 229);
                case GameMode.Keys5: return skin?.GameMode5KColor ?? new Color(57, 85, 227);
                case GameMode.Keys6: return skin?.GameMode6KColor ?? new Color(106, 79, 224);
                case GameMode.Keys7: return skin?.GameMode7KColor ?? new Color(155, 81, 224);
                case GameMode.Keys8: return skin?.GameMode8KColor ?? new Color(185, 106, 232);
                case GameMode.Keys9: return skin?.GameMode9KColor ?? new Color(208, 132, 238);
                case GameMode.Keys10: return skin?.GameMode10KColor ?? new Color(232, 154, 244);
                default: return Color.Gray;
            }
        }

        private static Dictionary<string, Texture2D> GradientCache { get; } = new Dictionary<string, Texture2D>();

        private static Texture2D GetGradientTexture(List<GameMode> modes)
        {
            var key = string.Join("-", modes.OrderBy(x => x));

            if (GradientCache.ContainsKey(key))
                return GradientCache[key];

            var mask = SkinManager.Skin?.SongSelect?.GameModeMask ?? UserInterface.GameModeMask;

            if (mask == null)
                return null;

            var colors = new List<Color>();

            foreach (var mode in modes)
            {
                colors.Add(GetGameModeColor(mode));
            }

            var width = mask.Width;
            var height = mask.Height;
            var data = new Color[width * height];
            mask.GetData(data);

            var newData = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var t = (float)x / (width - 1);
                    Color finalColor;

                    if (colors.Count == 1)
                    {
                        finalColor = colors[0];
                    }
                    else
                    {
                        var segmentCount = colors.Count - 1;
                        var segment = (int)(t * segmentCount);
                        segment = System.Math.Min(segment, segmentCount - 1);
                        var segmentT = (t * segmentCount) - segment;

                        finalColor = Color.Lerp(colors[segment], colors[segment + 1], segmentT);
                    }

                    var maskPixel = data[y * width + x];
                    newData[y * width + x] = new Color(finalColor.R, finalColor.G, finalColor.B, maskPixel.A);
                }
            }

            var texture = new Texture2D(Wobble.GameBase.Game.GraphicsDevice, width, height);
            texture.SetData(newData);

            GradientCache[key] = texture;
            return texture;
        }
    }
}
