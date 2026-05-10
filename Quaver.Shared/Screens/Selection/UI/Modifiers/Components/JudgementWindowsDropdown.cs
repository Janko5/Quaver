using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Database.Judgements;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Screens.Selection.UI.FilterPanel.Dropdowns;
using Quaver.Shared.Screens.Selection.UI.Modifiers.Dialogs.Windows;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Managers;
using Wobble.Bindables;
using Wobble.Window;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Screens;
using Quaver.API.Maps.Processors.Scoring;
using Quaver.Shared.Assets;
using Wobble.Graphics.UI.Buttons;
using Quaver.Shared.Helpers;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers.Components
{
    public class JudgementWindowsDropdown : FilterPanelDropdownV2
    {
        /// <summary>
        ///    Whether any judgement windows dropdown is currently opened.
        /// </summary>
        public static bool AnyOpened { get; private set; }

        /// <summary>
        ///    The background dim when the dropdown is opened.
        /// </summary>
        private Button? BackgroundDim { get; set; }

        /// <summary>
        ///    The custom divider line above the Customize button.
        /// </summary>
        private Sprite? CustomizeButtonDividerLine { get; set; }

        /// <summary>
        ///    The original DrawOrder of the dropdown.
        /// </summary>
        private int OriginalDrawOrder { get; set; }

        /// <summary>
        ///    The original parent of the item container.
        /// </summary>
        private Drawable? OldParent { get; set; }

        /// <summary>
        ///    The original position of the item container.
        /// </summary>
        private ScalableVector2? OldPos { get; set; }

        /// <summary>
        ///    Characteristically yellowish judgement window color.
        /// </summary>
        public static Color JudgementColor => ColorHelper.HexToColor("#F2C94C");

        /// <summary>
        ///    Subtle gray for custom judgement windows.
        /// </summary>
        public static Color CustomColor => new Color(200, 200, 200);

        public JudgementWindowsDropdown() : base(new ScalableVector2(240, 40), GetItems(), GetSelectedIndex())
        {
            DefaultText = "Select JW";

            // Style natively using Dropdown's own properties.
            // This ensures proper layering and transparency handling by the Wobble engine.
            Dropdown.TextureClosed = SkinManager.Skin?.DropdownClose ?? UserInterface.DropdownClosed;
            Dropdown.TextureOpen = SkinManager.Skin?.DropdownOpen ?? UserInterface.DropdownOpen;
            Dropdown.Image = Dropdown.TextureClosed;
            Dropdown.Tint = SkinManager.Skin.DropdownCloseColor;

            // Styling
            Dropdown.ColorClosed = SkinManager.Skin.DropdownCloseColor;
            Dropdown.ColorOpen = SkinManager.Skin.DropdownOpenColor;
            Dropdown.DividerLineColor = SkinManager.Skin.DropdownSeparatorColor;

            // Style the base DividerLine to match the text color
            Dropdown.DividerLine.Tint = Dropdown.DividerLineColor;

            Dropdown.ItemSelected += OnItemSelected;

            // Add divider before the last item (Customize)
            AddDivider();

            // Style items (builtin vs custom)
            StyleItems();

            JudgementWindowsDatabaseCache.Selected.ValueChanged += OnSelectedPresetChanged;

            // Set initial color based on currently selected preset
            UpdateSelectedTextColor();

            Dropdown.OpenedEvent += OnOpened;
            Dropdown.ClosedEvent += OnClosed;

            JudgementWindowsDatabaseCache.PresetsChanged += OnPresetsChanged;
        }

        private void OnPresetsChanged(object? sender, EventArgs e) => Schedule(RefreshDropdownItems);

        private void RefreshDropdownItems()
        {
            // Reset dropdown content
            Dropdown.Options.Clear();
            Dropdown.Options.AddRange(GetItems());

            Dropdown.ItemContainer.ContentContainer.Children.ToList().ForEach(x => x.Destroy());
            CustomizeButtonDividerLine?.Destroy();
            CustomizeButtonDividerLine = null;

            Dropdown.Items.Clear();
            Dropdown.CreateItems();

            AddDivider();
            StyleItems();

            // Reselect current preset
            Dropdown.SelectedIndex = GetSelectedIndex();
            if (Dropdown.SelectedText != null)
                Dropdown.SelectedText.Text = Dropdown.Options[Dropdown.SelectedIndex] ?? "";

            // Update checkmark in items
            foreach (var item in Dropdown.Items)
                item.SetSelected(item.Index == Dropdown.SelectedIndex);

            UpdateSelectedTextColor();
        }

        private void UpdateSelectedTextColor()
        {
            if (Dropdown.SelectedText == null)
                return;

            var selected = JudgementWindowsDatabaseCache.Selected.Value;
            Dropdown.SelectedText.Tint = selected.IsDefault ? JudgementColor : CustomColor;
        }

        public override void Destroy()
        {
            if (Dropdown.Opened)
                AnyOpened = false;

            JudgementWindowsDatabaseCache.Selected.ValueChanged -= OnSelectedPresetChanged;
            JudgementWindowsDatabaseCache.PresetsChanged -= OnPresetsChanged;
            BackgroundDim?.Destroy();
            base.Destroy();
        }

        private static List<string> GetItems()
        {
            // Standard preset list order (not pinned to top)
            var items = JudgementWindowsDatabaseCache.Presets.Select(x => x.Name).ToList();
            items.Add("Customize");
            return items;
        }

        private static int GetSelectedIndex()
        {
            var selected = JudgementWindowsDatabaseCache.Selected.Value;
            var index = JudgementWindowsDatabaseCache.Presets.FindIndex(x => x.Name == selected.Name);
            return index >= 0 ? index : 0;
        }

        private void OnItemSelected(object sender, DropdownClickedEventArgs e)
        {
            if (e.Text == "Customize")
            {
                // Open dialog
                DialogManager.Show(new JudgementWindowDialog());

                // Revert selection to current preset so it doesn't stay on "Customize"
                Dropdown.SelectedIndex = GetSelectedIndex();
                
                if (Dropdown.SelectedText != null)
                    Dropdown.SelectedText.Text = Dropdown.Options[Dropdown.SelectedIndex];
                
                return;
            }

            var preset = JudgementWindowsDatabaseCache.Presets.FirstOrDefault(x => x.Name == e.Text);
            if (preset != null)
                JudgementWindowsDatabaseCache.Selected.Value = preset;
        }

        private void OnSelectedPresetChanged(object? sender, BindableValueChangedEventArgs<JudgementWindows> e)
        {
            // Find matching index
            var index = Dropdown.Options.IndexOf(e.Value.Name);
            if (index != -1)
            {
                Dropdown.SelectedIndex = index;
                if (Dropdown.SelectedText != null)
                    Dropdown.SelectedText.Text = Dropdown.Options[index];

                // Update checkmark in items
                foreach (var item in Dropdown.Items)
                    item.SetSelected(item.Index == index);

                UpdateSelectedTextColor();
            }
        }

        private void OnOpened(object sender, EventArgs e)
        {
            AnyOpened = true;
            var screenContainer = ((QuaverGame)Wobble.GameBase.Game).CurrentScreen.View.Container;

            // 1. Prepare Dim
            if (BackgroundDim == null)
            {
                BackgroundDim = new ImageButton(UserInterface.BlankBox)
                {
                    Parent = screenContainer,
                    Alignment = Alignment.TopLeft,
                    Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                    Tint = Color.Black,
                    Alpha = 0.85f,
                    DrawOrder = -100000,
                    IsClickable = true
                };
                BackgroundDim.Clicked += (s, ev) => Dropdown.Close();
            }
            
            BackgroundDim.Parent = screenContainer;
            BackgroundDim.Visible = true;
            BackgroundDim.IsClickable = true;
            // Update dim position to cover whole screen even if screenContainer is offset
            BackgroundDim.X = -screenContainer.AbsolutePosition.X;
            BackgroundDim.Y = -screenContainer.AbsolutePosition.Y;

            // 2. Prepare Dropdown - CAPTURE POSITION NOW
            var globalPos = AbsolutePosition;

            Schedule(() =>
            {
                if (!Dropdown.Opened)
                    return;

                OldParent = Parent;
                OldPos = new ScalableVector2(X, Y);
                Parent = screenContainer;
                
                // Align to captured global position. Use TopLeft to avoid parent-relative centering.
                Alignment = Alignment.TopLeft;
                X = globalPos.X - screenContainer.AbsolutePosition.X;
                Y = globalPos.Y - screenContainer.AbsolutePosition.Y;

                Dropdown.DividerLine.Alpha = 1f;
                Dropdown.DividerLine.Visible = true;

                Dropdown.DrawOrder = -100002;
                Dropdown.Items.ForEach(x => x.DrawOrder = -100002);

                if (CustomizeButtonDividerLine != null)
                {
                    CustomizeButtonDividerLine.Visible = true;
                    CustomizeButtonDividerLine.DrawOrder = -90000; // Above items
                }

                // Fix clipping: The base Dropdown starts an animation to OpenHeight in its Open() method.
                // We must override it with a custom animation that includes our extra padding for the divider and Customize button.
                // Using the same AnimationTime and Easing ensures visual consistency.
                Dropdown.ItemContainer.ClearAnimations();
                Dropdown.ItemContainer.ChangeHeightTo(Dropdown.OpenHeight + 35, Wobble.Graphics.Animations.Easing.OutQuint, Dropdown.AnimationTime);
                Dropdown.ItemContainer.ContentContainer.Height = Dropdown.OpenHeight + 35;
            });
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            AnyOpened = false;
            if (OldParent != null && OldPos != null)
            {
                Alignment = Alignment.MidLeft;
                Parent = OldParent;
                X = OldPos.Value.X.Value;
                Y = OldPos.Value.Y.Value;

                var drawOrder = ((QuaverGame)Wobble.GameBase.Game).CurrentScreen?.Type == QuaverScreenType.Editor ? 0 : 1;
                Dropdown.DrawOrder = drawOrder;
                Dropdown.Items.ForEach(x => x.DrawOrder = drawOrder);
            }

            if (BackgroundDim != null)
            {
                BackgroundDim.Visible = false;
                BackgroundDim.IsClickable = false;
            }

            if (CustomizeButtonDividerLine != null)
                CustomizeButtonDividerLine.Visible = false;

            Dropdown.DividerLine.Alpha = 0f;
            Dropdown.DividerLine.Visible = false;
        }

        private void AddDivider()
        {
            if (Dropdown.Items.Count < 2)
                return;

            var customizeItem = Dropdown.Items.Last();
            var dividerColor = SkinManager.Skin.DropdownSeparatorColor;

            customizeItem.Y += 4;

            CustomizeButtonDividerLine = new Sprite
            {
                Parent = Dropdown.ItemContainer,
                Size = new ScalableVector2(Dropdown.Width, 2),
                Tint = dividerColor,
                Alpha = 1f,
                X = 0,
                Y = customizeItem.Y - 4,
                Alignment = Alignment.TopLeft,
                Visible = Dropdown.Opened // Only visible if opened
            };

            // Ensure the last item uses the rounded bottom texture
            customizeItem.Image = SkinManager.Skin?.DropdownBottom ?? UserInterface.DropdownBottom;
        }

        private void StyleItems()
        {
            var presets = JudgementWindowsDatabaseCache.Presets;

            for (var i = 0; i < Dropdown.Items.Count; i++)
            {
                var item = Dropdown.Items[i];
                if (item.Index >= presets.Count)
                    continue;

                var preset = presets[item.Index];
                item.Text.Tint = preset.IsDefault ? JudgementColor : CustomColor;
            }
        }

    }
}
