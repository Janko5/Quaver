using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Graphics.Form.Dropdowns.RightClick;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Selection.UI.Playlists.Dialogs.Create;
using Quaver.Shared.Screens.Selection.UI.Playlists.Management;
using Quaver.Shared.Screens.Selection.UI.Playlists.Management.Maps;
using Quaver.Shared.Screens.Selection.UI.Playlists.Management.Mapsets;
using Wobble;
using Wobble.Managers;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Scheduling;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public class MapsetRightClickOptions : RightClickOptions
    {
        private DrawableMapset DrawableMapset { get; }

        private Mapset Mapset { get; }

        private const string ViewOnlineListing = "Online Listing";

        private const string AddToPlaylist = "Add To Playlist";

        private const string Delete = "Delete Mapset";


        private const string Export = "Export";

        private const string OpenMapsetFolder = "Open Folder";

        /// <summary>
        /// </summary>
        public MapsetRightClickOptions(DrawableMapset drawableMapset) : base(new Dictionary<string, Color>()
        {
            {AddToPlaylist, ColorHelper.HexToColor("#27B06E")},
            {Delete, ColorHelper.HexToColor($"#FF6868")},
            {Export, ColorHelper.HexToColor("#0787E3")},
            {OpenMapsetFolder, ColorHelper.HexToColor("#9B51E0")},
            {ViewOnlineListing, ColorHelper.HexToColor("#FFE76B")},
        }, new ScalableVector2(215, 40), 22)
        {
            DrawableMapset = drawableMapset;
            Mapset = DrawableMapset.Item;
            Anchor = drawableMapset;

            ItemSelected += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                var selectScreen = game.CurrentScreen as SelectionScreen;

                switch (args.Text)
                {
                    case ViewOnlineListing:
                        MapManager.ViewOnlineListing(Mapset.Maps.First());
                        break;
                    case Delete:
                        if (selectScreen == null)
                            return;

                        // Using First().Mapset to get the *real* and unfiltered mapset
                        DialogManager.Show(new DeleteMapsetDialog(Mapset.Maps.First().Mapset, selectScreen.AvailableMapsets.Value.IndexOf(Mapset)));
                        break;
                    case Export:
                        ThreadScheduler.Run(() =>
                        {
                            NotificationManager.Show(NotificationLevel.Info, "Exporting mapset to zip archive. Please wait!");

                            Mapset.Maps.First().Mapset.ExportToZip();

                            NotificationManager.Show(NotificationLevel.Success,
                                $"Successfully exported {MapManager.Selected.Value.Mapset.Artist} - {MapManager.Selected.Value.Mapset.Title}!");
                        });
                        break;
                    case OpenMapsetFolder:
                        Mapset.Maps.First().OpenFolder();
                        break;
                }
            };

            ClosedEvent += (sender, args) => ClosePlaylistSubMenu();

            // Setup "Add To Playlist" refinement
            var addToPlaylistItem = Items.FirstOrDefault(x => x.Text.Text == AddToPlaylist);
            if (addToPlaylistItem != null)
            {
                // Add Chevron
                var chevron = new Sprite
                {
                    Parent = addToPlaylistItem,
                    Alignment = Alignment.MidRight,
                    X = -10,
                    Size = new ScalableVector2(16, 16),
                    Image = TextureManager.Load("Quaver.Resources/Textures/FontAwesome/fa-right-chevron.png"),
                    Tint = Color.White,
                    UsePreviousSpriteBatchOptions = true
                };

                // Add Hover logic
                foreach (var item in Items)
                {
                    item.Hovered += (sender, args) =>
                    {
                        if (item == addToPlaylistItem)
                        {
                            ShowPlaylistSubMenu(addToPlaylistItem);
                        }
                        else
                        {
                            ClosePlaylistSubMenu();
                        }
                    };
                }
            }
        }

        private RightClickOptions? _playlistSubMenu;

        private void ShowPlaylistSubMenu(DropdownItem item)
        {
            if (_playlistSubMenu != null && !_playlistSubMenu.IsDisposed)
                return;

            var playlists = PlaylistManager.Playlists.FindAll(x => x.PlaylistGame == MapGame.Quaver);

            if (playlists.Count == 0)
                return;

            var options = new Dictionary<string, Color>();
            foreach (var pl in playlists)
                options.Add(pl.Name, Color.White);

            options.Add("Create New Playlist...", ColorHelper.HexToColor("#6888ff"));

            // MaxWidth: 165 (to clip 10px from checkbox at X=-12, so text ends at X=-22)
            // MaxHeight: 320 (8 items * 40px)
            _playlistSubMenu = new RightClickOptions(options, new ScalableVector2(215, 40), 22, 165, 320)
            {
                Parent = this,
                X = Width,
                Y = item.Y,
                MultiSelection = true,
                CloseOnSelect = false,
                UseCheckboxIcons = true
            };

            // Configure scrollbar appearance (like Options panel dropdowns)
            _playlistSubMenu.ItemContainer.Scrollbar.Alignment = Alignment.BotLeft;
            _playlistSubMenu.ItemContainer.Scrollbar.X = 0;
            _playlistSubMenu.ItemContainer.Scrollbar.Tint = Color.White;
            _playlistSubMenu.ItemContainer.Scrollbar.Width = 2;
            _playlistSubMenu.ItemContainer.EasingType = Easing.OutQuint;
            _playlistSubMenu.ItemContainer.TimeToCompleteScroll = 1200;
            _playlistSubMenu.ItemContainer.ScrollSpeed = 220;

            // Configure visuals for playlist items
            for (var i = 0; i < playlists.Count; i++)
            {
                var pl = playlists[i];
                var subItem = _playlistSubMenu.Items[i];

                // Check if mapset is in playlist (all maps)
                var allMapsInPlaylist = Mapset.Maps.All(m => pl.Maps.Any(pm => pm.Md5Checksum == m.Md5Checksum));
                subItem.SetSelected(allMapsInPlaylist);

                // Customize CheckIcon position
                subItem.CheckIcon.X = -12;

                // Hide original text and use MarqueeSpriteText for proper clipping
                var fullName = pl.Name;
                subItem.Text.Visible = false;

                var marquee = new MarqueeSpriteText(
                    subItem.Dropdown.SelectedText.Font,
                    fullName,
                    subItem.Dropdown.FontSize,
                    165) // MaxWidth for clipping
                {
                    Parent = subItem,
                    Alignment = Alignment.MidLeft,
                    X = Dropdown.PaddingX
                };

                // Activate marquee on hover
                subItem.Hovered += (sender, args) => marquee.IsActive = true;
                subItem.LeftHover += (sender, args) => marquee.IsActive = false;
            }


            // Configure Create Create Playlist item (last item)
            var createItem = _playlistSubMenu.Items.Last();
            createItem.CheckIcon.Visible = false; // Force hide even if UseCheckboxIcons is true?
            // Maybe add a plus icon? Or leave it text.

            _playlistSubMenu.ItemSelected += (sender, args) =>
            {
                var index = _playlistSubMenu.Items.IndexOf(args.Item);

                // Handle "Create New Playlist..."
                if (index == playlists.Count)
                {
                    Close(); // Close the main menu (and submenu) first
                    DialogManager.Show(new CreatePlaylistDialog());
                    return;
                }

                if (index >= 0 && index < playlists.Count)
                {
                    var playlist = playlists[index];
                    var isSelected = args.Item.IsSelected; // State AFTER toggle in Dropdown.SelectItem

                    if (isSelected)
                    {
                        // Add all maps
                        Mapset.Maps.ForEach(map =>
                        {
                            if (!playlist.Maps.Contains(map) && playlist.Maps.All(x => x.Md5Checksum != map.Md5Checksum))
                                playlist.Maps.Add(map);
                        });

                        ThreadScheduler.Run(() =>
                        {
                            Mapset.Maps.ForEach(x => PlaylistManager.AddMapToPlaylist(playlist, x));
                            PlaylistManager.EditPlaylist(playlist, null, ConfigManager.SelectGroupMapsetsBy.Value == GroupMapsetsBy.Playlists);
                        });
                    }
                    else
                    {
                        // Remove all maps
                        Mapset.Maps.ForEach(map => playlist.Maps.RemoveAll(x => x == map || x.Md5Checksum == map.Md5Checksum));

                        ThreadScheduler.Run(() =>
                        {
                            Mapset.Maps.ForEach(x => PlaylistManager.RemoveMapFromPlaylist(playlist, x));
                            PlaylistManager.EditPlaylist(playlist, null, ConfigManager.SelectGroupMapsetsBy.Value == GroupMapsetsBy.Playlists);
                        });
                    }

                    PlaylistManager.InvokePlaylistMapsManagedEvent(playlist);
                    Logger.Important($"Changed playlist: {playlist.Name} state to: {isSelected} for mapset: {Mapset.Artist} - {Mapset.Title}", LogType.Runtime);
                }
            };
        }

        /// <summary>
        ///     Update handler to manage playlist submenu scrollbar and input
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_playlistSubMenu != null && !_playlistSubMenu.IsDisposed)
            {
                // Show scrollbar when submenu is open
                _playlistSubMenu.ItemContainer.Scrollbar.Visible = true;
                _playlistSubMenu.ItemContainer.Scrollbar.Alpha = 1;

                // Enable input only when mouse is over the submenu to prevent background scrolling
                var mouseInSubmenu = GraphicsHelper.RectangleContains(
                    _playlistSubMenu.ItemContainer.ScreenRectangle,
                    MouseManager.CurrentState.Position);
                _playlistSubMenu.ItemContainer.InputEnabled = mouseInSubmenu;

                // Set static flag to block background container scrolling
                IsSubmenuScrollActive = mouseInSubmenu;
            }
            else
            {
                IsSubmenuScrollActive = false;
            }
        }


        private void ClosePlaylistSubMenu()
        {
            if (_playlistSubMenu != null)
            {
                _playlistSubMenu.Close();
                _playlistSubMenu.Destroy();
                _playlistSubMenu = null;
            }
        }

        /// <summary>
        ///     Selects a map and sets the appropriate index
        /// </summary>
        /// <param name="drawableMapset"></param>
        /// <param name="map"></param>
        /// <param name="mapset"></param>
        public static void SelectMap(DrawableMapset drawableMapset, Map map, Mapset mapset)
        {
            MapManager.Selected.Value = map;

            var container = (MapsetScrollContainer)drawableMapset.Container;

            var index = container.AvailableItems.IndexOf(mapset);

            if (index == -1)
                return;

            container.SelectedIndex.Value = index;
            container.ScrollToSelected();
        }

        protected override void OnClickedOutside(object sender, EventArgs e)
        {
            if (_playlistSubMenu != null && !_playlistSubMenu.IsDisposed)
            {
                var mouse = MouseManager.CurrentState.Position.ToPoint();
                if (_playlistSubMenu.ScreenRectangle.Contains(mouse) || _playlistSubMenu.ItemContainer.ScreenRectangle.Contains(mouse))
                    return;
            }

            base.OnClickedOutside(sender, e);
        }
    }
}