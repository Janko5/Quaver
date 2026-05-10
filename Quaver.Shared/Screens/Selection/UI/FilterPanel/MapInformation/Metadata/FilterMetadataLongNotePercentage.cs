using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Quaver.API.Helpers;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Skinning;
using Wobble.Bindables;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.MapInformation.Metadata
{
    public class FilterMetadataLongNotePercentage : TextKeyValue
    {
        public FilterMetadataLongNotePercentage() : base("LNs:", "10%", SkinManager.Skin?.UserInterfaceVersion >= 2f ? 24 : 20, ColorHelper.HexToColor($"#ffe76b"))
        {
            if (MapManager.Selected.Value != null)
                Value.Text = $"{GetPercentage()}";

            MapManager.Selected.ValueChanged += OnMapChanged;
            ModManager.ModsChanged += OnModsChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            MapManager.Selected.ValueChanged -= OnMapChanged;
            ModManager.ModsChanged -= OnModsChanged;

            base.Destroy();
        }

        private void OnModsChanged(object sender, ModsChangedEventArgs e) => SetText();

        private void OnMapChanged(object sender, BindableValueChangedEventArgs<Map> e) => SetText();

        private string GetPercentage()
        {
            if (MapManager.Selected.Value == null)
                return "0%";

            // Dynamic updates based on mods
            if (ModManager.Mods.HasFlag(Quaver.API.Enums.ModIdentifier.NoLongNotes))
                return "0%";

            if (ModManager.Mods.HasFlag(Quaver.API.Enums.ModIdentifier.FullLN))
                return "100%";

            if (ModManager.Mods.HasFlag(Quaver.API.Enums.ModIdentifier.Inverse))
                return $"{100 - (int)MapManager.Selected.Value.LNPercentage}%";

            return ((int)MapManager.Selected.Value.LNPercentage).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private void SetText() => ScheduleUpdate(() => Value.Text = GetPercentage());
    }
}