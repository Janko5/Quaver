using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Skinning;
using Wobble.Graphics.Animations;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    public class FilterPanelSquareButton : SquareButton
    {
        /// <summary>
        ///     Current state of the button
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// </summary>
        public FilterPanelSquareButton() : base(UserInterface.FilterPanelExpandIcon, null)
        {
            ActiveColor = SkinManager.Skin.ButtonNotActiveColor;
            InactiveColor = SkinManager.Skin.ButtonNotActiveColor;
            HoverColor = SkinManager.Skin.ButtonHoverColor;
            ContentColor = SkinManager.Skin.ButtonContentColor;

            AllowHoverWhenActive = true;

            IsActiveFunc = () => Active;

            Icon.Rotation = 0; // Default rotation

            Clicked += OnClicked;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked(object? sender, EventArgs e)
        {
            Active = !Active;

            // Rotate icon based on state
            Icon.ClearAnimations();
            Icon.Animations.Add(new Animation(AnimationProperty.Rotation, Easing.OutQuint, Icon.Rotation, Active ? MathHelper.Pi : 0, 200));
        }
    }
}
