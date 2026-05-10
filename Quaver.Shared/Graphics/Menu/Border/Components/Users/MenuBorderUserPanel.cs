using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client;
using Quaver.Server.Client.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.Menus;
using Steamworks;
using Wobble;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Menu.Border.Components.Users
{
    public class MenuBorderUserPanel : ImageButton, IMenuBorderItem
    {
        /// <inheritdoc />
        public bool UseCustomPaddingY => true;

        /// <inheritdoc />
        public int CustomPaddingY => 0;

        /// <inheritdoc />
        public bool UseCustomPaddingX => true;

        /// <inheritdoc />
        public int CustomPaddingX { get; set; }

        /// <summary>
        ///     The background NineSliceSprite
        /// </summary>
        private NineSliceSprite BackgroundPanel { get; }

        /// <summary>
        ///     The hover overlay NineSliceSprite
        /// </summary>
        private NineSliceSprite HoverOverlay { get; }

        /// <summary>
        ///     The actual avatar sprite.
        /// </summary>
        private Sprite Avatar { get; }

        /// <summary>
        ///     The activity light indicating online status.
        /// </summary>
        private Sprite ActivityLight { get; }

        /// <summary>
        ///     The country flag of the user.
        /// </summary>
        private Sprite Flag { get; }

        /// <summary>
        ///     The username of the user.
        /// </summary>
        private SpriteTextPlus Username { get; }

        /// <summary>
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        ///     The masked avatar texture.
        /// </summary>
        private Texture2D? MaskedAvatarTexture { get; set; }

        /// <summary>
        ///     Event invoked when the logged in user has been resized
        /// </summary>
        public event EventHandler<LoggedInUserResizedEventArgs>? Resized;

        /// <summary>
        /// </summary>
        /// <param name="paddingX"></param>
        public MenuBorderUserPanel(int paddingX = 20) : base(UserInterface.BlankBox)
        {
            CustomPaddingX = paddingX;
            Height = 50;
            Alpha = 0; // The base texture is hidden, we use children instead
            SetChildrenAlpha = false;

            var skin = SkinManager.Skin.MenuBorder;
            var bgColor = skin.SquareButtonNotActiveColor;
            var hoverColor = skin.SquareButtonHoverColor;

            // 1. Background
            var bgTexture = skin.UserPanelBackground ?? UserInterface.MenuBorderUserPanelBackground;
            BackgroundPanel = new NineSliceSprite(bgTexture ?? UserInterface.BlankBox, new SliceMargins(25, 25, 0, 0))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Tint = bgColor,
                Size = new ScalableVector2(100, Height),
                Alpha = 1f,
            };

            // 1b. Hover Overlay (Always uses SquareButtonHoverColor, just like SquareButton)
            HoverOverlay = new NineSliceSprite(bgTexture ?? UserInterface.BlankBox, new SliceMargins(25, 25, 0, 0))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Tint = hoverColor,
                Size = BackgroundPanel.Size,
                Alpha = 0,
            };

            // 2. Avatar (Alpha Blend Masked)
            Avatar = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 0,
                Size = new ScalableVector2(50, 50),
                Image = UserInterface.PlayercardAvatarOffline,
                Alpha = 1f,
            };

            // 3. Activity Light
            var lightTexture = skin.UserPanelActivityLight ?? UserInterface.MenuBorderUserPanelActivityLight;
            ActivityLight = new Sprite
            {
                Parent = this,
                Alignment = Alignment.BotLeft,
                X = Avatar.Width - 14,
                Y = 0,
                Image = lightTexture,
                Size = new ScalableVector2(14, 14),
                Tint = GetStatusColor(),
            };

            // 4. Country Flag
            Flag = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Avatar.Width + 10,
                Size = new ScalableVector2(20, 20),
                Image = Flags.Get(OnlineManager.Connected ? OnlineManager.Self?.OnlineUser?.CountryFlag ?? "XX" : "XX"),
            };

            // 5. Nickname
            Username = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), OnlineManager.Connected ? ConfigManager.Username?.Value : "Login", 22)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Flag.X + Flag.Width + 10,
                Tint = skin.SquareButtonContentColor, // Use skin content color for name too
            };

            UpdateSize();
            UpdateMaskedAvatar();

            if (ConfigManager.Username != null)
                ConfigManager.Username.ValueChanged += OnUsernameChanged;

            OnlineManager.Status.ValueChanged += OnOnlineStatusChanged;
            SteamManager.SteamUserAvatarLoaded += OnAvatarLoaded;

            Hovered += OnHovered;
            LeftHover += OnHoverLeft;
            Clicked += OnClicked;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            var skin = SkinManager.Skin.MenuBorder;
            var activeColor = IsOpen ? skin.SquareButtonActiveColor
                                       : skin.SquareButtonNotActiveColor;

            BackgroundPanel.Tint = activeColor;
            ActivityLight.Tint = GetStatusColor();

            var game = GameBase.Game as QuaverGame;

            if (MouseManager.IsUniqueClick(MouseButton.Left) && !IsHovered
               && game?.CurrentScreen?.ActiveLoggedInUserDropdown != null
               && !game.CurrentScreen.ActiveLoggedInUserDropdown.Button.IsHovered()
               && IsOpen)
            {
                FireButtonClickEvent();
            }

            base.Update(gameTime);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            if (ConfigManager.Username != null)
                ConfigManager.Username.ValueChanged -= OnUsernameChanged;

            OnlineManager.Status.ValueChanged -= OnOnlineStatusChanged;
            SteamManager.SteamUserAvatarLoaded -= OnAvatarLoaded;

            MaskedAvatarTexture?.Dispose();

            base.Destroy();
        }

        /// <summary>
        /// </summary>
        private void UpdateMaskedAvatar()
        {
            var rawTexture = GetAvatar();
            var mask = SkinManager.Skin.MenuBorder.UserPanelAvatarMask ?? UserInterface.MenuBorderUserPanelAvatarMask;

            if (rawTexture == null || mask == null)
                return;

            GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
            {
                if (IsDisposed || rawTexture.IsDisposed || mask.IsDisposed)
                    return;

                var masked = AvatarMaskingHelper.PerformBlend(rawTexture, mask);
                if (masked == null)
                    return;

                var oldTexture = MaskedAvatarTexture;
                MaskedAvatarTexture = masked;
                Avatar.Image = MaskedAvatarTexture;
                oldTexture?.Dispose();
            });
        }

        /// <summary>
        ///     Updates the size of the background panel based on the content.
        /// </summary>
        private void UpdateSize()
        {
            var contentWidth = Math.Max(100, Username.X + Username.Width + 15);
            BackgroundPanel.Size = new ScalableVector2(contentWidth, Height);
            HoverOverlay.Size = BackgroundPanel.Size;
            Width = contentWidth;

            Resized?.Invoke(this, new LoggedInUserResizedEventArgs());
        }

        /// <summary>
        ///     Handles hover event.
        /// </summary>
        private void OnHovered(object? sender, EventArgs e)
        {
            HoverOverlay.ClearAnimations();
            HoverOverlay.FadeTo(1f, Easing.OutQuint, 100);
        }

        /// <summary>
        ///     Handles hover leave event.
        /// </summary>
        private void OnHoverLeft(object? sender, EventArgs e)
        {
            HoverOverlay.ClearAnimations();
            HoverOverlay.FadeTo(0, Easing.OutQuint, 100);
        }

        /// <summary>
        ///     Handles click event.
        /// </summary>
        private void OnClicked(object? sender, EventArgs e)
        {
            IsOpen = !IsOpen;

            var game = GameBase.Game as QuaverGame;

            if (!IsOpen)
            {
                game?.CurrentScreen?.ActiveLoggedInUserDropdown?.Close();
                return;
            }

            if (game?.CurrentScreen?.ActiveLoggedInUserDropdown == null)
            {
                game?.CurrentScreen?.ActivateLoggedInUserDropdown(new LoggedInUserDropdown(),
                    new ScalableVector2(AbsolutePosition.X + AbsoluteSize.X - LoggedInUserDropdown.ContainerSize.X.Value,
                        AbsolutePosition.Y + AbsoluteSize.Y + 13));

                return;
            }

            game.CurrentScreen.ActiveLoggedInUserDropdown?.Open();
        }

        /// <summary>
        ///     Handles username changes.
        /// </summary>
        private void OnUsernameChanged(object? sender, BindableValueChangedEventArgs<string> e) => UpdatePanel();

        /// <summary>
        ///     Handles online status changes.
        /// </summary>
        private void OnOnlineStatusChanged(object? sender, BindableValueChangedEventArgs<ConnectionStatus> e) => UpdatePanel();

        /// <summary>
        ///     Schedules the panel UI to be updated on the main thread
        /// </summary>
        private void UpdatePanel() => ScheduleUpdate(() =>
        {
            Username.Text = OnlineManager.Connected ? ConfigManager.Username?.Value : "Login";
            Flag.Image = Flags.Get(OnlineManager.Connected ? OnlineManager.Self?.OnlineUser?.CountryFlag ?? "XX" : "XX");
            UpdateSize();
            UpdateMaskedAvatar();
        });

        /// <summary>
        ///     Handles steam avatar loading.
        /// </summary>
        private void OnAvatarLoaded(object? sender, SteamAvatarLoadedEventArgs e)
        {
            if (!OnlineManager.Connected || e.Texture == null)
                return;

            if (e.SteamId != SteamUser.GetSteamID().m_SteamID)
                return;

            ScheduleUpdate(() =>
            {
                UpdateMaskedAvatar();

                if (Avatar.Alpha < 1)
                {
                    Avatar.ClearAnimations();
                    Avatar.Animations.Add(new Animation(AnimationProperty.Alpha, Easing.Linear, 0, 1, 300));
                }
            });
        }

        /// <summary>
        ///     Gets the current status color based on online status.
        /// </summary>
        private Color GetStatusColor()
        {
            if (!OnlineManager.Connected || OnlineManager.Self?.CurrentStatus == null)
                return Color.Gray;

            return OnlineManager.Self.CurrentStatus.Status switch
            {
                ClientStatus.InMenus => Color.Green,
                ClientStatus.Playing => Color.Yellow,
                ClientStatus.Editing => Color.Purple,
                ClientStatus.Multiplayer => Color.Orange,
                ClientStatus.InLobby => Color.Cyan,
                _ => Color.Green
            };
        }

        /// <summary>
        ///     Gets the user's avatar texture.
        /// </summary>
        private Texture2D GetAvatar()
        {
            var image = UserInterface.PlayercardAvatarOffline;

            if (OnlineManager.Status.Value == ConnectionStatus.Connected && SteamManager.UserAvatars != null)
            {
                var id = SteamUser.GetSteamID().m_SteamID;

                if (SteamManager.UserAvatars.ContainsKey(id))
                    image = SteamManager.UserAvatars[id];
                else
                {
                    Avatar.Alpha = 0;
                    SteamManager.SendAvatarRetrievalRequest(id);
                }
            }

            return image;
        }
    }
}
