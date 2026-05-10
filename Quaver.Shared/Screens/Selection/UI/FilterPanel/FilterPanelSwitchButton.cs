using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    /// <summary>
    ///     A switch button for the filter panel (Mapsets/Playlists toggle)
    /// </summary>
    public class FilterPanelSwitchButton : ImageButton
    {
        /// <summary>
        ///     Whether this button is currently active
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        ///     The text label on the button
        /// </summary>
        private SpriteTextPlus Label { get; set; }

        /// <summary>
        ///     The hover overlay sprite
        /// </summary>
        private Sprite HoverSprite { get; set; }

        /// <summary>
        ///     Color when button is active
        /// </summary>
        private static Color ActiveColor => SkinManager.Skin.ButtonActiveColor;

        /// <summary>
        ///     Color when button is inactive
        /// </summary>
        private static Color InactiveColor => SkinManager.Skin.ButtonNotActiveColor;

        /// <summary>
        /// </summary>
        /// <param name="texture">The button texture (left or right)</param>
        /// <param name="text">The label text</param>
        /// <param name="isActive">Initial active state</param>
        public FilterPanelSwitchButton(Texture2D texture, string text, bool isActive)
            : base(texture, (sender, args) => { })
        {
            IsActive = isActive;

            // Use texture's native size
            Size = new ScalableVector2(texture.Width, texture.Height);

            // Create hover sprite
            HoverSprite = new Sprite
            {
                Parent = this,
                Size = Size,
                Image = texture,
                Tint = SkinManager.Skin.ButtonHoverColor,
                Alpha = 0
            };

            // Create label
            Label = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), text, 22)
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Tint = SkinManager.Skin.ButtonContentColor
            };

            Hovered += OnHovered;
            LeftHover += OnHoverLeft;

            UpdateTint();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHovered(object? sender, EventArgs e)
        {
            if (IsActive)
                return;

            HoverSprite.FadeTo(1f, Easing.OutQuint, 100);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHoverLeft(object? sender, EventArgs e)
        {
            HoverSprite.FadeTo(0f, Easing.OutQuint, 100);
        }

        /// <summary>
        ///     Sets the active state of this button
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            UpdateTint();

            // If active, hide hover immediately
            if (IsActive)
                HoverSprite.Alpha = 0;
            // If became inactive and still hovered, show hover? 
            // Simplified: just hide when state changes, let mouse movement handle re-hover if needed.
        }

        /// <summary>
        ///     Updates the tint based on current state
        /// </summary>
        private void UpdateTint()
        {
            Tint = IsActive ? ActiveColor : InactiveColor;
        }
    }
}
