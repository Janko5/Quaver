using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns;
using Wobble.Bindables;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Components
{
    public class LeaderboardTypeDropdown : FilterPanelDropdownV2
    {
        public LeaderboardTypeDropdown(bool isV2) : base(
            isV2 ? new ScalableVector2(204, 40) : new ScalableVector2(125, 30), GetDropdownItems(), GetSelectedIndex())
        {
            DefaultText = isV2 ? "Select ranking" : "Ranking";

            if (Dropdown.SelectedText != null)
                Dropdown.SelectedText.Text = Dropdown.Options[Dropdown.SelectedIndex];

            Dropdown.ItemSelected += OnItemSelected;
            ConfigManager.LeaderboardSection.ValueChanged += OnLeaderboardSectionChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            ConfigManager.LeaderboardSection.ValueChanged -= OnLeaderboardSectionChanged;
            base.Destroy();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static List<string> GetDropdownItems() => Enum.GetNames(typeof(LeaderboardType)).ToList();

        /// <summary>
        ///     Retrieves the index of the selected value
        /// </summary>
        /// <returns></returns>
        private static int GetSelectedIndex() => ConfigManager.LeaderboardSection != null ? (int) ConfigManager.LeaderboardSection.Value : (int) LeaderboardType.Global;

        /// <summary>
        ///     Called when a dropdown item has been selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemSelected(object? sender, DropdownClickedEventArgs e)
        {
            if (ConfigManager.LeaderboardSection == null)
                return;

            ConfigManager.LeaderboardSection.Value = (LeaderboardType) Enum.Parse(typeof(LeaderboardType), e.Text);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLeaderboardSectionChanged(object? sender, BindableValueChangedEventArgs<LeaderboardType> e)
        {
            Dropdown.SelectedIndex = (int) e.Value;

            if (Dropdown.SelectedText != null)
                Dropdown.SelectedText.Text = Dropdown.Options[(int) e.Value];
        }
    }
}