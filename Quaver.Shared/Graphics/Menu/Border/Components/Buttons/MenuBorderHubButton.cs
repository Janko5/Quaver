using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;

namespace Quaver.Shared.Graphics.Menu.Border.Components.Buttons
{
    public class MenuBorderHubButton : SquareButton
    {
        public MenuBorderHubButton(int paddingX = 10)
            : base(UserInterface.MenuBorderIconBurger, OnClicked)
        {
            CustomPaddingX = paddingX;
            ActiveColor = SkinManager.Skin.MenuBorder.SquareButtonActiveColor;
            InactiveColor = SkinManager.Skin.MenuBorder.SquareButtonNotActiveColor;
            HoverColor = SkinManager.Skin.MenuBorder.SquareButtonHoverColor;
            ContentColor = SkinManager.Skin.MenuBorder.SquareButtonContentColor;

            // Specifically for MenuBorder, we want the NineSlice margins to match what it had before
            Background.Margins = new SliceMargins(19, 19, 0, 0);
            HoverOverlay.Margins = new SliceMargins(19, 19, 0, 0);
        }

        public override void Update(GameTime gameTime)
        {
            if (GameBase.Game is QuaverGame game)
            {
                var isUnread = game.OnlineHub.Sections.Any(x => x.Value.IsUnread);
                var iconImage = isUnread ? UserInterface.MenuBorderIconBurgerRed : UserInterface.MenuBorderIconBurger;

                if (Icon.Image != iconImage)
                {
                    Icon.Image = iconImage;
                    Icon.Size = new Wobble.Graphics.ScalableVector2(iconImage.Width, iconImage.Height);
                }
            }

            base.Update(gameTime);
        }

        private static void OnClicked(object? sender, EventArgs e)
        {
            if (DialogManager.Dialogs.Count == 0)
            {
                DialogManager.Show(new OnlineHubDialog());
                return;
            }

            if (DialogManager.Dialogs.Last().GetType() != typeof(OnlineHubDialog))
                return;

            var dialog = (OnlineHubDialog)DialogManager.Dialogs.Last();
            dialog?.Close();
        }
    }
}
