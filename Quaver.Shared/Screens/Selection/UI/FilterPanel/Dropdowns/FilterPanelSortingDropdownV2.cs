using System;
using System.Collections.Generic;
using Quaver.API.Enums;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Wobble.Bindables;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns
{
    public class FilterPanelSortingDropdownV2 : FilterPanelDropdownV2
    {
        /// <summary>
        ///     The mapsets that are available to select
        /// </summary>
        private Bindable<List<Mapset>> AvailableMapsets { get; }

        /// <summary>
        ///     Mapping of Display Text -> Enum Value, sorted alphabetically by text
        /// </summary>
        private static readonly List<KeyValuePair<string, OrderMapsetsBy>> SortedMapping = new List<KeyValuePair<string, OrderMapsetsBy>>
        {
            // Alphabetical Order
            new KeyValuePair<string, OrderMapsetsBy>("Artist", OrderMapsetsBy.Artist),
            new KeyValuePair<string, OrderMapsetsBy>("BPM", OrderMapsetsBy.BPM),
            new KeyValuePair<string, OrderMapsetsBy>("Creator", OrderMapsetsBy.Creator),
            new KeyValuePair<string, OrderMapsetsBy>("Date Added", OrderMapsetsBy.DateAdded),
            new KeyValuePair<string, OrderMapsetsBy>("Date Ranked", OrderMapsetsBy.DateRanked),
            new KeyValuePair<string, OrderMapsetsBy>("Difficulty", OrderMapsetsBy.Difficulty),
            new KeyValuePair<string, OrderMapsetsBy>("Game", OrderMapsetsBy.Game),
            new KeyValuePair<string, OrderMapsetsBy>("Genre", OrderMapsetsBy.Genre),
            new KeyValuePair<string, OrderMapsetsBy>("Last Updated", OrderMapsetsBy.DateLastUpdated),
            new KeyValuePair<string, OrderMapsetsBy>("Length", OrderMapsetsBy.Length),
            new KeyValuePair<string, OrderMapsetsBy>("Long Note %", OrderMapsetsBy.LongNotePercentage),
            new KeyValuePair<string, OrderMapsetsBy>("Notes Per Sec.", OrderMapsetsBy.NotesPerSecond),
            new KeyValuePair<string, OrderMapsetsBy>("Online Grade", OrderMapsetsBy.OnlineGrade),
            new KeyValuePair<string, OrderMapsetsBy>("Ranked Status", OrderMapsetsBy.Status),
            new KeyValuePair<string, OrderMapsetsBy>("Recently Played", OrderMapsetsBy.RecentlyPlayed),
            new KeyValuePair<string, OrderMapsetsBy>("Source", OrderMapsetsBy.Source),
            new KeyValuePair<string, OrderMapsetsBy>("Times Played", OrderMapsetsBy.TimesPlayed),
            new KeyValuePair<string, OrderMapsetsBy>("Title", OrderMapsetsBy.Title)
        };

        /// <summary>
        ///     The button to toggle sort direction
        /// </summary>
        public FilterPanelSortDirectionButton SortDirectionButton { get; }

        /// <summary>
        /// </summary>
        /// <param name="availableMapsets"></param>
        public FilterPanelSortingDropdownV2(Bindable<List<Mapset>> availableMapsets) : base(new ScalableVector2(254, 40), GetDropdownItems(), GetSelectedIndex())
        {
            AvailableMapsets = availableMapsets;

            // Set default text for the base class handler
            DefaultText = "Sort by";
            Text.Text = DefaultText;

            // Ensure child visibility is synchronized with parent (critical for clipping animation)
            SetChildrenVisibility = true;

            // CRITICAL: Inherit SpriteBatch options from parent (ClippingContainer)
            // This ensures scissor rect clipping is preserved during collapse animation
            UsePreviousSpriteBatchOptions = true;

            // Add the sort direction button inside the container
            SortDirectionButton = new FilterPanelSortDirectionButton
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = 0,
                Y = 0
            };

            // Shift text and chevron to the right to accommodate the button
            if (Text != null)
                Text.X = 50;

            if (Dropdown.Chevron != null)
                Dropdown.Chevron.X = -10;

            // Handle selection
            Dropdown.ItemSelected += OnItemSelected;

            UpdateText();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemSelected(object sender, DropdownClickedEventArgs e)
        {
            if (ConfigManager.SelectOrderMapsetsBy == null)
                return;

            // Map index back to Enum
            if (e.Index >= 0 && e.Index < SortedMapping.Count)
            {
                ConfigManager.SelectOrderMapsetsBy.Value = SortedMapping[e.Index].Value;
            }

            UpdateText();
        }

        /// <summary>
        ///     Updates the main text to reflect the checked option
        /// </summary>
        private void UpdateText()
        {
            if (ConfigManager.SelectOrderMapsetsBy == null)
                return;

            var selectedEnum = ConfigManager.SelectOrderMapsetsBy.Value;

            // Find display text for current enum
            foreach (var kvp in SortedMapping)
            {
                if (kvp.Value == selectedEnum)
                {
                    Text.Text = kvp.Key; // Use original casing (Title Case)
                    break;
                }
            }
        }



        /// <summary>
        ///     Retrieves the index of the selected value based on current config and SortedMapping
        /// </summary>
        /// <returns></returns>
        private static int GetSelectedIndex()
        {
            if (ConfigManager.SelectOrderMapsetsBy == null)
                return 0;

            var currentVal = ConfigManager.SelectOrderMapsetsBy.Value;
            for (var i = 0; i < SortedMapping.Count; i++)
            {
                if (SortedMapping[i].Value == currentVal)
                    return i;
            }

            return 0;
        }

        /// <summary>
        ///     Retrieves a list of dropdown items from Key of SortedMapping
        /// </summary>
        /// <returns></returns>
        private static List<string> GetDropdownItems()
        {
            var list = new List<string>();
            foreach (var kvp in SortedMapping)
                list.Add(kvp.Key);
            return list;
        }
    }
}
