using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;

namespace Quaver.Shared.Skinning.Menus
{
    public class SkinMenuUniversal : SkinMenu
    {
        /// <summary>
        ///     Texture for the Universal header left part
        /// </summary>
        public Texture2D? HeaderLeft { get; private set; }

        /// <summary>
        ///     Texture for the Universal header right part
        /// </summary>
        public Texture2D? HeaderRight { get; private set; }

        /// <summary>
        ///     Texture for the Universal header
        /// </summary>
        public Texture2D? Header { get; private set; }

        /// <summary>
        ///     Color of the Universal header background
        /// </summary>
        public Color HeaderColor { get; private set; } = new Color(57,139,208,255);



        /// <summary>
        ///     The color of the button content (text/icons)
        /// </summary>
        public Color ButtonContentColor { get; private set; } = new Color(255,255,255,255);

        /// <summary>
        ///     The color of the button when it's not active
        /// </summary>
        public Color ButtonNotActiveColor { get; private set; } = new Color(24, 30, 37, 255);

        /// <summary>
        ///     The color of the button when it's active
        /// </summary>
        public Color ButtonActiveColor { get; private set; } = new Color(57, 139, 208, 255);

        /// <summary>
        ///     The color of the button when it's hovered
        /// </summary>
        public Color ButtonHoverColor { get; private set; } = new Color(54, 78, 103, 255);

        /// <summary>
        ///     Search help background texture.
        /// </summary>
        public Texture2D? SearchHelpBackground { get; private set; }

        /// <summary>
        ///     Search help second background texture.
        /// </summary>
        public Texture2D? SearchHelpSecondBackground { get; private set; }

        /// <summary>
        ///     The color of text descriptions in the search filter panel.
        /// </summary>
        public Color SearchFilterPanelDescriptionColor { get; private set; } = new Color(175, 201, 229, 255);

        /// <summary>
        ///     The color of accent text/icons in the search filter panel.
        /// </summary>
        public Color SearchFilterPanelTextColor { get; private set; } = new Color(255, 255, 255, 255);

        /// <summary>
        /// </summary>
        /// <param name="store"></param>
        /// <param name="config"></param>
        public SkinMenuUniversal(SkinStore store, IniData config) : base(store, config)
        {
        }

        /// <summary>
        /// </summary>
        protected override void ReadConfig()
        {
            HeaderColor = ConfigHelper.ReadColor(HeaderColor, Config["Universal"]["HeaderColor"]);
            ButtonContentColor = ConfigHelper.ReadColor(ButtonContentColor, Config["Universal"]["ButtonContentColor"]);
            ButtonNotActiveColor = ConfigHelper.ReadColor(ButtonNotActiveColor, Config["Universal"]["ButtonNotActiveColor"]);
            ButtonActiveColor = ConfigHelper.ReadColor(ButtonActiveColor, Config["Universal"]["ButtonActiveColor"]);
            ButtonHoverColor = ConfigHelper.ReadColor(ButtonHoverColor, Config["Universal"]["ButtonHoverColor"]);
            SearchFilterPanelDescriptionColor = ConfigHelper.ReadColor(SearchFilterPanelDescriptionColor, Config["Universal"]["SearchFilterPanelDescriptionColor"]);
            SearchFilterPanelTextColor = ConfigHelper.ReadColor(SearchFilterPanelTextColor, Config["Universal"]["SearchFilterPanelTextColor"]);
        }

        /// <summary>
        /// </summary>
        protected override void LoadElements()
        {
            HeaderLeft = LoadAndResizeSkinElement("Universal", "universal-header-left.png", 102, 40) ?? UserInterface.UniversalHeaderLeft;
            HeaderRight = LoadAndResizeSkinElement("Universal", "universal-header-right.png", 102, 40) ?? UserInterface.UniversalHeaderRight;
            Header = LoadAndResizeSkinElement("Universal", "universal-header.png", 102, 40) ?? UserInterface.UniversalHeader;

            SearchHelpBackground = LoadSkinElement("Universal", "search-filter-background.png") ?? UserInterface.FilterPanelSearchBg;
            SearchHelpSecondBackground = LoadSkinElement("Universal", "search-filter-second-background.png") ?? UserInterface.FilterPanelSearchSecondBg;
        }
    }
}
