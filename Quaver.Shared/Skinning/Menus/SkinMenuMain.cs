using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Config;

namespace Quaver.Shared.Skinning.Menus
{
    public class SkinMenuMain : SkinMenu
    {
        public Texture2D Background { get; private set; }

        public Texture2D NavigationButton { get; private set; }

        public Texture2D NavigationButtonSelected { get; private set; }

        public Texture2D NavigationButtonHovered { get; private set; }

        public float NavigationButtonHoveredAlpha { get; private set; } = 0.35f;

        public Texture2D TipPanel { get; private set; }

        public Texture2D NewsPanel { get; private set; }

        public Texture2D JukeboxOverlay { get; private set; }

        public Texture2D NoteVisualizer { get; private set; }

        public float NoteVisualizerOpacity { get; private set; } = 0.60f;

        public Color NavigationButtonTextColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color NavigationQuitButtonTextColor { get; private set; } = new Color(249, 100, 93, 255);

        public Color TipTitleColor { get; private set; } = new Color(69, 214, 245, 255);

        public Color TipTextColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color NewsTitleColor { get; private set; } = new Color(69, 214, 245, 255);

        public Color NewsDateColor { get; private set; } = new Color(128, 128, 128, 255);

        public Color NewsTextColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color JukeboxProgressBarColor { get; private set; } = new Color(255, 222, 124, 255);

        public Texture2D LogoBackground { get; private set; }

        public SkinMenuMain(SkinStore store, IniData config) : base(store, config)
        {
        }

        protected override void ReadConfig()
        {
            var ini = Config["MainMenu"];

            var navigationButtonHoveredAlpha = ini["NavigationButtonHoveredAlpha"];
            NavigationButtonHoveredAlpha = ConfigHelper.ReadFloat(NavigationButtonHoveredAlpha, navigationButtonHoveredAlpha);

            var noteVisualizerOpacity = ini["NoteVisualizerOpacity"];
            NoteVisualizerOpacity = ConfigHelper.ReadFloat(NoteVisualizerOpacity, noteVisualizerOpacity);

            var navBtnTextColor = ini["NavigationButtonTextColor"];
            NavigationButtonTextColor = ConfigHelper.ReadColor(NavigationButtonTextColor, navBtnTextColor);

            var navQuitBtnTextColor = ini["NavigationQuitButtonTextColor"];
            NavigationQuitButtonTextColor = ConfigHelper.ReadColor(NavigationQuitButtonTextColor, navQuitBtnTextColor);

            var tipTitleColor = ini["TipTitleColor"];
            TipTitleColor = ConfigHelper.ReadColor(TipTitleColor, tipTitleColor);

            var tipTextColor = ini["TipTextColor"];
            TipTextColor = ConfigHelper.ReadColor(TipTextColor, tipTextColor);

            var newsTitleColor = ini["NewsTitleColor"];
            NewsTitleColor = ConfigHelper.ReadColor(NewsTitleColor, newsTitleColor);

            var newsDateColor = ini["NewsDateColor"];
            NewsDateColor = ConfigHelper.ReadColor(NewsDateColor, newsDateColor);

            var newsTextColor = ini["NewsTextColor"];
            NewsTextColor = ConfigHelper.ReadColor(NewsTextColor, newsTextColor);

            var jukeboxProgressBarColor = ini["JukeboxProgressBarColor"];
            JukeboxProgressBarColor = ConfigHelper.ReadColor(JukeboxProgressBarColor, jukeboxProgressBarColor);
        }

        protected override void LoadElements()
        {
            const string folder = "MainMenu";

            Background = LoadSkinElement(folder, "menu-background.png");
            NavigationButton = LoadSkinElement(folder, "navigation-button.png");
            NavigationButtonSelected = LoadSkinElement(folder, "navigation-button-selected.png");
            NavigationButtonHovered = LoadSkinElement(folder, "navigation-button-hovered.png");
            TipPanel = LoadSkinElement(folder, "tip-panel.png");
            NewsPanel = LoadSkinElement(folder, "news-panel.png");
            JukeboxOverlay = LoadSkinElement(folder, "jukebox-overlay.png");
            NoteVisualizer = LoadSkinElement(folder, "note-visualizer.png");
            LogoBackground = LoadSkinElement(folder, "logo-background.png");
        }
    }
}
