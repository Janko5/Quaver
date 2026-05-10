using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Server.Client;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Handlers;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Menu.Border.Components
{
    public class DrawableOnlineFriends : Container
    {
        public NineSliceSprite Background { get; }
        public SpriteTextPlus CountText { get; }
        public SpriteTextPlus LabelText { get; }

        public DrawableOnlineFriends()
        {
            var font = FontManager.GetWobbleFont(Fonts.InterBold);
            var fontSize = 17;

            Size = new ScalableVector2(0, 22);

            Background = new NineSliceSprite(SkinManager.Skin?.MenuBorder?.InfoBackground ?? UserInterface.MenuBorderInfoBackground, new SliceMargins(11, 11, 0, 0))
            {
                Parent = this,
                Tint = SkinManager.Skin?.UserInterfaceVersion >= 2f ? SkinManager.Skin.MenuBorder.SquareButtonNotActiveColor : ColorHelper.HexToColor("#363636"),
                Size = new ScalableVector2(0, 22),
            };

            CountText = new SpriteTextPlus(font, "0", fontSize)
            {
                Parent = Background,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentSecondColor,
                Y = 0,
            };

            LabelText = new SpriteTextPlus(font, "  Friends Online", fontSize)
            {
                Parent = Background,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.MenuBorder.SquareButtonContentColor,
                Y = 0,
            };

            UpdateSizeAndText();

            OnlineManager.FriendsListUserChanged += OnFriendsListUserChanged;
            OnlineManager.Status.ValueChanged += OnOnlineStatusChanged;

            if (OnlineManager.Connected)
                SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (OnlineManager.Client == null) return;
            OnlineManager.Client.OnUserConnected += OnUserConnected;
            OnlineManager.Client.OnUserDisconnected += OnUserDisconnected;
            OnlineManager.Client.OnUsersOnline += OnUsersOnline;
        }

        private void UnsubscribeFromEvents()
        {
            if (OnlineManager.Client == null) return;
            OnlineManager.Client.OnUserConnected -= OnUserConnected;
            OnlineManager.Client.OnUserDisconnected -= OnUserDisconnected;
            OnlineManager.Client.OnUsersOnline -= OnUsersOnline;
        }

        private void OnFriendsListUserChanged(object? sender, FriendsListUserChangedEventArgs e) => UpdateSizeAndText();

        private void OnOnlineStatusChanged(object? sender, BindableValueChangedEventArgs<ConnectionStatus> e)
        {
            if (e.Value == ConnectionStatus.Connected)
                SubscribeToEvents();
            else
                UnsubscribeFromEvents();

            UpdateSizeAndText();
        }

        private void OnUserConnected(object? sender, UserConnectedEventArgs e) => UpdateSizeAndText();
        private void OnUserDisconnected(object? sender, UserDisconnectedEventArgs e) => UpdateSizeAndText();
        private void OnUsersOnline(object? sender, UsersOnlineEventArgs e) => UpdateSizeAndText();

        private void UpdateSizeAndText()
        {
            var friendsCount = OnlineManager.FriendsList?.Count(id => OnlineManager.OnlineUsers.ContainsKey(id)) ?? 0;

            CountText.Text = $"{friendsCount}";

            var countWidth = CountText.Width;
            var labelWidth = LabelText.Width;
            var totalTextWidth = countWidth + labelWidth;

            var contentWidth = totalTextWidth + 30;

            Size = new ScalableVector2(contentWidth, 22);
            Background.Size = new ScalableVector2(contentWidth, 22);

            CountText.X = 15;
            LabelText.X = 15 + countWidth;
        }

        public override void Destroy()
        {
            OnlineManager.FriendsListUserChanged -= OnFriendsListUserChanged;
            OnlineManager.Status.ValueChanged -= OnOnlineStatusChanged;
            base.Destroy();
        }
    }
}
