using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Helpers;
using Quaver.Shared.Skinning;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Graphics.UI.Form;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Search
{
    /// <summary>
    ///     Search box for v2.0 filter panel design - this is ONLY the text input area
    ///     Icon and map counter are separate elements in the parent container
    /// </summary>
    public class FilterPanelSearchBoxV2 : Textbox
    {
        /// <summary>
        ///     The current search term to be used
        /// </summary>
        private Bindable<string> CurrentSearchQuery { get; }

        /// <summary>
        /// </summary>
        private Bindable<bool> IsPlayTesting { get; }

        /// <summary>
        /// </summary>
        private Bindable<SelectContainerPanel> ActiveLeftPanel { get; }

        public static string PreviousSearchTerm { get; private set; } = "";

        /// <summary>
        /// </summary>
        public FilterPanelSearchBoxV2(Bindable<string> currentSearchQuery,
            Bindable<bool> isPlayTesting, Bindable<SelectContainerPanel> activeLeftPanel,
            float width, string placeHolderText)
            : base(new ScalableVector2(width, 40), FontManager.GetWobbleFont(Fonts.InterBold), 20, PreviousSearchTerm, placeHolderText)
        {
            CurrentSearchQuery = currentSearchQuery;
            IsPlayTesting = isPlayTesting;
            ActiveLeftPanel = activeLeftPanel;

            AllowSubmission = false;
            Tint = Color.Transparent; // Parent container handles background
            InputText.Alignment = Alignment.TopLeft;
            InputText.Y = 10;

            StoppedTypingActionCalltime = 400;
            OnStoppedTyping += StoppedTyping;

            // Reparent cursor to ContentContainer so it scrolls with the text
            Cursor.Parent = ContentContainer;

            // Initialize placeholder state and color
            IsPlaceholderActive = string.IsNullOrEmpty(RawText);
            InputText.Tint = IsPlaceholderActive ? SkinManager.Skin.SearchPlaceholderTextColor : SkinManager.Skin.SearchActiveTextColor;
            InputText.Alpha = 1.0f;
        }

        /// <summary>
        ///     If the placeholder is currently active
        /// </summary>
        private bool IsPlaceholderActive { get; set; } = true;

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            var shouldBeFocused = DialogManager.Dialogs.Count == 0 && (!IsPlayTesting.Value ||
                                  IsPlayTesting.Value && ActiveLeftPanel.Value != SelectContainerPanel.MapPreview);

            if (AlwaysFocused != shouldBeFocused)
            {
                AlwaysFocused = shouldBeFocused;
                Focused = shouldBeFocused;
            }

            // Update text color based on whether it's placeholder or actual text
            var isPlaceholder = InputText.Text == PlaceholderText;
            if (isPlaceholder != IsPlaceholderActive)
            {
                IsPlaceholderActive = isPlaceholder;

                if (IsPlaceholderActive)
                {
                    InputText.Tint = SkinManager.Skin.SearchPlaceholderTextColor;
                    // Force alpha to 1.0 because the base Textbox class sets it to 0.5 for placeholders
                    InputText.Alpha = 1.0f;
                }
                else
                {
                    InputText.Tint = SkinManager.Skin.SearchActiveTextColor;
                    InputText.Alpha = 1.0f;
                }
            }

            base.Update(gameTime);
        }



        /// <summary>
        ///     Called when the user has stopped typing in the textbox
        /// </summary>
        private void StoppedTyping(string filter)
        {
            CurrentSearchQuery.Value = filter;
            PreviousSearchTerm = filter;
        }
    }
}
