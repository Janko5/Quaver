using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;

namespace Quaver.Shared.Graphics.Menu.Border.Components
{
    public class IconTextButton : ImageButton
    {
        /// <summary>
        ///     The sprite that displays the icon
        /// </summary>
        public Sprite Icon { get; }

        /// <summary>
        ///     The displayed text
        /// </summary>
        public SpriteTextPlus Text { get; }

        /// <summary>
        ///     The spacing between the icon and text
        /// </summary>
        public int Spacing { get; set; } = SkinManager.Skin?.UserInterfaceVersion >= 2f ? 10 : 6;

        /// <summary>
        ///     Function that determines if the button should be colored as active.
        /// </summary>
        public Func<bool>? IsActiveFunc { get; set; }

        /// <summary>
        ///     The color when the button isn't hovered
        /// </summary>
        protected Color BaseColor { get; set; }

        /// <summary>
        ///     The color when the button is hovered
        /// </summary>
        protected Color HoveredColor { get; }

        /// <summary>
        ///     If the text tint should be set to the icon's
        /// </summary>
        public bool SetTextTint { get; set; } = true;

        /// <summary>
        ///     If the text should be forced to uppercase
        /// </summary>
        public bool UppercaseText { get; set; } = true;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="onClick"></param>
        /// <param name="baseColor"></param>
        /// <param name="hoveredColor"></param>
        /// <param name="textSize"></param>
        public IconTextButton(Texture2D icon, WobbleFontStore font, string text, EventHandler onClick = null, Color? baseColor = null, Color? hoveredColor = null, int textSize = 20)
            : base(WobbleAssets.WhiteBox, onClick)
        {
            BaseColor = SkinManager.Skin.MenuBorder.ButtonTextColor;
            HoveredColor = SkinManager.Skin.MenuBorder.ButtonTextHoveredColor;

            Icon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Image = icon,
                Size = new ScalableVector2(16, 16),
                Tint = BaseColor
            };

            Text = new SpriteTextPlus(font, UppercaseText ? text.ToUpper() : text, textSize)
            {
                Parent = Icon,
                Alignment = Alignment.MidLeft,
                X = Icon.X + Icon.Width + Spacing,
                Tint = BaseColor
            };

            UpdateSize();
            Alpha = 0;

            Hovered += (s, e) =>
            {
                if (IsActiveFunc?.Invoke() ?? false)
                    return;

                SkinManager.Skin?.SoundHover.CreateChannel().Play();
            };
            Clicked += OnClicked;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            var isActive = IsActiveFunc?.Invoke() ?? false;
            Icon.FadeToColor(IsHovered && !isActive ? HoveredColor : BaseColor, GameBase.Game.TimeSinceLastFrame, 30);

            if (SetTextTint)
                Text.Tint = Icon.Tint;

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        public void UpdateText(string text)
        {
            Text.Text = UppercaseText ? text.ToUpper() : text;
            UpdateSize();
        }

        /// <summary>
        /// </summary>
        public void UpdateSize() => Size = new ScalableVector2(Icon.Width + Text.Width + Spacing, Parent?.Height ?? MenuBorder.HEIGHT);

        public override void DrawToSpriteBatch()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnClicked(object sender, EventArgs e) => SkinManager.Skin?.SoundClick.CreateChannel().Play();
    }
}
