using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Server.Client;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Playercards
{
    public class UserPlayercardLoggedOut : Sprite
    {
        // ── Background layers ──────────────────────────────────────

        /// <summary>
        ///     The masked background image (playercard-background-default blended onto playercard-background-mask).
        ///     Owned by this class and disposed on Destroy.
        /// </summary>
        private Texture2D? MaskedBackgroundTexture { get; set; }

        /// <summary>
        ///     Sprite that renders the masked background.
        /// </summary>
        private Sprite BackgroundContent { get; set; }

        /// <summary>
        ///     Dark overlay applied on top of the background to dim it.
        ///     Uses the same shape-mask as the background.
        /// </summary>
        private Sprite DarknessOverlay { get; set; }

        // ── Avatar ─────────────────────────────────────────────────

        /// <summary>
        ///     The masked avatar sprite. Owns MaskedAvatarTexture.
        /// </summary>
        private Sprite Avatar { get; set; }

        /// <summary>
        ///     RenderTarget2D produced by avatar masking. Disposed on Destroy.
        /// </summary>
        private Texture2D? MaskedAvatarTexture { get; set; }

        // ── UI Elements ────────────────────────────────────────────

        /// <summary>
        ///     Status text (e.g. "Disconnected from the server!").
        /// </summary>
        private SpriteTextPlus Status { get; set; }

        /// <summary>
        ///     Login / loading indicator.
        /// </summary>
        public SquareButton LoginButton { get; private set; }

        private LoadingWheel Wheel { get; set; }

        // ── Layout constants ───────────────────────────────────────
        private const int CardWidth = 526;
        private const int CardHeight = 100;
        private const int AvatarSize = 80;
        private const int AvatarMargin = 10;

        public UserPlayercardLoggedOut()
        {
            // Base sprite is invisible – children draw everything.
            Size = new ScalableVector2(CardWidth, CardHeight);
            Image = UserInterface.BlankBox;
            Alpha = 0;
            SetChildrenAlpha = false;

            CreateBackground();
            CreateDarknessOverlay();
            CreateAvatar();
            CreateStatus();
            CreateLoginButton();
            SetUsePreviousSpriteBatchOptionsRecursive(LoginButton);
            CreateLoadingWheel();

            PerformBackgroundMasking();
            PerformAvatarMasking();

            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            var isDisconnected = OnlineManager.Status.Value == ConnectionStatus.Disconnected;
            LoginButton.Visible = isDisconnected;
            LoginButton.IsClickable = isDisconnected;
            Wheel.Visible = !isDisconnected;

            base.Update(gameTime);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;

            MaskedBackgroundTexture?.Dispose();
            MaskedAvatarTexture?.Dispose();

            base.Destroy();
        }

        // ── Creation helpers ───────────────────────────────────────

        /// <summary>
        ///     Creates the background sprite that will hold the masked texture.
        ///     The actual image is set asynchronously by <see cref="PerformBackgroundMasking"/>.
        /// </summary>
        private void CreateBackground()
        {
            BackgroundContent = new Sprite
            {
                Parent = this,
                Size = new ScalableVector2(CardWidth, CardHeight),
                Alignment = Alignment.TopLeft,
                Image = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates the dark colour overlay that sits above the background.
        ///     Same size as the card, tinted #061019 @ 70 %.
        /// </summary>
        private void CreateDarknessOverlay()
        {
            DarknessOverlay = new Sprite
            {
                Parent = this,
                Size = new ScalableVector2(CardWidth, CardHeight),
                Alignment = Alignment.TopLeft,
                Image = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask,
                Tint = Color.Black,
                Alpha = 0.50f,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates the avatar sprite. Its image is set after masking completes.
        /// </summary>
        private void CreateAvatar()
        {
            Avatar = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = AvatarMargin,
                Y = AvatarMargin,
                Size = new ScalableVector2(AvatarSize, AvatarSize),
                Image = UserInterface.PlayercardAvatarOffline,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates the status text label.
        ///     Positioned 10 px to the right of the avatar and 15 px from the top.
        /// </summary>
        private void CreateStatus()
        {
            Status = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 22)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = AvatarMargin + AvatarSize + 10,
                Y = 15,
                UsePreviousSpriteBatchOptions = true,
            };

            UpdateStatusText();
        }

        /// <summary>
        ///     Creates the login button using the shared <see cref="SquareButton"/> component.
        ///     Positioned in the bottom-right corner with a 10 px margin.
        /// </summary>
        private void CreateLoginButton()
        {
            var skin = SkinManager.Skin;

            LoginButton = new SquareButton(UserInterface.PlayercardLoginIcon, (_, _) => OnlineManager.Login())
            {
                Parent = this,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(30, 30),
                X = -10,
                Y = -10,
                ActiveColor = skin.ButtonNotActiveColor,
                InactiveColor = skin.ButtonNotActiveColor,
                HoverColor = skin.ButtonHoverColor,
                ContentColor = skin.ButtonContentColor,
                // Hidden initially – Update() toggles it based on connection status.
                Visible = false,
            };
        }

        /// <summary>
        ///     Creates the loading spinner, placed behind the login button position.
        /// </summary>
        private void CreateLoadingWheel()
        {
            Wheel = new LoadingWheel
            {
                Parent = this,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(24, 24),
                X = -13,
                Y = -13,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Recursively sets <see cref="Wobble.Graphics.Drawable.UsePreviousSpriteBatchOptions"/> to true
        ///     on <paramref name="root"/> and all of its descendants.
        ///     This ensures every node in <see cref="LoginButton"/>'s tree (including internal
        ///     private Containers inside <see cref="SquareButton"/>) respects the parent
        ///     <see cref="Wobble.Graphics.UI.ScrollContainer"/>'s scissor SpriteBatch.
        /// </summary>
        private static void SetUsePreviousSpriteBatchOptionsRecursive(Wobble.Graphics.Drawable root)
        {
            root.UsePreviousSpriteBatchOptions = true;
            foreach (var child in root.Children)
                SetUsePreviousSpriteBatchOptionsRecursive(child);
        }

        // ── Masking helpers ────────────────────────────────────────

        /// <summary>
        ///     Schedules a render-thread blend of the background default image onto the shape mask,
        ///     then assigns the result to <see cref="BackgroundContent"/>.
        ///     This mirrors the pattern used in MenuBorderUserPanel for avatars.
        /// </summary>
        private void PerformBackgroundMasking()
        {
            var bgDefault = UserInterface.PlayercardBackgroundDefault;
            var bgMask = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask;

            GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
            {
                if (IsDisposed || bgDefault.IsDisposed || bgMask.IsDisposed)
                    return;

                var masked = AvatarMaskingHelper.PerformBlend(bgDefault, bgMask);
                if (masked == null)
                    return;

                var old = MaskedBackgroundTexture;
                MaskedBackgroundTexture = masked;
                BackgroundContent.Image = MaskedBackgroundTexture;
                old?.Dispose();
            });
        }

        /// <summary>
        ///     Schedules a render-thread blend of the offline avatar onto the avatar mask.
        /// </summary>
        private void PerformAvatarMasking()
        {
            var avatarTex = UserInterface.PlayercardAvatarOffline;
            var avatarMask = SkinManager.Skin?.MenuBorder?.PlayercardAvatarMask ?? UserInterface.PlayercardAvatarMask;

            GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
            {
                if (IsDisposed || avatarTex.IsDisposed || avatarMask.IsDisposed)
                    return;

                var masked = AvatarMaskingHelper.PerformBlend(avatarTex, avatarMask);
                if (masked == null)
                    return;

                var old = MaskedAvatarTexture;
                MaskedAvatarTexture = masked;
                Avatar.Image = MaskedAvatarTexture;
                old?.Dispose();
            });
        }

        // ── Event handlers ─────────────────────────────────────────

        private void UpdateStatusText() => ScheduleUpdate(() =>
        {
            Status.Text = OnlineManager.Status.Value switch
            {
                ConnectionStatus.Disconnected => "Disconnected from the server!",
                ConnectionStatus.Connecting => "Connecting to the server...",
                ConnectionStatus.Connected => "Connected!",
                ConnectionStatus.Reconnecting => "Reconnecting to the server...",
                _ => throw new ArgumentOutOfRangeException()
            };
        });

        private void OnConnectionStatusChanged(object sender,
            BindableValueChangedEventArgs<ConnectionStatus> e) => UpdateStatusText();
    }
}