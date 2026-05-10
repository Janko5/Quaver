using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Skinning;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Graphics.UI.Form;
using Wobble.Input;
using Quaver.Shared.Graphics.Components;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Graphics.Overlays.Volume
{
    public class VolumeControl : Container
    {
        private static int PANEL_WIDTH { get; } = 510;
        private static int PANEL_HEIGHT { get; } = 280;

        private VolumeControlSwitchRow? FocusedRow { get; set; }

        public double TimeInactive { get; private set; } = 2500;
        private double TimeElapsedSinceLastVolumeChange { get; set; } = 50;

        /// <summary>
        ///     If the input focus is currently grabbed by this container.
        /// </summary>
        private bool _isInputFocused;

        public bool IsManuallyToggled { get; set; }

        public List<VolumeControlSwitchRow> Rows { get; }

        private Sprite BackgroundPanel { get; }
        private NineSliceSprite? UniversalHeader { get; set; }
        private NineSliceSprite? KeysoundsHeader { get; set; }
        private ModifierSwitch? KeysoundsSwitch { get; set; }
        private ImageButton DimScreen { get; }

        public VolumeControl()
        {
            Alignment = Alignment.BotRight;
            Size = new ScalableVector2(PANEL_WIDTH, PANEL_HEIGHT);
            X = Width + 50;
            Y = -95;

            // Header Setup
            var headerTexture = UserInterface.UniversalHeader;
            if (headerTexture != null)
            {
                var headerText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Volume Controller", 22)
                {
                    Alignment = Alignment.MidCenter
                };

                UniversalHeader = new NineSliceSprite(headerTexture, new SliceMargins(20, 20, 0, 0))
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    Y = -headerTexture.Height - 10,
                    Width = headerText.Width + 20,
                    Height = headerTexture.Height,
                    Tint = SkinManager.Skin.VolumeController.VolumeControllerHeaderColor
                };

                headerText.Parent = UniversalHeader;
            }

            // Keysounds Header Setup
            if (headerTexture != null)
            {
                var keysoundsText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Keysounds", 22)
                {
                    Alignment = Alignment.MidLeft,
                    X = 10
                };

                KeysoundsHeader = new NineSliceSprite(headerTexture, new SliceMargins(20, 20, 0, 0))
                {
                    Parent = this,
                    Alignment = Alignment.TopRight,
                    Y = UniversalHeader?.Y ?? -70,
                    Width = keysoundsText.Width + 105,
                    Height = headerTexture.Height,
                    Tint = SkinManager.Skin.VolumeController.VolumeControllerHeaderColor
                };

                keysoundsText.Parent = KeysoundsHeader;

                KeysoundsSwitch = new ModifierSwitch(ConfigManager.EnableKeysounds.Value, isOn =>
                {
                    ConfigManager.EnableKeysounds.Value = isOn;
                    ConfigManager.EnableHitsounds.Value = !isOn;
                })
                {
                    Parent = KeysoundsHeader,
                    Alignment = Alignment.MidRight,
                    X = -10,
                    Size = new ScalableVector2(70, 24)
                };

                ConfigManager.EnableKeysounds.ValueChanged += OnEnableKeysoundsChanged;
            }

            // Main Panel Setup
            BackgroundPanel = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopCenter,
                Size = new ScalableVector2(PANEL_WIDTH, PANEL_HEIGHT),
                Image = UserInterface.VolumeControllerPanel,
                Tint = SkinManager.Skin.VolumeController.VolumeControllerBackgroundColor
            };

            // Dim Screen
            DimScreen = new ImageButton(WobbleAssets.WhiteBox, (sender, args) => { IsManuallyToggled = false; TimeInactive = 2500; })
            {
                Parent = this,
                Tint = Color.Black,
                Alpha = 0f,
                Visible = false,
                IsClickable = false,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height - 100)
            };
            Children.Remove(DimScreen);
            Children.Insert(0, DimScreen);

            Rows = new()
            {
                new("Master", UserInterface.VolumeIconMaster, new(32, 26), new(405, ConfigManager.VolumeGlobal)),
                new("Music", UserInterface.VolumeIconMusic, new(26, 26), new(405, ConfigManager.VolumeMusic)),
                new("Effects", UserInterface.VolumeIconEffect, new(30, 26), new(405, ConfigManager.VolumeEffect))
            };

            PositionRows();

            SkinManager.SkinLoaded += OnSkinLoaded;
        }

        private void OnSkinLoaded(object? sender, SkinReloadedEventArgs e)
        {
            if (UniversalHeader != null)
                UniversalHeader.Tint = SkinManager.Skin.VolumeController.VolumeControllerHeaderColor;

            if (KeysoundsHeader != null)
                KeysoundsHeader.Tint = SkinManager.Skin.VolumeController.VolumeControllerHeaderColor;

            BackgroundPanel.Tint = SkinManager.Skin.VolumeController.VolumeControllerBackgroundColor;
        }

        private void OnEnableKeysoundsChanged(object? sender, BindableValueChangedEventArgs<bool> e)
        {
            if (KeysoundsSwitch != null && KeysoundsSwitch.IsOn != e.Value)
                KeysoundsSwitch.Toggle();
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            DimScreen?.Destroy();
            SkinManager.SkinLoaded -= OnSkinLoaded;
            ConfigManager.EnableKeysounds.ValueChanged -= OnEnableKeysoundsChanged;

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            TimeElapsedSinceLastVolumeChange += gameTime.ElapsedGameTime.TotalMilliseconds;
            TimeInactive += gameTime.ElapsedGameTime.TotalMilliseconds;

            base.Update(gameTime);

            HandleInput();
            HandleSliderColorChanges();

            if (!IsManuallyToggled && TimeInactive >= 800 && Animations.Count == 0 && X < 0)
                MoveToX(Width + 50, Easing.OutQuint, 450);
            else if (!IsManuallyToggled && TimeInactive == 0f && Animations.Count == 0 && X > 0)
                MoveToX(-30, Easing.OutQuint, 450);

            if (X > 0 && Rows.Count > 0)
                FocusedRow = Rows.First();

            if (DimScreen != null)
            {
                var borderHeight = Quaver.Shared.Graphics.Menu.Border.MenuBorder.HEIGHT;
                DimScreen.Size = new ScalableVector2(WindowManager.Width, WindowManager.Height - borderHeight * 2);
                DimScreen.X = -AbsolutePosition.X;
                DimScreen.Y = -AbsolutePosition.Y + borderHeight;
                
                float fullyHiddenX = Width + 50;
                float fullyVisibleX = -30;
                float percentVisible = 1f - MathHelper.Clamp((X - fullyVisibleX) / (fullyHiddenX - fullyVisibleX), 0f, 1f);
                
                DimScreen.Alpha = 0.85f * percentVisible;
                DimScreen.Visible = percentVisible > 0.01f;
                DimScreen.IsClickable = DimScreen.Visible;

                // Handle InputStack focus
                if (IsManuallyToggled && DimScreen.Visible && !_isInputFocused)
                {
                    Wobble.Graphics.UI.Buttons.ButtonManager.PushInputRoot(this);
                    _isInputFocused = true;
                }
                else if ((!IsManuallyToggled || !DimScreen.Visible) && _isInputFocused)
                {
                    Wobble.Graphics.UI.Buttons.ButtonManager.PopInputRoot();
                    _isInputFocused = false;
                }
            }
        }

        public void ToggleManual()
        {
            if (X < 10)
            {
                IsManuallyToggled = false;
                TimeInactive = 2500;
            }
            else
            {
                IsManuallyToggled = true;
                TimeInactive = 0;
                MoveToX(-30, Easing.OutQuint, 450);
            }
        }

        private void PositionRows()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                row.Parent = BackgroundPanel;
                row.X = 15;

                if (i == 0)
                {
                    row.Y = 13;
                    continue;
                }

                var previous = Rows[i - 1];
                row.Y = previous.Y + previous.Height + 10;
            }
        }

        private void HandleInput()
        {
            SetFocusedSlider();

            var isAltHeld = KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt) || KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt);

            // Check for hovered row - verify mouse is within row bounds
            var hoveredRow = Rows.Find(x => GraphicsHelper.RectangleContains(x.ScreenRectangle, MouseManager.CurrentState.Position));

            if (MouseManager.IsScrolling)
            {
                // Capture direction BEFORE consuming
                var scrolledUp = MouseManager.IsScrollingUp(ConfigManager.InvertScrolling.Value);
                var scrolledDown = MouseManager.IsScrollingDown(ConfigManager.InvertScrolling.Value);

                if (hoveredRow != null || isAltHeld)
                {
                    TimeInactive = 0;
                    MouseManager.ConsumeScroll();

                    if (scrolledUp)
                        UpdateVolume(5);
                    else if (scrolledDown)
                        UpdateVolume(-5);
                }
            }

            // Keyboard and arrows still require Alt
            if (!isAltHeld)
                return;

            if (KeyboardManager.IsUniqueKeyPress(Keys.Up) || KeyboardManager.IsUniqueKeyPress(Keys.Down) ||
                KeyboardManager.IsUniqueKeyPress(Keys.Left) || KeyboardManager.IsUniqueKeyPress(Keys.Right))
            {
                TimeInactive = 0;
            }

            if (KeyboardManager.CurrentState.IsKeyDown(Keys.Right))
            {
                if (TimeElapsedSinceLastVolumeChange >= 5)
                    UpdateVolume(1);
            }
            else if (KeyboardManager.CurrentState.IsKeyDown(Keys.Left))
            {
                if (TimeElapsedSinceLastVolumeChange >= 5)
                    UpdateVolume(-1);
            }
        }

        private void SetFocusedSlider()
        {
            var focused = Rows.Find(x => x.SliderControl.Slider.MouseInHoldSequence) ?? Rows.Find(x => Wobble.Graphics.GraphicsHelper.RectangleContains(x.ScreenRectangle, MouseManager.CurrentState.Position) || x.SliderControl.Slider.IsHovered);

            if (focused != null && X <= 0)
            {
                FocusedRow = focused;
                TimeInactive = 0;
            }

            if (KeyboardManager.IsUniqueKeyPress(Keys.Up) && (KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt) || KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt)))
            {
                if (FocusedRow == null) return;

                SkinManager.Skin?.SoundHover?.CreateChannel()?.Play();
                TimeInactive = 0;

                if (FocusedRow == Rows.First())
                {
                    FocusedRow = Rows.Last();
                    return;
                }

                var index = Rows.IndexOf(FocusedRow);
                FocusedRow = Rows[index - 1];
                return;
            }

            if (KeyboardManager.IsUniqueKeyPress(Keys.Down) && (KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt) || KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt)))
            {
                if (FocusedRow == null) return;

                SkinManager.Skin?.SoundHover?.CreateChannel()?.Play();
                TimeInactive = 0;

                if (FocusedRow == Rows.Last())
                {
                    FocusedRow = Rows.First();
                    return;
                }

                var index = Rows.IndexOf(FocusedRow);
                FocusedRow = Rows[index + 1];
            }
        }

        private void UpdateVolume(int amount)
        {
            if (FocusedRow == null)
                return;

            FocusedRow.SliderControl.BindedValue.Value += amount;
            TimeInactive = 0;
            TimeElapsedSinceLastVolumeChange = 0;
        }

        private void HandleSliderColorChanges()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] == FocusedRow)
                    Rows[i].Select();
                else
                    Rows[i].Deselect();
            }
        }
    }
}
