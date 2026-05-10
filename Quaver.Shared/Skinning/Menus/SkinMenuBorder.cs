using System;
using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;

namespace Quaver.Shared.Skinning.Menus
{
    public class SkinMenuBorder : SkinMenu
    {
        public Color BackgroundLineColor { get; private set; } = new Color(57, 139, 208, 255);

        public Color ForegroundLineColor { get; private set; } = new Color(217, 227, 244, 255);

        public Color ButtonTextColor { get; private set; } = new Color(175, 201, 229, 255);

        public Color ButtonTextHoveredColor { get; private set; } = new Color(81, 197, 249, 255);

        public Texture2D Background { get; private set; }

        public Texture2D BackgroundFooter { get; private set; }

        public Texture2D InfoBackground { get; private set; }

        public Texture2D UserPanelBackground { get; private set; }

        public Texture2D UserPanelAvatarMask { get; private set; }

        public Texture2D? UserPanelActivityLight { get; private set; }

        public Texture2D? FpsBackground { get; private set; }

        public Texture2D PlayercardAvatarMask { get; private set; }

        public Texture2D PlayercardBackgroundMask { get; private set; }

        public Texture2D PlayercardInfoBackground { get; private set; }

        public Color SquareButtonNotActiveColor { get; private set; } = new Color(40,48,56,255);

        public Color SquareButtonActiveColor { get; private set; } = new Color(57,139,208,255);

        public Color SquareButtonHoverColor { get; private set; } = new Color(54,78,103,255);

        public Color SquareButtonContentColor { get; private set; } = new Color(255,255,255,255);

        public Color SquareButtonContentSecondColor { get; private set; } = new Color(57,139,208,255);


        /// <summary>
        ///     Whether the border line (foreground + animated) is visible
        /// </summary>
        public bool ShowLine { get; private set; } = false;

        public SkinMenuBorder(SkinStore store, IniData config) : base(store, config)
        {
        }

        protected override void ReadConfig()
        {
            var ini = Config["MenuBorder"];

            var bgLineColor = ini["BackgroundLineColor"];
            BackgroundLineColor = ConfigHelper.ReadColor(BackgroundLineColor, bgLineColor);

            var fgLineColor = ini["ForegroundLineColor"];
            ForegroundLineColor = ConfigHelper.ReadColor(ForegroundLineColor, fgLineColor);

            var btnTextColor = ini["ButtonTextColor"];
            ButtonTextColor = ConfigHelper.ReadColor(ButtonTextColor, btnTextColor);

            var btnTextHoveredColor = ini["ButtonTextHoveredColor"];
            ButtonTextHoveredColor = ConfigHelper.ReadColor(ButtonTextHoveredColor, btnTextHoveredColor);


            var showLine = ini["MenuBorderLine"];
            ShowLine = ConfigHelper.ReadBool(ShowLine, showLine);

            var btnMenuBorderNotActive = ini["SquareButtonNotActiveColor"];
            SquareButtonNotActiveColor = ConfigHelper.ReadColor(SquareButtonNotActiveColor, btnMenuBorderNotActive);

            var btnMenuBorderActive = ini["SquareButtonActiveColor"];
            SquareButtonActiveColor = ConfigHelper.ReadColor(SquareButtonActiveColor, btnMenuBorderActive);

            var btnMenuBorderHover = ini["SquareButtonHoverColor"];
            SquareButtonHoverColor = ConfigHelper.ReadColor(SquareButtonHoverColor, btnMenuBorderHover);

            var btnMenuBorderContent = ini["SquareButtonContentColor"];
            SquareButtonContentColor = ConfigHelper.ReadColor(SquareButtonContentColor, btnMenuBorderContent);

            var btnMenuBorderContentSecond = ini["SquareButtonContentSecondColor"];
            SquareButtonContentSecondColor = ConfigHelper.ReadColor(SquareButtonContentSecondColor, btnMenuBorderContentSecond);
        }

        protected override void LoadElements()
        {
            Background = LoadSkinElement("MenuBorder", "menu-border-background.png");
            BackgroundFooter = LoadSkinElement("MenuBorder", "menu-border-background-footer.png");
            InfoBackground = LoadSkinElement("MenuBorder", "info-background.png");
            UserPanelBackground = LoadSkinElement("MenuBorder", "userpanel-background.png");
            UserPanelAvatarMask = LoadSkinElement("MenuBorder", "userpanel-avatar-mask.png");
            UserPanelActivityLight = LoadSkinElement("MenuBorder", "userpanel-activity-light.png");
            FpsBackground = LoadSkinElement("MenuBorder", "fps-background.png") ?? UserInterface.MenuBorderFpsBackground;
            PlayercardAvatarMask = LoadAndResizeSkinElement("MenuBorder", "playercard-avatar-mask.png", 80, 80);
            PlayercardBackgroundMask = LoadAndResizeSkinElement("MenuBorder", "playercard-background-mask.png", 526, 100);
            PlayercardInfoBackground = LoadAndResizeSkinElement("MenuBorder", "playercard-info-background.png", 526, 40);
        }
    }
}
