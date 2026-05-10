using Quaver.Shared.Database.Maps;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    /// <summary>
    ///     Represents a mapset in the song select container
    /// </summary>
    public class SelectableMapset : SelectableItem
    {
        /// <inheritdoc />
        public override SelectableItemType Type => SelectableItemType.Mapset;

        /// <summary>
        ///     The mapset this item represents
        /// </summary>
        public Mapset Mapset { get; set; }

        /// <summary>
        ///     Whether the mapset is currently expanded to show its difficulties
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="mapset"></param>
        public SelectableMapset(Mapset mapset)
        {
            Mapset = mapset;
            IsExpanded = false;
        }
    }
}
