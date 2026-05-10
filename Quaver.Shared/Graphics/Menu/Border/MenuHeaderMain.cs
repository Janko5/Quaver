using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Audio;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Quaver.Shared.Graphics.Menu.Border.Components.Buttons;
using Quaver.Shared.Graphics.Menu.Border.Components.Users;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens;
using Quaver.Shared.Screens.Main;
using Quaver.Shared.Screens.Edit;
using Quaver.Shared.Screens.Downloading;
using Quaver.Shared.Screens.MultiplayerLobby;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Overlays.Chatting;
using Quaver.Shared.Screens.Selection;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Logging;

namespace Quaver.Shared.Graphics.Menu.Border
{
    public class MenuHeaderMain : MenuBorder
    {
        /// <summary>
        /// </summary>
        private Sprite User { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public MenuHeaderMain() : base(MenuBorderType.Header, GetLeftItems(), new List<Drawable>())
        {
            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                RightAlignedItems.Add(new MenuBorderHubButton(10));

                User = new MenuBorderUserPanel(10);
                ((MenuBorderUserPanel)User).Resized += OnUserResized;
            }
            else
            {
                RightAlignedItems.Add(new IconTextButtonOnlineHub());

                User = new DrawableLoggedInUser();
                ((DrawableLoggedInUser)User).Resized += OnUserResized;
            }

            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                // Add FPS component BEFORE User so its position is swapped visually 
                RightAlignedItems.Add(new DrawableMenuBorderFps(10));

                RightAlignedItems.Add(User);

                RightAlignedItems.Add(new DrawableMenuBorderInfo(10));
            }
            else
            {
                RightAlignedItems.Add(User);
                RightAlignedItems.Add(new DrawableSessionTime());
            }

            AlignRightItems();
        }

        /// <summary>
        ///     A transparent sprite grouping both info panels perfectly parallel to UserProfile.
        /// </summary>
        private class DrawableMenuBorderInfo : Sprite, IMenuBorderItem
        {
            public bool UseCustomPaddingY { get; } = true;
            public int CustomPaddingY { get; } = 0; // Moved 1px down from -1
            public bool UseCustomPaddingX { get; } = true;
            public int CustomPaddingX { get; }

            private DrawableSessionTime SessionTime { get; }
            private DrawableOnlineFriends OnlineFriends { get; }

            public DrawableMenuBorderInfo(int paddingX)
            {
                CustomPaddingX = paddingX;

                // Essential for Sprite behavior matching v1 and MenuBorderUserPanel flawlessly
                Image = UserInterface.BlankBox;
                Alpha = 0;
                SetChildrenAlpha = false;

                // 22px (time) + 6px (gap) + 22px (friends) = 50px (matching UserProfile perfectly)
                Height = 50;

                SessionTime = new DrawableSessionTime
                {
                    Parent = this,
                    Alignment = Alignment.TopRight,
                    Y = 0
                };

                OnlineFriends = new DrawableOnlineFriends
                {
                    Parent = this,
                    Alignment = Alignment.BotRight,
                    Y = 0 // BotRight mathematically snaps it exactly to the bottom of the 58px sprite!
                };
            }

            public override void Update(GameTime gameTime)
            {
                // Evaluate MaxWidth so AlignRightItems() registers pushed offset X accurately!
                Width = Math.Max(SessionTime.Width, OnlineFriends.Width);
                base.Update(gameTime);
            }
        }

        private static List<Drawable> GetLeftItems()
        {
            var leftItems = new List<Drawable>
            {
                new MenuBorderLogo()
            };

            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                // Home Button
                var homeButton = new SquareButton(UserInterface.MenuBorderIconHome, (sender, e) =>
                {
                    var game = (QuaverGame)GameBase.Game;
                    if (OnlineManager.CurrentGame != null)
                        OnlineManager.LeaveGame();

                    game.CurrentScreen.Exit(() => new MainMenuScreen());
                }, "Home", () => ((QuaverGame)GameBase.Game).CurrentScreen is MainMenuScreen);
                SetMenuBorderStyle(homeButton, 20);
                leftItems.Add(homeButton);

                // Singleplayer Button
                var singleplayerButton = new SquareButton(UserInterface.MenuBorderIconSingleplayer, (sender, e) =>
                {
                    var game = (QuaverGame)GameBase.Game;
                    if (game.CurrentScreen is SelectionScreen)
                        return;

                    game.CurrentScreen.Exit(() => new SelectionScreen());
                }, "Singleplayer", () => ((QuaverGame)GameBase.Game).CurrentScreen is SelectionScreen);
                SetMenuBorderStyle(singleplayerButton, 10);
                leftItems.Add(singleplayerButton);

                // Multiplayer Button
                var multiplayerButton = new SquareButton(UserInterface.MenuBorderIconMultiplayer, (sender, e) =>
                {
                    var game = (QuaverGame)GameBase.Game;
                    if (game.CurrentScreen is MultiplayerLobbyScreen)
                        return;

                    game.CurrentScreen.Exit(() => new MultiplayerLobbyScreen());
                }, "Multiplayer - Lobby", () => ((QuaverGame)GameBase.Game).CurrentScreen is MultiplayerLobbyScreen);
                SetMenuBorderStyle(multiplayerButton, 10);
                leftItems.Add(multiplayerButton);

                // Download Maps Button
                var downloadButton = new SquareButton(UserInterface.MenuBorderIconDownload, (sender, e) =>
                {
                    var game = (QuaverGame)GameBase.Game;
                    if (game.CurrentScreen.Type == QuaverScreenType.Multiplayer)
                    {
                        NotificationManager.Show(NotificationLevel.Warning, "You can only download maps in multiplayer while you are host during song select!");
                        return;
                    }

                    game.CurrentScreen.Exit(() => new DownloadingScreen(game.CurrentScreen.Type));
                }, "Download Maps", () => ((QuaverGame)GameBase.Game).CurrentScreen is DownloadingScreen);
                SetMenuBorderStyle(downloadButton, 10);
                leftItems.Add(downloadButton);

                // Editor Button
                var editorButton = new SquareButton(UserInterface.MenuBorderIconEditor, (sender, e) =>
                {
                    var game = (QuaverGame)GameBase.Game;
                    if (MapManager.Selected == null || MapManager.Selected.Value == null)
                    {
                        NotificationManager.Show(NotificationLevel.Error, "You cannot edit without a map selected.");
                        return;
                    }

                    game.CurrentScreen.Exit(() =>
                    {
                        if (AudioEngine.Track.IsPlaying)
                            AudioEngine.Track?.Pause();

                        try
                        {
                            return new EditScreen(MapManager.Selected.Value, AudioEngine.LoadMapAudioTrack(MapManager.Selected.Value));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, LogType.Runtime);
                            NotificationManager.Show(NotificationLevel.Error, "Unable to read map file!");
                            return new MainMenuScreen();
                        }
                    });
                }, "Editor", () => ((QuaverGame)GameBase.Game).CurrentScreen is EditScreen);
                SetMenuBorderStyle(editorButton, 10);
                leftItems.Add(editorButton);

                // Clan Button
                var clanButton = new SquareButton(UserInterface.MenuBorderIconClan, (sender, e) =>
                {
                    BrowserHelper.OpenURL("https://two.quavergame.com/leaderboard/clans");
                }, "Clan");
                SetMenuBorderStyle(clanButton, 10);
                leftItems.Add(clanButton);

                // Workshop Button
                var workshopButton = new SquareButton(UserInterface.MenuBorderIconWorkshop, (sender, e) =>
                {
                    BrowserHelper.OpenURL($"https://steamcommunity.com/app/{SteamManager.ApplicationId}/workshop/");
                });
                SetMenuBorderStyle(workshopButton, 10);
                leftItems.Add(workshopButton);

                // Chat Button
                var chatButton = new SquareButton(UserInterface.MenuBorderIconChat, (sender, e) =>
                {
                    var chat = OnlineChat.Instance;
                    if (chat.IsOpen)
                        chat.Close();
                    else
                        chat.Open();
                });
                SetMenuBorderStyle(chatButton, 10);
                leftItems.Add(chatButton);

                // Player Button
                var playerButton = new SquareButton(UserInterface.MenuBorderIconPlayer, (sender, e) =>
                {
                    NotificationManager.Show(NotificationLevel.Info, "Player WIP");
                });
                SetMenuBorderStyle(playerButton, 10);
                leftItems.Add(playerButton);
            }
            else
            {
                leftItems.Add(new IconTextButtonHome());
                leftItems.Add(new IconTextButtonDownloadMaps());
                leftItems.Add(new IconTextButtonClans());
                leftItems.Add(new IconTextButtonMusicPlayer());
                leftItems.Add(new IconTextButtonSkins());
                leftItems.Add(new IconTextButtonDonate());
            }

            return leftItems;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            if (User is DrawableLoggedInUser u1)
                u1.Resized -= OnUserResized;
            else if (User is MenuBorderUserPanel u2)
                u2.Resized -= OnUserResized;

            base.Destroy();
        }

        private void OnUserResized(object? sender, LoggedInUserResizedEventArgs e) => AlignRightItems();

        /// <summary>
        ///     Helper to set the menu border style for a square button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="paddingX"></param>
        private static void SetMenuBorderStyle(SquareButton button, int paddingX)
        {
            button.CustomPaddingX = paddingX;
            button.ActiveColor = SkinManager.Skin.MenuBorder.SquareButtonActiveColor;
            button.InactiveColor = SkinManager.Skin.MenuBorder.SquareButtonNotActiveColor;
            button.HoverColor = SkinManager.Skin.MenuBorder.SquareButtonHoverColor;
            button.ContentColor = SkinManager.Skin.MenuBorder.SquareButtonContentColor;

            // Specifically for MenuBorder, we want the NineSlice margins to match what it had before
            button.Background.Margins = new SliceMargins(19, 19, 0, 0);
            button.HoverOverlay.Margins = new SliceMargins(19, 19, 0, 0);
        }
    }
}