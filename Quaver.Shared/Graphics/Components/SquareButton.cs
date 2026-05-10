using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Components
{
    public class SquareButton : ImageButton, IMenuBorderItem
    {
        /// <inheritdoc />
        public bool UseCustomPaddingY { get; set; } = false;

        /// <inheritdoc />
        public int CustomPaddingY { get; set; } = 0;

        /// <inheritdoc />
        public bool UseCustomPaddingX { get; set; } = true;

        /// <inheritdoc />
        public int CustomPaddingX { get; set; }

        /// <summary>
        ///     The background NineSliceSprite
        /// </summary>
        public NineSliceSprite Background { get; }

        /// <summary>
        ///     The icon inside the button
        /// </summary>
        public Sprite Icon { get; }

        /// <summary>
        ///     The hover overlay NineSliceSprite
        /// </summary>
        public NineSliceSprite HoverOverlay { get; }

        /// <summary>
        ///     The text displayed when active
        /// </summary>
        public SpriteTextPlus Text { get; }

        /// <summary>
        ///     Function that determines if the button should be colored as active.
        /// </summary>
        public Func<bool>? IsActiveFunc { get; set; }

        /// <summary>
        ///     If true, allows hover effects even when the button is active.
        /// </summary>
        public bool AllowHoverWhenActive { get; set; } = false;

        /// <summary>
        ///     Color when button is active
        /// </summary>
        public Color ActiveColor { get; set; }

        /// <summary>
        ///     Color when button is inactive
        /// </summary>
        public Color InactiveColor { get; set; }

        /// <summary>
        ///     Color of the hover overlay
        /// </summary>
        public Color HoverColor { get; set; }

        /// <summary>
        ///     Color of the icon and text
        /// </summary>
        public Color ContentColor { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="iconImage">The button icon</param>
        /// <param name="onClick">Click handler</param>
        /// <param name="text">Text to display when active</param>
        /// <param name="isActiveFunc">Function to determine active state</param>
        public SquareButton(Texture2D? iconImage, EventHandler? onClick, string text = "", Func<bool>? isActiveFunc = null)
            : base(SkinManager.Skin.SquareButton ?? UserInterface.SquareButton)
        {
            IsActiveFunc = isActiveFunc;

            // Wobble's Sprite/Button renders a WhiteBox if Image is null.
            // We set Alpha to 0 to hide it, but keep children visible.
            Alpha = 0;
            SetChildrenAlpha = false;

            Size = new ScalableVector2(40, 40);

            Background = new NineSliceSprite
            {
                Parent = this,
                Size = new ScalableVector2(40, 40),
                Image = SkinManager.Skin.SquareButton ?? UserInterface.SquareButton,
                Margins = new SliceMargins(19, 19, 0, 0),
            };

            HoverOverlay = new NineSliceSprite
            {
                Parent = this,
                Size = new ScalableVector2(40, 40),
                Image = SkinManager.Skin.SquareButton ?? UserInterface.SquareButton,
                Margins = new SliceMargins(19, 19, 0, 0),
                Alpha = 0
            };

            var iconContainer = new Container
            {
                Parent = this,
                Size = new ScalableVector2(40, 40),
                Alignment = Alignment.MidLeft
            };

            Icon = new Sprite
            {
                Parent = iconContainer,
                Image = iconImage,
                Size = new ScalableVector2(iconImage?.Width ?? 0, iconImage?.Height ?? 0),
                Alignment = Alignment.MidCenter,
            };

            Text = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), text, 22)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 40,
                Visible = false
            };

            Hovered += OnHovered;
            LeftHover += OnHoverLeft;

            if (onClick != null)
                Clicked += onClick;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            var isActive = IsActiveFunc?.Invoke() ?? false;

            Background.Tint = isActive ? ActiveColor : InactiveColor;
            HoverOverlay.Tint = HoverColor;
            Icon.Tint = ContentColor;
            Text.Tint = ContentColor;

            if (isActive && !string.IsNullOrEmpty(Text.Text))
            {
                Text.Visible = true;

                // Round up to avoid subpixel artifacts (white pixels on edges)
                var targetWidth = (int)Math.Ceiling(40 + Text.Width + 10);
                Size = new ScalableVector2(targetWidth, 40);

                Icon.Alignment = Alignment.MidLeft;
                Icon.X = 10;
                Text.X = 40;
            }
            else
            {
                Text.Visible = false;
                Size = new ScalableVector2(40, 40);

                Icon.Alignment = Alignment.MidCenter;
                Icon.X = 0;
            }

            Background.Size = Size;
            HoverOverlay.Size = Size;

            if (IsHovered && (!isActive || AllowHoverWhenActive))
            {
                if (HoverOverlay.Alpha < 1f && HoverOverlay.Animations.Count == 0)
                    HoverOverlay.FadeTo(1f, Easing.OutQuint, 100);
            }
            else
            {
                if (HoverOverlay.Alpha > 0f && HoverOverlay.Animations.Count == 0)
                    HoverOverlay.FadeTo(0f, Easing.OutQuint, 100);
            }

            // Force hover overlay to stay hidden if active and not allowed
            if (isActive && !AllowHoverWhenActive)
            {
                HoverOverlay.ClearAnimations();
                HoverOverlay.Alpha = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnHovered(object? sender, EventArgs e)
        {
            if (IsActiveFunc?.Invoke() ?? false && !AllowHoverWhenActive)
                return;

            HoverOverlay.ClearAnimations();
            HoverOverlay.FadeTo(1f, Easing.OutQuint, 100);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnHoverLeft(object? sender, EventArgs e)
        {
            HoverOverlay.ClearAnimations();
            HoverOverlay.FadeTo(0, Easing.OutQuint, 100);
        }
    }
}
