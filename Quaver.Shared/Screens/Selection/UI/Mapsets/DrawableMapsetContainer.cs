using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public class DrawableMapsetContainer : Sprite
    {
        /// <summary>
        ///     The parent mapset
        /// </summary>
        public DrawableMapset ParentMapset { get; }

        /// <summary>
        ///     The button/clickable area of the mapset
        /// </summary>
        private ImageButton Button { get; set; }

        /// <summary>
        ///     Displays the map background/banner for the mapset
        /// </summary>
        private DrawableBanner Banner { get; set; }

        /// <summary>
        ///     The title of the map
        /// </summary>
        private SpriteTextPlus _title;
        public SpriteTextPlus Title { get => MarqueeTitle != null ? MarqueeTitle.TextSprite : _title; set => _title = value; }

        /// <summary>
        ///     Displays the artist of the song
        /// </summary>
        private SpriteTextPlus _artist;
        private SpriteTextPlus Artist { get => MarqueeArtist != null ? MarqueeArtist.TextSprite : _artist; set => _artist = value; }

        /// <summary>
        ///     Marquee wrappers for Title and Artist (V2 only)
        /// </summary>
        private MarqueeSpriteText MarqueeTitle { get; set; }
        private MarqueeSpriteText MarqueeArtist { get; set; }

        /// <summary>
        ///     The divider line between <see cref="Artist"/> and <see cref="Creator"/>
        /// </summary>
        private SpriteTextPlus DividerLine { get; set; }

        /// <summary>
        ///    Displays the creator of the map
        /// </summary>
        private SpriteTextPlus Creator { get; set; }

        /// <summary>
        ///     The amount of x axis spacing between the artist and creator
        /// </summary>
        private const int ArtistCreatorSpacingX = 4;

        /// <summary>
        ///     The X position of the title/first element
        /// </summary>
        private int TitleX => SkinManager.Skin.SongSelect.MapsetPanelMarginLeft;

        /// <summary>
        ///     The ranked status of the map
        /// </summary>
        private Sprite RankedStatusSprite { get; set; }

        /// <summary>
        ///     The game modes the mapset has
        /// </summary>
        private Sprite GameModes { get; set; }

        /// <summary>
        ///    The game mode text
        /// </summary>
        private SpriteTextPlus GameModeText { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus ByText { get; set; }

        /// <summary>
        ///     When the mapsets are sorted by difficulty/grade achieved
        ///     this will display the difficulty rating & name to make it act as an individual map
        /// </summary>
        public SpriteTextPlus DifficultyName { get; private set; }

        /// <summary>
        ///     The highest online grade that the user has achieved
        /// </summary>
        public Sprite OnlineGrade { get; set; }

        private bool _isCached = true;
        public bool IsCached
        {
            get => _isCached;
            set
            {
                if (value == _isCached)
                    return;

                SetCaching(value);
                _isCached = value;
            }
        }

        /// <summary>Container for BPM overlay on banner</summary>
        private NineSliceSprite BpmOverlay { get; set; }

        /// <summary>Container for Length overlay on banner</summary>
        private NineSliceSprite LengthOverlay { get; set; }

        private SpriteTextPlus BpmText { get; set; }
        private SpriteTextPlus LengthText { get; set; }
        private Sprite BpmIcon { get; set; }
        private Sprite LengthIcon { get; set; }

        // Performance optimization: cache icon widths (constant values)
        private float _bpmIconWidth;
        private float _lengthIconWidth;

        // Performance optimization: flag to track if overlay texts need recalculation
        private bool _needsOverlayUpdate = true;

        private int _notHoveredFrames;

        /// <summary>
        ///     Duration in milliseconds for all fade animations (select/deselect/overlays)
        /// </summary>
        private const int FadeAnimationDuration = 250;


        /// <summary>
        /// </summary>
        /// <param name="mapset"></param>
        public DrawableMapsetContainer(DrawableMapset mapset)
        {
            ParentMapset = mapset;
            Parent = ParentMapset;

            // Check skin version for design selection
            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                InitializeLayoutV2();
            }
            else
            {
                InitializeLayoutV1();
            }

            // Subscribe to mod changes for real-time BPM/Length overlay updates
            ModManager.ModsChanged += OnModsChanged;

            UsePreviousSpriteBatchOptions = true;
        }

        /// <summary>
        ///    Initializes the layout for version 1.0 (Original design)
        /// </summary>
        private void InitializeLayoutV1()
        {
            Size = new ScalableVector2(1188, 86);

            CreateButton();
            CreateTitle();
            CreateArtist();
            CreateDifficultyName();
            CreateDividerLine();
            CreateCreator();
            CreateBannerImage();
            CreateRankedStatus();
            CreateGameModes();
            CreateOnlineGrade();
            CreateBpmOverlay();
            CreateLengthOverlay();
        }

        /// <summary>
        ///    Initializes the layout for version 2.0 (New design)
        /// </summary>
        private void InitializeLayoutV2()
        {
            Size = new ScalableVector2(900, 100);

            CreateButton();
            CreateTitle();
            CreateArtist();
            CreateDifficultyName();
            CreateDividerLine();
            CreateCreator();
            CreateBannerImage();
            CreateRankedStatus();
            CreateGameModes();
            CreateOnlineGrade();
            CreateBpmOverlay();
            CreateLengthOverlay();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            ModManager.ModsChanged -= OnModsChanged;
            base.Destroy();
        }

        /// <summary>
        ///     Called when mods change - update BPM/Length overlay values
        /// </summary>
        private void OnModsChanged(object sender, ModsChangedEventArgs e)
        {
            // Performance: mark overlays as needing update
            _needsOverlayUpdate = true;

            // If overlays are currently visible (mapset selected/hovered), update immediately
            if (BpmOverlay.Visible)
                UpdateOverlayTexts();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Performance: only update width if it actually changed
            if (Math.Abs(Button.Width - Width) > 0.01f)
                Button.Width = Width;

            PerformHoverAnimation();
            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public void UpdateContent(Mapset item, int index)
        {
            Creator.Text = $"{item.Creator}";

            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                UpdateContentV2(item, index);
            }
            else
            {
                UpdateContentV1(item, index);
            }

            RankedStatusSprite.Image = GetRankedStatusImage();

            var modes = item.AllModesInSet.Count > 0 ? item.AllModesInSet : item.Maps.Select(x => x.Mode);
            GameModeHelper.SetGameModeTexture(modes, GameModes, GameModeText);

            if (ParentMapset.IsSelected)
                Select(true);
            else
                Deselect(true);

            Banner.UpdateContent(ParentMapset);

            // Performance: calculate overlay texts here so they're ready for first hover/select
            UpdateOverlayTexts();
        }

        /// <summary>
        ///     Updates content for V1 layout
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        private void UpdateContentV1(Mapset item, int index)
        {
            Title.Y = 18;
            Artist.Y = 54;
            DifficultyName.Y = 54;
            DividerLine.Y = 54;
            ByText.Y = 54;
            Creator.Y = 54;

            if (item.Maps.Count == 1)
            {
                Title.FontSize = 22;
                Title.Text = item.Title;

                var map = ParentMapset.Item.Maps.First();
                Artist.Text = $"{item.Artist}";
                Artist.Visible = true;

                if (map.OnlineGrade != Grade.None)
                {
                    const int width = 40;

                    OnlineGrade.Visible = true;
                    OnlineGrade.Image = SkinManager.Skin.Grades[map.OnlineGrade];
                    OnlineGrade.Size = new ScalableVector2(width, OnlineGrade.Image.Height / OnlineGrade.Image.Width * width);
                    OnlineGrade.Y = 12;

                    Title.X = OnlineGrade.X + OnlineGrade.Width + 16;
                    Title.TruncateWithEllipsis(250 - (int)OnlineGrade.Width - 16);
                    Artist.TruncateWithEllipsis(250 - (int)OnlineGrade.Width - 16);
                    Artist.X = Title.X;
                }
                else
                {
                    Title.X = TitleX;
                    Title.TruncateWithEllipsis(250);
                    Artist.TruncateWithEllipsis(250);
                    Artist.X = TitleX;
                    OnlineGrade.Visible = false;
                }

                DividerLine.X = Artist.X + Artist.Width + ArtistCreatorSpacingX;
                DifficultyName.Visible = false;
            }
            else
            {
                Title.FontSize = 26;
                Title.Text = item.Title;
                Title.TruncateWithEllipsis(250);

                Artist.Text = $"{item.Artist}";
                Artist.TruncateWithEllipsis(250);

                Title.X = TitleX;
                Artist.X = TitleX;
                DividerLine.X = Artist.X + Artist.Width + ArtistCreatorSpacingX;
                Artist.Visible = true;
                OnlineGrade.Visible = false;
                DifficultyName.Visible = false;
            }

            ByText.X = DividerLine.X + DividerLine.Width + ArtistCreatorSpacingX;
            Creator.X = ByText.X + ByText.Width + ArtistCreatorSpacingX;

            // Truncate Creator to avoid overlapping with GameMode/Status icons (starting around X=515)
            var availableCreatorWidth = 500 - Creator.X;
            if (availableCreatorWidth > 0)
                Creator.TruncateWithEllipsis((int)availableCreatorWidth);
        }

        /// <summary>
        ///     Updates content for V2 layout
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        private void UpdateContentV2(Mapset item, int index)
        {
            // V2 Layout: 3 Rows
            // Row 1: Title (26px font)
            Title.FontSize = 26;
            Title.Text = item.Title;

            if (MarqueeTitle != null)
            {
                MarqueeTitle.Y = 13;
                MarqueeTitle.X = TitleX;
            }
            else
            {
                Title.Y = 13;
                Title.X = TitleX;
                Title.TruncateWithEllipsis(270);
            }

            // Row 2: Artist (22px font)
            Artist.FontSize = 22;
            Artist.Text = $"{item.Artist}";

            if (MarqueeArtist != null)
            {
                MarqueeArtist.Y = 42;
                MarqueeArtist.X = TitleX;
                MarqueeArtist.Visible = true;
            }
            else
            {
                Artist.Y = 42;
                Artist.X = TitleX;
                Artist.Visible = true;
                Artist.TruncateWithEllipsis(270);
            }

            // Row 3: Creator (18px font)
            ByText.FontSize = 18;
            ByText.Y = 69;
            ByText.X = TitleX;

            Creator.FontSize = 18;
            Creator.Y = 68;
            Creator.X = ByText.X + ByText.Width + ArtistCreatorSpacingX;

            // Truncate Creator to avoid overlapping icons (starting around X=288 in V2)
            var availableCreatorWidth = 270 - Creator.X;
            if (availableCreatorWidth > 0)
                Creator.TruncateWithEllipsis((int)availableCreatorWidth);

            DividerLine.Visible = false;
            DifficultyName.Visible = false; // Usually hidden in default V2 view unless single-diff sorted
            OnlineGrade.Visible = false;
        }

        /// <summary>
        ///     Creates <see cref="Button"/>
        /// </summary>
        private void CreateButton()
        {
            var container = (SongSelectContainer<Mapset>)ParentMapset.Container;

            Button = new SongSelectContainerButton(SkinManager.Skin?.SongSelect?.MapsetHovered ?? UserInterface.MapsetHovered, container.ClickableArea)
            {
                Parent = this,
                Size = Size,
                Alpha = 0,
                Alignment = Alignment.MidCenter,
                UsePreviousSpriteBatchOptions = true,
                Depth = 1
            };

            Button.Clicked += (sender, args) => OnMapsetClicked();

            Button.RightClicked += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;

                if (ParentMapset.Item.Maps.Count == 1)
                    game?.CurrentScreen?.ActivateRightClickOptions(new MapRightClickOptions(ParentMapset));
                else
                    game?.CurrentScreen?.ActivateRightClickOptions(new MapsetRightClickOptions(ParentMapset));
            };

            // Show overlays on hover (if not already selected)
            Button.Hovered += (sender, args) => OnMapsetHovered();

            // Hide overlays when leaving hover (if not selected)
            Button.LeftHover += (sender, args) => OnMapsetLeftHover();
        }

        /// <summary>
        ///    Creates <see cref="Banner"/>
        /// </summary>
        private void CreateBannerImage()
        {
            var bannerSize = SkinManager.Skin.SongSelect.MapsetPanelBannerSize;

            Banner = new DrawableBanner(ParentMapset)
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = bannerSize,
                X = -SkinManager.Skin.SongSelect.MapsetPanelMarginRight,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates <see cref="Title"/>
        /// </summary>
        private void CreateTitle()
        {
            var font = FontManager.GetWobbleFont(Fonts.InterBold);
            var titleTint = SkinManager.Skin.SongSelect.MapsetPanelSongTitleColor;

            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                MarqueeTitle = new MarqueeSpriteText(font, "", 26, 350)
                {
                    Parent = this,
                    UsePreviousSpriteBatchOptions = true
                };
                MarqueeTitle.TextSprite.Tint = titleTint;
            }
            else
            {
                _title = new SpriteTextPlus(font, "", 22)
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    UsePreviousSpriteBatchOptions = true,
                    Tint = titleTint
                };
            }
        }

        /// <summary>
        ///     Creates <see cref="Artist"/>
        /// </summary>
        private void CreateArtist()
        {
            var font = FontManager.GetWobbleFont(Fonts.InterBold);
            var artistTint = SkinManager.Skin.SongSelect.MapsetPanelSongArtistColor;

            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                MarqueeArtist = new MarqueeSpriteText(font, "", 22, 350)
                {
                    Parent = this,
                    UsePreviousSpriteBatchOptions = true
                };
                MarqueeArtist.TextSprite.Tint = artistTint;
            }
            else
            {
                _artist = new SpriteTextPlus(font, "", 18)
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    UsePreviousSpriteBatchOptions = true,
                    Tint = artistTint
                };
            }
        }

        /// <summary>
        ///     Creates <see cref="DividerLine"/>
        /// </summary>
        private void CreateDividerLine()
        {
            DividerLine = new SpriteTextPlus(Artist.Font, "|", Artist.FontSize)
            {
                Parent = this,
                Position = new ScalableVector2(Artist.X + Artist.Width + ArtistCreatorSpacingX, Artist.Y),
                Tint = ColorHelper.HexToColor("#808080"),
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates <see cref="Creator"/>
        /// </summary>
        private void CreateCreator()
        {
            ByText = new SpriteTextPlus(Title.Font, "By:", Artist.FontSize)
            {
                Parent = this,
                Position = new ScalableVector2(DividerLine.X + DividerLine.Width + ArtistCreatorSpacingX, Artist.Y),
                Tint = SkinManager.Skin.SongSelect.MapsetPanelByColor,
                UsePreviousSpriteBatchOptions = true
            };

            Creator = new SpriteTextPlus(Title.Font, "Creator", Artist.FontSize)
            {
                Parent = this,
                Position = new ScalableVector2(ByText.X + ByText.Width + ArtistCreatorSpacingX, Artist.Y),
                Tint = SkinManager.Skin.SongSelect.MapsetPanelCreatorColor,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates <see cref="RankedStatusSprite"/>
        /// </summary>
        private void CreateRankedStatus()
        {
            RankedStatusSprite = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(124, 30),
                X = Banner.X - Banner.Width + SkinManager.Skin.SongSelect.RankedStatusPosOffsetX,
                Y = SkinManager.Skin.SongSelect.RankedStatusPosOffsetY,
                Image = UserInterface.StatusPanel,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        private void CreateGameModes()
        {
            GameModes = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(90, 30),
                X = RankedStatusSprite.X - RankedStatusSprite.Width + SkinManager.Skin.SongSelect.GameModePosOffsetX,
                Y = SkinManager.Skin.SongSelect.GameModePosOffsetY,
                UsePreviousSpriteBatchOptions = true
            };

            GameModeText = new SpriteTextPlus(Title.Font, "", 16)
            {
                Parent = GameModes,
                Alignment = Alignment.MidCenter,
                UsePreviousSpriteBatchOptions = true,
                Tint = Color.White,
            };
        }

        /// <summary>
        /// </summary>
        private void CreateOnlineGrade()
        {
            OnlineGrade = new Sprite()
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Visible = false,
                Alpha = 0,
                X = TitleX,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        /// </summary>
        private void CreateDifficultyName()
        {
            DifficultyName = new SpriteTextPlus(Title.Font, "Difficulty", 20)
            {
                Parent = this,
                Position = new ScalableVector2(Title.X, Artist.Y),
                UsePreviousSpriteBatchOptions = true,
                Alpha = 0
            };
        }

        /// <summary>
        ///     Retrieves the color of a map's ranked status
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Texture2D GetRankedStatusImage()
        {
            if (ParentMapset.Item.Maps.First().Game != MapGame.Quaver)
            {
                switch (ParentMapset.Item.Maps.First().Game)
                {
                    case MapGame.Osu:
                        return SkinManager.Skin?.SongSelect?.StatusOsu ?? UserInterface.StatusOtherGameOsu;
                    case MapGame.Etterna:
                        return SkinManager.Skin?.SongSelect?.StatusStepmania ?? UserInterface.StatusOtherGameEtterna;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            switch (ParentMapset.Item.Maps.Max(x => x.RankedStatus))
            {
                case RankedStatus.NotSubmitted:
                    return SkinManager.Skin?.SongSelect?.StatusNotSubmitted ?? UserInterface.StatusNotSubmitted;
                case RankedStatus.Unranked:
                    return SkinManager.Skin?.SongSelect?.StatusUnranked ?? UserInterface.StatusUnranked;
                case RankedStatus.Ranked:
                    return SkinManager.Skin?.SongSelect?.StatusRanked ?? UserInterface.StatusRanked;
                case RankedStatus.DanCourse:
                    return SkinManager.Skin?.SongSelect?.StatusNotSubmitted ?? UserInterface.StatusNotSubmitted;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Checks for sticky hover and triggers LeftHover if needed
        /// </summary>
        private void PerformHoverAnimation()
        {
            if (Button == null)
                return;

            // Manual hover check for maximum reliability.
            // ScreenRectangle is updated in Draw, but usually contains valid data during Update for elements in view.
            var isActuallyHovered = Button.IsHovered && Button.ScreenRectangle.Contains(MouseManager.CurrentState.Position);

            // Failsafe: if the button thinks it's hovered but physically isn't (sticky hover),
            // trigger the removal of hovered effects. Only check this if currently not selected.
            if (!ParentMapset.IsSelected && Button.IsHovered && !isActuallyHovered)
            {
                _notHoveredFrames++;

                if (_notHoveredFrames > 2)
                {
                    if ((MarqueeTitle?.IsActive ?? false) || (MarqueeArtist?.IsActive ?? false) || BpmOverlay.Alpha > 0)
                        OnMapsetLeftHover();
                }
            }
            else
            {
                _notHoveredFrames = 0;
            }
        }

        /// <summary>
        /// </summary>
        public void Select(bool instantSizeChange = false)
        {
            Image = SkinManager.Skin?.SongSelect?.MapsetSelected ?? UserInterface.SelectedMapset;

            var fade = 1f;

            if (MarqueeTitle != null) MarqueeTitle.IsActive = true;
            if (MarqueeArtist != null) MarqueeArtist.IsActive = true;

            if (instantSizeChange)
            {
                // Skip animations during recycling — they'd be overwritten immediately
                Title.Alpha = fade;
                Artist.Alpha = fade;
                DividerLine.Alpha = fade;
                Creator.Alpha = fade;
                ByText.Alpha = fade;
                RankedStatusSprite.Alpha = fade;
                GameModes.Alpha = fade;

                if (ParentMapset.Item.Maps.Count == 1)
                {
                    DifficultyName.Alpha = fade;
                    OnlineGrade.Alpha = fade;
                }

                if (Banner.HasBannerLoaded)
                    Banner.Alpha = 1;

                if (_needsOverlayUpdate)
                    UpdateOverlayTexts();

                FadeInOverlays();

                if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
                    Width = 950;
                else
                    Width = ParentMapset.Width;
            }
            else
            {
                var time = FadeAnimationDuration;

                Title.ClearAnimations();
                Title.FadeTo(fade, Easing.Linear, time);

                Artist.ClearAnimations();
                Artist.FadeTo(fade, Easing.Linear, time);

                DividerLine.ClearAnimations();
                DividerLine.FadeTo(fade, Easing.Linear, time);

                Creator.ClearAnimations();
                Creator.FadeTo(fade, Easing.Linear, time);

                ByText.ClearAnimations();
                ByText.FadeTo(fade, Easing.Linear, time);

                RankedStatusSprite.ClearAnimations();
                RankedStatusSprite.FadeTo(fade, Easing.Linear, time);

                GameModes.ClearAnimations();
                GameModes.FadeTo(fade, Easing.Linear, time);

                if (ParentMapset.Item.Maps.Count == 1)
                {
                    DifficultyName.ClearAnimations();
                    DifficultyName.FadeTo(fade, Easing.Linear, time);

                    OnlineGrade.ClearAnimations();
                    OnlineGrade.FadeTo(fade, Easing.Linear, time);
                }

                if (Banner.HasBannerLoaded)
                {
                    Banner.ClearAnimations();
                    Banner.FadeTo(1, Easing.Linear, time);
                }

                if (_needsOverlayUpdate)
                    UpdateOverlayTexts();

                FadeInOverlays();

                ClearAnimations();
                var targetWidth = SkinManager.Skin?.UserInterfaceVersion >= 2f ? 950 : (int)ParentMapset.Width;
                ChangeWidthTo(targetWidth, Easing.OutQuint, time + 400);
            }
        }

        /// <summary>
        /// </summary>
        public void Deselect(bool changeWidthInstantly = false)
        {
            Image = SkinManager.Skin?.SongSelect.MapsetDeselected ?? UserInterface.DeselectedMapset;

            var fade = 0.85f;

            if (MarqueeTitle != null) MarqueeTitle.IsActive = false;
            if (MarqueeArtist != null) MarqueeArtist.IsActive = false;

            if (changeWidthInstantly)
            {
                // Skip animations during recycling — they'd be overwritten immediately
                Title.Alpha = fade;
                Artist.Alpha = fade;
                DividerLine.Alpha = fade;
                Creator.Alpha = fade;
                ByText.Alpha = fade;
                RankedStatusSprite.Alpha = fade;
                GameModes.Alpha = fade;

                if (ParentMapset.Item.Maps.Count == 1)
                {
                    DifficultyName.Alpha = fade;
                    OnlineGrade.Alpha = fade;
                }

                if (Banner.HasBannerLoaded)
                    Banner.Alpha = DrawableBanner.DeselectedAlpha;

                BpmOverlay.Visible = false;
                LengthOverlay.Visible = false;

                if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
                    Width = 900;
                else
                    Width = ParentMapset.Width - 50;
            }
            else
            {
                var time = FadeAnimationDuration;

                Title.ClearAnimations();
                Title.FadeTo(fade, Easing.Linear, time);

                Artist.ClearAnimations();
                Artist.FadeTo(fade, Easing.Linear, time);

                DividerLine.ClearAnimations();
                DividerLine.FadeTo(fade, Easing.Linear, time);

                Creator.ClearAnimations();
                Creator.FadeTo(fade, Easing.Linear, time);

                ByText.ClearAnimations();
                ByText.FadeTo(fade, Easing.Linear, time);

                RankedStatusSprite.ClearAnimations();
                RankedStatusSprite.FadeTo(fade, Easing.Linear, time);

                GameModes.ClearAnimations();
                GameModes.FadeTo(fade, Easing.Linear, time);

                if (ParentMapset.Item.Maps.Count == 1)
                {
                    DifficultyName.ClearAnimations();
                    DifficultyName.FadeTo(fade, Easing.Linear, time);

                    OnlineGrade.ClearAnimations();
                    OnlineGrade.FadeTo(fade, Easing.Linear, time);
                }

                if (Banner.HasBannerLoaded)
                {
                    Banner.ClearAnimations();
                    Banner.FadeTo(DrawableBanner.DeselectedAlpha, Easing.Linear, time);
                }

                BpmOverlay.Visible = false;
                LengthOverlay.Visible = false;

                ClearAnimations();
                var targetWidth = SkinManager.Skin?.UserInterfaceVersion >= 2f ? 900 : (int)ParentMapset.Width - 50;
                ChangeWidthTo(targetWidth, Easing.OutQuint, time + 400);
            }
        }

        /// <summary>
        ///     Called when the mapset has been clicked
        /// </summary>
        private void OnMapsetClicked()
        {
            if (ParentMapset.Container != null)
            {
                var container = (MapsetScrollContainer)ParentMapset.Container;
                container.SelectedIndex.Value = ParentMapset.Index;

                // If a mapset is clicked, then we want to expand/collapse the difficulties
                if (ParentMapset.IsSelected)
                {
                    // Go straight to gameplay if sorting by diff (single difficulty per mapset)
                    // Toggle expand/collapse for inline difficulty display
                    container.ToggleMapsetExpanded(ParentMapset.Item);

                    return;
                }
            }

            // Mapset is already selected, so go play the current map.
            if (ParentMapset.IsSelected)
            {
                Logger.Important($"User clicked on mapset to play: {MapManager.Selected.Value}", LogType.Runtime, false);
                return;
            }

            Logger.Important($"User opened mapset: {ParentMapset.Item.Artist} - {ParentMapset.Item.Title}", LogType.Runtime, false);

            if (ParentMapset.Container != null)
            {
                var container = (MapsetScrollContainer)ParentMapset.Container;
                // Expand the mapset visually first so that when the MapManager triggers a Selected event,
                // the ScrollContainer knows we are targeting an expanded nested panel.
                if (!container.IsMapsetExpanded(ParentMapset.Item))
                    container.ExpandMapset(ParentMapset.Item);
            }

            // This will trigger MapManager.Selected.ValueChanged, which calls ScrollToSelected()
            MapManager.SelectMapFromMapset(ParentMapset.Item);

            // Automatically expand the mapset after selecting it
            if (ParentMapset.Container != null)
            {
                var container = (MapsetScrollContainer)ParentMapset.Container;
                container.ExpandMapset(ParentMapset.Item);
            }
        }

        /// <summary>
        ///     Called when the mapset is hovered
        /// </summary>
        private void OnMapsetHovered()
        {
            // Swap Image to hovered texture directly (no overlay alpha needed)
            if (!ParentMapset.IsSelected)
            {
                Image = SkinManager.Skin?.SongSelect?.MapsetHovered ?? UserInterface.MapsetHovered;

                // Performance: update texts only if needed (mods changed)
                if (_needsOverlayUpdate)
                    UpdateOverlayTexts();

                FadeInOverlays();

                // Enable marquee on hover if V2
                if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
                {
                    if (MarqueeTitle != null) MarqueeTitle.IsActive = true;
                    if (MarqueeArtist != null) MarqueeArtist.IsActive = true;
                }
            }
        }

        /// <summary>
        ///     Called when the mapset loses hover
        /// </summary>
        private void OnMapsetLeftHover()
        {
            // Restore Image to deselected texture
            if (!ParentMapset.IsSelected)
            {
                Image = SkinManager.Skin?.SongSelect?.MapsetDeselected ?? UserInterface.DeselectedMapset;

                FadeOutOverlays();

                // Disable marquee when leaving hover if not selected
                if (MarqueeTitle != null) MarqueeTitle.IsActive = false;
                if (MarqueeArtist != null) MarqueeArtist.IsActive = false;
            }
        }

        /// <summary>
        ///     Fades in all BPM and Length overlay elements (background, icon, text)
        /// </summary>
        private void FadeInOverlays()
        {
            // Only reset alpha to 0 if the overlays are currently hidden
            // This prevents blinking if FadeInOverlays is called when they are already visible (e.g. re-selection/refresh)
            if (!BpmOverlay.Visible)
            {
                BpmOverlay.Alpha = 0;
                BpmIcon.Alpha = 0;
                BpmText.Alpha = 0;
                LengthOverlay.Alpha = 0;
                LengthIcon.Alpha = 0;
                LengthText.Alpha = 0;
            }

            // Make overlays visible
            BpmOverlay.Visible = true;
            LengthOverlay.Visible = true;

            // Clear any existing animations
            BpmOverlay.ClearAnimations();
            BpmIcon.ClearAnimations();
            BpmText.ClearAnimations();
            LengthOverlay.ClearAnimations();
            LengthIcon.ClearAnimations();
            LengthText.ClearAnimations();

            // Fade in all elements
            BpmOverlay.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
            BpmIcon.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
            BpmText.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
            LengthOverlay.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
            LengthIcon.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
            LengthText.FadeTo(1f, Easing.Linear, FadeAnimationDuration);
        }

        /// <summary>
        ///     Fades out all BPM and Length overlay elements (background, icon, text)
        /// </summary>
        private void FadeOutOverlays()
        {
            // Clear any existing animations
            BpmOverlay.ClearAnimations();
            BpmIcon.ClearAnimations();
            BpmText.ClearAnimations();
            LengthOverlay.ClearAnimations();
            LengthIcon.ClearAnimations();
            LengthText.ClearAnimations();

            // Fade out all elements
            BpmOverlay.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
            BpmIcon.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
            BpmText.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
            LengthOverlay.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
            LengthIcon.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
            LengthText.FadeTo(0f, Easing.Linear, FadeAnimationDuration);
        }

        /// <summary>
        ///     Enables/disables caching of frequently changed strings
        /// </summary>
        private void SetCaching(bool cache)
        {
            Title.IsCached = cache;
            Artist.IsCached = cache;
            Creator.IsCached = cache;
            DifficultyName.IsCached = cache;
        }

        /// <summary>
        ///     Creates the BPM overlay container positioned on the banner
        /// </summary>
        private void CreateBpmOverlay()
        {
            var skinSelect = SkinManager.Skin?.SongSelect;
            // Use universal info background texture for both BPM and Length overlays
            var bgTexture = SkinManager.Skin?.InfoBackground ?? UserInterface.BlankBox;
            // universal-info-background fallback color: (6, 16, 25, 255)
            var bgColor = skinSelect?.MapsetBpmOverlayColor ?? SkinManager.Skin?.PlayercardInfoBackgroundColor ?? Color.White;

            // Offsets from bottom-right of banner (positive values = inward from edge)
            var offsetX = skinSelect?.MapsetBpmOverlayOffsetX ?? 10f;
            var offsetY = skinSelect?.MapsetBpmOverlayOffsetY ?? 10f;

            // Create icon and text first to measure their widths
            BpmIcon = new Sprite
            {
                Image = UserInterface.MapsetBpmIcon,
                UsePreviousSpriteBatchOptions = true
            };
            BpmIcon.Size = new ScalableVector2(BpmIcon.Image.Width, BpmIcon.Image.Height);

            // Performance: cache icon width (constant value)
            _bpmIconWidth = BpmIcon.Width;

            // BPM Text with "BPM" suffix
            BpmText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "000 BPM", 18)
            {
                Tint = SkinManager.Skin.SongSelect.MapsetBpmTextColor,
                UsePreviousSpriteBatchOptions = true
            };

            // Calculate dynamic width: 10px margin + icon + 10px gap + text + 10px margin
            const int margin = 10;
            const int iconTextGap = 10;
            var contentWidth = BpmIcon.Width + iconTextGap + BpmText.Width;
            var overlayWidth = contentWidth + (margin * 2);

            var bannerHeight = Banner.Height;
            var bottomGap = (Height - bannerHeight) / 2f;
            var marginRight = SkinManager.Skin.SongSelect.MapsetPanelMarginRight;

            // SliceMargins(15, 0) = horizontal 15px, vertical 0px - preserve rounded corners
            BpmOverlay = new NineSliceSprite(bgTexture, new SliceMargins(15, 0))
            {
                Parent = this,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(overlayWidth, bgTexture.Height),
                X = -(marginRight + offsetX),
                Y = -(bottomGap + offsetY),
                Visible = false, // Initially hidden
                UsePreviousSpriteBatchOptions = true,
                Tint = bgColor
            };

            // Now parent the icon and text to the overlay, centered
            BpmIcon.Parent = BpmOverlay;
            BpmIcon.Alignment = Alignment.MidLeft;
            BpmIcon.X = margin;

            BpmText.Parent = BpmOverlay;
            BpmText.Alignment = Alignment.MidLeft;
            BpmText.X = BpmIcon.X + BpmIcon.Width + iconTextGap;
        }

        /// <summary>
        ///     Creates the Length overlay container positioned on the banner
        /// </summary>
        private void CreateLengthOverlay()
        {
            var skinSelect = SkinManager.Skin?.SongSelect;
            // Use universal info background texture for both BPM and Length overlays
            var bgTexture = SkinManager.Skin?.InfoBackground ?? UserInterface.BlankBox;
            // universal-info-background fallback color: (6, 16, 25, 255)
            var bgColor = skinSelect?.MapsetLengthOverlayColor ?? SkinManager.Skin?.PlayercardInfoBackgroundColor ?? Color.White;

            // Get BPM offset to calculate Length position (to the left of BPM)
            var bpmOffsetX = skinSelect?.MapsetBpmOverlayOffsetX ?? 10f;
            var gapBetween = skinSelect?.MapsetLengthOverlayOffsetX ?? 8f;
            var offsetY = skinSelect?.MapsetLengthOverlayOffsetY ?? 10f;

            // Create icon and text first to measure their widths
            LengthIcon = new Sprite
            {
                Image = UserInterface.MapsetLengthIcon,
                UsePreviousSpriteBatchOptions = true
            };
            LengthIcon.Size = new ScalableVector2(LengthIcon.Image.Width, LengthIcon.Image.Height);

            // Performance: cache icon width (constant value)
            _lengthIconWidth = LengthIcon.Width;

            // Length Text
            LengthText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00:00", 18)
            {
                Tint = SkinManager.Skin.SongSelect.MapsetLengthTextColor,
                UsePreviousSpriteBatchOptions = true
            };

            // Calculate dynamic width: 10px margin + icon + 10px gap + text + 10px margin
            const int margin = 10;
            const int iconTextGap = 10;
            var contentWidth = LengthIcon.Width + iconTextGap + LengthText.Width;
            var overlayWidth = contentWidth + (margin * 2);

            // Length X = BPM offset + BPM width + gap
            var totalOffsetX = bpmOffsetX + BpmOverlay.Width + gapBetween;

            var bannerHeight = Banner.Height;
            var bottomGap = (Height - bannerHeight) / 2f;
            var marginRight = SkinManager.Skin.SongSelect.MapsetPanelMarginRight;

            // SliceMargins(15, 0) = horizontal 15px, vertical 0px - preserve rounded corners
            LengthOverlay = new NineSliceSprite(bgTexture, new SliceMargins(15, 0))
            {
                Parent = this,
                Alignment = Alignment.BotRight,
                Size = new ScalableVector2(overlayWidth, bgTexture.Height),
                X = -(marginRight + totalOffsetX),
                Y = -(bottomGap + offsetY),
                Visible = false, // Initially hidden
                UsePreviousSpriteBatchOptions = true,
                Tint = bgColor
            };

            // Now parent the icon and text to the overlay, centered
            LengthIcon.Parent = LengthOverlay;
            LengthIcon.Alignment = Alignment.MidLeft;
            LengthIcon.X = margin;

            LengthText.Parent = LengthOverlay;
            LengthText.Alignment = Alignment.MidLeft;
            LengthText.X = LengthIcon.X + LengthIcon.Width + iconTextGap;
        }

        /// <summary>
        ///     Updates BPM and Length text values based on this mapset
        /// </summary>
        private void UpdateOverlayTexts()
        {
            var mapset = ParentMapset.Item;
            if (mapset?.Maps == null || mapset.Maps.Count == 0)
                return;

            var rate = Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods);

            // Check if all maps have the same audio file (to detect VARIOUS)
            var firstAudio = mapset.Maps[0].AudioPath;
            var hasVariousAudio = false;
            for (var i = 1; i < mapset.Maps.Count; i++)
            {
                if (mapset.Maps[i].AudioPath != firstAudio)
                {
                    hasVariousAudio = true;
                    break;
                }
            }

            if (hasVariousAudio)
            {
                // Multiple audio files = VARIOUS
                BpmText.Text = "VARIOUS";
                LengthText.Text = "VARIOUS";
            }
            else
            {
                // All maps share same audio - use first map's values
                var map = mapset.Maps[0];

                // BPM with rate mods applied and "BPM" suffix
                var bpm = (int)(map.Bpm * rate);
                BpmText.Text = $"{bpm} BPM";

                // Length with rate mods applied
                var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
                LengthText.Text = length.Hours > 0 ? length.ToString(@"hh\:mm\:ss") : length.ToString(@"mm\:ss");
            }

            // Recalculate overlay widths after text changes
            UpdateOverlaySizes();

            // Performance: reset flag after update complete
            _needsOverlayUpdate = false;
        }

        /// <summary>
        ///     Recalculates overlay sizes based on current text content
        /// </summary>
        private void UpdateOverlaySizes()
        {
            var skinSelect = SkinManager.Skin?.SongSelect;
            const int margin = 10;
            const int iconTextGap = 10;

            // Performance: use cached icon widths instead of accessing texture properties
            // Recalculate BPM overlay width
            var bpmContentWidth = _bpmIconWidth + iconTextGap + BpmText.Width;
            var bpmOverlayWidth = bpmContentWidth + (margin * 2);
            BpmOverlay.Width = bpmOverlayWidth;

            // Recalculate Length overlay width and position
            var lengthContentWidth = _lengthIconWidth + iconTextGap + LengthText.Width;
            var lengthOverlayWidth = lengthContentWidth + (margin * 2);
            LengthOverlay.Width = lengthOverlayWidth;

            // Recalculate Length X position (to the left of BPM with gap)
            var bpmOffsetX = skinSelect?.MapsetBpmOverlayOffsetX ?? 10f;
            var gapBetween = skinSelect?.MapsetLengthOverlayOffsetX ?? 8f;
            var marginRight = skinSelect.MapsetPanelMarginRight;
            LengthOverlay.X = -(marginRight + bpmOffsetX + BpmOverlay.Width + gapBetween);
        }
    }
}
