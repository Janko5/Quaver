using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Helpers;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel
{
    /// <summary>
    ///     Simplified map counter for v2.0 design
    /// </summary>
    public class FilterPanelMapsAvailableV2 : Container
    {
        /// <summary>
        /// </summary>
        private Bindable<List<Mapset>> AvailableMapsets { get; }

        /// <summary>
        ///    The amount of maps there are
        /// </summary>
        public SpriteTextPlus TextCount { get; private set; }

        /// <summary>
        ///     The text that displays "Maps"
        /// </summary>
        public SpriteTextPlus TextMaps { get; private set; }

        /// <summary>
        ///     The help icon that displays a tooltip
        /// </summary>
        public ImageButton HelpIcon { get; private set; }

        /// <summary>
        ///     The amount of space between TextCount and TextMaps
        /// </summary>
        private const int TextSpacing = 4;

        /// <summary>
        /// </summary>
        public FilterPanelMapsAvailableV2(Bindable<List<Mapset>> availableMapsets)
        {
            AvailableMapsets = availableMapsets;

            CreateTextCount();
            CreateTextMaps();
            CreateHelpIcon();

            UpdateText();

            AvailableMapsets.ValueChanged += OnAvailableMapsetsChanged;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (HelpIcon != null)
            {
                HelpIcon.Tint = HelpIcon.IsHovered
                    ? SkinManager.Skin.SearchHelpColorActive
                    : SkinManager.Skin.SearchHelpColorNotActive;
            }
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            AvailableMapsets.ValueChanged -= OnAvailableMapsetsChanged;

            base.Destroy();
        }

        /// <summary>
        ///     Creates the text that displays the map count
        /// </summary>
        private void CreateTextCount()
        {
            TextCount = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "0", 20)
            {
                Parent = this,
                Tint = SkinManager.Skin.SearchCounterTextColor
            };
        }

        /// <summary>
        ///     Creates TextMaps
        /// </summary>
        private void CreateTextMaps()
        {
            TextMaps = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Maps", 20)
            {
                Parent = this,
                Tint = SkinManager.Skin.SearchCounterTextColor,
                X = TextCount.Width + TextSpacing
            };
        }

        /// <summary>
        ///     Creates the help icon
        /// </summary>
        private void CreateHelpIcon()
        {
            HelpIcon = new ImageButton(FontAwesome.Get(FontAwesomeIcon.fa_question_mark_on_a_circular_black_background), (sender, args) => { })
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(16, 16),
                Tint = SkinManager.Skin.SearchHelpColorNotActive,
                X = TextMaps.X + TextMaps.Width + 10
            };
        }

        /// <summary>
        ///     Updates the text with the proper state and updates the container size
        /// </summary>
        private void UpdateText()
        {
            ScheduleUpdate(() =>
            {
                var count = GetMapCount();

                TextCount.Text = $"{count:n0}";
                TextMaps.X = TextCount.Width + TextSpacing;

                if (HelpIcon != null)
                    HelpIcon.X = TextMaps.X + TextMaps.Width + 10;

                Size = new ScalableVector2((int)(HelpIcon.X + HelpIcon.Width),
                    (int)TextMaps.Height);
            });
        }

        /// <summary>
        ///     Gets the total amount of maps in AvailableMapsets
        /// </summary>
        private int GetMapCount()
        {
            var total = 0;

            foreach (var mapset in AvailableMapsets.Value)
            {
                if (mapset.Maps == null || mapset.Maps.Count == 0)
                    continue;

                total += mapset.Maps.Count;
            }

            return total;
        }

        /// <summary>
        ///     Called when the available mapsets has changed.
        /// </summary>
        private void OnAvailableMapsetsChanged(object sender, BindableValueChangedEventArgs<List<Mapset>> e) => UpdateText();
    }
}
