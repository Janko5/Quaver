using System;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Skinning;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    public class FilterPanelSortDirectionButton : SquareButton
    {
        /// <summary>
        /// </summary>
        public FilterPanelSortDirectionButton() : base(null, null)
        {
            Icon.Image = GetIconTexture();
            Icon.Size = new ScalableVector2(20, 20);

            ActiveColor = SkinManager.Skin.ButtonNotActiveColor;
            InactiveColor = SkinManager.Skin.ButtonNotActiveColor;
            HoverColor = SkinManager.Skin.ButtonHoverColor;
            ContentColor = SkinManager.Skin.ButtonContentColor;

            Clicked += OnClicked;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Texture2D GetIconTexture()
        {
            if (ConfigManager.SelectSortDirection.Value == SortDirection.Descending)
                return FontAwesome.Get(FontAwesomeIcon.fa_arrow_down_wide_short);

            return FontAwesome.Get(FontAwesomeIcon.fa_arrow_down_short_wide);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked(object? sender, EventArgs e)
        {
            // Toggle direction
            ConfigManager.SelectSortDirection.Value = ConfigManager.SelectSortDirection.Value == SortDirection.Descending
                ? SortDirection.Ascending
                : SortDirection.Descending;

            // Update icon
            Icon.Image = GetIconTexture();

            // Re-center icon because image size might change slightly (though FA icons should be consistent)
            Icon.Size = new ScalableVector2(20, 20);
        }
    }
}
