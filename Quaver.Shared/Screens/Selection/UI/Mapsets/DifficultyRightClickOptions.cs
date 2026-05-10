using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Graphics.Form.Dropdowns.RightClick;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Screens.Selection.UI.Playlists.Dialogs.Create;
using Quaver.Shared.Screens.Selection.UI.Playlists.Management.Maps;
using Wobble;
using Wobble.Managers;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Scheduling;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public class DifficultyRightClickOptions : RightClickOptions
    {
        private Map Map { get; }

        private const string Play = "Play";
        private const string Edit = "Edit";
        private const string AddToPlaylist = "Add To Playlist";
        private const string DeleteMap = "Delete Map";
        private const string DeleteLocalScores = "Delete Local Scores";
        private const string OpenFolder = "Open Folder";
        private const string OnlineListing = "Online Listing";

        public DifficultyRightClickOptions(Map map, Drawable anchor) : base(GetOptions(), new ScalableVector2(215, 40), 22)
        {
            Map = map;
            Anchor = anchor;

            ItemSelected += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                var selectScreen = game.CurrentScreen as SelectionScreen;

                switch (args.Text)
                {
                    case Play:
                        selectScreen?.ExitToGameplay();
                        break;
                    case Edit:
                        selectScreen?.ExitToEditor();
                        break;
                    case DeleteMap:
                        DialogManager.Show(new DeleteMapDialog(Map, Map.Mapset.Maps.IndexOf(Map)));
                        break;
                    case DeleteLocalScores:
                        DialogManager.Show(new DeleteLocalScoresDialog(Map));
                        break;
                    case OpenFolder:
                        Map.OpenFolder();
                        break;
                    case OnlineListing:
                        MapManager.ViewOnlineListing(Map);
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

                // Check if map is in playlist
                var mapInPlaylist = pl.Maps.Any(x => x.Md5Checksum == Map.Md5Checksum);
                subItem.SetSelected(mapInPlaylist);

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


            // Configure Create Playlist item
            var createItem = _playlistSubMenu.Items.Last();
            createItem.CheckIcon.Visible = false;

            _playlistSubMenu.ItemSelected += (sender, args) =>
            {
                var index = _playlistSubMenu.Items.IndexOf(args.Item);

                // Handle "Create New Playlist..."
                if (index == playlists.Count)
                {
                    Close(); // Close main menu first
                    DialogManager.Show(new CreatePlaylistDialog());
                    return;
                }

                if (index >= 0 && index < playlists.Count)
                {
                    var playlist = playlists[index];
                    var isSelected = args.Item.IsSelected; // State AFTER toggle

                    if (isSelected)
                    {
                        // Add to playlist
                        if (!playlist.Maps.Contains(Map) && playlist.Maps.All(x => x.Md5Checksum != Map.Md5Checksum))
                            playlist.Maps.Add(Map);

                        ThreadScheduler.Run(() =>
                        {
                            PlaylistManager.AddMapToPlaylist(playlist, Map);
                            PlaylistManager.EditPlaylist(playlist, null, false);
                        });
                    }
                    else
                    {
                        // Remove from playlist
                        playlist.Maps.RemoveAll(x => x == Map || x.Md5Checksum == Map.Md5Checksum);

                        ThreadScheduler.Run(() =>
                        {
                            PlaylistManager.RemoveMapFromPlaylist(playlist, Map);
                            PlaylistManager.EditPlaylist(playlist, null, false);
                        });
                    }

                    PlaylistManager.InvokePlaylistMapsManagedEvent(playlist);
                    Logger.Important($"Changed playlist: {playlist.Name} state to: {isSelected} for map: {Map}", LogType.Runtime);
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

        private static Dictionary<string, Color> GetOptions() => new Dictionary<string, Color>()
        {
            {Play, Color.White},
            {Edit, ColorHelper.HexToColor("#F2994A")},
            {AddToPlaylist, ColorHelper.HexToColor("#27B06E")},
            {DeleteMap, ColorHelper.HexToColor($"#FF6868")},
            {DeleteLocalScores, ColorHelper.HexToColor($"#FF6868")},
            {OpenFolder, ColorHelper.HexToColor("#9B51E0")},
            {OnlineListing, ColorHelper.HexToColor("#FFE76B")},
        };

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
