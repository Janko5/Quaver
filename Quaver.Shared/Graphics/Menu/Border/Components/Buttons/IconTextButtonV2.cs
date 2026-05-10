using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using Wobble.Managers;

#nullable enable

namespace Quaver.Shared.Graphics.Menu.Border.Components.Buttons
{
    /// <summary>
    ///     Base class for all IconTextButtons in the v2 bottom menu border.
    ///     This encapsulates the hardcoded v2 styles: Inter Bold, size 28, 10px spacing.
    /// </summary>
    public class IconTextButtonV2 : IconTextButton
    {
        public IconTextButtonV2(Texture2D icon, string text, EventHandler? onClick = null)
            : base(icon, FontManager.GetWobbleFont(Fonts.InterBold), text, onClick, textSize: 28)
        {
            UppercaseText = false;
            Text.Text = text; // Need to refresh text because base set it to upper
            Spacing = 10;
            Icon.Size = new Wobble.Graphics.ScalableVector2(icon.Width, icon.Height);
            Text.X = Icon.Width + Spacing; // Relative to Icon (since Parent = Icon and MidLeft aligned)
            UpdateSize();
        }

        public IconTextButtonV2(FontAwesomeIcon icon, string text, EventHandler? onClick = null)
            : base(FontAwesome.Get(icon), FontManager.GetWobbleFont(Fonts.InterBold), text, onClick, textSize: 28)
        {
            UppercaseText = false;
            Text.Text = text; // Refresh text
            Spacing = 10;
            Text.X = Icon.Width + Spacing; // Relative to Icon
            UpdateSize();
        }
    }
}
