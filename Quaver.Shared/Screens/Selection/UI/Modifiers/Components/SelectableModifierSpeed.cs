using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Modifiers.Mods;
using Quaver.Shared.Online;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Form;
using Wobble.Input;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers.Components
{
    public class SelectableModifierSpeed : SelectableModifier
    {
        #region Layout Constants (Figma)
        
        private const float TrackWidth = 310f;
        private const float TrackHeight = 20f;
        private const float TrackPadding = 10f; // Marginesy zewnętrzne (lewy i prawy)
        private const float ThumbWidth = 10f;
        private const float TextIndicatorWidth = 52f; // Szerokość miejsca na tekst "x1.00"
        private const float TextGap = 10f; // Odstęp między toru a tekstem

        // Wzory wyliczane na podstawie bazy
        private const float SliderPanelWidth = TrackPadding + TrackWidth + TextGap + TextIndicatorWidth + TrackPadding; // 10+310+10+52+10 = 392
        private const float SliderPanelHeight = PanelHeight;
        private const float LeftPanelWidth = TotalWidth - Padding - SliderPanelWidth; // 705-10-392 = 303
        private const float LeftPanelHeight = PanelHeight;

        private const int FontSize = 22;
        private const float RateChangerXOffset = -30f;
        private const int RateRoundingPrecision = 2;
        private const int DoubleClickThresholdMs = 500;

        #endregion

        #region UI Components

        /// <summary>
        ///     V1 Layout Components
        /// </summary>
        private HorizontalSelector RateChanger { get; set; }

        /// <summary>
        ///    V2 Layout Components
        /// </summary>
        private SpriteTextPlus SpeedIndicator { get; set; }
        private NineSliceSprite SliderBackground { get; set; }
        private NineSliceSprite SliderTrack { get; set; }
        private NineSliceSprite SliderActiveBar { get; set; }
        private NineSliceSprite SliderThumb { get; set; }

        #endregion

        #region Data Sources

        /// <summary>
        ///    Speed values as floats - Single source of truth.
        /// </summary>
        private static readonly float[] SpeedValues = {
            0.50f, 0.55f, 0.60f, 0.65f, 0.70f, 0.75f, 0.80f, 0.85f, 0.90f, 0.95f,
            1.00f, 1.05f, 1.10f, 1.15f, 1.20f, 1.25f, 1.30f, 1.35f, 1.40f, 1.45f,
            1.50f, 1.55f, 1.60f, 1.65f, 1.70f, 1.75f, 1.80f, 1.85f, 1.90f, 1.95f, 2.00f
        };

        /// <summary>
        ///    Generates string representation for V1 selector on demand.
        /// </summary>
        private static List<string> Speeds => SpeedValues.Select(v => $"{v:0.0}x").ToList();

        /// <summary>
        ///    Mapping speed rates to colors.
        /// </summary>
        private static readonly Dictionary<float, Color> SpeedColors = new Dictionary<float, Color>
        {
            { 0.50f, new Color(90, 108, 255) }, { 0.55f, new Color(95, 107, 253) }, { 0.60f, new Color(100, 106, 251) },
            { 0.65f, new Color(104, 106, 249) }, { 0.70f, new Color(109, 105, 247) }, { 0.75f, new Color(113, 104, 245) },
            { 0.80f, new Color(118, 103, 243) }, { 0.85f, new Color(123, 102, 241) }, { 0.90f, new Color(128, 101, 239) },
            { 0.95f, new Color(132, 100, 237) }, { 1.00f, new Color(137, 99, 235) }, { 1.05f, new Color(141, 98, 233) },
            { 1.10f, new Color(145, 97, 231) }, { 1.15f, new Color(150, 97, 229) }, { 1.20f, new Color(155, 96, 227) },
            { 1.25f, new Color(160, 94, 224) }, { 1.30f, new Color(164, 93, 222) }, { 1.35f, new Color(164, 94, 223) },
            { 1.40f, new Color(169, 93, 221) }, { 1.45f, new Color(173, 92, 219) }, { 1.50f, new Color(178, 91, 217) },
            { 1.55f, new Color(183, 90, 215) }, { 1.60f, new Color(187, 89, 213) }, { 1.65f, new Color(192, 89, 211) },
            { 1.70f, new Color(197, 88, 208) }, { 1.75f, new Color(201, 87, 206) }, { 1.80f, new Color(206, 86, 204) },
            { 1.85f, new Color(210, 85, 202) }, { 1.90f, new Color(215, 84, 200) }, { 1.95f, new Color(220, 83, 198) },
            { 2.00f, new Color(224, 82, 196) }
        };

        #endregion

        #region State Management

        private bool IsDraggingSlider { get; set; }
        private float CurrentPreviewRate { get; set; }
        private long LastClickTime { get; set; }

        private readonly bool _useV2;

        #endregion

        public SelectableModifierSpeed(int width) : base(width, new ModSpeed(ModIdentifier.None))
        {
            _useV2 = IsV2;

            ModManager.ModsChanged += OnModsChanged;
            Clicked += OnClicked;

            UpdateUIState();
        }

        #region Layout Setup

        protected override void SetupV1Layout(int width)
        {
            base.SetupV1Layout(width);

            var game = GameBase.Game as QuaverGame;
            var depth = game?.CurrentScreen?.Type == QuaverScreenType.Editor ? -1 : 0;

            RateChanger = new HorizontalSelector(Speeds, new ScalableVector2(100, 32),
                FontManager.GetWobbleFont(Fonts.InterBold), FontSize,
                FontAwesome.Get(FontAwesomeIcon.fa_chevron_pointing_to_the_left),
                FontAwesome.Get(FontAwesomeIcon.fa_right_chevron), new ScalableVector2(20, 20), 0, (val, index) => ApplyRate(SpeedValues[index]), GetCurrentRateIndex())
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = RateChangerXOffset,
                UsePreviousSpriteBatchOptions = true,
                Alpha = 0,
                ButtonSelectLeft = { UsePreviousSpriteBatchOptions = true, Depth = depth },
                ButtonSelectRight = { UsePreviousSpriteBatchOptions = true, Depth = depth },
                SelectedItemText = { UsePreviousSpriteBatchOptions = true, Tint = Mod.ModColor },
                SetChildrenAlpha = true
            };
        }

        protected override void SetupV2Layout()
        {
            base.SetupV2Layout();

            if (Background != null)
            {
                Background.Size = new ScalableVector2(LeftPanelWidth, LeftPanelHeight);
                Background.Alignment = Alignment.MidLeft;
            }

            SliderBackground = new NineSliceSprite(SkinManager.Skin?.SongSelect?.ModifierBackground ?? UserInterface.ModifierBackground, new SliceMargins(10))
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = LeftPanelWidth + Padding,
                Size = new ScalableVector2(SliderPanelWidth, SliderPanelHeight),
                UsePreviousSpriteBatchOptions = true,
                SetChildrenAlpha = true
            };

            SliderTrack = new NineSliceSprite(SkinManager.Skin?.SongSelect?.ModifierSliderElement ?? UserInterface.ModifierSliderElement, new SliceMargins(5))
            {
                Parent = SliderBackground,
                Alignment = Alignment.MidLeft,
                X = TrackPadding,
                Size = new ScalableVector2(TrackWidth, TrackHeight),
                Tint = SkinManager.Skin.SongSelect.ModifiersSliderBackgroundColor,
                UsePreviousSpriteBatchOptions = true
            };

            SliderActiveBar = new NineSliceSprite(SkinManager.Skin?.SongSelect?.ModifierSliderElement ?? UserInterface.ModifierSliderElement, new SliceMargins(5))
            {
                Parent = SliderTrack,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(0, TrackHeight),
                UsePreviousSpriteBatchOptions = true
            };

            SliderThumb = new NineSliceSprite(SkinManager.Skin?.SongSelect?.ModifierSliderElement ?? UserInterface.ModifierSliderElement, new SliceMargins(5))
            {
                Parent = SliderTrack,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(ThumbWidth, TrackHeight),
                Tint = SkinManager.Skin.SongSelect.ModifiersSliderThumbColor,
                UsePreviousSpriteBatchOptions = true
            };

            SpeedIndicator = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "x1.00", FontSize)
            {
                Parent = SliderBackground,
                Alignment = Alignment.MidLeft,
                X = SliderTrack.X + SliderTrack.Width + TextGap,
                UsePreviousSpriteBatchOptions = true
            };

            UpdateUIState();
        }

        #endregion

        #region Logic & Operations

        private void ApplyRate(float rate)
        {
            if (!CanActivateMultiplayerMod())
            {
                UpdateUIState();
                return;
            }

            if (Math.Abs(rate - 1.0f) < 0.001f)
                ModManager.RemoveSpeedMods(true);
            else
                ModManager.AddMod(ModHelper.GetModsFromRate((float)Math.Round(rate, RateRoundingPrecision)), true);
        }

        private void UpdateUIState()
        {
            var rate = ModHelper.GetRateFromMods(ModManager.Mods);
            CurrentPreviewRate = rate;

            if (_useV2)
                UpdateV2Visuals(rate);
            else
                UpdateV1Visuals(rate);
        }

        private void UpdateV1Visuals(float rate)
        {
            if (RateChanger?.SelectedItemText == null) return;
            var index = GetCurrentRateIndex();
            RateChanger.SelectedIndex = index;
            RateChanger.SelectedItemText.Text = Speeds[index];
        }

        private void UpdateV2Visuals(float rate)
        {
            if (SliderTrack == null || SliderThumb == null) return;

            var minRate = SpeedValues.First();
            var maxRate = SpeedValues.Last();
            var percent = (rate - minRate) / (maxRate - minRate);

            SliderThumb.X = percent * (SliderTrack.Width - SliderThumb.Width);
            SliderActiveBar.Width = SliderThumb.X + SliderThumb.Width;

            var color = SpeedColors.TryGetValue((float)Math.Round(rate, RateRoundingPrecision), out var c) ? c : Mod.ModColor;
            SliderActiveBar.Tint = color;
            SpeedIndicator.Tint = color;
            SpeedIndicator.Text = $"x{rate:0.00}";
            
            if (Name != null)
                Name.Tint = color;

            if (Tooltip?.Border != null)
                Tooltip.Border.Tint = color;

            UpdateIcon(rate);
        }

        private void UpdateIcon(float rate)
        {
            var newImage = Math.Abs(rate - 1.0f) < 0.001f
                ? TextureManager.Load(@"Quaver.Resources/Textures/UI/Mods/1.0x.png")
                : ModManager.GetTexture(ModHelper.GetModsFromRate((float)Math.Round(rate, RateRoundingPrecision)));

            if (Icon == null)
                return;

            if (Icon.Image != newImage)
                Icon.Image = newImage;

            Icon.Tint = Color.White;
        }

        private int GetCurrentRateIndex()
        {
            var rate = (float)Math.Round(ModHelper.GetRateFromMods(ModManager.Mods), RateRoundingPrecision);
            var index = Array.IndexOf(SpeedValues, rate);
            return index == -1 ? Array.IndexOf(SpeedValues, 1.0f) : index;
        }

        protected override Texture2D GetTexture()
        {
            var rate = (float)Math.Round(ModHelper.GetRateFromMods(ModManager.Mods), RateRoundingPrecision);
            
            try
            {
                return Math.Abs(rate - 1.0f) < 0.001f
                    ? TextureManager.Load(@"Quaver.Resources/Textures/UI/Mods/1.0x.png")
                    : ModManager.GetTexture(ModHelper.GetModsFromRate(rate));
            }
            catch
            {
                return base.GetTexture();
            }
        }

        #endregion

        #region Update & Input Handling

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_useV2)
            {
                HandleV2Input();
                PerformV2HoverAnimations();
                SyncV2Alpha();
            }
            else
            {
                if (RateChanger?.SelectedItemText != null && RateChanger?.ButtonSelectLeft != null && RateChanger?.ButtonSelectRight != null)
                {
                    RateChanger.SelectedItemText.Alpha = Name.Alpha;
                    RateChanger.ButtonSelectLeft.Alpha = Name.Alpha;
                    RateChanger.ButtonSelectRight.Alpha = Name.Alpha;
                }
            }
        }

        private void HandleV2Input()
        {
            var mouse = MouseManager.CurrentState;
            var sliderHovered = SliderBackground.ScreenRectangle.Contains(mouse.Position);

            if (JudgementWindowsDropdown.AnyOpened)
                return;

            if (MouseManager.IsUniquePress(MouseButton.Left) && sliderHovered && CanActivateMultiplayerMod())
                IsDraggingSlider = true;

            if (mouse.LeftButton != ButtonState.Pressed && IsDraggingSlider)
            {
                IsDraggingSlider = false;
                ApplyRate(CurrentPreviewRate);
            }

            if (IsDraggingSlider)
            {
                var relativeX = mouse.X - SliderTrack.ScreenRectangle.X;
                var percent = Microsoft.Xna.Framework.MathHelper.Clamp(relativeX / SliderTrack.ScreenRectangle.Width, 0f, 1f);
                var stepIndex = (int)Math.Round(percent * (SpeedValues.Length - 1));
                
                CurrentPreviewRate = SpeedValues[stepIndex];
                UpdateV2Visuals(CurrentPreviewRate);
            }
        }

        private void PerformV2HoverAnimations()
        {
            var mouse = MouseManager.CurrentState.Position;
            var skin = SkinManager.Skin?.SongSelect;
            var jwOpened = JudgementWindowsDropdown.AnyOpened;
            
            // Left Panel Highlight - Driven by base IsHovered (constrained to Left Panel)
            if (Background != null)
            {
                var hoveredImage = skin?.ModifierBackgroundHovered ?? UserInterface.ModifierBackground;
                var normalImage = skin?.ModifierBackground ?? UserInterface.ModifierBackground;
                var shouldHighlight = IsHovered && !jwOpened;
                Background.Image = shouldHighlight ? hoveredImage : normalImage;
            }

            // Slider Background Highlight - Independent of Left Panel hover state
            if (SliderBackground != null)
            {
                var hoveredImage = skin?.ModifierBackgroundHovered ?? UserInterface.ModifierBackground;
                var normalImage = skin?.ModifierBackground ?? UserInterface.ModifierBackground;
                var rightHovered = SliderBackground.ScreenRectangle.Contains(mouse) && !jwOpened;
                
                SliderBackground.Image = rightHovered ? hoveredImage : normalImage;
            }
        }

        private void SyncV2Alpha()
        {
            // Inherit alpha from Name (base component sync)
            if (SliderBackground != null) SliderBackground.Alpha = Name.Alpha;
            if (SpeedIndicator != null) SpeedIndicator.Alpha = Name.Alpha;
            if (SliderTrack != null) SliderTrack.Alpha = Name.Alpha;
            if (SliderActiveBar != null) SliderActiveBar.Alpha = Name.Alpha;
            if (SliderThumb != null) SliderThumb.Alpha = Name.Alpha;
        }

        protected override bool IsMouseInClickArea()
        {
            if (!_useV2) return base.IsMouseInClickArea();
            
            // Restrictions for base button clicks/tooltips - only trigger on icons/text part
            var rect = new Rectangle((int)AbsolutePosition.X, (int)AbsolutePosition.Y, (int)(LeftPanelWidth * WindowManager.ScreenScale.X), (int)AbsoluteSize.Y);
            return rect.Contains(MouseManager.CurrentState.Position);
        }

        #endregion

        #region Events

        private void OnClicked(object sender, EventArgs e)
        {
            var time = GameBase.Game.TimeRunning;
            if (time - LastClickTime <= DoubleClickThresholdMs && CanActivateMultiplayerMod())
            {
                ApplyRate(1.0f);
                LastClickTime = 0;
            }
            else
            {
                LastClickTime = time;
            }
        }

        private void OnModsChanged(object sender, ModsChangedEventArgs e) => ScheduleUpdate(UpdateUIState);

        public override void Destroy()
        {
            ModManager.ModsChanged -= OnModsChanged;
            base.Destroy();
        }

        #endregion
    }
}
