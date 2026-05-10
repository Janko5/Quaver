using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Wobble.Graphics;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Quaver.Shared.Helpers;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns
{
    public class FilterPanelDropdownV2 : Container
    {
        /// <summary>
        ///    The dropdown selector
        /// </summary>
        public Dropdown Dropdown { get; }

        /// <summary>
        ///     Event invoked when the dropdown is opened
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        ///     Event invoked when the dropdown is closed
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        ///     Expose SelectedText for subclasses
        /// </summary>
        public SpriteTextPlus Text => Dropdown.SelectedText;

        /// <summary>
        ///     Constructor for GameMode dropdown (default behavior)
        /// </summary>
        public FilterPanelDropdownV2() : this(new ScalableVector2(204, 40), GetGameModeItems(), GetGameModeSelectedIndex())
        {
            // Default behavior for GameMode dropdown:
            Dropdown.ItemSelected += OnGameModeItemSelected;

            // Apply colors to items
            for (var i = 1; i < Dropdown.Items.Count; i++) // Skip index 0 (All Modes)
            {
                var mode = ModeHelper.AllModes[i - 1];
                Dropdown.Items[i].Text.Tint = GameModeHelper.GetGameModeColor(mode);
            }

            // Set initial SelectedText color
            if (Dropdown.SelectedIndex > 0 && Dropdown.SelectedIndex <= ModeHelper.AllModes.Length)
            {
                var mode = ModeHelper.AllModes[Dropdown.SelectedIndex - 1];
                Dropdown.SelectedText.Tint = GameModeHelper.GetGameModeColor(mode);
            }
        }

        /// <summary>
        ///     Protected constructor for subclasses to provide their own items and size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="items"></param>
        /// <param name="selectedIndex"></param>
        protected FilterPanelDropdownV2(ScalableVector2 size, List<string> items, int selectedIndex = 0)
        {
            // Create the dropdown
            Dropdown = new Dropdown(items, size, 22, Color.White, selectedIndex)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                // Use custom textures for V2 dropdown
                TextureClosed = SkinManager.Skin.DropdownClose,
                TextureOpen = SkinManager.Skin.DropdownOpen,
                // Initial image
                Image = SkinManager.Skin.DropdownClose,
                Tint = SkinManager.Skin.DropdownCloseColor,
                // Styling
                ColorClosed = SkinManager.Skin.DropdownCloseColor,
                ColorOpen = SkinManager.Skin.DropdownOpenColor,
                DividerLineColor = SkinManager.Skin.DropdownSeparatorColor,
                ItemBackgroundColor = SkinManager.Skin.DropdownMiddleCloseColor,
                ItemHoverColor = SkinManager.Skin.DropdownHoverColor, // 50% opacity handled by HighlightAlpha
                HighlightAlpha = 1f,
                MainButtonHoverColor = SkinManager.Skin.DropdownHoverColor,
                AnimationTime = 200
            };

            // Customize the dropdown appearance for V2
            if (Dropdown.SelectedText != null)
            {
                Dropdown.SelectedText.Font = FontManager.GetWobbleFont(Fonts.InterBold);
                Dropdown.SelectedText.Tint = SkinManager.Skin.DropdownTextColor;

                // Synchronize items font with header font
                foreach (var item in Dropdown.Items)
                    item.Text.Font = Dropdown.SelectedText.Font;
            }

            // Customize Chevron and DividerLine
            if (Dropdown.Chevron != null)
            {
                Dropdown.Chevron.Tint = SkinManager.Skin.DropdownTextColor;
                Dropdown.Chevron.Size = new ScalableVector2(16, 16);
            }

            if (Dropdown.DividerLine != null)
                Dropdown.DividerLine.Tint = SkinManager.Skin.DropdownTextColor;

            // Hook up common events
            Dropdown.OpenedEvent += OnDropdownOpened;
            Dropdown.ClosedEvent += OnDropdownClosed;

            // Size the container to the dropdown
            Size = Dropdown.Size;
        }

        /// <summary>
        ///     Get default GameMode items
        /// </summary>
        /// <returns></returns>
        private static List<string> GetGameModeItems()
        {
            var values = new List<string>
            {
                "All Modes"
            };

            foreach (var mode in ModeHelper.AllModes)
            {
                var text = ModeHelper.ToLongHand(mode);

                if (mode == GameMode.Keys4 || mode == GameMode.Keys7)
                    text += " (Ranked)";

                values.Add(text);
            }

            return values;
        }

        /// <summary>
        ///     Get default selected index for GameMode
        /// </summary>
        /// <returns></returns>
        private static int GetGameModeSelectedIndex()
        {
            return (int)(ConfigManager.SelectFilterGameModeBy?.Value ?? 0);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            Dropdown.ItemSelected -= OnGameModeItemSelected;
            Dropdown.OpenedEvent -= OnDropdownOpened;
            Dropdown.ClosedEvent -= OnDropdownClosed;
            base.Destroy();
        }

        /// <summary>
        ///     Default handler for GameMode selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameModeItemSelected(object? sender, DropdownClickedEventArgs e)
        {
            if (ConfigManager.SelectFilterGameModeBy == null)
                return;

            // Update config
            // Index 0 is "All Modes", mapped to 0.
            // Index 1+ maps to ModeHelper.AllModes[e.Index - 1].
            var val = e.Index == 0 ? 0 : (int)ModeHelper.AllModes[e.Index - 1];

            ConfigManager.SelectFilterGameModeBy.Value = (GameMode)val;

            // If the user selects a keymode that is not rankable, and the status filter is set to Ranked,
            // we should reset it to All statuses to avoid an empty list.
            if (val != 0 && !ModeHelper.IsRanked((GameMode)val))
            {
                if (ConfigManager.SelectFilterStatusBy != null && ConfigManager.SelectFilterStatusBy.Value == RankedStatusFilter.Ranked)
                    ConfigManager.SelectFilterStatusBy.Value = RankedStatusFilter.All;
            }
        }

        /// <summary>
        ///     The default text to display when the dropdown is open
        /// </summary>
        protected string DefaultText { get; set; } = "Select Keymode";

        /// <summary>
        ///     Virtual handler for open event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDropdownOpened(object? sender, EventArgs e)
        {
            if (Dropdown.SelectedText != null)
            {
                // Default
                Dropdown.SelectedText.Text = DefaultText;
                Dropdown.SelectedText.Visible = true;
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Virtual handler for closed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDropdownClosed(object? sender, EventArgs e)
        {
            if (Dropdown.SelectedText != null)
                Dropdown.SelectedText.Text = Dropdown.Options[Dropdown.SelectedIndex];

            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
