using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Selection.UI.Modifiers.Components;
using TagLib.Id3v2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers
{
    public class ModifierSection
    {
        /// <summary>
        ///     Header sprite for the section
        /// </summary>
        public Sprite Header { get; }

        /// <summary>
        ///     The icon to represent the section
        /// </summary>
        public Sprite Icon { get; }

        /// <summary>
        ///     The name of the modifier section
        /// </summary>
        public SpriteTextPlus Name { get; }

        /// <summary>
        ///     Describes when the modifier section is for
        /// </summary>
        public SpriteTextPlus SubText { get; }

        /// <summary>
        /// </summary>
        public bool IsRanked { get; }

        /// <summary>
        /// </summary>
        private Sprite? HeaderContainer { get; set; }

        /// <summary>
        /// </summary>
        private NineSliceSprite? HeaderLeft { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? HeaderLeftText { get; set; }

        /// <summary>
        /// </summary>
        private NineSliceSprite? HeaderRight { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? HeaderRightText { get; set; }

        /// <summary>
        /// </summary>
        private bool IsV2 => SkinManager.Skin?.UserInterfaceVersion >= 2f;

        private const float HeaderPadding = 10f;
        private const float HeaderHeight = 40f;

        /// <summary>
        ///     The list of modifiers to go under this section
        /// </summary>
        public List<SelectableModifier> Modifiers { get; }

        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="icon"></param>
        /// <param name="name"></param>
        /// <param name="subText"></param>
        /// <param name="color"></param>
        /// <param name="modifiers"></param>
        /// <param name="isRanked"></param>
        public ModifierSection(int width, Texture2D icon, string name, string subText, Color color, List<SelectableModifier> modifiers, bool isRanked = true)
        {
            Modifiers = modifiers;
            IsRanked = isRanked;

            if (IsV2)
            {
                Header = new Sprite
                {
                    Size = new ScalableVector2(width, 60),
                    UsePreviousSpriteBatchOptions = true,
                    Tint = Color.Transparent
                };

                Icon = null!;
                Name = null!;
                SubText = null!;
                CreateV2Header();
                return;
            }

            // Original V1 Layout
            Header = new Sprite
            {
                Size = new ScalableVector2(width, 70),
                UsePreviousSpriteBatchOptions = true,
                Tint = ColorHelper.HexToColor("#181E25")
            };

            const int paddingLeft = 12;

            Icon = new Sprite()
            {
                Parent = Header,
                Tint = color,
                Size = new ScalableVector2(22, 22),
                Alignment = Alignment.MidLeft,
                X = paddingLeft,
                Image = icon,
                UsePreviousSpriteBatchOptions = true
            };

            Name = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), name.ToUpper(), 22)
            {
                Parent = Header,
                Tint = color,
                X = Icon.X + Icon.Width + paddingLeft,
                Y = 12,
                UsePreviousSpriteBatchOptions = true
            };

            SubText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), subText.ToUpper(), 18)
            {
                Parent = Header,
                Alignment = Alignment.BotLeft,
                UsePreviousSpriteBatchOptions = true,
                X = Name.X,
                Y = -11
            };
        }

        /// <summary>
        /// </summary>
        private void CreateV2Header()
        {
            Header.Tint = Color.Transparent;

            HeaderContainer = new Sprite
            {
                Parent = Header,
                Alignment = Alignment.TopLeft,
                X = HeaderPadding,
                Y = HeaderPadding,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = Color.Transparent,
            };

            var leftColor = IsRanked
                ? SkinManager.Skin.SongSelect.ModifiersRankedHeaderLeftColor
                : SkinManager.Skin.SongSelect.ModifiersUnrankedHeaderLeftColor;

            var rightColor = IsRanked
                ? SkinManager.Skin.SongSelect.ModifiersRankedHeaderRightColor
                : SkinManager.Skin.SongSelect.ModifiersUnrankedHeaderRightColor;

            var leftTextColor = IsRanked
                ? SkinManager.Skin.SongSelect.ModifiersRankedLeftTextColor
                : SkinManager.Skin.SongSelect.ModifiersUnrankedLeftTextColor;

            var rightTextColor = IsRanked
                ? SkinManager.Skin.SongSelect.ModifiersRankedRightTextColor
                : SkinManager.Skin.SongSelect.ModifiersUnrankedRightTextColor;

            HeaderLeft = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderLeft ?? UserInterface.UniversalHeaderLeft,
                new SliceMargins(20, 20, 20, 20))
            {
                Parent = HeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = leftColor
            };

            HeaderLeftText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), IsRanked ? "Ranked" : "Unranked", 22)
            {
                Parent = HeaderLeft,
                Alignment = Alignment.MidLeft,
                X = HeaderPadding,
                Tint = leftTextColor
            };

            HeaderRight = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderRight ?? UserInterface.UniversalHeaderRight,
                new SliceMargins(20, 20, 20, 20))
            {
                Parent = HeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(40, HeaderHeight),
                Tint = rightColor
            };

            HeaderRightText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold),
                IsRanked ? "These mods can be used for ranked scores" : "Scores will not be submitted while using these", 22)
            {
                Parent = HeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = rightTextColor,
                X = HeaderPadding
            };

            UpdateHeaderWidth();
        }

        /// <summary>
        /// </summary>
        private void UpdateHeaderWidth()
        {
            if (HeaderLeft == null || HeaderLeftText == null || HeaderRight == null || HeaderRightText == null)
                return;

            HeaderLeft.Width = HeaderLeftText.Width + HeaderPadding * 2;
            HeaderRight.X = HeaderLeft.Width;
            HeaderRight.Width = HeaderRightText.Width + HeaderPadding * 2;

            if (HeaderContainer != null)
                HeaderContainer.Width = HeaderLeft.Width + HeaderRight.Width;
        }
    }
}
