using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Overlays.Volume
{
    public class VolumeControlSwitchRow : Sprite
    {
        private Sprite LeftPanel { get; }
        private Sprite RightPanel { get; }
        private Sprite IconSprite { get; }

        public VolumeControlSlider SliderControl { get; private set; }

        private SpriteTextPlus LabelText { get; }
        private SpriteTextPlus PercentageText { get; }

        public VolumeControlSwitchRow(string name, Texture2D icon, Vector2 iconSize, VolumeControlSlider sliderControl)
        {
            SliderControl = sliderControl;

            // This is essentially a hit box for the hover detection
            Alignment = Alignment.TopLeft;
            Size = new ScalableVector2(480, 78);
            X = 0;
            Tint = Color.Transparent;

            LabelText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), name, 22)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = 0,
                Y = 0,
                Tint = SkinManager.Skin.VolumeController.VolumeControllerTitlesColor
            };

            PercentageText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0%", 22)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                X = 0,
                Y = 0,
                Tint = SkinManager.Skin.VolumeController.VolumeControllerPercentsColor
            };

            // Panels shifted down by (13px text slot + 15px spacing) = 28px
            const float panelY = 28;

            // Left panel
            LeftPanel = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Image = UserInterface.VolumeSwitchButtonLeft,
                Width = 55,
                Height = 50,
                Y = panelY,
                Tint = SkinManager.Skin.VolumeController.VolumeLeftPanelWithIconColor
            };

            // Right panel
            RightPanel = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Image = UserInterface.VolumeSwitchButtonRight,
                X = 55,
                Y = panelY,
                Width = 425,
                Height = 50,
                Tint = SkinManager.Skin.VolumeController.VolumeRightPanelColor
            };

            // Icon centered inside LeftPanel
            IconSprite = new Sprite
            {
                Parent = LeftPanel,
                Alignment = Alignment.MidCenter,
                Image = icon,
                Size = new ScalableVector2(iconSize.X, iconSize.Y),
                Tint = SkinManager.Skin.VolumeController.VolumeIconsColor
            };

            // Reparent the existing slider control to be centered within the right panel
            SliderControl.Parent = RightPanel;
            SliderControl.Alignment = Alignment.MidLeft;
            SliderControl.X = 10;
            SliderControl.Y = 0;

            SliderControl.BindedValue.ValueChanged += OnSliderValueChanged;
            UpdatePercentageText();

            SkinManager.SkinLoaded += OnSkinLoaded;
        }

        private void OnSkinLoaded(object? sender, SkinReloadedEventArgs e)
        {
            LeftPanel.Tint = SkinManager.Skin.VolumeController.VolumeLeftPanelWithIconColor;
            RightPanel.Tint = SkinManager.Skin.VolumeController.VolumeRightPanelColor;
            
            // Re-apply hover tints if necessary, or just base colors
            Deselect();
        }

        private void OnSliderValueChanged(object? sender, Wobble.Bindables.BindableValueChangedEventArgs<int> e)
        {
            UpdatePercentageText();
        }

        private void UpdatePercentageText()
        {
            if (SliderControl.BindedValue.MaxValue <= 0)
                return;

            float val = (float)SliderControl.BindedValue.Value / SliderControl.BindedValue.MaxValue * 100;
            PercentageText.Text = $"{(int)val}%";
        }

        public void Select()
        {
            IconSprite.Tint = SkinManager.Skin.VolumeController.VolumeHoverColor;
            SliderControl.Select();
            LabelText.Tint = SkinManager.Skin.VolumeController.VolumeHoverColor;
            PercentageText.Tint = SkinManager.Skin.VolumeController.VolumeHoverColor;
        }

        public void Deselect()
        {
            IconSprite.Tint = SkinManager.Skin.VolumeController.VolumeIconsColor;
            SliderControl.Deselect();
            LabelText.Tint = SkinManager.Skin.VolumeController.VolumeControllerTitlesColor;
            PercentageText.Tint = SkinManager.Skin.VolumeController.VolumeControllerPercentsColor;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            SliderControl.BindedValue.ValueChanged -= OnSliderValueChanged;
            base.Destroy();
        }
    }
}
