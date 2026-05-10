using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers.Mods;
using Quaver.Shared.Screens.Selection.UI.Modifiers.Components;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Wobble.Graphics.UI.Buttons;
using Quaver.Shared.Skinning;
using Quaver.Shared.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers
{
    public class ModifierSelectorContainer : Sprite
    {
        /// <summary>
        ///    Whether the modifier selector is using the V2 layout.
        /// </summary>
        private bool IsV2 => SkinManager.Skin?.UserInterfaceVersion >= 2f;

        private Bindable<SelectContainerPanel> ActiveLeftPanel { get; }

        private SpriteTextPlus? Header { get; set; }

        private SpriteTextPlus? SubHeader { get; set; }

        private Sprite ModifierSelectorBackground { get; set; } = null!;

        private ModifierSelector Selector { get; set; } = null!;

        private ImageButton? ResetButton { get; set; }


        public ModifierSelectorContainer(Bindable<SelectContainerPanel> activeLeftPanel)
        {
            ActiveLeftPanel = activeLeftPanel;

            Size = IsV2 ? new ScalableVector2(725, 850) : new ScalableVector2(564, 838);
            Alpha = 0f;
            AutoScaleHeight = true;

            CreateHeaderText();
            CreateSubHeaderText();
            CreateModifierSelectorBackground();
        }

        /// <summary>
        ///    Creates <see cref="Header"/>
        /// </summary>
        private void CreateHeaderText()
        {
            if (IsV2)
                return;

            Header = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "MODIFIERS", 30)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
            };
        }

        /// <summary>
        ///    Creates <see cref="SubHeader"/>
        /// </summary>
        private void CreateSubHeaderText()
        {
            if (IsV2)
                return;

            SubHeader = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Customize gameplay to your heart's desire".ToUpper(), 18)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Tint = ColorHelper.HexToColor("#808080")
            };

            if (SubHeader == null || Header == null)
                return;

            SubHeader.Y = Header.Y + Header.Height - SubHeader.Height - 3;
        }

        /// <summary>
        ///     Creates <see cref="ModifierSelectorBackground"/>
        /// </summary>
        private void CreateModifierSelectorBackground()
        {
            var headerHeight = Header != null && !Header.IsDisposed ? Header.Height : 0;
            var subHeaderHeight = SubHeader != null && !SubHeader.IsDisposed ? SubHeader.Height : 0;

            ModifierSelectorBackground = new Sprite
            {
                Parent = this,
                Image = SkinManager.Skin?.SongSelect?.ModifierSelectorBackground ?? UserInterface.ModifierSelectorBackground,
                Size = IsV2 ? new ScalableVector2(725, 850) : new ScalableVector2(Width, Height - headerHeight - 8),
                Y = IsV2 ? 0 : (Header?.Y ?? 0) + headerHeight + 8,
            };

            var width = (int)(Width - 4);

            var rankedMods = new List<SelectableModifier>()
            {
                new SelectableModifierJudgementWindows(width),
                new SelectableModifierSpeed(width),
                new SelectableModifierBool(width, new ModMirror()),
                new SelectableModifierBool(width, new ModNoMiss())
            };

            var unrankedMods = new List<SelectableModifier>()
            {
                new SelectableModifierBool(width, new ModAutoplay()),
                new SelectableModifierBool(width, new ModNoFail()),
                new SelectableModifierBool(width, new ModRandomize()),
                new SelectableModifierBool(width, new ModNoSliderVelocities()),
                new SelectableModifierBool(width, new ModNoLongNotes()),
                new SelectableModifierBool(width, new ModFullLN()),
                new SelectableModifierBool(width, new ModInverse()),
                new SelectableModifierBool(width, new ModCoop()),
            };


            var selectorHeight = IsV2 ? ModifierSelectorBackground.Height - 80 : ModifierSelectorBackground.Height - 4;

            Selector = new ModifierSelector(ActiveLeftPanel,
                new ScalableVector2(width, selectorHeight), new List<ModifierSection>
                {
                    new ModifierSection(width, FontAwesome.Get(FontAwesomeIcon.fa_check_mark),"Ranked",
                        "These mods can be used for ranked scores", ColorHelper.HexToColor("#27B06E"), rankedMods, true),

                    new ModifierSection(width, FontAwesome.Get(FontAwesomeIcon.fa_warning_sign_on_a_triangular_background), "Unranked",
                            "Scores will not be submitted while using these", ColorHelper.HexToColor("#F2C94C"), unrankedMods, false),
                })
            {
                Parent = ModifierSelectorBackground,
                Alignment = IsV2 ? Alignment.TopCenter : Alignment.MidCenter,
                Y = IsV2 ? 4 : 0
            };

            if (IsV2)
                CreateResetButton();
        }

        /// <summary>
        ///     Creates the reset button for V2
        /// </summary>
        private void CreateResetButton()
        {
            var skin = SkinManager.Skin?.Universal;

            ResetButton = new ImageButton(UserInterface.SquareButton, null)
            {
                Parent = ModifierSelectorBackground,
                Alignment = Alignment.BotCenter,
                Y = -10,
                Size = new ScalableVector2(204, 40),
                Alpha = 0,
                SetChildrenAlpha = false
            };

            var background = new NineSliceSprite(UserInterface.SquareButton, new SliceMargins(20))
            {
                Parent = ResetButton,
                Alignment = Alignment.MidCenter,
                Size = ResetButton.Size,
                Tint = skin?.ButtonNotActiveColor ?? SkinManager.Skin.ButtonNotActiveColor,
                UsePreviousSpriteBatchOptions = true
            };

            var text = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Reset Modifiers", 22)
            {
                Parent = ResetButton,
                Alignment = Alignment.MidCenter,
                Tint = skin?.ButtonContentColor ?? Color.White,
                UsePreviousSpriteBatchOptions = true
            };

            ResetButton.Hovered += (s, e) => background.Tint = skin?.ButtonHoverColor ?? new Color(37, 110, 170, 255);
            ResetButton.LeftHover += (s, e) => background.Tint = skin?.ButtonNotActiveColor ?? SkinManager.Skin.ButtonNotActiveColor;

            ResetButton.Clicked += (s, e) =>
            {
                if (Quaver.Shared.Online.OnlineManager.CurrentGame != null &&
                    (Quaver.Shared.Online.OnlineManager.CurrentGame.HostId != Quaver.Shared.Online.OnlineManager.Self?.OnlineUser?.Id && Quaver.Shared.Online.OnlineManager.CurrentGame.FreeModType == 0))
                {
                    return;
                }

                Quaver.Shared.Modifiers.ModManager.RemoveAllMods();
            };
        }
    }
}
