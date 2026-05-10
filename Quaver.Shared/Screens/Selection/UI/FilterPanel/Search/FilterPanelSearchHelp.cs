using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Assets;
using Wobble.Graphics.Sprites.Text;
using Quaver.Shared.Graphics;
using Wobble.Managers;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.FilterPanel.Search
{
    public class FilterPanelSearchHelp : Container
    {
        /// <summary>
        ///     The background of the help panel
        /// </summary>
        private NineSliceSprite PanelBackground { get; }

        private Color TextColor => SkinManager.Skin.Universal.SearchFilterPanelDescriptionColor;
        private Color AccentColor => SkinManager.Skin.Universal.SearchFilterPanelTextColor;
        private Texture2D SearchHelpBackground => SkinManager.Skin?.Universal?.SearchHelpBackground ?? UserInterface.FilterPanelSearchBg;
        private Texture2D SearchHelpSecondBackground => SkinManager.Skin?.Universal?.SearchHelpSecondBackground ?? UserInterface.FilterPanelSearchSecondBg;

        public FilterPanelSearchHelp()
        {
            SetChildrenVisibility = true;

            // Main Panel Content
            PanelBackground = new NineSliceSprite(SearchHelpBackground, new SliceMargins(0, 0, 6, 6))
            {
                Parent = this, // Parent to this, not DimContainer
                Y = 10,
                Size = new ScalableVector2(100, 100) // Placeholder size
            };

            // Header Text
            var headerText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "SEARCH FILTERS", 22)
            {
                Parent = PanelBackground,
                Tint = TextColor,
                X = 15,
                Y = 15
            };

            // Operators Row
            var operatorsContainer = new Container
            {
                Parent = PanelBackground,
                X = 15,
                Y = headerText.Y + headerText.Height + 15,
                Width = PanelBackground.Width - 30, // Full width minus margins
                Height = 32
            };

            var operatorsText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Operators", 20)
            {
                Parent = operatorsContainer,
                Alignment = Alignment.MidLeft,
                Tint = TextColor,
            };

            // Columns Container
            var columnsContainer = new Container
            {
                Parent = PanelBackground,
                X = 15,
                Y = operatorsContainer.Y + operatorsContainer.Height,
                Height = 64,
                Width = PanelBackground.Width - 30
            };

            // 1st Column
            var firstColumn = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = columnsContainer,
                X = 0,
                Width = 25,
                Height = 64
            };

            CreateOperatorInfo(firstColumn, "=", "equal", 18, 74, 10);
            CreateOperatorInfo(firstColumn, "!=", "not equal", 18, 74, 42);


            // 2nd Column
            var secondColumn = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = columnsContainer,
                X = firstColumn.X + firstColumn.Width + 5,
                Width = 25,
                Height = 64
            };

            CreateOperatorInfo(secondColumn, ">", "greater than", 22, 169, 10);
            CreateOperatorInfo(secondColumn, ">=", "greater than or equal", 22, 169, 42);

            // 3rd Column
            var thirdColumn = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = columnsContainer,
                X = secondColumn.X + secondColumn.Width + 5,
                Width = 25,
                Height = 64
            };

            CreateOperatorInfo(thirdColumn, "<", "less than", 22, 141, 10);
            CreateOperatorInfo(thirdColumn, "<=", "less than or equal", 22, 141, 42);

            // 4th Column
            var fourthColumn = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = columnsContainer,
                X = thirdColumn.X + thirdColumn.Width + 5,
                Width = 25,
                Height = 64
            };

            CreateOperatorInfo(fourthColumn, "/", "or", 8, 30, 10);
            CreateOperatorInfo(fourthColumn, ",", "and", 8, 30, 42);

            // 5th Column
            var fifthColumn = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = columnsContainer,
                X = fourthColumn.X + fourthColumn.Width + 5,
                Width = 25,
                Height = 64
            };

            CreateOperatorInfo(fifthColumn, ":", "contains", 4, 69, 10);

            // Headers Row
            float headerY = columnsContainer.Y + columnsContainer.Height + 15;
            float currentX = columnsContainer.X;

            CreateHeaderInfo(currentX, headerY, 105, "Filters");
            currentX += 105 + 35;

            CreateHeaderInfo(currentX, headerY, 120, "Flags");
            currentX += 120 + 35;

            CreateHeaderInfo(currentX, headerY, 190, "Arguments");
            currentX += 190 + 30;

            CreateHeaderInfo(currentX, headerY, 200, "Example");

            // Sample Filter: Artist
            float filterY = headerY + 32;
            float rowHeight;
            float rowGap = 5; // Small gap between rows

            // Artist
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Artist:", "artists | a", "string", "artists:xi | a:xi");
            filterY += rowHeight + rowGap;

            // Title
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Title:", "title | ti", "string", "title:freedom | ti:freedom");
            filterY += rowHeight + rowGap;

            // Creator
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Creator:", "creators | c", "string", "creators:star | c:star");
            filterY += rowHeight + rowGap;

            // Difficulty
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Difficulty:", "difficulty | d", "number", "difficulty>5 | d>5");
            filterY += rowHeight + rowGap;

            // Difficulty Name
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Diff Name:", "diffname | diffn", "string", "diffname:easy | diffn:easy");
            filterY += rowHeight + rowGap;

            // Source
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Source:", "sources | so", "string", "sources:ost | so:ost");
            filterY += rowHeight + rowGap;

            // Length
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Length:", "length | l", "number | time (m:ss)", "length>2:00 | l>2:00");
            filterY += rowHeight + rowGap;

            // BPM
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "BPM:", "bpm | b", "number", "bpm>180 | b>180");
            filterY += rowHeight + rowGap;

            // Ranked Status
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Status:", "status | s", "ranked | r, unranked | u", "status=ranked | s=ranked");
            filterY += rowHeight + rowGap;

            // Tags
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Tags:", "tags | ta", "string", "tags:anime | ta:anime");
            filterY += rowHeight + rowGap;

            // Long Note %
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "LN %:", "lns | ln", "int | percentage", "lns>40 | ln>40");
            filterY += rowHeight + rowGap;

            // Keys
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Keys:", "keys | k", "1-10", "keys=4 | k=4");
            filterY += rowHeight + rowGap;

            // Game
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Game:", "game | g", "quaver | q, osu | o", "game=osu | g=osu");
            filterY += rowHeight + rowGap;

            // Genre
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Genre:", "genre | ge", "string", "genre:rock | ge:rock");
            filterY += rowHeight + rowGap;

            // Description
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Desc:", "description | de", "string", "description:hard | de:hard");
            filterY += rowHeight + rowGap;

            // Notes Per Second
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "NPS:", "nps | n", "number", "nps>10 | n>10");
            filterY += rowHeight + rowGap;

            // Times Played
            rowHeight = CreateFilterRow(columnsContainer.X, filterY, fifthColumn.X + fifthColumn.Width, "Plays:", "timesplayed | t", "number", "timesplayed>10 | t>10");
            filterY += rowHeight + rowGap;
            // Set dynamic height for the entire panel
            PanelBackground.Height = filterY + 12; // Add some padding
            PanelBackground.Width = 782;
            Height = PanelBackground.Height;
        }



        public override void Update(GameTime gameTime)
        {
            if (Visible)
            {
                // Height is now set dynamically in constructor
            }

            base.Update(gameTime);
        }
        private void CreateOperatorInfo(Drawable parent, string symbol, string name, float symbolWidth, float nameWidth, float y)
        {
            var symbolBox = new Container
            {
                Parent = parent,
                X = 10,
                Y = y,
                Width = symbolWidth,
                Height = 12
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), symbol, 20)
            {
                Parent = symbolBox,
                Alignment = Alignment.MidLeft,
                Tint = AccentColor
            };

            var nameBox = new Container
            {
                Parent = parent,
                X = symbolBox.X + symbolBox.Width + 15,
                Y = y,
                Width = nameWidth,
                Height = 12
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), name, 20)
            {
                Parent = nameBox,
                Alignment = Alignment.MidLeft,
                Tint = TextColor,
                Alpha = 0.85f
            };

            parent.Width = nameBox.X + nameBox.Width + 10;
        }

        private void CreateHeaderInfo(float x, float y, float width, string text)
        {
            var header = new Container
            {
                Parent = PanelBackground,
                X = x,
                Y = y,
                Width = width,
                Height = 32
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), text, 20)
            {
                Parent = header,
                Alignment = Alignment.MidLeft,
                Tint = TextColor
            };
        }

        private float CreateFilterRow(float x, float y, float width, string name, string flags, string args, string example)
        {
            var rowBackground = new NineSliceSprite(SearchHelpSecondBackground, new SliceMargins(10, 10, 10, 10))
            {
                Parent = PanelBackground,
                X = x,
                Y = y,
                Width = width
            };

            // Calculate X positions based on header layout
            // Filters (0) -> Flags (140) -> Arguments (295) -> Example (520)
            float flagsX = 140;
            float argsX = 295;
            float exampleX = 515;

            // Name
            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), name, 20)
            {
                Parent = rowBackground,
                X = 10,
                Y = 6, // Vertical centering for single line (32px height)
                Tint = AccentColor
            };

            // Flags
            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), flags, 20)
            {
                Parent = rowBackground,
                X = flagsX,
                Y = 6,
                Tint = TextColor,
                Alpha = 0.85f
            };

            // Arguments (Dynamic Height)
            var argsText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), args, 20)
            {
                Parent = rowBackground,
                X = argsX,
                Y = 6,
                MaxWidth = 200,
                Tint = TextColor,
                Alpha = 0.85f
            };


            // Example (Dynamic Height)
            var exampleText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), example, 20)
            {
                Parent = rowBackground,
                X = exampleX,
                Y = 6,
                MaxWidth = 210,
                Tint = TextColor,
                Alpha = 0.85f
            };

            // Calculate height based on content
            float maxContentHeight = System.Math.Max(argsText.Height, exampleText.Height);

            // Set dynamic height with padding
            rowBackground.Height = System.Math.Max(32, maxContentHeight + 12);
            return rowBackground.Height;
        }
    }
}
