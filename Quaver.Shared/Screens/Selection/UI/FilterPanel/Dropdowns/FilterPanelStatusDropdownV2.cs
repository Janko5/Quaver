using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns
{
    public class FilterPanelStatusDropdownV2 : FilterPanelDropdownV2
    {
        /// <summary>
        /// </summary>
        public FilterPanelStatusDropdownV2() : base(new ScalableVector2(204, 40), GetDropdownItems(), GetSelectedIndex())
        {
            // Set default text for base class handler
            DefaultText = "Select Status";

            // Ensure child visibility is synchronized with parent (critical for clipping animation)
            SetChildrenVisibility = true;

            // CRITICAL: Inherit SpriteBatch options from parent (ClippingContainer)
            // This ensures scissor rect clipping is preserved during collapse animation
            UsePreviousSpriteBatchOptions = true;

            // Handle selection
            Dropdown.ItemSelected += OnItemSelected;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static List<string> GetDropdownItems()
        {
            return new List<string>
            {
                "All Statuses",
                "Ranked",
                "Unranked",
                "Not Submitted",
                "This Game",
                "Other Games"
            };
        }

        /// <summary>
        ///     Retrieves the index of the selected value
        /// </summary>
        /// <returns></returns>
        private static int GetSelectedIndex()
        {
            if (ConfigManager.SelectFilterStatusBy != null)
                return (int)ConfigManager.SelectFilterStatusBy.Value;

            return 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemSelected(object sender, DropdownClickedEventArgs e)
        {
            // Update config based on dropdown index
            RankedStatusFilter val = e.Index switch
            {
                0 => RankedStatusFilter.All,
                1 => RankedStatusFilter.Ranked,
                2 => RankedStatusFilter.Unranked,
                3 => RankedStatusFilter.NotSubmitted,
                4 => RankedStatusFilter.ThisGame,
                5 => RankedStatusFilter.OtherGames,
                _ => RankedStatusFilter.All
            };

            if (ConfigManager.SelectFilterStatusBy != null)
                ConfigManager.SelectFilterStatusBy.Value = val;
        }



    }
}
