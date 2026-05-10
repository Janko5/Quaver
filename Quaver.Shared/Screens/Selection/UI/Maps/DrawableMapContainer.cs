using System;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.MapInformation.Metadata;
using Quaver.Shared.Screens.Selection.UI.Maps.Components;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Maps
{
    public class DrawableMapContainer : Sprite
    {
        /// <summary>
        /// </summary>
        private DrawableMap ParentMap { get; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus Name { get; set; }

        /// <summary>Displays the difficulty rating value</summary>
        private SpriteTextPlus RatingText { get; set; }

        /// <summary>Displays the LN percentage</summary>
        private TextKeyValue LongNotePercentage { get; set; }

        /// <summary>Displays the Notes Per Second</summary>
        private TextKeyValue NotesPerSecond { get; set; }

        private SpriteTextPlus BpmText { get; set; }
        private SpriteTextPlus LengthText { get; set; }

        /// <summary>
        /// </summary>
        private ImageButton Button { get; set; }

        /// <summary>
        /// </summary>
        private Sprite OnlineGrade { get; set; }

        /// <summary>
        ///     The amount of spacing between the panel and first value
        /// </summary>
        private const int PaddingX = 26;

        /// <summary>
        /// </summary>
        /// <param name="parentMap"></param>
        public DrawableMapContainer(DrawableMap parentMap)
        {
            ParentMap = parentMap;
            Parent = ParentMap;

            // Check skin version for design selection
            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                InitializeLayoutV2();
            }
            else
            {
                InitializeLayoutV1();
            }

            UsePreviousSpriteBatchOptions = true;

            ModManager.ModsChanged += OnModsChanged;
        }

        /// <summary>
        ///    Initializes the layout for version 1.0 (Original design)
        /// </summary>
        private void InitializeLayoutV1()
        {
            Size = new ScalableVector2(1000, 60);

            CreateButton();
            CreateDifficultyName();
            CreateDifficultyRating();
            CreateMetadata();
            CreateBpmLengthInfo();
            CreateGrade();
        }

        /// <summary>
        ///    Initializes the layout for version 2.0 (New design)
        /// </summary>
        private void InitializeLayoutV2()
        {
            Size = new ScalableVector2(1000, 60);

            CreateButton();
            CreateDifficultyName();
            CreateDifficultyRating();
            CreateMetadata();
            CreateBpmLengthInfo();
            CreateGrade();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            Button.Width = Width;

            PerformHoverAnimation(gameTime);
            base.Update(gameTime);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            ModManager.ModsChanged -= OnModsChanged;
            base.Destroy();
        }

        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="index"></param>
        public void UpdateContent(Map map, int index)
        {
            if (SkinManager.Skin?.UserInterfaceVersion >= 2f)
            {
                UpdateContentV2(map, index);
            }
            else
            {
                UpdateContentV1(map, index);
            }

            if (ParentMap.IsSelected)
                Select(true);
            else
                Deselect(true);
        }

        /// <summary>
        ///     Updates content for V1 layout
        /// </summary>
        /// <param name="map"></param>
        /// <param name="index"></param>
        private void UpdateContentV1(Map map, int index)
        {
            var difficulty = map.DifficultyFromMods(ModManager.Mods);
            var rate = Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods);
            const int spacing = 10;
            Name.Text = StringHelper.GetFormatDifficultyName(map.DifficultyName, map.Mode, map.HasScratchKey, map.Mapset?.HasMultipleKeymodes ?? false);
            Name.Tint = ColorHelper.DifficultyToColor((float)difficulty);

            // Update rating
            RatingText.Text = StringHelper.RatingToString(difficulty);
            RatingText.Tint = ColorHelper.DifficultyToColor((float)difficulty);

            var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
            LengthText.Text = length.Hours > 0 ? length.ToString(@"hh\:mm\:ss") : length.ToString(@"mm\:ss");

            var bpm = (int)(map.Bpm * rate);
            BpmText.Text = $"{bpm} BPM";

            if (map.OnlineGrade != Grade.None)
            {
                const int width = 40;

                OnlineGrade.Visible = true;
                OnlineGrade.Image = SkinManager.Skin.Grades[map.OnlineGrade];
                OnlineGrade.Size = new ScalableVector2(width, (float)OnlineGrade.Image.Height / OnlineGrade.Image.Width * width);

                Name.X = OnlineGrade.X + OnlineGrade.Width + 16;
            }
            else
            {
                Name.X = PaddingX;
                OnlineGrade.Visible = false;
            }

            RatingText.X = Name.X + Name.Width + spacing;
            LengthText.X = RatingText.X + RatingText.Width + spacing;
            BpmText.X = LengthText.X + LengthText.Width + spacing;

            UpdateMetadata();
        }

        /// <summary>
        ///     Updates content for V2 layout
        /// </summary>
        /// <param name="map"></param>
        /// <param name="index"></param>
        private void UpdateContentV2(Map map, int index)
        {
            var difficulty = map.DifficultyFromMods(ModManager.Mods);
            var rate = Quaver.API.Helpers.ModHelper.GetRateFromMods(ModManager.Mods);
            const int spacing = 10;
            Name.Text = StringHelper.GetFormatDifficultyName(map.DifficultyName, map.Mode, map.HasScratchKey, map.Mapset?.HasMultipleKeymodes ?? false);
            Name.Tint = ColorHelper.DifficultyToColor((float)difficulty);

            // Update rating
            RatingText.Text = StringHelper.RatingToString(difficulty);
            RatingText.Tint = ColorHelper.DifficultyToColor((float)difficulty);

            var length = TimeSpan.FromMilliseconds(map.SongLength / rate);
            LengthText.Text = length.Hours > 0 ? length.ToString(@"hh\:mm\:ss") : length.ToString(@"mm\:ss");

            var bpm = (int)(map.Bpm * rate);
            BpmText.Text = $"{bpm} BPM";

            if (map.OnlineGrade != Grade.None)
            {
                const int width = 40;

                OnlineGrade.Visible = true;
                OnlineGrade.Image = SkinManager.Skin.Grades[map.OnlineGrade];
                OnlineGrade.Size = new ScalableVector2(width, (float)OnlineGrade.Image.Height / OnlineGrade.Image.Width * width);

                Name.X = OnlineGrade.X + OnlineGrade.Width + 16;
            }
            else
            {
                Name.X = PaddingX;
                OnlineGrade.Visible = false;
            }

            RatingText.X = Name.X + Name.Width + spacing;
            LengthText.X = RatingText.X + RatingText.Width + spacing;
            BpmText.X = LengthText.X + LengthText.Width + spacing;

            UpdateMetadata();
        }

        /// <summary>
        /// </summary>
        public void Select(bool changeWidthInstantly = false)
        {
            Image = SkinManager.Skin?.SongSelect?.MapsetSelected ?? UserInterface.SelectedMapset;

            var fade = 1f;
            var time = 200;

            Name.ClearAnimations();
            Name.FadeTo(fade, Easing.Linear, time);

            RatingText.ClearAnimations();
            RatingText.FadeTo(fade, Easing.Linear, time);

            LongNotePercentage.RemoveAnimations();
            LongNotePercentage.FadeTo(fade, Easing.Linear, time);

            NotesPerSecond.RemoveAnimations();
            NotesPerSecond.FadeTo(fade, Easing.Linear, time);

            BpmText.ClearAnimations();
            BpmText.FadeTo(fade, Easing.Linear, time);

            LengthText.ClearAnimations();
            LengthText.FadeTo(fade, Easing.Linear, time);

            OnlineGrade.ClearAnimations();
            OnlineGrade.FadeTo(fade, Easing.Linear, time);

            ClearAnimations();

            if (changeWidthInstantly)
                Width = ParentMap.Width;
            else
                ChangeWidthTo((int)ParentMap.Width, Easing.OutQuint, time + 400);
        }

        /// <summary>
        /// </summary>
        public void Deselect(bool changeWidthInstantly = false)
        {
            Image = SkinManager.Skin?.SongSelect.MapsetDeselected ?? UserInterface.DeselectedMapset;

            var fade = 0.85f;
            var time = 200;

            Name.ClearAnimations();
            Name.FadeTo(fade, Easing.Linear, time);

            RatingText.ClearAnimations();
            RatingText.FadeTo(fade, Easing.Linear, time);

            LongNotePercentage.RemoveAnimations();
            LongNotePercentage.FadeTo(fade, Easing.Linear, time);

            NotesPerSecond.RemoveAnimations();
            NotesPerSecond.FadeTo(fade, Easing.Linear, time);

            BpmText.ClearAnimations();
            BpmText.FadeTo(fade, Easing.Linear, time);

            LengthText.ClearAnimations();
            LengthText.FadeTo(fade, Easing.Linear, time);

            OnlineGrade.ClearAnimations();
            OnlineGrade.FadeTo(fade, Easing.Linear, time);

            ClearAnimations();

            if (changeWidthInstantly)
                Width = ParentMap.Width - 50;
            else
                ChangeWidthTo((int)ParentMap.Width - 50, Easing.OutQuint, time + 400);
        }

        /// <summary>
        ///     Creates <see cref="Button"/>
        /// </summary>
        private void CreateButton()
        {
            var container = (SongSelectContainer<Map>)ParentMap.Container;

            Button = new SongSelectContainerButton(SkinManager.Skin?.SongSelect?.MapsetHovered ?? UserInterface.SelectedMapset, container.ClickableArea)
            {
                Parent = this,
                Size = Size,
                Alpha = 0,
                Alignment = Alignment.MidCenter,
                UsePreviousSpriteBatchOptions = true,
                Depth = 1
            };

            Button.Clicked += (sender, args) => OnMapClicked();

            Button.RightClicked += (sender, args) =>
            {
                var game = (QuaverGame)GameBase.Game;
                game?.CurrentScreen?.ActivateRightClickOptions(new MapRightClickOptions(ParentMap));
            };
        }

        /// <summary>
        ///     Creates <see cref="Name"/>
        /// </summary>
        private void CreateDifficultyName()
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
            var size = isV2 ? 24 : 26;

            Name = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "DIFFICULTY", size)
            {
                Parent = this,
                Position = new ScalableVector2(PaddingX, 18),
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <summary>
        ///     Creates <see cref="RatingText"/>
        /// </summary>
        private void CreateDifficultyRating()
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
            var size = isV2 ? 24 : 20;

            RatingText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0.0", size)
            {
                Parent = this,
                Position = new ScalableVector2(Name.X + Name.Width + 10, Name.Y),
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates metadata components (LN%, NPS) displayed on the right side
        /// </summary>
        private void CreateMetadata()
        {
            const int spacing = 20;
            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
            var size = isV2 ? 24 : 20;

            LongNotePercentage = new TextKeyValue("LNs:", "10%", size, ColorHelper.HexToColor($"#ffe76b"))
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -PaddingX - spacing,
                UsePreviousSpriteBatchOptions = true
            };

            NotesPerSecond = new TextKeyValue("NPS:", "0", size, ColorHelper.HexToColor($"#ffe76b"))
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = LongNotePercentage.X - LongNotePercentage.Width - spacing,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        ///     Creates BPM and Length text displays (positioned after RatingText)
        /// </summary>
        private void CreateBpmLengthInfo()
        {
            var isV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;
            var size = isV2 ? 24 : 16;

            // Length Text (positioned after RatingText)
            LengthText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00:00", size)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Y = 0,
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            // BPM Text (positioned after LengthText)
            BpmText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "000 BPM", size)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Y = 0,
                Tint = Color.White,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        private void CreateGrade()
        {
            OnlineGrade = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Visible = false,
                X = PaddingX,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        private void PerformHoverAnimation(GameTime gameTime)
        {
            var targetAlpha = Button.IsHovered ? SkinManager.Skin.SongSelect.MapsetPanelHoveringAlpha : 0;

            Button.Alpha = MathHelper.Lerp(Button.Alpha, targetAlpha,
                (float)Math.Min(gameTime.ElapsedGameTime.TotalMilliseconds / 30, 1));
        }

        /// <summary>
        ///     Called when the map has been clicked
        /// </summary>
        private void OnMapClicked()
        {
            if (ParentMap.Container != null)
            {
                var container = (MapScrollContainer)ParentMap.Container;
                container.SelectedIndex.Value = ParentMap.Index;
            }

            // Map is already selected. Second click should be to play the map
            if (ParentMap.IsSelected)
            {
                Logger.Important($"User clicked on map to play: {ParentMap.Item}", LogType.Runtime, false);

                var game = (QuaverGame)GameBase.Game;
                var screen = game.CurrentScreen as SelectionScreen;
                screen?.ExitToGameplay();

                return;
            }

            MapManager.Selected.Value = ParentMap.Item;
            Logger.Important($"User selected map: {ParentMap.Item}", LogType.Runtime, false);
        }

        /// <summary>
        ///     Called when the activated modifiers has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object sender, ModsChangedEventArgs e)
        {
            var difficulty = ParentMap.Item.DifficultyFromMods(ModManager.Mods);
            var color = ColorHelper.DifficultyToColor((float)difficulty);

            Name.Tint = color;
            RatingText.Text = StringHelper.RatingToString(difficulty);
            RatingText.Tint = color;

            UpdateMetadata();
        }

        private void UpdateMetadata()
        {
            if (ParentMap?.Item == null) return;

            // Update NPS
            var nps = Math.Floor(ParentMap.Item.NotesPerSecond * 100) / 100;
            NotesPerSecond.ChangeValue(nps.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Update LN% logic
            var lnText = ((int)ParentMap.Item.LNPercentage).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";

            if (ModManager.Mods.HasFlag(ModIdentifier.NoLongNotes))
                lnText = "0%";
            else if (ModManager.Mods.HasFlag(ModIdentifier.FullLN))
                lnText = "100%";
            else if (ModManager.Mods.HasFlag(ModIdentifier.Inverse))
                lnText = $"{100 - (int)ParentMap.Item.LNPercentage}%";

            LongNotePercentage.ChangeValue(lnText);

            // Re-align metadata — recalculate positions after both sizes are updated
            const int spacing = 20;
            LongNotePercentage.X = -PaddingX - spacing;
            NotesPerSecond.X = LongNotePercentage.X - LongNotePercentage.Width - spacing;
        }
    }
}
