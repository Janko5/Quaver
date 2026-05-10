using System;
using System.Collections.Generic;
using Quaver.Shared;
using Wobble;
using Wobble.Graphics.Animations;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Quaver.Shared.Graphics.Menu.Border.Components.Buttons;
using Wobble.Graphics;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Managers;
using Quaver.Shared.Screens.Selection; // Assuming SelectionScreen is in this namespace or similar
using Quaver.Shared.Skinning;
using Quaver.Shared.Graphics.Notifications;

namespace Quaver.Shared.Screens.Selection.UI.Borders.Footer
{
    public class SelectMenuFooter : MenuBorder
    {
        public SelectMenuFooter(SelectionScreen screen) : base(MenuBorderType.Footer, new List<Drawable>(), new List<Drawable>())
        {
            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                var leftAlignedItems = new List<Drawable>
                {
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-back.png"), "Back", (sender, args) => screen.HandleBackAction()),
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-options.png"), "Options", (sender, args) => DialogManager.Show(new Quaver.Shared.Screens.Options.OptionsDialog())),
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-donate.png"), "Donate", (sender, args) => NotificationManager.Show(NotificationLevel.Info, "Donating is currently unavailable from in-game and can only be done on the website.\n\nWe are working on adding this back soon."))
                };

                var rightAlignedItems = new List<Drawable>
                {
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-play.png"), "Play", (sender, args) =>
                    {
                        switch (screen.ActiveScrollContainer.Value)
                        {
                            case SelectScrollContainerType.Mapsets:
                                screen.ExitToGameplay();
                                break;
                            case SelectScrollContainerType.Maps:
                                screen.ExitToGameplay();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }),
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-volume.png"), "Volume", (sender, args) =>
                    {
                        var volControl = ((QuaverGame)GameBase.Game).VolumeController;
                        volControl.ToggleManual();
                    }),
                    new IconTextButtonV2(TextureManager.Load("Quaver.Resources/Textures/UI/MenuBorder/icon-random.png"), "Random", (sender, args) =>
                    {
                        screen.SelectRandomMap();
                    })
                };

                // Add to Collections
                LeftAlignedItems = leftAlignedItems;
                RightAlignedItems = rightAlignedItems;

                LeftAlignedItems.ForEach(x => x.Parent = this);
                RightAlignedItems.ForEach(x => x.Parent = this);
            }
            else
            {
                LeftAlignedItems = new List<Drawable>
                {
                    new IconTextButtonSelectBack(screen),
                    new IconTextButtonOptions(),
                    new IconTextButtonModifiers(screen.ActiveLeftPanel),
                    new IconTextButtonMapPreview(screen.ActiveLeftPanel)
                };

                RightAlignedItems = new List<Drawable>
                {
                    new IconTextButtonPlay(screen),
                    new IconTextButtonEdit(screen),
                    new IconTextButtonRandom(screen),
                    new IconTextButtonCreatePlaylist()
                };

                LeftAlignedItems.ForEach(x => x.Parent = this);
                RightAlignedItems.ForEach(x => x.Parent = this);
            }
        }
    }
}