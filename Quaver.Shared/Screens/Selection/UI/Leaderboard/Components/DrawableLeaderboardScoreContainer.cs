using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.API.Maps.Processors.Rating;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Quaver.Shared.Screens.Results;
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
using Wobble.Logging;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Components
{
    public class DrawableLeaderboardScoreContainer : Sprite
    {
        /// <summary>
        ///     The parent leaderboard score
        /// </summary>
        private DrawableLeaderboardScore Score { get; set; }

        /// <summary>
        ///     Whether the V2 layout is active
        /// </summary>
        private bool IsV2 => (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f;

        /// <summary>
        ///     The amount of padding from the left side that the elements will begin
        /// </summary>
        private int PaddingLeft => Score.IsPersonalBest ? 10 : 20;

        /// <summary>
        ///     Makes the leaderboard score clickable/hoverable
        /// </summary>
        private DrawableLeaderboardScoreButton Button { get; set; }

        /// <summary>
        ///     Displays the rank of the score
        /// </summary>
        private SpriteTextPlus? Rank { get; set; }

        /// <summary>
        ///     The container for the rank in V2
        /// </summary>
        private Container? RankContainer { get; set; }

        /// <summary>
        ///     The grade the user achieved on the score
        /// </summary>
        private Sprite Grade { get; set; }

        /// <summary>
        ///     The user's avatar from the score
        /// </summary>
        private SpriteAlphaMaskBlend Avatar { get; set; }

        /// <summary>
        ///     Static cache for masked avatar textures (LRU-like behavior managed by simple dictionary for now)
        ///     Key: SteamID, Value: Masked texture
        /// </summary>
        private static ConcurrentDictionary<ulong, Texture2D> MaskedAvatarCache { get; set; } = new ConcurrentDictionary<ulong, Texture2D>();

        /// <summary>
        ///     Displays the username of the player
        /// </summary>
        private SpriteTextPlus Username { get; set; }

        /// <summary>
        ///     Displays the performance rating of the score
        /// </summary>
        private SpriteTextPlus PerformanceRating { get; set; }

        /// <summary>
        ///     Displays the accuracy% and max combo the user achieved on the score
        /// </summary>
        private SpriteTextPlus AccuracyMaxCombo { get; set; }

        /// <summary>
        ///     Displays the mods the player used in the play
        /// </summary>
        private SpriteTextPlus Mods { get; set; }

        /// <summary>
        ///     The y position of the username
        /// </summary>
        private float UsernameY { get; } = 6;

        /// <summary>
        ///     A sprite displayed when the score can't be beaten with the activated mods
        /// </summary>
        private IconButton CantBeatAlert { get; set; }

        /// <summary>
        ///     Sprite displayed which tells the user what accuracy they need to achieve in order to beat the score
        ///     with their current mods
        /// </summary>
        private IconButton RequiredAccuracyAlert { get; set; }

        /// <summary>
        ///     The x position of <see cref="PerformanceRating"/>
        /// </summary>
        /// <summary>
        ///     Default X position for the rating text
        /// </summary>
        private const float DefaultRatingX = -12;

        /// <summary>
        ///     Spacing between the icon and the rating text
        /// </summary>
        private const float ElementSpacing = 10;

        /// <summary>
        ///     Height for the small icon (RatingLeft/RatingRight)
        /// </summary>
        private const float SmallIconHeight = 16;

        /// <summary>
        ///     Height for the large icon (RatingAccRight)
        /// </summary>
        private const float LargeIconHeight = 50;

        /// <summary>
        ///     Width for the large icon (RatingAccRight)
        /// </summary>
        private const float LargeIconWidth = 5;

        /// <summary>
        ///     The x position of <see cref="PerformanceRating"/>
        /// </summary>
        private float PerformanceRatingX => Score.IsPersonalBest ? -10 : DefaultRatingX;

        /// <summary>
        ///     Returns the background color of the table
        /// </summary>
        private Color BackgroundColor
        {
            get
            {
                if (Score.Index % 2 == 0)
                    return SkinManager.Skin.SongSelect.LeaderboardScoreColorOdd;

                return SkinManager.Skin.SongSelect.LeaderboardScoreColorEven;
            }
        }

        /// <summary>
        ///     Tooltip that displays when hovering over <see cref="CantBeatAlert"/>
        /// </summary>
        private Tooltip UnbeatableTooltip { get; set; }

        /// <summary>
        ///     An icon to represent the time the score was played at
        /// </summary>
        private Sprite Clock { get; set; }

        /// <summary>
        ///     How much time ago the score was submitted
        /// </summary>
        private SpriteTextPlus Time { get; set; }

        /// <summary>
        ///     The modifiers that the player is using on the score
        /// </summary>
        private List<DrawableModifier> Modifiers { get; set; }

        /// <summary>
        ///     Displays the flag of the user
        /// </summary>
        private Sprite Flag { get; set; }

        /// <summary>
        ///     A blank texture2D image
        /// </summary>
        private Texture2D BlankImage { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="score"></param>
        public DrawableLeaderboardScoreContainer(DrawableLeaderboardScore score)
        {
            Score = score;
            var isV2 = (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f;
            Size = new ScalableVector2(Score.GetScoreWidth(), isV2 ? 70 : 66);

            if (Score.Item.IsEmptyScore)
                return;

            CreateButton();
            CreateGrade();
            CreateAvatar();

            if (!Score.IsPersonalBest)
                CreateRankText();

            CreateFlag();
            CreateUsername();
            CreatePerformanceRating();
            CreateAccuracyMaxCombo();
            CreateMods();
            CreateCantBeatAlert();
            CreateRequiredAccuracyAlert();
            CreateTime();

            SteamManager.SteamUserAvatarLoaded += OnSteamAvatarLoaded;
            ModManager.ModsChanged += OnModsChanged;
            ConfigManager.LeaderboardRankedAccuracy.ValueChanged += OnAccuracyDisplayChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            PerformHoverAnimation(gameTime);
            ContainAlertIconClickableStatus();

            if (Button == null)
            {
                base.Update(gameTime);
                return;
            }

            // Failsafe: if the alerts/button think they are hovered, but the mouse is physically outside,
            // ensure the tooltips are deactivated. This fixes "sticky tooltips".
            // We only trigger this if the internal state is currently reported as hovered.
            var isStickyHovered = (Button.IsHovered && !Button.ScreenRectangle.Contains(MouseManager.CurrentState.Position)) ||
                                  (CantBeatAlert != null && CantBeatAlert.IsHovered && !CantBeatAlert.ScreenRectangle.Contains(MouseManager.CurrentState.Position)) ||
                                  (RequiredAccuracyAlert != null && RequiredAccuracyAlert.IsHovered && !RequiredAccuracyAlert.ScreenRectangle.Contains(MouseManager.CurrentState.Position));

            if (isStickyHovered)
            {
                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.DeactivateTooltip();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="score"></param>
        public void UpdateContent(DrawableLeaderboardScore score)
        {
            Score = score;

            AddScheduledUpdate(() =>
            {
                var isV2 = (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f;
                Size = new ScalableVector2(Score.GetScoreWidth(), isV2 ? 70 : 66);

                if (Button != null)
                    Button.Size = Size;

                Image = SkinManager.Skin?.SongSelect?.LeaderboardScoreMask ?? UserInterface.LeaderboardScoreMask;
                Tint = BackgroundColor;

                // Empty scores don't need to update its state
                if (Score.Item.IsEmptyScore)
                    return;

                Tint = (Button?.IsHovered ?? false) || (CantBeatAlert?.IsHovered ?? false) || (RequiredAccuracyAlert?.IsHovered ?? false)
                    ? SkinManager.Skin.SongSelect.LeaderboardScoreHoverColor : BackgroundColor;

                // Ranks don't show on PB scores.
                if (!Score.IsPersonalBest && Rank != null)
                    Rank.Text = $"{Score.Index + 1}.";

                Username.Text = $"{score.Item.Name}";

                if (score.Item.Name == ConfigManager.Username.Value)
                    Username.Tint = SkinManager.Skin.SongSelect.LeaderboardScoreUsernameSelfColor;
                else
                    Username.Tint = SkinManager.Skin.SongSelect.LeaderboardScoreUsernameOtherColor;

                PerformanceRating.Text = StringHelper.RatingToString(score.Item.PerformanceRating);

                UpdateAccuracyMode(score);

                UpdateTime();
                UpdateModifiers();
                UpdateAvatar();
                UpdateCantBeatAlert();
                UpdateRequiredAccuracyAlert();
                UpdateFlag();
            });
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            UnbeatableTooltip?.Destroy();
            BlankImage?.Dispose();

            // ReSharper disable once DelegateSubtraction
            SteamManager.SteamUserAvatarLoaded -= OnSteamAvatarLoaded;
            ModManager.ModsChanged -= OnModsChanged;
            ConfigManager.LeaderboardRankedAccuracy.ValueChanged -= OnAccuracyDisplayChanged;

            base.Destroy();
        }

        /// <summary>
        ///     Creates <see cref="Button"/>
        /// </summary>
        private void CreateButton()
        {
            Button = new DrawableLeaderboardScoreButton(Score.Container as LeaderboardScoresContainer, WobbleAssets.WhiteBox)
            {
                Parent = this,
                Size = Size,
                Alpha = 0,
                Depth = 1,
                UsePreviousSpriteBatchOptions = true
            };

            Button.Clicked += (sender, args) =>
            {
                if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                    return;

                var game = (QuaverGame)GameBase.Game;

                if (OnlineManager.CurrentGame != null)
                    return;

                game?.CurrentScreen?.Exit(() => new ResultsScreen(MapManager.Selected.Value, Score.Item));
            };

            Button.RightClicked += (sender, args) =>
            {
                if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                    return;

                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.ActivateRightClickOptions(new LeaderboardScoreRightClickOptions(Score.Item));
            };
        }

        /// <summary>
        ///     Creates <see cref="Rank"/>
        /// </summary>
        private void CreateRankText()
        {
            if (IsV2)
            {
                RankContainer = new Container
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    Size = new ScalableVector2(36, 36),
                    X = 10,
                };

                Rank = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "10.", 22)
                {
                    Parent = RankContainer,
                    Alignment = Alignment.MidCenter,
                    UsePreviousSpriteBatchOptions = true,
                    Alpha = 0,
                    Tint = SkinManager.Skin.SongSelect.LeaderboardScoreRankColor
                };

                return;
            }

            Rank = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "10.", 22)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = PaddingLeft,
                UsePreviousSpriteBatchOptions = true,
                Alpha = 0,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreRankColor
            };
        }

        /// <summary>
        ///     Creates <see cref="Avatar"/>
        /// </summary>
        private void CreateAvatar()
        {
            BlankImage = new Texture2D(GameBase.Game.GraphicsDevice, 1, 1);

            var mask = SkinManager.Skin?.SongSelect?.LeaderboardAvatarMask ?? UserInterface.LeaderboardAvatarMask;

            Avatar = new SpriteAlphaMaskBlend
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Grade.X + Grade.Width + (IsV2 ? 10 : 15),
                Size = new ScalableVector2(IsV2 ? 50 : 45, IsV2 ? 50 : 45),
                UsePreviousSpriteBatchOptions = true,
                Image = BlankImage,
                Alpha = 0
            };

            if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                Avatar.Size = new ScalableVector2(0, 0);
        }

        /// <summary>
        ///     Creates <see cref="Grade"/>
        /// </summary>
        private void CreateGrade()
        {
            var x = IsV2 ? 10 + 36 + 10 : 60;

            Grade = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(40, 40),
                X = Score.IsPersonalBest ? PaddingLeft : x,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates <see cref="Flag"/>
        /// </summary>
        private void CreateFlag()
        {
            var x = IsV2 ? Avatar.X + Avatar.Width + 10 : Avatar.X + Avatar.Width + PaddingLeft / 2f;

            Flag = new Sprite()
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Position = new ScalableVector2(x, UsernameY + 5),
                UsePreviousSpriteBatchOptions = true,
                Size = new ScalableVector2(24, 24),
                Image = Flags.Get("XX")
            };

            if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                Flag.Size = new ScalableVector2(0, 0);
        }

        /// <summary>
        ///     Creates <see cref="Username"/>
        /// </summary>
        private void CreateUsername()
        {
            var x = IsV2 ? Flag.X + Flag.Width + 5 : Flag.X + Flag.Width + PaddingLeft / 4f;

            Username = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Player", IsV2 ? 22 : 24)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Position = new ScalableVector2(x, UsernameY + 5),
                UsePreviousSpriteBatchOptions = true
            };

        }

        /// <summary>
        ///     Creates <see cref="PerformanceRating"/>
        /// </summary>
        private void CreatePerformanceRating()
        {
            PerformanceRating = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00.00", IsV2 ? 26 : 28)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = 11,
                X = PerformanceRatingX,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreRatingColor,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates <see cref="AccuracyMaxCombo"/>
        /// </summary>
        private void CreateAccuracyMaxCombo()
        {
            AccuracyMaxCombo = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00.00% | 0,000x", IsV2 ? 22 : 21)
            {
                Parent = this,
                Alignment = Alignment.BotRight,
                X = DefaultRatingX,
                Y = IsV2 ? -14 : -6,
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreAccuracyColor
            };
        }

        /// <summary>
        ///     Creates <see cref="Mods"/>
        /// </summary>
        private void CreateMods()
        {
            Mods = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 18)
            {
                Parent = this,
                Alignment = Alignment.BotLeft,
                X = Username.X,
                Y = -Username.Y,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates <see cref="CantBeatAlert"/>
        /// </summary>
        private void CreateCantBeatAlert()
        {
            var texture = SkinManager.Skin?.SongSelect?.LeaderboardWarning ?? UserInterface.LeaderboardWarning;
            var height = SmallIconHeight;

            CantBeatAlert = new FadeableButton(texture)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Size = new ScalableVector2(texture.Width * height / texture.Height, height),
                UsePreviousSpriteBatchOptions = true,
                Y = IsV2 ? 11 : PerformanceRating.Y + 5,
                X = PerformanceRatingX,
                Alpha = 0
            };

            UnbeatableTooltip = new Tooltip("You cannot beat this score with your currently activated modifiers!",
                Color.Crimson)
            {
                DestroyIfParentIsNull = false
            };

            CantBeatAlert.Hovered += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game.CurrentScreen.ActivateTooltip(UnbeatableTooltip);
            };

            CantBeatAlert.LeftHover += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game.CurrentScreen.DeactivateTooltip();
            };

            if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                CantBeatAlert.Size = new ScalableVector2(0, 0);
        }

        /// <summary>
        /// </summary>
        private void CreateRequiredAccuracyAlert()
        {
            var texture = SkinManager.Skin?.SongSelect?.LeaderboardInfo ?? UserInterface.LeaderboardInfo;
            var height = SmallIconHeight;

            RequiredAccuracyAlert = new FadeableButton(texture)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Size = new ScalableVector2(texture.Width * height / texture.Height, height),
                UsePreviousSpriteBatchOptions = true,
                Y = IsV2 ? 11 : PerformanceRating.Y + 5,
                X = PerformanceRatingX,
                Alpha = 0,
            };

            RequiredAccuracyAlert.Hovered += (sender, args) => ActivateRequiredAccuracyTooltip();

            RequiredAccuracyAlert.LeftHover += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game.CurrentScreen.DeactivateTooltip();
            };

            if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                RequiredAccuracyAlert.Size = new ScalableVector2(0, 0);
        }

        /// <summary>
        ///     Creates <see cref="Time"/>
        /// </summary>
        private void CreateTime()
        {
            Clock = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                UsePreviousSpriteBatchOptions = true,
                Image = UserInterface.LeaderboardClock,
                Size = new ScalableVector2(12, 12),
            };

            Time = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 18)
            {
                Parent = Clock,
                Alignment = Alignment.MidLeft,
                X = Clock.Width + 2,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Sets <see cref="Time"/>'s text to the correct time ago
        /// </summary>
        private void UpdateTime()
        {
            if (Time == null)
                return;

            var date = DateTime.Parse(Score.Item.DateTime);
            var timeDifference = DateTime.Now - date;

            Clock.Y = Username.Y + Username.Height / 2f - Clock.Height / 2f;
            Clock.X = Username.X + Username.Width + 8;

            Time.Parent = Clock;
            Time.Alignment = Alignment.MidLeft;
            Time.X = Clock.Width + 2;

            // Years
            if ((int)timeDifference.TotalDays > 365)
                Time.Text = $"{(int)(timeDifference.TotalDays / 365)}y";
            // Months
            else if ((int)timeDifference.TotalDays > 30)
                Time.Text = $"{(int)(timeDifference.TotalDays / 30)}mo";
            // Weeks
            else if ((int)timeDifference.TotalDays > 7)
                Time.Text = $"{(int)(timeDifference.TotalDays / 7)}w";
            // Days
            else if ((int)timeDifference.TotalDays > 0)
                Time.Text = $"{(int)timeDifference.TotalDays}d";
            // Hours
            else if ((int)timeDifference.TotalHours > 0)
                Time.Text = $"{(int)timeDifference.TotalHours}h";
            // Minutes
            else if ((int)timeDifference.TotalMinutes > 0)
                Time.Text = $"{(int)timeDifference.TotalMinutes}m";
            // Seconds
            else
            {
                var seconds = (int)timeDifference.TotalSeconds;

                if (seconds <= 0)
                    Time.Text = "now";
                else
                    Time.Text = $"{seconds}s";
            }


            Time.Tint = timeDifference.TotalMilliseconds < 86400000 ? ColorHelper.HexToColor("#6EF7F7") : ColorHelper.HexToColor("#808080");
            Clock.Tint = Time.Tint;
        }

        /// <summary>
        ///     Performs an animation when hovered over the button
        /// </summary>
        /// <param name="gameTime"></param>
        private void PerformHoverAnimation(GameTime gameTime)
        {
            if (Button == null)
                return;

            var isHovered = Button.IsHovered || (CantBeatAlert?.IsHovered ?? false) || (RequiredAccuracyAlert?.IsHovered ?? false);
            var color = isHovered ? SkinManager.Skin.SongSelect.LeaderboardScoreHoverColor : BackgroundColor;

            FadeToColor(color, gameTime.ElapsedGameTime.TotalMilliseconds, 30);

            if (Rank != null && Rank.Alpha < 1)
                Rank.Alpha += 0.05f * (float)gameTime.ElapsedGameTime.TotalMilliseconds / 10;
        }

        /// <summary>
        ///     Creates and updates <see cref="Modifiers"/>
        /// </summary>
        private void UpdateModifiers()
        {
            Modifiers?.ForEach(x => x.Destroy());
            Modifiers?.Clear();

            Modifiers = new List<DrawableModifier>();

            var modsList = ModManager.GetModsList((ModIdentifier)Score.Item.Mods);

            if (modsList.Count == 0)
                modsList.Add(ModIdentifier.None);

            for (var i = 0; i < modsList.Count; i++)
            {
                try
                {
                    int width = IsV2 ? 45 : 45;
                    int height = IsV2 ? 18 : 18;
                    int horizontalSpacing = IsV2 ? 5 : 5;
                    float startX = IsV2 ? Avatar.X + Avatar.Width + 10 : Flag.X;

                    var mod = new DrawableModifier(modsList[i])
                    {
                        Parent = this,
                        Alignment = Alignment.TopLeft,
                        X = startX + (width + horizontalSpacing) * Modifiers.Count,
                        Y = Flag.Y + Flag.Height + 4,
                        UsePreviousSpriteBatchOptions = true,
                        Size = new ScalableVector2(width, height),
                        Alpha = 1
                    };

                    if (modsList.Count > 5 && i != 0)
                    {
                        mod.X = startX + width * 0.70f * i;
                    }

                    Modifiers.Add(mod);
                }
                catch (Exception e)
                {
                    Logger.Error(e, LogType.Runtime);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <summary>
        /// </summary>
        private void UpdateAvatar()
        {
            var steamId = (ulong)Score.Item.SteamId;

            if (ConfigManager.LeaderboardSection?.Value == LeaderboardType.Local)
                steamId = SteamUser.GetSteamID().m_SteamID;

            lock (Avatar)
            {
                if (Score.IsPersonalBest && !Score.Item.IsOnline)
                {
                    var rawTexture = SteamManager.GetAvatarOrUnknown(steamId);

                    // If the avatar is already cached, use it to avoid redundant blending operations
                    if (MaskedAvatarCache.TryGetValue(steamId, out var cached))
                    {
                        Avatar.Image = cached;
                        Avatar.Alpha = 1;
                        return;
                    }

                    Avatar.Image = rawTexture;
                    Avatar.Alpha = 1;

                    // Capture local variable to ensure thread safety in the lambda
                    var textureToMask = rawTexture;

                    GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
                    {
                        if (textureToMask.IsDisposed)
                            return;

                        var mask = SkinManager.Skin?.SongSelect?.LeaderboardAvatarMask ?? UserInterface.LeaderboardAvatarMask;

                        // PerformBlend can return null or be invalid if device is lost, but usually returns a texture.
                        // We use textureToMask instead of Avatar.Image to ensure we are blending the INTENDED texture.

                        // Use Helper
                        var maskedTexture = AvatarMaskingHelper.PerformBlend(textureToMask, mask);

                        if (maskedTexture == null)
                            return;

                        if (!Avatar.IsDisposed)
                            Avatar.Image = maskedTexture;

                        MaskedAvatarCache.TryAdd(steamId, maskedTexture);
                    });

                    return;
                }

                if (SteamManager.UserAvatars.ContainsKey(steamId))
                {
                    var rawTexture = SteamManager.UserAvatars[steamId];

                    // Use cached if available
                    if (MaskedAvatarCache.TryGetValue(steamId, out var cached))
                    {
                        Avatar.Image = cached;
                        Avatar.Alpha = 0;
                        Avatar.ClearAnimations();
                        Avatar.FadeTo(1, Easing.Linear, 400);
                        return;
                    }

                    if (Avatar.Image == rawTexture)
                        return;

                    Avatar.Alpha = 0;
                    Avatar.ClearAnimations();
                    Avatar.FadeTo(1, Easing.Linear, 400);
                    Avatar.Image = rawTexture;

                    // Capture local variable
                    var textureToMask = rawTexture;

                    GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
                    {
                        if (textureToMask.IsDisposed)
                            return;

                        var mask = SkinManager.Skin?.SongSelect?.LeaderboardAvatarMask ?? UserInterface.LeaderboardAvatarMask;

                        // Use Helper
                        var maskedTexture = AvatarMaskingHelper.PerformBlend(textureToMask, mask);

                        if (maskedTexture == null)
                            return;

                        if (!Avatar.IsDisposed)
                            Avatar.Image = maskedTexture;

                        MaskedAvatarCache.TryAdd(steamId, maskedTexture);
                    });

                    return;
                }

                if (MaskedAvatarCache.TryGetValue(0, out var cachedPlaceholder))
                {
                    Avatar.Image = cachedPlaceholder;
                }
                else
                {
                    Avatar.Image = UserInterface.UnknownAvatar;

                    GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
                    {
                        var mask = SkinManager.Skin?.SongSelect?.LeaderboardAvatarMask ?? UserInterface.LeaderboardAvatarMask;
                        var maskedTexture = AvatarMaskingHelper.PerformBlend(UserInterface.UnknownAvatar, mask);

                        if (maskedTexture == null)
                            return;

                        if (!Avatar.IsDisposed)
                            Avatar.Image = maskedTexture;

                        MaskedAvatarCache.TryAdd(0, maskedTexture);
                    });
                }

                Avatar.ClearAnimations();
                Avatar.Alpha = 1;
            }

            SteamManager.SendAvatarRetrievalRequest(steamId);
        }

        /// <summary>
        ///     Updates the state of <see cref="CantBeatAlert"/>
        /// </summary>
        private void UpdateCantBeatAlert()
        {
            var pos = SkinManager.Skin?.SongSelect?.LeaderboardIconPosition ?? SkinLeaderboardIconPosition.RatingLeft;

            // Handle if it is impossible to beat this score with the currently activated mods
            if (new RatingProcessorKeys(MapManager.Selected.Value.DifficultyFromMods(ModManager.Mods)).CalculateRating(100) >=
                Score.Item.PerformanceRating)
            {
                CantBeatAlert.Visible = false;
            }
            else
            {
                UpdateLeaderboardIconLayout(CantBeatAlert, pos);
                CantBeatAlert.Visible = true;
            }
        }

        /// <summary>
        /// </summary>
        private void UpdateRequiredAccuracyAlert()
        {
            var pos = SkinManager.Skin?.SongSelect?.LeaderboardIconPosition ?? SkinLeaderboardIconPosition.RatingLeft;

            if (CantBeatAlert.Visible ||
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                ModHelper.GetRateFromMods(ModManager.Mods) == ModHelper.GetRateFromMods((ModIdentifier)Score.Item.Mods))
            {
                RequiredAccuracyAlert.Visible = false;

                // Reset text if no icons are visible at all (and CantBeatAlert is hidden)
                if (!CantBeatAlert.Visible)
                {
                    ResetTextLayout();
                }

                return;
            }

            UpdateLeaderboardIconLayout(RequiredAccuracyAlert, pos);
            RequiredAccuracyAlert.Visible = true;
        }

        /// <summary>
        ///     Updates the layout of the leaderboard icon and text based on the skin configuration.
        /// </summary>
        private void UpdateLeaderboardIconLayout(IconButton activeAlert, SkinLeaderboardIconPosition pos)
        {
            var iconWidth = 0f;
            var height = SmallIconHeight;

            if (pos == SkinLeaderboardIconPosition.RatingLeft)
            {
                // Reset Text Positions
                ResetTextLayout();

                height = SmallIconHeight;
                iconWidth = activeAlert.Image.Width * height / activeAlert.Image.Height;

                activeAlert.X = PerformanceRating.X - PerformanceRating.Width - ElementSpacing;
                activeAlert.Y = PerformanceRating.Y + 5;
                activeAlert.Height = height;
                activeAlert.Width = iconWidth;
            }
            else if (pos == SkinLeaderboardIconPosition.RatingRight)
            {
                height = SmallIconHeight;
                iconWidth = activeAlert.Image.Width * height / activeAlert.Image.Height;

                activeAlert.X = DefaultRatingX;
                activeAlert.Y = PerformanceRating.Y + 5;
                activeAlert.Height = height;
                activeAlert.Width = iconWidth;

                // Shift Text
                PerformanceRating.X = activeAlert.X - iconWidth - ElementSpacing;
                AccuracyMaxCombo.X = PerformanceRating.X;
            }
            else if (pos == SkinLeaderboardIconPosition.RatingAccRight)
            {
                height = LargeIconHeight;
                iconWidth = LargeIconWidth;

                activeAlert.X = DefaultRatingX;
                activeAlert.Y = (Height - height) / 2f;
                activeAlert.Height = height;
                activeAlert.Width = iconWidth;

                // Shift Text
                PerformanceRating.X = activeAlert.X - iconWidth - ElementSpacing;
                AccuracyMaxCombo.X = PerformanceRating.X;
            }
        }

        /// <summary>
        ///     Resets the text layout to default positions.
        /// </summary>
        private void ResetTextLayout()
        {
            PerformanceRating.X = PerformanceRatingX;
            AccuracyMaxCombo.X = PerformanceRatingX;
        }

        /// <summary>
        ///     Updates the state of <see cref="Flag"/>
        /// </summary>
        private void UpdateFlag()
        {
            // Get user's current country
            if (!Score.Item.IsOnline)
            {
                Flag.Image = Flags.Get("XX");
                return;
            }
            try
            {
                Flag.Image = Flags.Get(Score.Item.Country);
            }
            catch (Exception e)
            {
                Wobble.Logging.Logger.Error(e, LogType.Runtime);
                Flag.Image = Flags.Get("XX");
            }
        }

        /// <summary>
        ///     Called when a user's steam avatar has loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSteamAvatarLoaded(object? sender, SteamAvatarLoadedEventArgs e)
        {
            if (e.SteamId != (ulong)Score.Item.SteamId)
                return;

            lock (Avatar)
            {
                // Use cache if available
                if (MaskedAvatarCache.TryGetValue(e.SteamId, out var cached))
                {
                    Avatar.Image = cached;
                    Avatar.Alpha = 0;
                    Avatar.ClearAnimations();
                    Avatar.FadeTo(1, Easing.Linear, 400);
                    return;
                }

                Avatar.Alpha = 0;
                Avatar.ClearAnimations();
                Avatar.FadeTo(1, Easing.Linear, 400);
                Avatar.Image = e.Texture;

                // Capture local variable
                var textureToMask = e.Texture;

                GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
                {
                    if (textureToMask.IsDisposed)
                        return;

                    var mask = SkinManager.Skin?.SongSelect?.LeaderboardAvatarMask ?? UserInterface.LeaderboardAvatarMask;

                    var maskedTexture = AvatarMaskingHelper.PerformBlend(textureToMask, mask);

                    if (maskedTexture == null)
                        return;

                    if (!Avatar.IsDisposed)
                        Avatar.Image = maskedTexture;

                    MaskedAvatarCache.TryAdd(e.SteamId, maskedTexture);
                });
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object? sender, ModsChangedEventArgs e)
        {
            var game = GameBase.Game as QuaverGame;

            if (!RequiredAccuracyAlert.IsHovered || !RequiredAccuracyAlert.Visible)
                return;

            if (game?.CurrentScreen?.ActiveTooltip == UnbeatableTooltip)
                return;

            game?.CurrentScreen?.DeactivateTooltip();
            ActivateRequiredAccuracyTooltip();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccuracyDisplayChanged(object? sender, BindableValueChangedEventArgs<bool> e) =>
            AddScheduledUpdate(() => UpdateAccuracyMode(Score));

        /// <summary>
        /// Called when <see cref="AccuracyMaxCombo"/> and <see cref="Grade"> might need to be updated
        /// </summary>
        /// <param name="score"></param>
        private void UpdateAccuracyMode(DrawableLeaderboardScore score)
        {
            var acc = (float)(ConfigManager.LeaderboardSection.Value == LeaderboardType.Local &&
                ConfigManager.LeaderboardRankedAccuracy.Value
                    ? score.Item.RankedAccuracy
                    : score.Item.Accuracy);

            if (ConfigManager.LeaderboardSection.Value == LeaderboardType.Clan)
                AccuracyMaxCombo.Text = $"{StringHelper.AccuracyToString(acc)}";
            else
                AccuracyMaxCombo.Text = $"{score.Item.MaxCombo:N0}x | {StringHelper.AccuracyToString(acc)}";

            var grade = score.Item.Grade == API.Enums.Grade.F
                ? API.Enums.Grade.F
                : GradeHelper.GetGradeFromAccuracy(acc);
            Grade.Image = SkinManager.Skin?.Grades[grade] ?? UserInterface.Logo;
        }

        /// <summary>
        /// </summary>
        private void ActivateRequiredAccuracyTooltip()
        {
            var game = (QuaverGame)GameBase.Game;

            var processor = new RatingProcessorKeys(MapManager.Selected.Value.DifficultyFromMods(ModManager.Mods));

            var requiredAcc = processor.GetAccuracyFromRating(Score.Item.PerformanceRating);

            var tooltip = new Tooltip("In order to beat this score with your current modifiers,\n" +
                                      $"you must achieve higher than {StringHelper.AccuracyToString((float)requiredAcc)} accuracy.",
                ColorHelper.HexToColor("#5dc7f9"));

            game.CurrentScreen.ActivateTooltip(tooltip);
        }

        /// <summary>
        ///     Fades all of the objects in from zero
        /// </summary>
        public void FadeIn()
        {
            const int targetAlpha = 1;
            var time = 200;
            const Easing easing = Easing.Linear;

            Alpha = 0;

            if (!Score.Item.IsEmptyScore)
            {
                Username.Alpha = 0;
                Grade.Alpha = 0;
                Time.Alpha = 0;
                Clock.Alpha = 0;
                PerformanceRating.Alpha = 0;
                AccuracyMaxCombo.Alpha = 0;
                Flag.Alpha = 0;
                if (Rank != null)
                    Rank.Alpha = 0;
            }

            FadeTo(targetAlpha, easing, time);

            if (!Score.Item.IsEmptyScore)
            {
                Username.FadeTo(targetAlpha, easing, time);
                Grade.FadeTo(targetAlpha, easing, time);
                Time.FadeTo(targetAlpha, easing, time);
                Clock.FadeTo(targetAlpha, easing, time);
                PerformanceRating.FadeTo(targetAlpha, easing, time);
                AccuracyMaxCombo.FadeTo(targetAlpha, easing, time);
                Flag.FadeTo(targetAlpha, easing, time);
                if (Rank != null)
                    Rank.FadeTo(targetAlpha, easing, time);

                Modifiers?.ForEach(x =>
                {
                    x.Alpha = 0;
                    x.FadeTo(targetAlpha, easing, time);
                });
            }
        }

        /// <summary>
        ///     Makes sure that <see cref="CantBeatAlert"/> and <see cref="RequiredAccuracyAlert"/>
        ///     can only be clickable if the score is visible in the leaderboard
        /// </summary>
        private void ContainAlertIconClickableStatus()
        {
            if (Score.IsPersonalBest || Score.Item.IsEmptyScore)
                return;

            CantBeatAlert.IsClickable = CantBeatAlert.ScreenRectangle.Intersects(Score.Container.ScreenRectangle);
            RequiredAccuracyAlert.IsClickable = CantBeatAlert.IsClickable;
        }
    }
}
