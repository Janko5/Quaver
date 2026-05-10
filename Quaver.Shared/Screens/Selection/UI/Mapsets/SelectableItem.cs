namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    /// <summary>
    ///     Represents an item that can be displayed in the song select scroll container
    /// </summary>
    public abstract class SelectableItem
    {
        /// <summary>
        ///     The type of selectable item
        /// </summary>
        public abstract SelectableItemType Type { get; }

        /// <summary>
        ///     The display index of this item in the flattened list
        /// </summary>
        public int DisplayIndex { get; set; }
    }
}
