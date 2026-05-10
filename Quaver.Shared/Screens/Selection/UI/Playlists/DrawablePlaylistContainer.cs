using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Selection.UI.Maps;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Quaver.Shared.Screens.Selection.UI.Playlists.Dialogs;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Playlists
{
    public class DrawablePlaylistContainer : Sprite
    {
        /// <summary>
        /// </summary>
        private DrawablePlaylist Playlist { get; }

        /// <summary>
        /// </summary>
        private ImageButton Button { get; set; }

        /// <summary>
        /// </summary>
        public SpriteTextPlus Title { get; private set; }

        /// <summary>
        /// </summary>
        private DrawableBanner Banner { get; set; }

        /// <summary>
        /// </summary>
        private PlaylistKeyValueDisplay MapCount { get; set; }

        /// <summary>
        /// </summary>
        private PlaylistKeyValueDisplay Creator { get; set; }

        /// <summary>
        /// </summary>
        private PlaylistDifficultyDisplay DifficultyDisplay { get; set; }

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
        ///     Signifies if the playlist is online
        /// </summary>
        private Sprite OnlineMapPoolIcon { get; set; }

        /// <summary>
        ///     Signifies if the playlist is from another game (Osu/Etterna)
        /// </summary>
        private Sprite OtherGameIcon { get; set; }

        /// <summary>
        ///     Quantity panel overlay components
        /// </summary>
        private NineSliceSprite QuantityOverlay { get; set; }
        private Sprite QuantityIcon { get; set; }
        private SpriteTextPlus QuantityText { get; set; }
        private float _quantityIconWidth;

        /// <summary>
        ///     Difficulty range panel overlay components
        /// </summary>
        private NineSliceSprite DifficultyRangeOverlay { get; set; }
        private Sprite DifficultyRangeIcon { get; set; }

        private SpriteTextPlus DifficultyRangeMinText { get; set; }
        private SpriteTextPlus DifficultyRangeDashText { get; set; }
        private SpriteTextPlus DifficultyRangeMaxText { get; set; }
        private float _difficultyRangeIconWidth;

        /// <summary>
        ///     Description text for V2
        /// </summary>
        private MarqueeSpriteText DescriptionText { get; set; }

        /// <summary>
        ///    Creator text for V2
        /// </summary>
        private SpriteTextPlus CreatorPrefixTextV2 { get; set; }

        /// <summary>
        ///    Creator text for V2
        /// </summary>
        private SpriteTextPlus CreatorNameTextV2 { get; set; }

        /// <summary>
        ///     The X position of the title/first element
        /// </summary>
        private int TitleX => SkinManager.Skin?.SongSelect?.PlaylistPanelMarginLeft ?? 26;

        /// <summary>
        /// </summary>
        public DrawablePlaylistContainer(DrawablePlaylist playlist)
        {
            Playlist = playlist;
            Parent = Playlist;

            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
            var width = isV2 ? 1005 : (int)Playlist.Width;
            var height = isV2 ? 100 : 86;
            Size = new ScalableVector2(width, height);
            Image = SkinManager.Skin?.SongSelect?.PlaylistDeselected ?? UserInterface.PlaylistDeselected;
            UsePreviousSpriteBatchOptions = true;

            CreateButton();
            CreateTitle();
            CreateBannerImage();
            CreateMapCount();
            CreateCreator();
            CreateDifficultyDisplay();
            CreateRankedStatus();
            CreateGameModes();
            CreateOnlineMapPoolIcon();
            CreateOtherGameIcon();
            CreateQuantityPanel();
            CreateDifficultyRangePanel();
            CreateDescription();
            CreateCreatorV2();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (Button.Width != Width)
                Button.Width = Width;

            PerformHoverAnimation(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public void UpdateContent(Playlist item, int index)
        {
            OnlineMapPoolIcon.Visible = item.IsOnlineMapPool();
            OtherGameIcon.Visible = item.PlaylistGame != MapGame.Quaver;

            Title.Text = item.Name;
            Title.TruncateWithEllipsis(400);

            // X Positioning Logic
            var currentX = TitleX;
            const int iconGap = 5;

            if (OnlineMapPoolIcon.Visible)
            {
                OnlineMapPoolIcon.X = currentX;
                currentX += (int)OnlineMapPoolIcon.Width + iconGap;
            }

            if (OtherGameIcon.Visible)
            {
                OtherGameIcon.X = currentX;
                currentX += (int)OtherGameIcon.Width + iconGap;
            }

            Title.X = currentX;

            // Match icon tint with title
            OnlineMapPoolIcon.Tint = Title.Tint;
            OtherGameIcon.Tint = Title.Tint;

            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;

            if (!isV2)
            {
                var titleCenterY = Title.Y + (Title.Height / 2);

                if (OnlineMapPoolIcon.Visible)
                    OnlineMapPoolIcon.Y = titleCenterY - (OnlineMapPoolIcon.Height / 2);

                if (OtherGameIcon.Visible)
                    OtherGameIcon.Y = titleCenterY - (OtherGameIcon.Height / 2);
            }

            if (isV2)
            {
                MapCount.Visible = false;
                Creator.Visible = false;
                DifficultyDisplay.Visible = false;

                // Handle description fallback
                var description = item.Description;
                if (string.IsNullOrWhiteSpace(description))
                    description = "No description";

                DescriptionText.Visible = true;
                DescriptionText.TextSprite.Text = description;
                DescriptionText.TextSprite.Tint = SkinManager.Skin.SongSelect.PlaylistPanelDescriptionColor;
                DescriptionText.IsActive = false; // Disable marquee by default (enabled on hover)

                CreatorPrefixTextV2.Visible = true;
                CreatorNameTextV2.Visible = true;
                CreatorNameTextV2.Text = item.Creator;

                // Vertical Centering Logic for V2 with Mapset-like spacing
                // Mapset offsets: Title -> Artist (+29), Artist -> Creator (+27 approx)
                const int titleToDesc = 29;
                const int descToCreator = 27;

                // Calculate total height: from Title top to Creator bottom
                // We assume the elements are positioned relatively:
                // Title at 0
                // Description at titleToDesc
                // Creator at titleToDesc + descToCreator
                // Total Height = (titleToDesc + descToCreator) + CreatorTextV2.Height
                var totalContentHeight = (titleToDesc + descToCreator) + CreatorNameTextV2.Height;
                var startY = (Height - totalContentHeight) / 2;

                // Center the group
                Title.Y = startY;
                // Align icons with Title Y in V2
                var titleCenterY = Title.Y + (Title.Height / 2);

                if (OnlineMapPoolIcon.Visible)
                    OnlineMapPoolIcon.Y = titleCenterY - (OnlineMapPoolIcon.Height / 2);

                if (OtherGameIcon.Visible)
                    OtherGameIcon.Y = titleCenterY - (OtherGameIcon.Height / 2);

                DescriptionText.Y = Title.Y + titleToDesc + 1; // Moved up 2px from +3
                CreatorPrefixTextV2.Y = DescriptionText.Y + descToCreator - 2; // Moved up 2px relative to desc (4px total)
                CreatorNameTextV2.Y = DescriptionText.Y + descToCreator - 2;

                CreatorNameTextV2.X = CreatorPrefixTextV2.X + CreatorPrefixTextV2.Width;
            }
            else
            {
                MapCount.Visible = true;
                Creator.Visible = true;
                DifficultyDisplay.Visible = true;

                DescriptionText.Visible = false;
                CreatorPrefixTextV2.Visible = false;
                CreatorNameTextV2.Visible = false;

                MapCount.ChangeValue(item.Maps.Count.ToString("n0"));

                const int metadataSpacing = 4;

                Creator.ChangeValue(item.Creator);
                Creator.X = MapCount.X + MapCount.Width + metadataSpacing;

                if (item.Maps.Count != 0)
                {
                    DifficultyDisplay.ChangeValue(item.Maps.Min(x => x.DifficultyFromMods(ModManager.Mods)),
                        item.Maps.Max(x => x.DifficultyFromMods(ModManager.Mods)));
                }
                else
                    DifficultyDisplay.ChangeValue(0, 0);

                DifficultyDisplay.X = Creator.X + Creator.Width + metadataSpacing;
            }

            RankedStatusSprite.Image = GetRankedStatusImage();
            
            // OPTIMIZATION: Use a specialized method to get mode icons instead of LINQ Select
            UpdateGameModeTextures(item);
            
            Banner.UpdateContent(Playlist.Item);

            QuantityText.Text = item.Maps.Count.ToString();

            if (item.Maps.Count > 0)
            {
                var minDiff = item.Maps.Min(x => x.DifficultyFromMods(ModManager.Mods));
                var maxDiff = item.Maps.Max(x => x.DifficultyFromMods(ModManager.Mods));

                DifficultyRangeMinText.Text = $"{minDiff:0.00}";
                DifficultyRangeMinText.Tint = ColorHelper.DifficultyToColor((float)minDiff);

                DifficultyRangeMaxText.Text = $"{maxDiff:0.00}";
                DifficultyRangeMaxText.Tint = ColorHelper.DifficultyToColor((float)maxDiff);
            }
            else
            {
                DifficultyRangeMinText.Text = "0.00";
                DifficultyRangeMinText.Tint = Color.White;

                DifficultyRangeMaxText.Text = "0.00";
                DifficultyRangeMaxText.Tint = Color.White;
            }

            UpdatePanelSizes();

            // Always keep the visual state as "Deselected" (default)
            Deselect(true);
        }

        /// <summary>
        ///    Optimized helper to update game mode textures without LINQ allocations
        /// </summary>
        /// <param name="item"></param>
        private void UpdateGameModeTextures(Playlist item)
        {
            var modes = new HashSet<GameMode>();
            for (var i = 0; i < item.Maps.Count; i++)
                modes.Add(item.Maps[i].Mode);
            
            GameModeHelper.SetGameModeTexture(modes, GameModes, GameModeText);
        }

        /// <summary>
        ///     Creates <see cref="Button"/>
        /// </summary>
        private void CreateButton()
        {
            Wobble.Graphics.Container clickableArea = null;
            if (Playlist.MapsetContainer != null)
                clickableArea = Playlist.MapsetContainer.ClickableArea;
            else if (Playlist.Container is PlaylistContainer pc1)
                clickableArea = pc1.ClickableArea;

            Button = new SongSelectContainerButton(SkinManager.Skin?.SongSelect?.PlaylistHovered ?? UserInterface.PlaylistHovered, clickableArea)
            {
                Parent = this,
                Size = Size,
                Alpha = 0,
                Alignment = Alignment.MidCenter,
                UsePreviousSpriteBatchOptions = true,
                Depth = 1
            };

            // Show overlays on hover (if not already selected)
            Button.Hovered += (sender, args) =>
            {
                OnMapsetHovered();
                // Existing logic for overlays might be needed here if not present
                if (!Playlist.IsSelected)
                {
                    // Logic to hide overlays
                }
            };

            // Hide overlays when leaving hover (if not selected)
            Button.LeftHover += (sender, args) =>
            {
                OnMapsetLeftHover();
                if (!Playlist.IsSelected)
                {
                    // Logic to hide overlays
                }
            };

            Button.Clicked += (sender, args) =>
            {
                // Always set as selected in data (required for game logic)
                PlaylistManager.Selected.Value = Playlist.Item;

                if (Playlist.MapsetContainer != null)
                {
                    // Collapsing the top playlist panel in the mapsets list: go back to playlists list
                    Playlist.MapsetContainer.ActiveScrollContainer.Value = SelectScrollContainerType.Playlists;
                    return;
                }

                if (Playlist.Container == null)
                    return;

                var pc = Playlist.Container as PlaylistContainer;
                if (pc != null)
                {
                    pc.SelectedIndex.Value = Playlist.Index;

                    // No maps inside playlist. Prevent opening
                    if (PlaylistManager.Selected.Value.Maps.Count == 0)
                    {
                        NotificationManager.Show(NotificationLevel.Error, "There are no maps inside of this playlist! You can right-click maps to add to it");
                        return;
                    }

                    // Always open the playlist (single click behavior)
                    pc.ActiveScrollContainer.Value = SelectScrollContainerType.Mapsets;
                }
            };

            Button.RightClicked += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.ActivateRightClickOptions(new PlaylistRightClickOptions(Playlist));
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        private void PerformHoverAnimation(GameTime gameTime)
        {
            var targetAlpha = Button.IsHovered ? SkinManager.Skin.SongSelect.PlaylistPanelHoveringAlpha : 0;

            Button.Alpha = MathHelper.Lerp(Button.Alpha, targetAlpha,
                (float)Math.Min(gameTime.ElapsedGameTime.TotalMilliseconds / 30, 1));
        }

        /// <summary>
        /// </summary>
        private void OnMapsetHovered()
        {
            SetHovered(true);

            // Copied logic from mapset if any specific hover logic exists there, 
            // but for playlists, we mainly focus on the marquee and overlays.
            // Existing logic uses Button.Hovered to show overlays via Button_Hovered event if defined,
            // but here we are inside CreateButton, using inline lambdas?
            // Wait, CreateButton uses inline lambdas calling OnMapsetHovered? 
            // No, the code I read earlier had logic in CreateButton.
            // Let's check CreateButton again.
        }

        /// <summary>
        /// </summary>
        private void OnMapsetLeftHover()
        {
            SetHovered(false);
        }

        /// <summary>
        ///     Sets the hover state for the marquee
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (DescriptionText != null)
                DescriptionText.IsActive = hovered;
        }

        /// <summary>
        /// </summary>
        public void Select(bool changeWidthInstantly = false)
        {
            Image = SkinManager.Skin?.SongSelect?.PlaylistDeselected ?? UserInterface.PlaylistDeselected;

            const int time = 200;
            AnimateSprites(1, 200);

            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;
            var targetWidth = isV2 ? 1005 : (int)Playlist.Width;

            if (changeWidthInstantly)
                Width = targetWidth;
            else
                ChangeWidthTo(targetWidth, Easing.OutQuint, time + 400);
        }

        /// <summary>
        /// </summary>
        public void Deselect(bool changeWidthInstantly = false)
        {
            Image = SkinManager.Skin?.SongSelect?.PlaylistDeselected ?? UserInterface.PlaylistDeselected;

            const int time = 200;
            AnimateSprites(1f, 200);

            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;
            var targetWidth = isV2 ? 1005 : (int)Playlist.Width - 50;

            if (changeWidthInstantly)
                Width = targetWidth;
            else
                ChangeWidthTo(targetWidth, Easing.OutQuint, time + 400);
        }

        /// <summary>
        ///     Creates <see cref="Title"/>
        /// </summary>
        private void CreateTitle()
        {
            Title = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "PLAYLIST TITLE", 26)
            {
                Parent = this,
                Position = new ScalableVector2(TitleX, 18),
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.PlaylistPanelTitleColor
            };
        }

        /// <summary>
        ///    Creates <see cref="Banner"/>
        /// </summary>
        private void CreateBannerImage()
        {
            var bannerSize = SkinManager.Skin.SongSelect.PlaylistPanelBannerSize;

            // 0 = no banner
            if (bannerSize <= 0)
                return;

            Banner = new DrawableBanner(Playlist.Item)
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(bannerSize, bannerSize),
                Image = UserInterface.PlaylistDefaultBanner,
                X = -(SkinManager.Skin?.SongSelect?.PlaylistPanelMarginRight ?? 2),
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        private void CreateMapCount()
        {
            MapCount = new PlaylistKeyValueDisplay("Maps:", "0", ColorHelper.HexToColor("#00FFDE"))
            {
                Parent = this,
                Position = new ScalableVector2(Title.X, Title.Y + Title.Height + 5),
                Key = { Tint = SkinManager.Skin.SongSelect.PlaylistPanelByColor }
            };
        }

        /// <summary>
        /// </summary>
        private void CreateCreator()
        {
            Creator = new PlaylistKeyValueDisplay("By:", "Me",
                SkinManager.Skin.SongSelect.PlaylistPanelCreatorColor)
            {
                Parent = this,
                Position = new ScalableVector2(Title.X, MapCount.Y),
                UsePreviousSpriteBatchOptions = true,
                Key = { Tint = SkinManager.Skin.SongSelect.PlaylistPanelByColor }
            };
        }

        /// <summary>
        ///
        /// </summary>
        private void CreateDifficultyDisplay()
        {
            DifficultyDisplay = new PlaylistDifficultyDisplay()
            {
                Parent = this,
                UsePreviousSpriteBatchOptions = true,
                Y = MapCount.Y,
                Key = { Tint = SkinManager.Skin.SongSelect.PlaylistPanelByColor }
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
                X = Banner.X - Banner.Width - 10,
                Y = 20,
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
                X = Banner.X - Banner.Width - 10,
                Y = -20,
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
        /// <exception cref="NotImplementedException"></exception>
        private void CreateOnlineMapPoolIcon()
        {
            OnlineMapPoolIcon = new Sprite()
            {
                Parent = this,
                Size = new ScalableVector2(16, 16),
                Image = FontAwesome.Get(FontAwesomeIcon.fa_earth_globe),
                UsePreviousSpriteBatchOptions = true,
                Visible = false,
                X = TitleX,
                Y = Title.Y + 4
            };
        }

        /// <summary>
        ///     Creates <see cref="OtherGameIcon"/>
        /// </summary>
        private void CreateOtherGameIcon()
        {
            OtherGameIcon = new Sprite()
            {
                Parent = this,
                Size = new ScalableVector2(16, 16),
                Image = SkinManager.Skin?.SongSelect?.PlaylistOtherGameIcon ?? UserInterface.PlaylistOtherGameIcon,
                UsePreviousSpriteBatchOptions = true,
                Visible = false,
                X = TitleX,
                Y = Title.Y + 4
            };
        }

        /// <summary>
        ///     Retrieves the color of a map's ranked status
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Texture2D GetRankedStatusImage()
        {
            if (Playlist.Item.Maps.Count == 0)
                return SkinManager.Skin?.SongSelect?.StatusNone ?? UserInterface.StatusNone;

            if (Playlist.Item.PlaylistGame != MapGame.Quaver)
            {
                switch (Playlist.Item.PlaylistGame)
                {
                    case MapGame.Osu:
                        return SkinManager.Skin?.SongSelect?.StatusOsu ?? UserInterface.StatusOtherGameOsu;
                    case MapGame.Etterna:
                        return SkinManager.Skin?.SongSelect?.StatusStepmania ?? UserInterface.StatusOtherGameEtterna;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (Playlist.Item.Maps.Any(o => o.RankedStatus != Playlist.Item.Maps.First().RankedStatus))
                return SkinManager.Skin?.SongSelect?.StatusVarious ?? UserInterface.StatusVarious;

            switch (Playlist.Item.Maps.Max(x => x.RankedStatus))
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
        ///     Creates the Quantity panel overlay positioned on the banner
        /// </summary>
        private void CreateQuantityPanel()
        {
            var bgTexture = SkinManager.Skin.InfoBackground;
            var bgColor = SkinManager.Skin.PlayercardInfoBackgroundColor;

            // Create icon and text first to measure their widths
            QuantityIcon = new Sprite
            {
                Image = UserInterface.PlaylistPanelQuantityIcon,
                UsePreviousSpriteBatchOptions = true
            };
            QuantityIcon.Size = new ScalableVector2(QuantityIcon.Image.Width, QuantityIcon.Image.Height);

            // Performance: cache icon width (constant value)
            _quantityIconWidth = QuantityIcon.Width;

            // Quantity text
            QuantityText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0", 18)
            {
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            // Calculate dynamic width: 10px margin + icon + 10px gap + text + 10px margin
            const int margin = 10;
            const int iconTextGap = 10;
            var contentWidth = QuantityIcon.Width + iconTextGap + QuantityText.Width;
            var overlayWidth = contentWidth + (margin * 2);

            // SliceMargins(15, 0) = horizontal 15px, vertical 0px - preserve rounded corners
            QuantityOverlay = new NineSliceSprite(bgTexture, new SliceMargins(15, 0))
            {
                Parent = this,
                Visible = SkinManager.Skin?.UserInterfaceVersion == 2,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(overlayWidth, bgTexture.Height),
                X = GameModes.X - GameModes.Width - 10,
                Y = -20,
                UsePreviousSpriteBatchOptions = true,
                Tint = bgColor
            };

            // Now parent the icon and text to the overlay, centered
            QuantityIcon.Parent = QuantityOverlay;
            QuantityIcon.Alignment = Alignment.MidLeft;
            QuantityIcon.X = margin;

            QuantityText.Parent = QuantityOverlay;
            QuantityText.Alignment = Alignment.MidLeft;
            QuantityText.X = QuantityIcon.X + QuantityIcon.Width + iconTextGap;
        }

        /// <summary>
        ///     Creates the Difficulty Range panel overlay positioned on the banner
        /// </summary>
        private void CreateDifficultyRangePanel()
        {
            var bgTexture = SkinManager.Skin.InfoBackground;
            var bgColor = SkinManager.Skin.PlayercardInfoBackgroundColor;

            // Create icon and text first to measure their widths
            DifficultyRangeIcon = new Sprite
            {
                Image = UserInterface.PlaylistPanelDifficultyRangeIcon,
                UsePreviousSpriteBatchOptions = true
            };
            DifficultyRangeIcon.Size = new ScalableVector2(DifficultyRangeIcon.Image.Width, DifficultyRangeIcon.Image.Height);

            // Performance: cache icon width (constant value)
            _difficultyRangeIconWidth = DifficultyRangeIcon.Width;

            // Difficulty range text components
            DifficultyRangeMinText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0.00", 18)
            {
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            DifficultyRangeDashText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), " - ", 18)
            {
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            DifficultyRangeMaxText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0.00", 18)
            {
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            // Calculate dynamic width: 10px margin + icon + 10px gap + text + 10px margin
            const int margin = 10;
            const int iconTextGap = 10;
            var contentWidth = DifficultyRangeIcon.Width + iconTextGap +
                               DifficultyRangeMinText.Width + DifficultyRangeDashText.Width + DifficultyRangeMaxText.Width;
            var overlayWidth = contentWidth + (margin * 2);

            // SliceMargins(15, 0) = horizontal 15px, vertical 0px - preserve rounded corners
            DifficultyRangeOverlay = new NineSliceSprite(bgTexture, new SliceMargins(15, 0))
            {
                Parent = this,
                Visible = SkinManager.Skin?.UserInterfaceVersion == 2,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(overlayWidth, bgTexture.Height),
                X = RankedStatusSprite.X - RankedStatusSprite.Width - 10,
                Y = 20,
                UsePreviousSpriteBatchOptions = true,
                Tint = bgColor
            };

            // Now parent the icon and text to the overlay, centered
            DifficultyRangeIcon.Parent = DifficultyRangeOverlay;
            DifficultyRangeIcon.Alignment = Alignment.MidLeft;
            DifficultyRangeIcon.X = margin;

            DifficultyRangeMinText.Parent = DifficultyRangeOverlay;
            DifficultyRangeMinText.Alignment = Alignment.MidLeft;
            DifficultyRangeMinText.X = DifficultyRangeIcon.X + DifficultyRangeIcon.Width + iconTextGap;

            DifficultyRangeDashText.Parent = DifficultyRangeOverlay;
            DifficultyRangeDashText.Alignment = Alignment.MidLeft;
            DifficultyRangeDashText.X = DifficultyRangeMinText.X + DifficultyRangeMinText.Width;

            DifficultyRangeMaxText.Parent = DifficultyRangeOverlay;
            DifficultyRangeMaxText.Alignment = Alignment.MidLeft;
            DifficultyRangeMaxText.X = DifficultyRangeDashText.X + DifficultyRangeDashText.Width;
        }

        /// <summary>
        ///     Recalculates panel overlay sizes based on current text content
        /// </summary>
        private void UpdatePanelSizes()
        {
            const int margin = 10;
            const int iconTextGap = 10;

            // Recalculate Quantity overlay width
            var quantityContentWidth = _quantityIconWidth + iconTextGap + QuantityText.Width;
            var quantityOverlayWidth = quantityContentWidth + (margin * 2);
            QuantityOverlay.Width = quantityOverlayWidth;

            // Recalculate Difficulty Range overlay width
            var diffTextWidth = DifficultyRangeMinText.Width + DifficultyRangeDashText.Width + DifficultyRangeMaxText.Width;
            var difficultyRangeContentWidth = _difficultyRangeIconWidth + iconTextGap + diffTextWidth;
            var difficultyRangeOverlayWidth = difficultyRangeContentWidth + (margin * 2);
            DifficultyRangeOverlay.Width = difficultyRangeOverlayWidth;

            // Update sub-element positions since widths changed
            DifficultyRangeMinText.X = DifficultyRangeIcon.X + DifficultyRangeIcon.Width + iconTextGap;
            DifficultyRangeDashText.X = DifficultyRangeMinText.X + DifficultyRangeMinText.Width;
            DifficultyRangeMaxText.X = DifficultyRangeDashText.X + DifficultyRangeDashText.Width;

            // Recalculate positions based on dynamic widths (anchored to previous elements)
            QuantityOverlay.X = GameModes.X - GameModes.Width - 10;

            // Note: If DifficultyRangeOverlay needs to be positioned relative to QuantityOverlay or other dynamic elements, update X here.
            // Currently it is anchored to RankedStatusSprite which has fixed position/size relative to Banner? 
            // Actually RankedStatusSprite is relative to Banner.
            DifficultyRangeOverlay.X = RankedStatusSprite.X - RankedStatusSprite.Width - 10;
        }

        /// <summary>
        /// </summary>
        /// <param name="fade"></param>
        /// <param name="time"></param>
        private void AnimateSprites(float fade, int time)
        {
            Title.ClearAnimations();
            Title.FadeTo(fade, Easing.Linear, time);

            if (Banner.HasBannerLoaded)
            {
                Banner.ClearAnimations();
                Banner.FadeTo(1, Easing.Linear, time);
            }

            if (OnlineMapPoolIcon != null)
            {
                OnlineMapPoolIcon.ClearAnimations();
                OnlineMapPoolIcon.FadeTo(fade, Easing.Linear, time);
            }

            if (OtherGameIcon != null)
            {
                OtherGameIcon.ClearAnimations();
                OtherGameIcon.FadeTo(fade, Easing.Linear, time);
            }
            MapCount.RemoveAnimations();
            MapCount.FadeTo(fade, Easing.Linear, time);

            Creator.RemoveAnimations();
            Creator.FadeTo(fade, Easing.Linear, time);

            DifficultyDisplay.RemoveAnimations();
            DifficultyDisplay.FadeTo(fade, Easing.Linear, time);

            RankedStatusSprite.ClearAnimations();
            RankedStatusSprite.FadeTo(fade, Easing.Linear, time);

            GameModes.ClearAnimations();
            GameModes.FadeTo(fade, Easing.Linear, time);

            QuantityOverlay.ClearAnimations();
            QuantityOverlay.FadeTo(fade, Easing.Linear, time);

            QuantityIcon.ClearAnimations();
            QuantityIcon.FadeTo(fade, Easing.Linear, time);

            QuantityText.ClearAnimations();
            QuantityText.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeOverlay.ClearAnimations();
            DifficultyRangeOverlay.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeIcon.ClearAnimations();
            DifficultyRangeIcon.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeMinText.ClearAnimations();
            DifficultyRangeMinText.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeDashText.ClearAnimations();
            DifficultyRangeDashText.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeMaxText.ClearAnimations();
            DifficultyRangeMaxText.FadeTo(fade, Easing.Linear, time);

            DifficultyRangeMaxText.ClearAnimations();
            DifficultyRangeMaxText.FadeTo(fade, Easing.Linear, time);

            DescriptionText.TextSprite.ClearAnimations();
            DescriptionText.TextSprite.FadeTo(fade, Easing.Linear, time);

            CreatorPrefixTextV2.ClearAnimations();
            CreatorPrefixTextV2.FadeTo(fade, Easing.Linear, time);

            CreatorNameTextV2.ClearAnimations();
            CreatorNameTextV2.FadeTo(fade, Easing.Linear, time);

            ClearAnimations();
        }
        /// <summary>
        ///    Creates <see cref="DescriptionText"/>
        /// </summary>
        private void CreateDescription()
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;

            // MarqueeSpriteText(WobbleFontStore font, string text, int fontSize, float width)
            DescriptionText = new MarqueeSpriteText(FontManager.GetWobbleFont(Fonts.InterBold), "", 20, 550)
            {
                Parent = this,
                Visible = isV2, // Initial visibility
                Position = new ScalableVector2(TitleX, 0), // Y is set in UpdateContent
                UsePreviousSpriteBatchOptions = true,
                Height = 30 // Approximate height for size 20 font
            };
        }

        /// <summary>
        ///    Creates <see cref="CreatorPrefixTextV2"/> and <see cref="CreatorNameTextV2"/>
        /// </summary>
        private void CreateCreatorV2()
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion == 2;

            CreatorPrefixTextV2 = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "By: ", 22)
            {
                Parent = this,
                Visible = isV2, // Initial visibility
                // Y position set in UpdateContent
                X = TitleX,
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.PlaylistPanelByColor
            };

            CreatorNameTextV2 = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "", 22)
            {
                Parent = this,
                Visible = isV2, // Initial visibility
                // Y position set in UpdateContent
                UsePreviousSpriteBatchOptions = true,
                Tint = SkinManager.Skin.SongSelect.PlaylistPanelCreatorColor
            };
        }
    }
}
