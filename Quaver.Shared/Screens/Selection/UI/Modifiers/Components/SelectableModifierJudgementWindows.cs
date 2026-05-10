using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Maps.Processors.Scoring;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Judgements;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Modifiers.Mods;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Quaver.Shared.Screens.Selection.UI.Modifiers.Dialogs.Windows;
using Quaver.Shared.Skinning;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.UI.Buttons;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers.Components
{
    public class SelectableModifierJudgementWindows : SelectableModifier
    {
        #region Layout Constants

        private const float LeftPanelWidth = 455f;
        private const float DropdownWidth = 240f;

        private const float V1ButtonWidth = 102f;
        private const float V1ButtonHeight = 22f;

        private const float AlphaClickThreshold = 0.9f;

        private Drawable OldParent { get; set; }
        private ScalableVector2 OldPos { get; set; }
        private Alignment OldAlignment { get; set; }

        #endregion

        #region UI Components

        /// <summary>
        ///    The dropdown for selection (V2 Only)
        /// </summary>
        public JudgementWindowsDropdown Dropdown { get; private set; }

        #endregion

        #region Data Sources

        /// <summary>
        ///    Suffix mapping for judgement window textures.
        /// </summary>
        private static readonly Dictionary<string, string> SuffixMap = new Dictionary<string, string>
        {
            { "Chill", "CHILL" },
            { "Extreme", "EXT" },
            { "Impossible", "IMP" },
            { "Lenient", "LEN" },
            { "Peaceful", "PEAC" },
            { "Standard", "STD" },
            { "Strict", "STR" },
            { "Tough", "TOU" }
        };

        #endregion

        #region Initialization

        private readonly bool _useV2;

        public SelectableModifierJudgementWindows(int width) : base(width, new ModJudgementWindows())
        {
            _useV2 = IsV2;

            Clicked += OnClicked;
            JudgementWindowsDatabaseCache.Selected.ValueChanged += OnJudgementWindowsChanged;
        }

        protected override void SetupV1Layout(int width)
        {
            base.SetupV1Layout(width);

            // ReSharper disable once ObjectCreationAsStatement
            new IconButton(UserInterface.CustomizeButton, (sender, args) => DialogManager.Show(new JudgementWindowDialog()))
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -Padding,
                Size = new ScalableVector2(V1ButtonWidth, V1ButtonHeight),
            };
        }

        protected override void SetupV2Layout()
        {
            base.SetupV2Layout();

            if (Background != null)
            {
                Background.Size = new ScalableVector2(LeftPanelWidth, PanelHeight);
                Background.Alignment = Alignment.MidLeft;
            }

            Dropdown = new JudgementWindowsDropdown
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = LeftPanelWidth + Padding
            };

            Dropdown.Dropdown.OpenedEvent += OnDropdownOpened;
            Dropdown.Dropdown.ClosedEvent += OnDropdownClosed;
        }

        #endregion

        #region Interaction & Logic

        private bool IsDropdownOpened { get; set; }

        private void OnDropdownOpened(object? sender, EventArgs e)
        {
            IsDropdownOpened = true;
            var screenContainer = ((QuaverGame)Wobble.GameBase.Game).CurrentScreen.View.Container;
            var globalPos = AbsolutePosition;

            OldParent = Parent;
            OldPos = new ScalableVector2(X, Y);
            OldAlignment = Alignment;

            Parent = screenContainer;
            Alignment = Alignment.TopLeft;

            X = globalPos.X - screenContainer.AbsolutePosition.X;
            Y = globalPos.Y - screenContainer.AbsolutePosition.Y;

            // Draw above the dim (-100000)
            DrawOrder = -100001;
        }

        private void OnDropdownClosed(object? sender, EventArgs e)
        {
            IsDropdownOpened = false;
            if (OldParent == null) return;

            Parent = OldParent;
            X = OldPos.X.Value;
            Y = OldPos.Y.Value;
            Alignment = OldAlignment;

            DrawOrder = 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_useV2)
                PerformV2Animations();
        }

        private void PerformV2Animations()
        {
            if (Background == null) return;

            var skin = SkinManager.Skin?.SongSelect;
            
            // Left Panel Highlight - Active if mouse is over OR dropdown is opened
            var shouldHighlight = (IsHovered && IsMouseInClickArea()) || IsDropdownOpened;
            var hoveredImage = skin?.ModifierBackgroundHovered ?? UserInterface.ModifierBackground;
            var normalImage = skin?.ModifierBackground ?? UserInterface.ModifierBackground;
            
            Background.Image = shouldHighlight ? hoveredImage : normalImage;

            if (Dropdown != null)
            {
                // Sync alpha for secondary components
                Dropdown.Dropdown.Alpha = Background.Alpha;
                
                // Disable if faded out
                Dropdown.Dropdown.IsClickable = Name.Alpha > AlphaClickThreshold;
            }
        }

        private void OnClicked(object? sender, EventArgs e)
        {
            // Base click only triggers if mouse is in the left panel due to IsMouseInClickArea override
            DialogManager.Show(new JudgementWindowDialog());
        }

        private void OnJudgementWindowsChanged(object? sender, BindableValueChangedEventArgs<JudgementWindows> e)
        {
            Icon.Image = GetTexture();
        }

        #endregion

        #region Overrides

        protected override bool IsMouseInClickArea()
        {
            if (!_useV2) return base.IsMouseInClickArea();

            // Constraint base button interaction only to the left panel
            var rect = new Rectangle((int)AbsolutePosition.X, (int)AbsolutePosition.Y, (int)(LeftPanelWidth * WindowManager.ScreenScale.X), (int)AbsoluteSize.Y);
            return rect.Contains(MouseManager.CurrentState.Position);
        }

        protected override Texture2D GetTexture()
        {
            var name = JudgementWindowsDatabaseCache.Selected.Value?.Name ?? "Standard";
            var suffix = "Custom";

            foreach (var pair in SuffixMap)
            {
                if (name.Contains(pair.Key))
                {
                    suffix = pair.Value;
                    break;
                }
            }

            try
            {
                return TextureManager.Load($@"Quaver.Resources/Textures/UI/Mods/JW-{suffix}.png");
            }
            catch
            {
                return TextureManager.Load($@"Quaver.Resources/Textures/UI/Mods/N-JW.png");
            }
        }

        public override void Destroy()
        {
            if (Dropdown != null)
            {
                Dropdown.Dropdown.OpenedEvent -= OnDropdownOpened;
                Dropdown.Dropdown.ClosedEvent -= OnDropdownClosed;
            }

            JudgementWindowsDatabaseCache.Selected.ValueChanged -= OnJudgementWindowsChanged;
            base.Destroy();
        }

        #endregion
    }
}