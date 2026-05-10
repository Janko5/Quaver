using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Wobble.Graphics;
using Wobble.Graphics.UI.Buttons;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Graphics.Form.Dropdowns.RightClick
{
    public class RightClickOptions : Dropdown
    {
        /// <summary>
        /// </summary>
        protected new Dictionary<string, Color> Options { get; }

        /// <summary>
        ///     Static flag indicating a submenu is currently capturing scroll input.
        ///     Used to prevent background containers from scrolling.
        /// </summary>
        public static bool IsSubmenuScrollActive { get; set; }


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="size"></param>
        /// <param name="fontSize"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        public Drawable? Anchor { get; set; }

        private Vector2 _anchorOffset;
        private bool _initializedAnchor;

        public RightClickOptions(Dictionary<string, Color> options, ScalableVector2 size, int fontSize,
            int maxWidth = 0, int maxHeight = 0)
            : base(options.Keys.ToList(), size, fontSize, SkinManager.Skin.DropdownRightClickOptionsColor, 0, maxWidth, maxHeight)
        {
            Options = options;

            // Fix: Apply the skin color to the item background as well, since the base constructor only applies it to HoverColor
            ItemBackgroundColor = SkinManager.Skin.DropdownRightClickOptionsColor;
            ItemHoverColor = SkinManager.Skin.DropdownHoverColor;
            HighlightAlpha = 1f;

            Chevron.Visible = false;
            SelectedText.Visible = false;
            DividerLine.Visible = false;
            HoverSprite.Visible = false;
            Alpha = 0;
            IsClickable = false;
            DestroyIfParentIsNull = false;
            ItemContainer.Y = 0;

            // Remove the button entirely to prevent prevent depth collisions since the original dropdown opener isn't needed
            ButtonManager.Remove(this);

            ShowCheckOnSelection = false;

            var i = 0;

            foreach (var option in options)
            {
                if (i == 0)
                    Items[i].Image = SkinManager.Skin?.DropdownOpen ?? UserInterface.DropdownOpen;

                Items[i].Text.Tint = option.Value;
                i++;
            }

            Open();

            SelectedIndex = -1;
            ItemSelected += (sender, args) => SelectedIndex = -1;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Anchor != null)
            {
                if (!_initializedAnchor)
                {
                    // Calculate offset in screen space
                    var currentPos = new Vector2(Position.X.Value, Position.Y.Value);
                    _anchorOffset = currentPos - Anchor.AbsolutePosition;
                    _initializedAnchor = true;
                }

                var targetPos = Anchor.AbsolutePosition + _anchorOffset;
                Position = new ScalableVector2(targetPos.X, targetPos.Y);
            }
        }
    }
}