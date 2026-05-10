using Microsoft.Xna.Framework.Media;
using osu.Shared;
using Quaver.API.Helpers;
using Quaver.API.Maps.Processors.Scoring;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Judgements;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.MapInformation.Metadata;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.MapInformation
{
    public class FilterPanelMapInfo : ScrollContainer
    {
        private FilterMetadataMods Mods { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public FilterPanelMapInfo() : base(new ScalableVector2(520, 72), new ScalableVector2(520, 72))
        {
            Alpha = 0f;
            InputEnabled = false;

            CreateMetadata();
        }

        private void CreateMetadata()
        {
            Mods = new FilterMetadataMods()
            {
                Parent = this,
                Alignment = Alignment.BotLeft,
                X = 0
            };
        }
    }
}