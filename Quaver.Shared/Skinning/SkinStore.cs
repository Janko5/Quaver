/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using IniFileParser;
using IniFileParser.Exceptions;
using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoreLinq.Extensions;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Skinning.Menus;
using Wobble;
using Wobble.Assets;
using Wobble.Audio.Samples;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Form;
using Wobble.Logging;

namespace Quaver.Shared.Skinning
{
    public class SkinStore
    {
        /// <summary>
        ///     The folder name of the skin
        /// </summary>
        public string Skin { get; }

        /// <summary>
        ///     The directory of the skin.
        /// </summary>
        public string Dir
        {
            get
            {
                if (ConfigManager.UseSteamWorkshopSkin == null)
                    return "";

                if (!ConfigManager.UseSteamWorkshopSkin.Value)
                    return $"{ConfigManager.SkinDirectory.Value}/{Skin}";

                return $"{ConfigManager.SteamWorkshopDirectory.Value}/{Skin}";
            }
        }

        // byte[].GetHashCode() is unreliable so we use int instead
        private readonly Dictionary<int, Texture2D> _textureCache = new();
        private int _cacheCount = 0;

        /// <summary>
        ///     The skin.ini file.
        /// </summary>
        internal IniData Config { get; private set; }

        /// <summary>
        ///     Dictionary that contains both skins for 4K & 7K
        /// </summary>
        internal Dictionary<GameMode, SkinKeys> Keys { get; }

        /// <summary>
        ///     Skinning for the menu borders
        /// </summary>
        internal SkinMenuBorder MenuBorder { get; }

        /// <summary>
        ///     Skinning for the main menu
        /// </summary>
        internal SkinMenuMain MainMenu { get; }

        /// <summary>
        ///     Skinning for the Results menu
        /// </summary>
        internal SkinMenuResults Results { get; }

        /// <summary>
        ///     Skinning for the song select menu
        /// </summary>
        internal SkinMenuSongSelect SongSelect { get; }

        /// <summary>
        ///     Skinning for universal elements (volume controller, notifications, etc.)
        /// </summary>
        internal SkinMenuUniversal Universal { get; }

        /// <summary>
        ///     Skinning for the music visualizer
        /// </summary>
        internal SkinMusicVisualizer MusicVisualizer { get; }

        /// <summary>
        ///     Skinning for the Volume Controller
        /// </summary>
        internal SkinMenuVolumeController VolumeController { get; }

        /// <summary>
        ///     The name of the skin.
        /// </summary>
        internal string Name { get; private set; } = "Default Quaver Skin";

        /// <summary>
        ///     The author of the skin.
        /// </summary>
        internal string Author { get; private set; } = "Quaver Team";

        /// <summary>
        ///     The version of the skin.
        /// </summary>
        internal string Version { get; private set; } = "v0.1";

        /// <summary>
        ///     Regular expression for spritesheets
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static string SpritesheetRegex(string element) => $@"^{element}@(\d+)x(\d+).png$";

        /// <summary>
        ///     The user's mouse cursor.
        /// </summary>
        internal Texture2D Cursor { get; private set; }

        /// <summary>
        ///     Whether the cursor should be centered.
        /// </summary>
        internal bool CenterCursor { get; private set; }

        /// <summary>
        ///     Whether the skin uses its own backgrounds.
        /// </summary>
        internal bool UseSkinBackgrounds { get; private set; }

        /// <summary>
        ///     The version of the user interface (1.0 = legacy, 2.0 = modern).
        /// </summary>
        public float UserInterfaceVersion { get; private set; } = 1.0f;

        /// <summary>
        ///     Grade Textures.
        /// </summary>
        internal Dictionary<Grade, Texture2D> Grades { get; } = new Dictionary<Grade, Texture2D>();

        /// <summary>
        ///     Grade Textures for Results.
        /// </summary>
        internal Dictionary<Grade, Texture2D> GradesLarge { get; } = new Dictionary<Grade, Texture2D>();

        /// <summary>
        ///     Grade Textures.
        /// </summary>
        internal Texture2D HitBubbles { get; private set; }

        /// <summary>
        ///     The health bar displayed in the background. (Non-Moving one.)
        /// </summary>
        internal List<Texture2D> HitBubblesBackground { get; private set; }

        /// <summary>
        ///     Judgement animation elements
        /// </summary>
        internal Dictionary<Judgement, List<Texture2D>> Judgements { get; } = new Dictionary<Judgement, List<Texture2D>>();

        /// <summary>
        ///     The numbers that display the user's current score.
        /// </summary>
        internal Texture2D[] ScoreDisplayNumbers { get; } = new Texture2D[10];

        /// <summary>
        ///     The decimal "." character in the score display.
        /// </summary>
        internal Texture2D ScoreDisplayDecimal { get; private set; }

        /// <summary>
        ///     The percent "%" character in the score display.
        /// </summary>
        internal Texture2D ScoreDisplayPercent { get; private set; }

        /// <summary>
        ///     The numbers that display the user's current combo
        /// </summary>
        internal Texture2D[] ComboDisplayNumbers = new Texture2D[10];

        /// <summary>
        ///     The numbers that display the current song time.
        /// </summary>
        internal Texture2D[] SongTimeDisplayNumbers = new Texture2D[10];

        /// <summary>
        ///     The ":" character displayed in the song time display.
        /// </summary>
        internal Texture2D SongTimeDisplayColon { get; private set; }

        /// <summary>
        ///     The minus "-" character displayed in the song time display.
        /// </summary>
        internal Texture2D SongTimeDisplayMinus { get; private set; }

        /// <summary>
        ///     The user's background displayed during the pause menu.
        /// </summary>
        internal Texture2D PauseBackground { get; private set; }

        /// <summary>
        ///     The continue button displayed in the pause menu
        /// </summary>
        internal Texture2D PauseContinue { get; private set; }

        /// <summary>
        ///     The retry button displayed in the pause menu
        /// </summary>
        internal Texture2D PauseRetry { get; private set; }

        /// <summary>
        ///     The back button displayed in the pause menu.
        /// </summary>
        internal Texture2D PauseBack { get; private set; }

        /// <summary>
        ///     The overlay that displayed the judgement counts.
        /// </summary>
        internal Dictionary<Judgement, Texture2D> JudgementOverlay { get; } = new Dictionary<Judgement, Texture2D>();

        /// <summary>
        ///     The background of the judgement overlay.
        /// </summary>
        internal Dictionary<Judgement, Texture2D> JudgementOverlayBackground { get; } = new Dictionary<Judgement, Texture2D>();

        /// <summary>
        ///     The scoreboard displayed on the screen for the player.
        /// </summary>
        internal Texture2D Scoreboard { get; private set; }

        /// <summary>
        ///     The scoreboard displayed for other players.
        /// </summary>
        internal Texture2D ScoreboardOther { get; private set; }

        /// <summary>
        ///     The scoreboard for the red team
        /// </summary>
        internal Texture2D ScoreboardRedTeam { get; set; }

        /// <summary>
        ///     The scoreboard for the red team (other players)
        /// </summary>
        internal Texture2D ScoreboardRedTeamOther { get; set; }

        /// <summary>
        ///     The scoreboard for the blue team
        /// </summary>
        internal Texture2D ScoreboardBlueTeam { get; set; }

        /// <summary>
        ///     The scoreboard for the blue team (other players)
        /// </summary>
        internal Texture2D ScoreboardBlueTeamOther { get; set; }

        /// <summary>
        ///     The health bar displayed in the background. (Non-Moving one.)
        /// </summary>
        internal List<Texture2D> HealthBarBackground { get; private set; }

        /// <summary>
        ///     The health bar displayed in the foreground (Moving)
        /// </summary>
        internal List<Texture2D> HealthBarForeground { get; private set; }

        /// <summary>
        ///     Skip animation when user is on a break.
        /// </summary>
        internal List<Texture2D> Skip { get; private set; }

        /// <summary>
        ///     Displayed when the user achieves high combos
        /// </summary>
        internal List<Texture2D> ComboAlerts { get; private set; }

        /// <summary>
        ///     Displayed when being eliminated from battle royale
        /// </summary>
        internal Texture2D BattleRoyaleEliminated { get; private set; }

        /// <summary>
        ///     Displayed when in danger of being eliminated
        /// </summary>
        internal Texture2D BattleRoyaleWarning { get; private set; }

        /// <summary>
        ///     Backgrounds for the skin. Only loaded if UseSkinBackgrounds is true.
        /// </summary>
        internal List<string> BackgroundPaths { get; private set; }

        /// <summary>
        ///     The texture for an open dropdown.
        /// </summary>
        internal Texture2D DropdownOpen { get; private set; }

        /// <summary>
        ///     The texture for a closed dropdown.
        /// </summary>
        internal Texture2D DropdownClose { get; private set; }

        /// <summary>
        ///     The texture for the bottom of a dropdown when open.
        /// </summary>
        internal Texture2D DropdownBottom { get; private set; }

        /// <summary>
        ///     The texture for the middle of a dropdown when open.
        /// </summary>
        internal Texture2D DropdownTop { get; private set; }
        internal Texture2D DropdownMiddle { get; private set; }

        /// <summary>
        ///     The universal scrollbar track texture.
        /// </summary>
        internal Texture2D? Scrollbar { get; private set; }

        /// <summary>
        ///     The universal square button texture used in filter panel buttons.
        /// </summary>
        internal Texture2D? SquareButton { get; private set; }

        /// <summary>
        ///     The universal background texture used across multiple screens.
        /// </summary>
        internal Texture2D? Background { get; private set; }

        /// <summary>
        ///     The universal info background texture used in the Playercard.
        /// </summary>
        public Texture2D? InfoBackground { get; private set; }

        /// <summary>
        ///     The universal header left texture.
        /// </summary>
        public Texture2D? HeaderLeft { get; private set; }

        /// <summary>
        ///     The universal header right texture.
        /// </summary>
        public Texture2D? HeaderRight { get; private set; }

        /// <summary>
        ///     The universal header texture.
        /// </summary>
        public Texture2D? Header { get; private set; }

        /// <summary>
        ///     The universal switch background texture.
        /// </summary>
        public Texture2D? UniversalSwitchBackground { get; private set; }

        /// <summary>
        ///     The universal switch on thumb texture.
        /// </summary>
        public Texture2D? UniversalSwitchOn { get; private set; }

        /// <summary>
        ///     The universal switch off thumb texture.
        /// </summary>
        public Texture2D? UniversalSwitchOff { get; private set; }

        public Texture2D? More { get; private set; }
        public Texture2D? MoreHover { get; private set; }

        internal Color DropdownCloseColor { get; private set; }
        internal Color DropdownOpenColor { get; private set; }
        internal Color DropdownSeparatorColor { get; private set; }
        internal Color DropdownMiddleCloseColor { get; private set; }

        /// <summary>
        ///     The background color for universal-info-background used e.g. in the Playercard.
        /// </summary>
        public Color PlayercardInfoBackgroundColor { get; private set; }


        /// <summary>
        ///     The main background color of the universal switch.
        /// </summary>
        public Color SwitchMainBackgroundColor { get; private set; }

        /// <summary>
        ///     The background color of the universal switch when turned on.
        /// </summary>
        public Color SwitchOnBackgroundColor { get; private set; }

        /// <summary>
        ///     The background color of the universal switch when turned off.
        /// </summary>
        public Color SwitchOffBackgroundColor { get; private set; }

        /// <summary>
        ///     The color of the universal switch on icon.
        /// </summary>
        public Color SwitchOnColor { get; private set; }

        /// <summary>
        ///     The color of the universal switch off icon.
        /// </summary>
        public Color SwitchOffColor { get; private set; }


        /// <summary>
        ///    The background color of the filter panel dropdowns when hovered.
        /// </summary>
        internal Color DropdownHoverColor { get; private set; }

        /// <summary>
        ///   The text color of the filter panel dropdowns.
        /// </summary>
        internal Color DropdownTextColor { get; private set; }

        /// <summary>
        ///   The background color of the right click options dropdowns.
        /// </summary>
        internal Color DropdownRightClickOptionsColor { get; private set; }

        /// <summary>
        ///  The background color of the filter panel search box.
        /// </summary>
        internal Color SearchFilterPanelColor { get; private set; }



        /// <summary>
        ///  The text color of the filter panel search box when active.
        /// </summary>
        internal Color SearchActiveTextColor { get; private set; }
        
        internal Color SearchPlaceholderTextColor { get; private set; }
        internal Color SearchCounterTextColor { get; private set; }
        internal Color SearchHelpColorActive { get; private set; }
        internal Color SearchHelpColorNotActive { get; private set; }

        /// <summary>
        ///  The background color of the filter panel buttons when not active.
        /// </summary>
        internal Color ButtonNotActiveColor { get; private set; }

        /// <summary>
        ///  The background color of the filter panel buttons when active.
        /// </summary>
        internal Color ButtonActiveColor { get; private set; }

        /// <summary>
        ///  The background color of the filter panel buttons when hovered.
        /// </summary>
        internal Color ButtonHoverColor { get; private set; }

        /// <summary>
        ///  The content color of the filter panel buttons.
        /// </summary>
        internal Color ButtonContentColor { get; private set; }

        /// <summary>
        ///  The background color of the difficulty slider values.
        /// </summary>
        internal Color DifficultySliderValuesBackgroundColor { get; private set; }

        /// <summary>
        ///  The background color of the difficulty slider track when not selected.
        /// </summary>
        internal Color DifficultySliderNotSelectedBackgroundColor { get; private set; }

        /// <summary>
        ///  The background color of the difficulty slider track when selected.
        /// </summary>
        internal Color DifficultySliderSelectedBackgroundColor { get; private set; }

        /// <summary>
        ///  The color of the difficulty slider thumbs.
        /// </summary>
        internal Color DifficultySliderThumbColor { get; private set; }

        /// <summary>
        ///     If we should use difficulty colors for the difficulty slider range bar.
        /// </summary>
        internal bool UseDifficultyColorsInDifficultySlider { get; private set; }

        /// <summary>
        ///     The color of the universal scrollbar thumb (draggable part).
        /// </summary>
        internal Color ScrollbarThumbColor { get; private set; }

        /// <summary>
        ///     The background color of the universal scrollbar track.
        /// </summary>
        internal Color ScrollbarBackgroundColor { get; private set; }

        /// <summary>
        ///     The top/bottom slice margins for the scrollbar NineSliceSprite.
        ///     Left and Right are always 0. The ini value is "top,bottom" (e.g. "8,8").
        /// </summary>
        internal SliceMargins ScrollbarTopBottomMargins { get; private set; }

        /// <summary>
        ///     Sound effect elements.
        /// </summary>
        internal AudioSample SoundHit { get; private set; }
        internal AudioSample SoundHitClap { get; private set; }
        internal AudioSample SoundHitWhistle { get; private set; }
        internal AudioSample SoundHitFinish { get; private set; }
        internal AudioSample SoundComboBreak { get; private set; }
        internal AudioSample SoundMineExplode { get; private set; }
        internal AudioSample SoundApplause { get; private set; }
        internal AudioSample SoundScreenshot { get; private set; }
        internal AudioSample SoundClick { get; private set; }
        internal AudioSample SoundSelect { get; private set; }
        internal AudioSample SoundBack { get; private set; }
        internal AudioSample SoundHover { get; private set; }
        internal AudioSample SoundFailure { get; private set; }
        internal AudioSample SoundRetry { get; private set; }
        internal List<AudioSample> SoundComboAlerts { get; private set; }
        internal List<AudioSample> SoundMenuKeyClick { get; private set; }

        /// <summary>
        ///     Ctor - Loads up a skin from a given directory.
        /// </summary>
        internal SkinStore(string skin = null, bool editor = false)
        {
            Stopwatch totalSW = new();
            Dictionary<GameMode, (Stopwatch sw, int cacheHits)> keyTimingInfo = new();
            totalSW.Restart();

            Skin = string.IsNullOrEmpty(skin) ? ConfigManager.Skin?.Value : skin;
            LoadConfig();

            // Load up Keys game mode skins.
            Keys = new Dictionary<GameMode, SkinKeys>();
            // keyCount 0 is the shared keys folder
            for (var keyCount = 0; keyCount <= ModeHelper.MaxKeyCount; keyCount++)
            {
                int cacheCountStart = _cacheCount;
                Stopwatch sw = new();
                sw.Restart();

                var mode = keyCount == 0 ? 0 : ModeHelper.FromKeyCount(keyCount);
                Keys.Add(mode, new SkinKeys(this, mode, keyCount == 0 ? null : Keys[0], editor ? ConfigManager.DefaultEditorSkin.Value?.ToString() : null));

                sw.Stop();
                keyTimingInfo.Add(mode, (sw, _cacheCount - cacheCountStart));
            }

            try
            {
                MenuBorder = new SkinMenuBorder(this, Config);
                MainMenu = new SkinMenuMain(this, Config);
                Results = new SkinMenuResults(this, Config);
                SongSelect = new SkinMenuSongSelect(this, Config);
                Universal = new SkinMenuUniversal(this, Config);
                VolumeController = new SkinMenuVolumeController(this, Config);
                MusicVisualizer = new SkinMusicVisualizer(this, Config);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }

            LoadUniversalElements();

            // Change cursor image.
            GameBase.Game.GlobalUserInterface.Cursor.Image = Cursor;
            GameBase.Game.GlobalUserInterface.Cursor.Center = CenterCursor;

            totalSW.Stop();

            string keyTimeString = "";
            foreach ((GameMode mode, (Stopwatch sw, int cacheHits)) in keyTimingInfo)
            {
                if (!string.IsNullOrEmpty(keyTimeString))
                {
                    keyTimeString += ", ";
                }
                keyTimeString += $"{SkinKeys.ModeShorthand(mode)}:{(double)sw.ElapsedTicks / Stopwatch.Frequency * 1000d:0.00}[{cacheHits}]";
            }

            Logger.Important($"skin loading times:\n" +
                $"total: {(double)totalSW.ElapsedTicks / Stopwatch.Frequency * 1000d:0.00} [{_cacheCount}]\n" + keyTimeString,
                LogType.Runtime
            );
        }

        /// <summary>
        ///     Loads up the config file and its default elements.
        /// </summary>
        private void LoadConfig()
        {
            const string name = "skin.ini";

            if (!File.Exists($"{Dir}/{name}"))
                return;

            try
            {
                Config = new IniFileParser.IniFileParser(new ConcatenateDuplicatedKeysIniDataParser()).ReadFile($"{Dir}/{name}");

                // Parse very general things in config.
                Name = ConfigHelper.ReadString(Name, Config["General"]["Name"]);
                Author = ConfigHelper.ReadString(Author, Config["General"]["Author"]);
                Version = ConfigHelper.ReadString(Version, Config["General"]["Version"]);
                CenterCursor = ConfigHelper.ReadBool(false, Config["General"]["CenterCursor"]);
                UseSkinBackgrounds = ConfigHelper.ReadBool(false, Config["General"]["UseSkinBackgrounds"]);
                UserInterfaceVersion = ConfigHelper.ReadFloat(1.0f, Config["General"]["UserInterfaceVersion"]);

            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
        }

        /// <summary>
        ///     Loads universal skin elements used across every single game mode.
        /// </summary>
        private void LoadUniversalElements()
        {
            const string cursor = "main-cursor";
            Cursor = LoadSingleTexture($"{Dir}/Cursor/{cursor}", $"Quaver.Resources/Textures/Skins/Shared/Cursor/{cursor}.png");

            LoadGradeElements();
            LoadHitBubbleElements();
            LoadJudgements();
            LoadNumberDisplays();
            LoadPause();
            LoadScoreboard();
            LoadHealthBar();
            LoadSkip();
            LoadComboAlert();
            LoadMultiplayerElements();
            LoadBackgrounds();
            LoadSoundEffects();
            LoadUniversalTextures();
            LoadUniversalSkinOptions();
        }

        /// <summary>
        ///     Loads all skinnable textures from the Universal skin folder.
        /// </summary>
        private void LoadUniversalTextures()
        {
            var folder = $"{Dir}/Universal";

            const string top = "universal-dropdown-top";
            DropdownTop = LoadSingleTexture($"{folder}/{top}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{top}.png", UserInterface.DropdownMiddle);

            const string open = "universal-dropdown-open";
            DropdownOpen = LoadSingleTexture($"{folder}/{open}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{open}.png");

            const string close = "universal-dropdown-close";
            DropdownClose = LoadSingleTexture($"{folder}/{close}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{close}.png");

            const string bottom = "universal-dropdown-bottom";
            DropdownBottom = LoadSingleTexture($"{folder}/{bottom}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{bottom}.png", UserInterface.DropdownBottom);

            const string middle = "universal-dropdown-middle";
            DropdownMiddle = LoadSingleTexture($"{folder}/{middle}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{middle}.png", UserInterface.DropdownMiddle);

            const string scrollbar = "universal-scrollbar";
            Scrollbar = LoadSingleTexture($"{folder}/{scrollbar}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{scrollbar}.png");

            const string squareButton = "universal-square-button";
            SquareButton = LoadSingleTexture($"{folder}/{squareButton}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{squareButton}.png");

            const string background = "universal-background";
            Background = LoadSingleTexture($"{folder}/{background}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{background}.png");

            const string infoBg = "universal-info-background";
            InfoBackground = LoadSingleTexture($"{folder}/{infoBg}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{infoBg}.png");

            const string headerLeft = "universal-header-left";
            HeaderLeft = LoadSingleTexture($"{folder}/{headerLeft}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{headerLeft}.png");

            const string headerRight = "universal-header-right";
            HeaderRight = LoadSingleTexture($"{folder}/{headerRight}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{headerRight}.png");

            const string header = "universal-header";
            Header = LoadSingleTexture($"{folder}/{header}", $"Quaver.Resources/Textures/Skins/Shared/Universal/{header}.png");

            const string switchBg = "universal-switch-background";
            UniversalSwitchBackground = LoadSingleTexture($"{folder}/{switchBg}", $"Quaver.Resources/Textures/UI/Universal/{switchBg}.png", UserInterface.PlayercardSwitchBackground);

            const string switchOn = "universal-switch-on";
            UniversalSwitchOn = LoadSingleTexture($"{folder}/{switchOn}", $"Quaver.Resources/Textures/UI/Universal/{switchOn}.png");

            const string switchOff = "universal-switch-off";
            UniversalSwitchOff = LoadSingleTexture($"{folder}/{switchOff}", $"Quaver.Resources/Textures/UI/Universal/{switchOff}.png");

            More = LoadSingleTexture($"{folder}/More", "Quaver.Resources/Textures/UI/Mods/More.png");
            MoreHover = LoadSingleTexture($"{folder}/More-hover", "Quaver.Resources/Textures/UI/Mods/More-hover.png");
        }

        private void LoadUniversalSkinOptions()
        {
            // Check if Universal section exists in skin.ini to avoid NullReferenceException
            var universalSection = Config?["Universal"];

            DropdownCloseColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DropdownCloseColor"]);
            DropdownOpenColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DropdownOpenColor"]);
            DropdownSeparatorColor = ConfigHelper.ReadColor(new Color(255, 255, 255, 255), universalSection?["DropdownSeparatorColor"]);
            DropdownMiddleCloseColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DropdownMiddleCloseColor"]);
            DropdownHoverColor = ConfigHelper.ReadColor(new Color(54, 78, 103, 255), universalSection?["DropdownHoverColor"]);
            DropdownTextColor = ConfigHelper.ReadColor(new Color(255,255,255,255), universalSection?["DropdownTextColor"]);
            DropdownRightClickOptionsColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DropdownRightClickOptionsColor"]);

            PlayercardInfoBackgroundColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["PlayercardInfoBackgroundColor"]);

            SearchFilterPanelColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["SearchFilterPanelColor"]);
            SearchActiveTextColor = ConfigHelper.ReadColor(new Color(217, 227, 244, 255), universalSection?["SearchActiveTextColor"]);
            SearchPlaceholderTextColor = ConfigHelper.ReadColor(new Color(217, 227, 244, 128), universalSection?["SearchPlaceholderTextColor"]);
            SearchCounterTextColor = ConfigHelper.ReadColor(new Color(217, 227, 244, 255), universalSection?["SearchCounterTextColor"]);
            SearchHelpColorActive = ConfigHelper.ReadColor(new Color(57, 139, 208, 255), universalSection?["SearchHelpColorActive"]);
            SearchHelpColorNotActive = ConfigHelper.ReadColor(new Color(217, 227, 244, 255), universalSection?["SearchHelpColorNotActive"]);

            ButtonNotActiveColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["ButtonNotActiveColor"]);
            ButtonActiveColor = ConfigHelper.ReadColor(new Color(57, 139, 208, 255), universalSection?["ButtonActiveColor"]);
            ButtonHoverColor = ConfigHelper.ReadColor(new Color(54, 78, 103, 255), universalSection?["ButtonHoverColor"]);
            ButtonContentColor = ConfigHelper.ReadColor(new Color(255,255,255,255), universalSection?["ButtonContentColor"]);

            DifficultySliderValuesBackgroundColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DifficultySliderValuesBackgroundColor"]);
            DifficultySliderNotSelectedBackgroundColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["DifficultySliderNotSelectedBackgroundColor"]);
            DifficultySliderSelectedBackgroundColor = ConfigHelper.ReadColor(new Color(57, 139, 208, 255), universalSection?["DifficultySliderSelectedBackgroundColor"]);
            DifficultySliderThumbColor = ConfigHelper.ReadColor(new Color(255,255,255,255), universalSection?["DifficultySliderThumbColor"]);
            UseDifficultyColorsInDifficultySlider =
                ConfigHelper.ReadBool(UseDifficultyColorsInDifficultySlider,
                    universalSection?["UseDifficultyColorsInDifficultySlider"]);

            ScrollbarThumbColor = ConfigHelper.ReadColor(new Color(217, 227, 244, 255), universalSection?["ScrollbarThumbColor"]);
            ScrollbarBackgroundColor = ConfigHelper.ReadColor(new Color(24, 30, 37, 255), universalSection?["ScrollbarBackgroundColor"]);

            // Format: "top,bottom" (e.g. "8,8") — Left and Right are always 0
            var scrollbarMarginsRaw = universalSection?["ScrollbarTopBottomMargins"];
            if (!string.IsNullOrEmpty(scrollbarMarginsRaw))
            {
                var parts = scrollbarMarginsRaw.Split(',');
                if (parts.Length == 2
                    && int.TryParse(parts[0].Trim(), out var scrollTop)
                    && int.TryParse(parts[1].Trim(), out var scrollBottom))
                    ScrollbarTopBottomMargins = new SliceMargins(0, 0, scrollTop, scrollBottom);
                else
                    ScrollbarTopBottomMargins = new SliceMargins(0, 0, 8, 8);
            }
            else
            {
                ScrollbarTopBottomMargins = new SliceMargins(0, 0, 8, 8);
            }

            SwitchMainBackgroundColor = ConfigHelper.ReadColor(new Color(39, 48, 56, 255), universalSection?["SwitchMainBackgroundColor"]);
            SwitchOnBackgroundColor = ConfigHelper.ReadColor(new Color(37,200,140,255), universalSection?["SwitchOnBackgroundColor"]);
            SwitchOffBackgroundColor = ConfigHelper.ReadColor(new Color(255,58,111,255), universalSection?["SwitchOffBackgroundColor"]);
            SwitchOnColor = ConfigHelper.ReadColor(new Color(255,255,255,255), universalSection?["SwitchOnColor"]);
            SwitchOffColor = ConfigHelper.ReadColor(new Color(255,255,255,255), universalSection?["SwitchOffColor"]);
        }

        private Texture2D GetTextureFromCacheOr(byte[] buffer, Func<byte[], Texture2D> func)
        {
            var md5 = MD5.HashData(buffer);

            var hash = new HashCode();
            hash.AddBytes(md5);
            int hc = hash.ToHashCode();

            if (_textureCache.ContainsKey(hc))
            {
                _cacheCount++;
                return _textureCache[hc];
            }

            Texture2D texture = func(buffer);
            _textureCache.Add(hc, texture);
            return texture;
        }

        /// <summary>
        ///     Loads a single texture element.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="resource"></param>
        /// <param name="extension"></param>
        internal Texture2D LoadSingleTexture(string path, string resource, Texture2D? fallback = null, string extension = ".png")
        {
            path += extension;

            try
            {
                byte[] buffer;
                if (File.Exists(path))
                {
                    buffer = File.ReadAllBytes(path);
                }
                else
                {
                    if (fallback != null)
                    {
                        return fallback;
                    }
                    buffer = GameBase.Game.Resources.Get(resource);
                    if (buffer == null)
                    {
                        return UserInterface.BlankBox;
                    }
                }
                return GetTextureFromCacheOr(buffer, AssetLoader.LoadTexture2D);
            }
            catch (Exception)
            {
                Logger.Warning($"Failed to load: {resource}. Using default!", LogType.Runtime, false);
                return UserInterface.BlankBox;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="element"></param>
        /// <param name="resource"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        internal List<Texture2D> LoadSpritesheet(string folder, string element, string resource, int rows, int columns, List<Texture2D>? fallback = null)
        {
            try
            {
                var dir = $"{Dir}/{folder}";

                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir);

                    foreach (var f in files)
                    {
                        var regex = new Regex(SpritesheetRegex($"{element}"));
                        var match = regex.Match(Path.GetFileName(f));

                        // See if the file matches the regex.
                        if (match.Success)
                        {
                            byte[] file = File.ReadAllBytes(f);

                            // Load it up if so.
                            var texture = GetTextureFromCacheOr(file, AssetLoader.LoadTexture2D);

                            return AssetLoader.LoadSpritesheetFromTexture(texture, int.Parse(match.Groups[1].Value),
                                int.Parse(match.Groups[2].Value));
                        }

                        // Otherwise check to see if that base element (without animations) actually exists.
                        // if so, load it singularly into a list.
                        if (Path.GetFileNameWithoutExtension(f) == element)
                            return new List<Texture2D> { AssetLoader.LoadTexture2DFromFile(f) };
                    }
                }

                if (fallback != null)
                {
                    return fallback;
                }

                // If we end up getting down here, that means we need to load the spritesheet from our resources.
                // if 0x0 is specified for the default, then it'll simply load the element without rowsxcolumns
                if (rows == 0 && columns == 0)
                    return new List<Texture2D> { LoadSingleTexture($"{dir}/{element}", resource + ".png", null) };

                if (resource == null)
                    return new List<Texture2D> { UserInterface.BlankBox };

                var textureBytes = GameBase.Game.Resources.Get($"{resource}@{rows}x{columns}.png");

                if (textureBytes == null)
                    return new List<Texture2D> { UserInterface.BlankBox };

                return AssetLoader.LoadSpritesheetFromTexture(AssetLoader.LoadTexture2D(textureBytes), rows, columns);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                return new List<Texture2D> { UserInterface.BlankBox };
            }
        }

        /// <summary>
        ///     Loads .wav sound effect files.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private static AudioSample LoadSoundEffect(string path, string element, string resourceFolder)
        {
            path += ".wav";

            // Load the actual file stream if it exists.
            try
            {
                if (File.Exists(path))
                    return new AudioSample(path);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }

            // Load the default if the path doesn't exist
            return new AudioSample(GameBase.Game.Resources.Get($"Quaver.Resources/SFX/{resourceFolder}/{element}.wav"));
        }

        /// <summary>
        ///     Loads all grade texture elements
        /// </summary>
        private void LoadGradeElements()
        {
            // Load Grades
            foreach (Grade grade in Enum.GetValues(typeof(Grade)))
            {
                if (grade == Grade.None)
                    continue;

                Grades[grade] = LoadSingleTexture($"{Dir}/Grades/grade-small-{grade.ToString().ToLower()}", $"Quaver.Resources/Textures/Skins/Shared/Grades/grade-small-{grade.ToString().ToLower()}.png");
                GradesLarge[grade] = LoadSingleTexture($"{Dir}/Grades/grade-large-{grade.ToString().ToLower()}", $"Quaver.Resources/Textures/UI/Results/grade-large-{grade.ToString().ToLower()}.png");
            }
        }

        /// <summary>
        ///     Loads all grade texture elements
        /// </summary>
        private void LoadHitBubbleElements()
        {
            // Load Grades
            HitBubbles = LoadSingleTexture($"{Dir}/HitBubbles/bubble", $"Quaver.Resources/Textures/Skins/Shared/HitBubbles/bubble.png");

            var hitBubblesFolder = $"/HitBubbles/";

            const string bubblesBackground = "bubbles-background";
            HitBubblesBackground = LoadSpritesheet(hitBubblesFolder, bubblesBackground,
                $"Quaver.Resources/Textures/Skins/Shared/HitBubbles/bubbles-background", 0, 0);
        }

        /// <summary>
        ///     Loads judgement texture elements.
        /// </summary>
        private void LoadJudgements()
        {
            const string folder = "Judgements";

            // Load Judgements and judgement overlay
            foreach (Judgement j in Enum.GetValues(typeof(Judgement)))
            {
                if (j == Judgement.Ghost)
                    continue;

                var element = $"judge-{j.ToString().ToLower()}";
                var judgementOverlay = $"judgement-overlay-{j.ToString().ToLower()}";
                var judgementOverlayBackground = $"judgement-overlay-background-{j.ToString().ToLower()}";

                // Compatibility for old skin.
                if (!File.Exists($"{Dir}/{folder}/{judgementOverlay}.png"))
                    judgementOverlay = "judgement-overlay";

                Judgements[j] = LoadSpritesheet($"/{folder}/", element,
                    $"Quaver.Resources/Textures/Skins/Shared/Judgements/{element}", 0, 0);
                JudgementOverlay[j] = LoadSingleTexture($"{Dir}/{folder}/{judgementOverlay}",
                    $"Quaver.Resources/Textures/Skins/Shared/Judgements/judgement-overlay.png");
                JudgementOverlayBackground[j] = LoadSingleTexture($"{Dir}/{folder}/{judgementOverlayBackground}",
                    null);
            }
        }

        /// <summary>
        ///     Loads all number display skin elements.
        /// </summary>
        private void LoadNumberDisplays()
        {
            // Load Number Displays
            var numberDisplayFolder = $"{Dir}/Numbers/";
            for (var i = 0; i < 10; i++)
            {
                // Score
                var scoreElement = $"score-{i}";
                ScoreDisplayNumbers[i] = LoadSingleTexture($"{numberDisplayFolder}/{scoreElement}",
                    $"Quaver.Resources/Textures/Skins/Shared/Numbers/{scoreElement}.png");

                // Combo
                var comboElement = $"combo-{i}";
                ComboDisplayNumbers[i] = LoadSingleTexture($"{numberDisplayFolder}/{comboElement}",
                    $"Quaver.Resources/Textures/Skins/Shared/Numbers/{comboElement}.png");

                // Song Time
                var songTimeElement = $"song-time-{i}";
                SongTimeDisplayNumbers[i] = LoadSingleTexture($"{numberDisplayFolder}/{songTimeElement}",
                    $"Quaver.Resources/Textures/Skins/Shared/Numbers/{songTimeElement}.png");
            }

            const string scoreDecimal = "score-decimal";
            ScoreDisplayDecimal = LoadSingleTexture($"{numberDisplayFolder}/{scoreDecimal}", $"Quaver.Resources/Textures/Skins/Shared/Numbers/{scoreDecimal}.png");

            const string scorePercent = "score-percent";
            ScoreDisplayPercent = LoadSingleTexture($"{numberDisplayFolder}/{scorePercent}", $"Quaver.Resources/Textures/Skins/Shared/Numbers/{scorePercent}.png");

            const string songTimeColon = "song-time-colon";
            SongTimeDisplayColon = LoadSingleTexture($"{numberDisplayFolder}/{songTimeColon}", $"Quaver.Resources/Textures/Skins/Shared/Numbers/{songTimeColon}.png");

            const string songTimeMinus = "song-time-minus";
            SongTimeDisplayMinus = LoadSingleTexture($"{numberDisplayFolder}/{songTimeMinus}", $"Quaver.Resources/Textures/Skins/Shared/Numbers/{songTimeMinus}.png");
        }

        /// <summary>
        ///     Loads all pause menu elements.
        /// </summary>
        private void LoadPause()
        {
            var pauseFolder = $"{Dir}/Pause/";

            const string pauseBackground = "pause-background";
            PauseBackground = LoadSingleTexture($"{pauseFolder}/{pauseBackground}", $"Quaver.Resources/Textures/Skins/Shared/Pause/{pauseBackground}.png");

            const string pauseContinue = "pause-continue";
            PauseContinue = LoadSingleTexture($"{pauseFolder}/{pauseContinue}", $"Quaver.Resources/Textures/Skins/Shared/Pause/{pauseContinue}.png");

            const string pauseRetry = "pause-retry";
            PauseRetry = LoadSingleTexture($"{pauseFolder}/{pauseRetry}", $"Quaver.Resources/Textures/Skins/Shared/Pause/{pauseRetry}.png");

            const string pauseBack = "pause-back";
            PauseBack = LoadSingleTexture($"{pauseFolder}/{pauseBack}", $"Quaver.Resources/Textures/Skins/Shared/Pause/{pauseBack}.png");
        }

        /// <summary>
        ///     Loads all scoreboard elements.
        /// </summary>
        private void LoadScoreboard()
        {
            var scoreboardFolder = $"{Dir}/Scoreboard/";

            const string scoreboard = "scoreboard";
            Scoreboard = LoadSingleTexture($"{scoreboardFolder}/{scoreboard}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboard}.png");

            const string scoreboardOther = "scoreboard-other";
            ScoreboardOther = LoadSingleTexture($"{scoreboardFolder}/{scoreboardOther}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboardOther}.png");

            const string scoreboardRedTeam = "scoreboard-red-team";
            ScoreboardRedTeam = LoadSingleTexture($"{scoreboardFolder}/{scoreboardRedTeam}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboardRedTeam}.png");

            const string scoreboardRedTeamOther = "scoreboard-red-team-other";
            ScoreboardRedTeamOther = LoadSingleTexture($"{scoreboardFolder}/{scoreboardRedTeamOther}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboardRedTeamOther}.png");

            const string scoreboardBlueTeam = "scoreboard-blue-team";
            ScoreboardBlueTeam = LoadSingleTexture($"{scoreboardFolder}/{scoreboardBlueTeam}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboardBlueTeam}.png");

            const string scoreboardBlueTeamOther = "scoreboard-blue-team-other";
            ScoreboardBlueTeamOther = LoadSingleTexture($"{scoreboardFolder}/{scoreboardBlueTeamOther}", $"Quaver.Resources/Textures/Skins/Shared/Scoreboard/{scoreboardBlueTeamOther}.png");
        }

        /// <summary>
        ///     Loads all health bar elements.
        /// </summary>
        private void LoadHealthBar()
        {
            var healthFolder = $"/Health/";

            const string healthBackground = "health-background";
            HealthBarBackground = LoadSpritesheet(healthFolder, healthBackground,
                $"Quaver.Resources/Textures/Skins/Shared/Health/health-background", 0, 0);

            const string healthForeground = "health-foreground";
            HealthBarForeground = LoadSpritesheet(healthFolder, healthForeground,
                $"Quaver.Resources/Textures/Skins/Shared/Health/health-foreground", 0, 0);
        }

        /// <summary>
        ///     Loads the skip animation element.
        /// </summary>
        private void LoadSkip()
        {
            var skipFolder = $"/Skip/";
            const string skip = "skip";

            Skip = LoadSpritesheet(skipFolder, skip, $"Quaver.Resources/Textures/Skins/Shared/Skip/{skip}", 0, 0);
        }

        /// <summary>
        ///     Loads combo alerts if they exist
        /// </summary>
        private void LoadComboAlert()
        {
            var comboAlertFolder = $"{Dir}/Combo/";

            const string comboAlert = "combo-alert";

            ComboAlerts = new List<Texture2D>();

            for (var i = 0; i < 100 && File.Exists($"{comboAlertFolder}/{comboAlert}-{i + 1}.png"); i++)
            {
                ComboAlerts.Add(LoadSingleTexture($"{comboAlertFolder}/{comboAlert}-{i + 1}",
                    $"Quaver.Resources/Textures/Skins/Shared/Combo/{comboAlert}-{i + 1}.png"));
            }
        }

        private void LoadMultiplayerElements()
        {
            var multiplayerFolder = $"{Dir}/Multiplayer/";
            const string battleRoyaleEliminated = "eliminated";

            BattleRoyaleEliminated = LoadSingleTexture($"{multiplayerFolder}/{battleRoyaleEliminated}"
                , $"Quaver.Resources/Textures/Skins/Shared/Multiplayer/{battleRoyaleEliminated}.png");

            const string battleRoyaleWarning = "warning";
            BattleRoyaleWarning = LoadSingleTexture($"{multiplayerFolder}/{battleRoyaleWarning}"
                , $"Quaver.Resources/Textures/Skins/Shared/Multiplayer/{battleRoyaleWarning}.png");
        }

        private void LoadBackgrounds()
        {
            if (!UseSkinBackgrounds)
                return;

            var backgroundFolder = $"{Dir}/Backgrounds/";
            const string background = "background";

            BackgroundPaths = new List<string>();

            string[] validExtensions = { ".png", ".jpg", ".jpeg" };
            if (!Directory.Exists(backgroundFolder))
                return;

            var files = Directory.GetFiles(backgroundFolder);

            foreach (var f in files)
            {
                if (!validExtensions.Contains(Path.GetExtension(f).ToLower()))
                    continue;

                var metadata = SixLabors.ImageSharp.Image.Identify(f);
                if (metadata.Width > 2560 || metadata.Height > 1440)
                    continue;

                BackgroundPaths.Add(f);
            }
        }

        /// <summary>
        ///     Loads all sound effect elements.
        /// </summary>
        public void LoadSoundEffects()
        {
            var sfxFolder = $"{Dir}/SFX/";

            const string soundHit = "sound-hit";
            SoundHit = LoadSoundEffect($"{sfxFolder}/{soundHit}", soundHit, "Gameplay");

            const string soundHitClap = "sound-hitclap";
            SoundHitClap = LoadSoundEffect($"{sfxFolder}/{soundHitClap}", soundHitClap, "Gameplay");

            const string soundHitWhistle = "sound-hitwhistle";
            SoundHitWhistle = LoadSoundEffect($"{sfxFolder}/{soundHitWhistle}", soundHitWhistle, "Gameplay");

            const string soundHitFinish = "sound-hitfinish";
            SoundHitFinish = LoadSoundEffect($"{sfxFolder}/{soundHitFinish}", soundHitFinish, "Gameplay");

            const string soundComboBreak = "sound-combobreak";
            SoundComboBreak = LoadSoundEffect($"{sfxFolder}/{soundComboBreak}", soundComboBreak, "Gameplay");

            const string soundMineExplode = "sound-mineexplode";
            SoundMineExplode = LoadSoundEffect($"{sfxFolder}/{soundMineExplode}", soundMineExplode, "Gameplay");

            const string soundFailure = "sound-failure";
            SoundFailure = LoadSoundEffect($"{sfxFolder}/{soundFailure}", soundFailure, "Gameplay");

            const string soundRetry = "sound-retry";
            SoundRetry = LoadSoundEffect($"{sfxFolder}/{soundRetry}", soundRetry, "Gameplay");

            const string soundApplause = "sound-applause";
            SoundApplause = LoadSoundEffect($"{sfxFolder}/{soundApplause}", soundApplause, "Menu");

            const string soundScreenshot = "sound-screenshot";
            SoundScreenshot = LoadSoundEffect($"{sfxFolder}/{soundScreenshot}", soundScreenshot, "Menu");

            const string soundClick = "sound-click";
            SoundClick = LoadSoundEffect($"{sfxFolder}/{soundClick}", soundClick, "Menu");

            const string soundBack = "sound-back";
            SoundBack = LoadSoundEffect($"{sfxFolder}/{soundBack}", soundBack, "Menu");

            const string soundHover = "sound-hover";
            SoundHover = LoadSoundEffect($"{sfxFolder}/{soundHover}", soundHover, "Menu");

            const string soundSelect = "sound-select";
            if (File.Exists($"{sfxFolder}/{soundSelect}.wav"))
                SoundSelect = LoadSoundEffect($"{sfxFolder}/{soundSelect}", soundSelect, "Menu");

            const string soundComboAlert = "sound-combo-alert";
            SoundComboAlerts = new List<AudioSample>();

            for (var i = 0; i < 100 && File.Exists($"{sfxFolder}/{soundComboAlert}-{i + 1}.wav"); i++)
                SoundComboAlerts.Add(LoadSoundEffect($"{sfxFolder}/{soundComboAlert}-{i + 1}", soundComboAlert + "-" + i + 1, "Menu"));

            const string soundMenuKeyClick = "sound-menu-keyclick";
            SoundMenuKeyClick = new List<AudioSample>();

            for (var i = 0; i < 100 && File.Exists($"{sfxFolder}/{soundMenuKeyClick}-{i + 1}.wav"); i++)
                SoundMenuKeyClick.Add(LoadSoundEffect($"{sfxFolder}/{soundMenuKeyClick}-{i + 1}", soundMenuKeyClick + "-" + i + 1, "Menu"));
            Textbox.KeyClickSamples = SoundMenuKeyClick;
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            foreach (var p in GetType().GetProperties())
            {
                if (p.PropertyType == typeof(Texture2D))
                {
                    var tex = (Texture2D)p.GetValue(this);

                    if (tex != null && !tex.IsDisposed)
                        tex.Dispose();
                }
                else if (p.PropertyType == typeof(AudioSample))
                {
                    var sample = (AudioSample)p.GetValue(this);

                    if (sample != null && !sample.IsDisposed)
                        sample.Dispose();
                }
                else if (p.PropertyType == typeof(Texture2D[]))
                {
                    var textureList = (Texture2D[])p.GetValue(this);
                    textureList?.ForEach(x =>
                    {
                        if (x != null && !x.IsDisposed)
                            x.Dispose();
                    });
                }
                else if (p.PropertyType == typeof(List<Texture2D>))
                {
                    var textureList = (List<Texture2D>)p.GetValue(this);
                    textureList?.ForEach(x =>
                    {
                        if (x != null && !x.IsDisposed)
                            x.Dispose();
                    });
                }
                else if (p.PropertyType == typeof(List<AudioSample>))
                {
                    var textureList = (List<AudioSample>)p.GetValue(this);
                    textureList?.ForEach(x =>
                    {
                        if (x != null && !x.IsDisposed)
                            x.Dispose();
                    });
                }
            }

            foreach (var mode in Keys.Values)
            {
                foreach (var p in mode.GetType().GetProperties())
                {
                    if (p.PropertyType != typeof(Texture2D))
                        continue;

                    var tex = (Texture2D)p.GetValue(mode);

                    if (tex != null && !tex.IsDisposed)
                        tex.Dispose();
                }
            }
        }

        public static List<string> GetSkins()
        {

            var options = new List<string> { "Default Skin" };

            if (ConfigManager.SkinDirectory == null)
                return options;

            var skins = new List<string>();

            var skinDirectories = Directory.GetDirectories(ConfigManager.SkinDirectory.Value);

            var dirs = skinDirectories.Select(dir => new DirectoryInfo(dir).Name);
            skins.AddRange(dirs.ToList());

            var workshopDirectories = Directory.GetDirectories(ConfigManager.SteamWorkshopDirectory.Value);

            var workshopList = new List<string>();

            foreach (var directory in workshopDirectories)
            {
                if (File.Exists($"{directory}/skin.ini"))
                {
                    try
                    {
                        var data = new IniFileParser.IniFileParser(new ConcatenateDuplicatedKeysIniDataParser())
                            .ReadFile($"{directory}/skin.ini")["General"];
                        if (data["Name"] != null)
                            workshopList.Add($"{data["Name"]} <{new DirectoryInfo(directory).Name}>");
                    }
                    catch (ParsingException e)
                    {
                        Logger.Error($"Workshop skin at {directory} has an invalid skin.ini: {e}", LogType.Runtime);
                        NotificationManager.Show(NotificationLevel.Error,
                            $"Could not load workshop skin {new DirectoryInfo(directory).Name} because it contains errors!");
                        workshopList.Add($"Unknown <{new DirectoryInfo(directory).Name}>");
                    }
                }
                else
                    workshopList.Add($"({new DirectoryInfo(directory).Name})");
            }

            workshopList.Sort();
            skins.AddRange(workshopList);

            skins.Sort();
            options.AddRange(skins);
            return options;
        }
    }
}
