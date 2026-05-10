using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Backgrounds;
using Quaver.Shared.Skinning;
using Wobble.Bindables;
using Wobble.Graphics.Animations;
using Wobble.Graphics.UI;

namespace Quaver.Shared.Screens.Selection.UI.Background
{
    public class SongSelectBackground : BackgroundImage
    {
        /// <summary>
        ///     Particle animation overlay
        /// </summary>
        private BackgroundParticleSystem ParticleSystem { get; set; }

        public SongSelectBackground() : base(SkinManager.Skin?.Background ?? UserInterface.UniversalBackground, 
            (SkinManager.Skin != null && SkinManager.Skin.SongSelect.DisplayMapBackground) ? 0 : (100 - ConfigManager.BackgroundBrightness.Value), 
            false)
        {
            MapManager.Selected.ValueChanged += OnMapChanged;
            BackgroundHelper.Loaded += OnBackgroundLoaded;
            ConfigManager.BackgroundBrightness.ValueChanged += OnBackgroundBrightnessChanged;

            // Create particle system overlay
            ParticleSystem = new BackgroundParticleSystem()
            {
                Parent = this,
                Visible = Config.ConfigManager.DisplayBackgroundParticles?.Value ?? true
            };

            // Subscribe to config changes
            if (Config.ConfigManager.DisplayBackgroundParticles != null)
                Config.ConfigManager.DisplayBackgroundParticles.ValueChanged += OnDisplayParticlesChanged;
        }

        /// <summary>
        ///     Handle config change for particle visibility
        /// </summary>
        private void OnDisplayParticlesChanged(object sender, Wobble.Bindables.BindableValueChangedEventArgs<bool> e)
        {
            if (ParticleSystem != null)
                ParticleSystem.Visible = e.Value;
        }

        private void OnBackgroundBrightnessChanged(object sender, Wobble.Bindables.BindableValueChangedEventArgs<int> e)
        {
            if (SkinManager.Skin == null || !SkinManager.Skin.SongSelect.DisplayMapBackground)
            {
                BrightnessSprite.ClearAnimations();
                BrightnessSprite.FadeTo((100 - e.Value) / 100f, Easing.Linear, 250);
            }
        }

        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;
            BackgroundHelper.Loaded -= OnBackgroundLoaded;
            ConfigManager.BackgroundBrightness.ValueChanged -= OnBackgroundBrightnessChanged;

            if (Config.ConfigManager.DisplayBackgroundParticles != null)
                Config.ConfigManager.DisplayBackgroundParticles.ValueChanged -= OnDisplayParticlesChanged;

            base.Destroy();
        }

        private void OnMapChanged(object sender, BindableValueChangedEventArgs<Map> e)
        {
            if (SkinManager.Skin == null || !SkinManager.Skin.SongSelect.DisplayMapBackground)
            {
                Image = SkinManager.Skin?.Background ?? UserInterface.UniversalBackground;
                BrightnessSprite.ClearAnimations();
                BrightnessSprite.FadeTo((100 - ConfigManager.BackgroundBrightness.Value) / 100f, Easing.Linear, 250);
                return;
            }

            if (MapManager.GetBackgroundPath(e.Value) == MapManager.GetBackgroundPath(e.OldValue))
                return;

            BrightnessSprite.ClearAnimations();
            BrightnessSprite.FadeTo(1, Easing.Linear, 250);
        }

        private void OnBackgroundLoaded(object sender, BackgroundLoadedEventArgs e)
        {
            if (SkinManager.Skin == null || !SkinManager.Skin.SongSelect.DisplayMapBackground)
                return;

            if (e.Map != MapManager.Selected.Value)
                return;

            Image = e.Texture;
            BrightnessSprite.ClearAnimations();

            var brightness = SkinManager.Skin.SongSelect.MapBackgroundBrightness;
            BrightnessSprite.FadeTo((100 - brightness) / 100f, Easing.Linear, 250);
        }
    }
}
