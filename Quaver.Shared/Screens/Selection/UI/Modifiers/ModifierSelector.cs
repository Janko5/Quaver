using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Wobble;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers
{
    public class ModifierSelector : ScrollContainer
    {
        private List<ModifierSection> Sections { get; }

        private Bindable<SelectContainerPanel> ActiveLeftPanel { get; }

        /// <summary>
        ///    Whether the modifier selector is using the V2 layout.
        /// </summary>
        private bool IsV2 { get; }

        private const float ItemPadding = 10f;
        private const float SectionSpacing = 8f;

        private Sprite ButtonBackground { get; set; }

        /// <summary>
        ///     The currently active tooltip that is displayed on top of the container
        /// </summary>
        public Tooltip ActiveTooltip { get; set; }

        private ImageButton ResetModifiersButton { get; set; }

        private ImageButton ClosePanelButton { get; set; }

        /// <inheritdoc />
        /// <param name="activeLeftPanel"></param>
        /// <param name="size"></param>
        /// <param name="sections"></param>
        public ModifierSelector(Bindable<SelectContainerPanel> activeLeftPanel, ScalableVector2 size, List<ModifierSection> sections) : base(size, size)
        {
            ActiveLeftPanel = activeLeftPanel;
            Sections = sections;
            Alpha = 0;
            IsV2 = SkinManager.Skin?.UserInterfaceVersion >= 2f;

            AlignAndContainSections();
            CreateButtons();
        }

        /// <inheritdoc />
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            HandleTooltipAnimation();

            base.Update(gameTime);
        }

        /// <summary>
        ///     Makes sure each section is contained and aligned properly
        /// </summary>
        private void AlignAndContainSections()
        {
            var totalY = 0f;

            for (var i = 0; i < Sections.Count; i++)
            {
                var section = Sections[i];

                if (IsV2 && i > 0)
                    totalY += SectionSpacing;

                AddContainedDrawable(section.Header);
                section.Header.Y = totalY;
                totalY += section.Header.Height;

                // Contain & Position Modifiers
                for (var j = 0; j < section.Modifiers.Count; j++)
                {
                    var mod = section.Modifiers[j];
                    mod.Selector = this;

                    mod.OriginalColor = ColorHelper.HexToColor("#273038");
                    AddContainedDrawable(mod);

                    mod.Y = totalY;
                    mod.X = IsV2 ? ItemPadding : mod.X;
                    totalY += mod.Height + (IsV2 ? ItemPadding : 0);
                }
            }
        }

        private void CreateButtons()
        {
            if (IsV2)
                return;

            ButtonBackground = new Sprite
            {
                Parent = this,
                Alignment = Alignment.BotLeft,
                Size = new ScalableVector2(Width, 83),
                Tint = ColorHelper.HexToColor("#181E25")
            };

            ResetModifiersButton = new IconButton(UserInterface.ResetMods, (sender, args) =>
            {
                if (OnlineManager.CurrentGame != null &&
                    (OnlineManager.CurrentGame.HostId != OnlineManager.Self?.OnlineUser?.Id && OnlineManager.CurrentGame.FreeModType == 0))
                {
                    return;
                }

                ModManager.RemoveAllMods();
            })
            {
                Parent = ButtonBackground,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(250, 38),
                X = 12,
            };

            ClosePanelButton = new IconButton(UserInterface.ClosePanel, (sender, args) =>
            {
                if (ActiveLeftPanel == null)
                    return;

                var game = GameBase.Game as QuaverGame;

                switch (game?.CurrentScreen?.Type)
                {
                    case QuaverScreenType.Editor:
                    case QuaverScreenType.Select:
                        ActiveLeftPanel.Value = SelectContainerPanel.Leaderboard;
                        break;
                    case QuaverScreenType.Multiplayer:
                        ActiveLeftPanel.Value = SelectContainerPanel.MatchSettings;
                        break;
                }
            })
            {
                Parent = ButtonBackground,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(250, 38),
                X = -ResetModifiersButton.X,
            };
        }

        /// <summary>
        ///     Sets the active tooltip
        /// </summary>
        /// <param name="tooltip"></param>
        public void ActivateTooltip(Tooltip tooltip)
        {
            if (ActiveTooltip != null)
                ActiveTooltip.Parent = null;

            ActiveTooltip = tooltip;

            if (ActiveTooltip == null)
                return;

            ActiveTooltip.Parent = this;

            ActiveTooltip.Alpha = 0;
            ActiveTooltip.ClearAnimations();
            ActiveTooltip.FadeTo(1, Easing.Linear, 150);
        }

        private Vector2 _previousMousePosition;

        private Vector2 _previousAbsolutePosition;

        private void HandleTooltipAnimation()
        {
            if (ActiveTooltip == null)
                return;

            var currentMouse = MouseManager.CurrentState.Position;
            var currentAbsolutePos = AbsolutePosition;

            if (currentMouse == _previousMousePosition && currentAbsolutePos == _previousAbsolutePosition)
                return;

            _previousMousePosition = currentMouse;
            _previousAbsolutePosition = currentAbsolutePos;

            ActiveTooltip.X = MathHelper.Clamp(currentMouse.X - AbsolutePosition.X - ActiveTooltip.Width / 2f, 5, Width - ActiveTooltip.Width - 5);
            ActiveTooltip.Y = MathHelper.Clamp(currentMouse.Y - AbsolutePosition.Y - ActiveTooltip.Height - 2, 5, Height - ActiveTooltip.Height - 5);
        }
    }
}