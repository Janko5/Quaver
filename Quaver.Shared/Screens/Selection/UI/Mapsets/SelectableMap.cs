using Quaver.Shared.Database.Maps;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    /// <summary>
    ///     Represents an individual map/difficulty in the song select container
    /// </summary>
    public class SelectableMap : SelectableItem
    {
        /// <inheritdoc />
        public override SelectableItemType Type => SelectableItemType.Map;

        /// <summary>
        ///     The map this item represents
        /// </summary>
        public Map Map { get; set; }

        /// <summary>
        ///     The parent mapset that this map belongs to
        /// </summary>
        public Mapset ParentMapset { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="parentMapset"></param>
        public SelectableMap(Map map, Mapset parentMapset)
        {
            Map = map;
            ParentMapset = parentMapset;
        }
    }
}
