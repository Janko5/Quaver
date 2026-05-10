using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Borders.Footer
{
    public class IconTextButtonSelectBack : IconTextButton
    {
        public IconTextButtonSelectBack(SelectionScreen screen) : base(FontAwesome.Get(FontAwesomeIcon.fa_chevron_pointing_to_the_left),
            FontManager.GetWobbleFont(Fonts.InterBold), "Back", (sender, args) =>
            {
                screen.HandleBackAction();
            })
        {
        }
    }
}