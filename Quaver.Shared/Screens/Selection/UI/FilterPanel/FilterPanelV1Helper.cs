using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    public static class FilterPanelV1Helper
    {
        /// <summary>
        ///     Creates a dropdown with the V1 FilterPanel specific textures.
        /// </summary>
        public static Dropdown CreateDropdown(List<string> options, ScalableVector2 size, int fontSize, Color? color = null, int selectedIndex = 0)
        {
            var dropdown = new Dropdown(options, size, fontSize, color, selectedIndex);

            // Override textures with V1 specific ones
            dropdown.TextureClosed = SkinManager.Skin.DropdownClose;
            dropdown.TextureOpen = SkinManager.Skin.DropdownOpen;

            // Apply immediately to current state
            dropdown.Image = dropdown.Opened ? dropdown.TextureOpen : dropdown.TextureClosed;

            if (dropdown.HoverSprite != null)
                dropdown.HoverSprite.Image = dropdown.Image;

            return dropdown;
        }
    }
}
