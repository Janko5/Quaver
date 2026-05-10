using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.Server.Client;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Handlers;
using Quaver.Server.Client.Structures;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Components;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Playercards
{
    public class UserPlayercard : Sprite
    {
        // ── User data ──────────────────────────────────────────────
        private User User { get; set; }

        // ── Download Helpers ───────────────────────────────────────
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly ConcurrentDictionary<int, Texture2D> BackgroundCache = new ConcurrentDictionary<int, Texture2D>();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Quaver/1.0");
            return client;
        }

        // ── Background layers ──────────────────────────────────────

        /// <summary>
        ///     Container that wraps the 526×100 mask area. Buttons anchor here
        ///     so Alignment.BotRight is relative to the mask, not the full card.
        /// </summary>
        private Sprite MaskArea { get; set; }

        private Sprite BackgroundContent { get; set; }
        private Texture2D? MaskedBackgroundTexture { get; set; }

        private Sprite DarknessOverlay { get; set; }

        // ── Avatar ─────────────────────────────────────────────────
        private Sprite Avatar { get; set; }
        private Texture2D? MaskedAvatarTexture { get; set; }

        // ── User info ──────────────────────────────────────────────
        private Sprite Flag { get; set; }
        private SpriteTextPlus Username { get; set; }
        private SpriteTextPlus StatusText { get; set; }
        private Sprite RoleIcon { get; set; }

        // ── Bottom info panel ──────────────────────────────────────
        private Sprite InfoBackground { get; set; }
        private NineSliceSprite GlobalRankBackground { get; set; }
        private Sprite GlobalRankIcon { get; set; }
        private SpriteTextPlus GlobalRankText { get; set; }
        private NineSliceSprite OverallRatingBackground { get; set; }
        private Sprite OverallRatingIcon { get; set; }
        private SpriteTextPlus OverallRatingText { get; set; }
        private NineSliceSprite OverallAccuracyBackground { get; set; }
        private Sprite OverallAccuracyIcon { get; set; }
        private SpriteTextPlus OverallAccuracyText { get; set; }

        // ── Mode switch ────────────────────────────────────────────
        private IconButton ModeSwitch { get; set; }     // 58×20 gradient background (clickable)
        private Sprite ModeSwitchThumb { get; set; }    // 4K or 7K thumb

        // ── Action buttons ─────────────────────────────────────────
        public SquareButton LogoutButton { get; private set; }
        public SquareButton ViewProfileButton { get; private set; }

        // ── Layout constants ───────────────────────────────────────
        private const int CardWidth = 526;
        private const int MaskHeight = 100;
        private const int InfoHeight = 40;
        private const int InfoGap = 5;
        private const int CardHeight = MaskHeight + InfoGap + InfoHeight; // 145
        private const int AvatarSize = 80;
        private const int AvatarMargin = 10;
        // X where text column starts (after avatar + margin)
        private const int TextX = AvatarMargin + AvatarSize + 10;

        // ── Constructor ────────────────────────────────────────────

        public UserPlayercard(User user)
        {
            User = user;

            Size = new ScalableVector2(CardWidth, CardHeight);
            Image = UserInterface.BlankBox;
            Alpha = 0;
            SetChildrenAlpha = false;

            CreateMaskArea();
            CreateBackground();
            CreateDarknessOverlay();
            CreateAvatar();
            CreateFlag();
            CreateUsername();
            CreateStatusText();
            CreateRoleIcon();
            CreateInfoBackground();
            CreateGlobalRank();
            CreateOverallRating();
            CreateOverallAccuracy();
            CreateModeSwitch();
            CreateLogoutButton();
            CreateViewProfileButton();

            SetUsePreviousSpriteBatchOptionsRecursive(LogoutButton);
            SetUsePreviousSpriteBatchOptionsRecursive(ViewProfileButton);
            SetUsePreviousSpriteBatchOptionsRecursive(ModeSwitch);

            UpdateState();
            SubscribeToEvents();
            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
        }

        // ── Update / Destroy ───────────────────────────────────────

        public override void Update(GameTime gameTime)
        {
            if (OnlineManager.Self != User)
            {
                User = OnlineManager.Self;
                UpdateState();
            }

            base.Update(gameTime);
        }

        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;
            UnsubscribeFromEvents();

            MaskedBackgroundTexture?.Dispose();
            MaskedAvatarTexture?.Dispose();

            base.Destroy();
        }

        // ── Creation helpers ───────────────────────────────────────

        /// <summary>
        ///     Transparent sprite that covers only the 526×100 masked area.
        ///     Buttons are children of this so BotRight anchors correctly.
        /// </summary>
        private void CreateMaskArea()
        {
            MaskArea = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(CardWidth, MaskHeight),
                Image = UserInterface.BlankBox,
                Alpha = 0,
                SetChildrenAlpha = false,
            };
        }

        private void CreateBackground()
        {
            BackgroundContent = new Sprite
            {
                Parent = MaskArea,
                Size = new ScalableVector2(CardWidth, MaskHeight),
                Alignment = Alignment.TopLeft,
                Image = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private void CreateDarknessOverlay()
        {
            DarknessOverlay = new Sprite
            {
                Parent = MaskArea,
                Size = new ScalableVector2(CardWidth, MaskHeight),
                Alignment = Alignment.TopLeft,
                Image = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask,
                Tint = Color.Black,
                Alpha = 0.50f,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private void CreateAvatar()
        {
            Avatar = new Sprite
            {
                Parent = MaskArea,
                Alignment = Alignment.TopLeft,
                X = AvatarMargin,
                Y = AvatarMargin,
                Size = new ScalableVector2(AvatarSize, AvatarSize),
                Image = UserInterface.PlayercardAvatarOffline,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private void CreateFlag()
        {
            Flag = new Sprite
            {
                Parent = MaskArea,
                Alignment = Alignment.TopLeft,
                X = TextX,
                // Center vertically with the 22px text (Flag is 20px, so Y is +1px relative to text Y)
                Y = 12 + 1,
                Size = new ScalableVector2(20, 20),
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private void CreateUsername()
        {
            Username = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.LatoBlack), "", 22)
            {
                Parent = MaskArea,
                Alignment = Alignment.TopLeft,
                X = TextX + 20 + 5, // 20px flag + 5px gap
                Y = 12,
                UsePreviousSpriteBatchOptions = true,
            };
        }


        private void CreateStatusText()
        {
            StatusText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 20)
            {
                Parent = MaskArea,
                Alignment = Alignment.TopLeft,
                X = TextX,
                Y = 37, // Adjusted 7px higher 
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true,
            };
        }


        private void CreateRoleIcon()
        {
            RoleIcon = new Sprite
            {
                Parent = MaskArea,
                Alignment = Alignment.TopLeft,
                X = TextX,
                Y = 62, // Adjusted 12px higher
                Visible = false,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private static Texture2D _switchGradientTexture;

        /// <summary>
        ///     Returns (or generates once) a 4K-blue → 7K-purple gradient shaped to the switch background mask.
        ///     Technique is identical to <c>GameModeHelper.GetGradientTexture()</c>.
        /// </summary>
        private static Texture2D GetSwitchGradientTexture()
        {
            if (_switchGradientTexture != null)
                return _switchGradientTexture;

            var mask = UserInterface.PlayercardSwitchBackground;
            if (mask == null)
                return null;

            // 4K color = blue, 7K color = purple (from GameModeHelper.GetGameModeColor)
            var colorLeft = new Color(5, 135, 229);   // Keys4
            var colorRight = new Color(155, 81, 224);  // Keys7

            var width = mask.Width;
            var height = mask.Height;
            var data = new Color[width * height];
            mask.GetData(data);

            var newData = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var t = (float)x / Math.Max(width - 1, 1);
                    var blended = Color.Lerp(colorLeft, colorRight, t);
                    var maskPixel = data[y * width + x];
                    newData[y * width + x] = new Color(blended.R, blended.G, blended.B, maskPixel.A);
                }
            }

            _switchGradientTexture = new Texture2D(Wobble.GameBase.Game.GraphicsDevice, width, height);
            _switchGradientTexture.SetData(newData);
            return _switchGradientTexture;
        }

        private void CreateModeSwitch()
        {
            // Gradient background (58×20) — serves as the main clickable button
            ModeSwitch = new IconButton(GetSwitchGradientTexture() ?? UserInterface.PlayercardSwitchBackground)
            {
                Parent = InfoBackground,
                Alignment = Alignment.MidRight,
                X = -10,
                Size = new ScalableVector2(70, 24),
                UsePreviousSpriteBatchOptions = true,
                // Depth = -1 ensures this beats the transparent backdrop Button in LoggedInUserDropdown
                // (which covers the entire card at Depth=0 with a higher DrawOrder).
                // In Wobble ButtonManager: lower Depth = higher priority.
                Depth = -1,
            };

            var is4K = (ConfigManager.SelectedGameMode?.Value ?? GameMode.Keys4) == GameMode.Keys4;
            var thumbX = is4K ? 3 : 29;

            // Thumb — visual only, moves inside the switch
            ModeSwitchThumb = new Sprite
            {
                Parent = ModeSwitch,
                Alignment = Alignment.MidLeft,
                X = thumbX,
                Size = new ScalableVector2(38, 18),
                Image = is4K ? UserInterface.PlayercardSwitch4K : UserInterface.PlayercardSwitch7K,
                UsePreviousSpriteBatchOptions = true,
            };

            ModeSwitch.Clicked += (_, _) =>
            {
                var current = ConfigManager.SelectedGameMode?.Value ?? GameMode.Keys4;
                var next = current == GameMode.Keys4 ? GameMode.Keys7 : GameMode.Keys4;

                var targetX = next == GameMode.Keys4 ? 3 : 29;
                var nextImage = next == GameMode.Keys4 ? UserInterface.PlayercardSwitch4K : UserInterface.PlayercardSwitch7K;

                // Slide animation 150ms + image change
                ModeSwitchThumb.Animations.Clear();
                ModeSwitchThumb.Animations.Add(new Animation(AnimationProperty.X, Easing.OutQuint,
                    ModeSwitchThumb.X, targetX, 150));
                ModeSwitchThumb.Image = nextImage;

                if (ConfigManager.SelectedGameMode != null)
                    ConfigManager.SelectedGameMode.Value = next;
            };
        }

        private void UpdateModeSwitch()
        {
            if (ModeSwitchThumb == null || ModeSwitch == null) return;

            var is4K = (ConfigManager.SelectedGameMode?.Value ?? GameMode.Keys4) == GameMode.Keys4;
            var targetX = is4K ? 3 : 29;
            var nextImage = is4K ? UserInterface.PlayercardSwitch4K : UserInterface.PlayercardSwitch7K;

            ModeSwitchThumb.Animations.Clear();
            ModeSwitchThumb.Animations.Add(new Animation(AnimationProperty.X, Easing.OutQuint,
                ModeSwitchThumb.X, targetX, 150));
            ModeSwitchThumb.Image = nextImage;
        }

        private void CreateInfoBackground()
        {
            InfoBackground = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Y = MaskHeight + InfoGap,
                Size = new ScalableVector2(CardWidth, InfoHeight),
                Image = SkinManager.Skin?.MenuBorder?.PlayercardInfoBackground ?? UserInterface.PlayercardInfoBackground,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        private void CreateGlobalRank()
        {
            GlobalRankBackground = new NineSliceSprite(SkinManager.Skin.InfoBackground, new SliceMargins(15, 15, 0, 0))
            {
                Parent = InfoBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Height = 30,
                Tint = SkinManager.Skin.PlayercardInfoBackgroundColor,
                Visible = false,
                UsePreviousSpriteBatchOptions = true
            };

            GlobalRankIcon = new Sprite
            {
                Parent = GlobalRankBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Image = UserInterface.PlayercardGlobalRankIcon,
                Size = new ScalableVector2(23, 18),
                UsePreviousSpriteBatchOptions = true
            };

            GlobalRankText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 18)
            {
                Parent = GlobalRankBackground,
                Alignment = Alignment.MidLeft,
                X = 43,
                UsePreviousSpriteBatchOptions = true
            };
        }

        private void CreateOverallRating()
        {
            OverallRatingBackground = new NineSliceSprite(SkinManager.Skin.InfoBackground, new SliceMargins(15, 15, 0, 0))
            {
                Parent = InfoBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Height = 30,
                Tint = SkinManager.Skin.PlayercardInfoBackgroundColor,
                Visible = false,
                UsePreviousSpriteBatchOptions = true
            };

            OverallRatingIcon = new Sprite
            {
                Parent = OverallRatingBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Image = UserInterface.PlayercardOverallRatingIcon,
                Size = new ScalableVector2(24, 18),
                UsePreviousSpriteBatchOptions = true
            };

            OverallRatingText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 18)
            {
                Parent = OverallRatingBackground,
                Alignment = Alignment.MidLeft,
                X = 44,
                UsePreviousSpriteBatchOptions = true
            };
        }

        private void CreateOverallAccuracy()
        {
            OverallAccuracyBackground = new NineSliceSprite(SkinManager.Skin.InfoBackground, new SliceMargins(15, 15, 0, 0))
            {
                Parent = InfoBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Height = 30,
                Tint = SkinManager.Skin.PlayercardInfoBackgroundColor,
                Visible = false,
                UsePreviousSpriteBatchOptions = true
            };

            OverallAccuracyIcon = new Sprite
            {
                Parent = OverallAccuracyBackground,
                Alignment = Alignment.MidLeft,
                X = 10,
                Image = UserInterface.PlayercardOverallAccuracyIcon,
                Size = new ScalableVector2(18, 18),
                UsePreviousSpriteBatchOptions = true
            };

            OverallAccuracyText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 18)
            {
                Parent = OverallAccuracyBackground,
                Alignment = Alignment.MidLeft,
                X = 38,
                UsePreviousSpriteBatchOptions = true
            };
        }

        private void CreateLogoutButton()
        {
            var skin = SkinManager.Skin;

            LogoutButton = new SquareButton(UserInterface.PlayercardLogoutIcon, (_, _) =>
            {
                ThreadScheduler.Run(() => OnlineManager.Client?.Disconnect());
            })
            {
                Parent = MaskArea,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(30, 30),
                X = -10,
                Y = -10,
                ActiveColor = skin.ButtonNotActiveColor,
                InactiveColor = skin.ButtonNotActiveColor,
                HoverColor = skin.ButtonHoverColor,
                ContentColor = skin.ButtonContentColor,
            };
        }

        private void CreateViewProfileButton()
        {
            var skin = SkinManager.Skin;

            ViewProfileButton = new SquareButton(UserInterface.PlayercardViewProfileIcon, (_, _) =>
            {
                BrowserHelper.OpenURL($"https://quavergame.com/profile/{User?.OnlineUser?.Id}");
            })
            {
                Parent = MaskArea,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(30, 30),
                // 10px gap to the left of the logout button (30px) + 10px right margin + 10px extra
                X = -(10 + 30 + 10 + 10),
                Y = -10,
                ActiveColor = skin.ButtonNotActiveColor,
                InactiveColor = skin.ButtonNotActiveColor,
                HoverColor = skin.ButtonHoverColor,
                ContentColor = skin.ButtonContentColor,
            };
        }

        // ── Masking helpers ────────────────────────────────────────

        private void PerformBackgroundMasking(Texture2D bgTex = null)
        {
            var bgMask = SkinManager.Skin?.MenuBorder?.PlayercardBackgroundMask ?? UserInterface.PlayercardBackgroundMask;

            GameBase.Game.ScheduledRenderTargetDraws.Add(() =>
            {
                if (IsDisposed || bgMask.IsDisposed)
                    return;

                if (bgTex == null || bgTex.IsDisposed)
                    bgTex = UserInterface.PlayercardBackgroundDefault;

                var masked = AvatarMaskingHelper.PerformBackgroundBlend(bgTex, bgMask);
                if (masked == null)
                    return;

                var old = MaskedBackgroundTexture;
                MaskedBackgroundTexture = masked;
                BackgroundContent.Image = MaskedBackgroundTexture;
                old?.Dispose();
            });
        }

        private void PerformAvatarMasking(Texture2D avatarTex)
        {
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

        // ── State update ───────────────────────────────────────────

        private void UpdateState() => ScheduleUpdate(() =>
        {
            // ── Background ──────────────────────────────────────────
            if (User?.OnlineUser != null)
            {
                var userId = User.OnlineUser.Id;

                if (BackgroundCache.TryGetValue(userId, out var cachedBg))
                {
                    PerformBackgroundMasking(cachedBg);
                }
                else
                {
                    PerformBackgroundMasking(UserInterface.PlayercardBackgroundDefault);

                    Task.Run(async () =>
                    {
                        try
                        {
                            var response = await HttpClient.GetAsync($"https://cdn.quavergame.com/profile-covers/{userId}.jpg");
                            if (response.IsSuccessStatusCode)
                            {
                                using var stream = await response.Content.ReadAsStreamAsync();

                                // Create texture on the main thread when we have the MemoryStream
                                // FromStream needs to be called on a thread with GraphicsDevice but
                                // let's first copy to memory stream so we don't block
                                var ms = new MemoryStream();
                                await stream.CopyToAsync(ms);

                                ScheduleUpdate(() =>
                                {
                                    if (IsDisposed)
                                    {
                                        ms.Dispose();
                                        return;
                                    }

                                    try
                                    {
                                        ms.Position = 0;
                                        var tex = Texture2D.FromStream(GameBase.Game.GraphicsDevice, ms);
                                        BackgroundCache[userId] = tex;
                                        PerformBackgroundMasking(tex);
                                    }
                                    catch (Exception ex)
                                    {
                                        Wobble.Logging.Logger.Error(ex, Wobble.Logging.LogType.Runtime);
                                        PerformBackgroundMasking(UserInterface.PlayercardBackgroundDefault);
                                    }
                                    finally
                                    {
                                        ms.Dispose();
                                    }
                                });
                            }
                            else
                            {
                                // Don't permanently cache a failure, just set default for this instance
                                ScheduleUpdate(() =>
                                {
                                    if (!IsDisposed)
                                        PerformBackgroundMasking(UserInterface.PlayercardBackgroundDefault);
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Wobble.Logging.Logger.Error(e, Wobble.Logging.LogType.Network);
                            ScheduleUpdate(() =>
                            {
                                if (!IsDisposed)
                                    PerformBackgroundMasking(UserInterface.PlayercardBackgroundDefault);
                            });
                        }
                    });
                }
            }
            else
            {
                PerformBackgroundMasking(UserInterface.PlayercardBackgroundDefault);
            }

            // ── Avatar ──────────────────────────────────────────────
            if (User?.OnlineUser != null
                && SteamManager.UserAvatars != null
                && SteamManager.UserAvatars.ContainsKey((ulong)User.OnlineUser.SteamId))
            {
                PerformAvatarMasking(SteamManager.UserAvatars[(ulong)User.OnlineUser.SteamId]);
            }
            else
            {
                PerformAvatarMasking(UserInterface.PlayercardAvatarOffline);
            }

            // ── Flag ────────────────────────────────────────────────
            Flag.Image = User != null
                ? Flags.Get(User.OnlineUser.CountryFlag)
                : Flags.Get("XX");

            // ── Username ────────────────────────────────────────────
            var roleColor = Colors.GetUserChatColor(User?.OnlineUser?.UserGroups ?? UserGroups.Normal);
            Username.Text = User?.OnlineUser?.Username ?? "Player";
            Username.Tint = roleColor;

            // ── Status ──────────────────────────────────────────────
            StatusText.Text = GetStatusText();

            // ── Role icon ───────────────────────────────────────────
            var roleTexture = GetRoleIcon();
            if (roleTexture != null)
            {
                RoleIcon.Image = roleTexture;
                RoleIcon.Size = new ScalableVector2(roleTexture.Width, roleTexture.Height);
                RoleIcon.Visible = true;
            }
            else
            {
                RoleIcon.Visible = false;
            }

            // ── Stats (Global Rank & Overall Rating) ────────────────
            var currentMode = ConfigManager.SelectedGameMode.Value;

            if (User != null && User.Stats.TryGetValue(currentMode, out var stats))
            {
                if (stats.Rank > 0)
                {
                    GlobalRankText.Text = $"#{stats.Rank:n0}";
                    var textWidth = GlobalRankText.Font.Store.MeasureString(GlobalRankText.Text).X;
                    GlobalRankBackground.Width = 10 + 23 + 10 + textWidth + 10;
                    GlobalRankBackground.Visible = true;

                    OverallRatingBackground.X = GlobalRankBackground.X + GlobalRankBackground.Width + 10;
                }
                else
                {
                    GlobalRankBackground.Visible = false;
                    OverallRatingBackground.X = 10;
                }

                OverallRatingText.Text = stats.OverallPerformanceRating.ToString("N2");
                var ratingWidth = OverallRatingText.Font.Store.MeasureString(OverallRatingText.Text).X;
                OverallRatingBackground.Width = 10 + 24 + 10 + ratingWidth + 10;
                OverallRatingBackground.Visible = true;

                OverallAccuracyBackground.X = OverallRatingBackground.X + OverallRatingBackground.Width + 10;
                OverallAccuracyText.Text = $"{stats.OverallAccuracy:N2}%";
                var accuracyWidth = OverallAccuracyText.Font.Store.MeasureString(OverallAccuracyText.Text).X;
                OverallAccuracyBackground.Width = 10 + 18 + 10 + accuracyWidth + 10;
                OverallAccuracyBackground.Visible = true;
            }
            else
            {
                GlobalRankBackground.Visible = false;
                OverallRatingBackground.Visible = false;
                OverallAccuracyBackground.Visible = false;
            }
        });

        // ── Role / status helpers ──────────────────────────────────

        private Texture2D? GetRoleIcon()
        {
            var groups = User?.OnlineUser?.UserGroups ?? UserGroups.Normal;

            // Priority order matches Colors.GetUserChatColor for consistency
            if (groups.HasFlag(UserGroups.Developer)) return UserInterface.PlayercardRoleDeveloper;
            if (groups.HasFlag(UserGroups.GraphicDesigner)) return UserInterface.PlayercardRoleGraphicDesigner;
            if (groups.HasFlag(UserGroups.Admin)) return UserInterface.PlayercardRoleAdministrator;
            if (groups.HasFlag(UserGroups.Moderator)) return UserInterface.PlayercardRoleModerator;
            if (groups.HasFlag(UserGroups.RankingSupervisor)) return UserInterface.PlayercardRoleRankingSupervisor;
            if (groups.HasFlag(UserGroups.TrialRankingSupervisor)) return UserInterface.PlayercardRoleTRS;
            if (groups.HasFlag(UserGroups.Contributor)) return UserInterface.PlayercardRoleContributor;

            // No role icon for Normal, Donator, Bot, Swan
            return null;
        }

        private string GetStatusText()
        {
            if (User?.CurrentStatus == null)
                return "Idle";

            return User.CurrentStatus.Status switch
            {
                ClientStatus.InMenus => "Idle",
                ClientStatus.Selecting => "Selecting a song",
                ClientStatus.Playing => "Playing",
                ClientStatus.Paused => "Playing",
                ClientStatus.Watching => "Watching a replay",
                ClientStatus.Editing => "Editing",
                ClientStatus.InLobby => "Multiplayer Lobby",
                ClientStatus.Multiplayer => "Playing multiplayer",
                ClientStatus.Listening => "Listening",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // ── Event subscriptions ────────────────────────────────────

        private void OnConnectionStatusChanged(object sender, BindableValueChangedEventArgs<ConnectionStatus> e)
        {
            if (e.Value == ConnectionStatus.Connected)
            {
                UnsubscribeFromEvents();
                SubscribeToEvents();
            }
        }

        private void OnLoginSuccess(object sender, LoginReplyEventArgs e)
        {
            User = e.Self;
            UpdateState();
        }

        private void OnUserStatusReceived(object sender, UserStatusEventArgs e) => UpdateState();

        private void SubscribeToEvents()
        {
            if (OnlineManager.Client == null) return;
            OnlineManager.Client.OnLoginSuccess += OnLoginSuccess;
            OnlineManager.Client.OnUserStatusReceived += OnUserStatusReceived;
            if (ConfigManager.SelectedGameMode != null)
                ConfigManager.SelectedGameMode.ValueChanged += OnSelectedGameModeChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (OnlineManager.Client == null) return;
            OnlineManager.Client.OnLoginSuccess -= OnLoginSuccess;
            OnlineManager.Client.OnUserStatusReceived -= OnUserStatusReceived;
            if (ConfigManager.SelectedGameMode != null)
                ConfigManager.SelectedGameMode.ValueChanged -= OnSelectedGameModeChanged;
        }

        // ── Utility ────────────────────────────────────────────────

        /// <summary>
        ///     Recursively sets <see cref="Wobble.Graphics.Drawable.UsePreviousSpriteBatchOptions"/>
        ///     to true on <paramref name="root"/> and all descendants, so every node in a
        ///     <see cref="SquareButton"/>'s tree (including its private iconContainer) respects
        ///     the parent ScrollContainer's scissor SpriteBatch.
        /// </summary>
        private static void SetUsePreviousSpriteBatchOptionsRecursive(Wobble.Graphics.Drawable root)
        {
            root.UsePreviousSpriteBatchOptions = true;
            foreach (var child in root.Children)
                SetUsePreviousSpriteBatchOptionsRecursive(child);
        }

        private void OnSelectedGameModeChanged(object sender, BindableValueChangedEventArgs<GameMode> e)
        {
            ScheduleUpdate(UpdateModeSwitch);
            ScheduleUpdate(UpdateState);
        }
    }
}