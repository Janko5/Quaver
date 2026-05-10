using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Config;
using Quaver.Shared.Assets;
using Wobble.Graphics;
using System; // Added for Enum.TryParse

namespace Quaver.Shared.Skinning.Menus
{
    public enum SkinLeaderboardIconPosition
    {
        RatingLeft,
        RatingRight,
        RatingAccRight
    }

    public class SkinMenuSongSelect : SkinMenu
    {

        public bool DisplayMapBackground { get; private set; }


        public byte MapBackgroundBrightness { get; private set; } = 15;

        public Color MapsetPanelSongTitleColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color MapsetPanelSongArtistColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color MapsetPanelCreatorColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color MapsetPanelByColor { get; private set; } = new Color(255, 255, 255, 255);

        public ScalableVector2 MapsetPanelBannerSize { get; private set; } = new ScalableVector2(380, 80);

        public float MapsetPanelHoveringAlpha { get; private set; } = 1f;

        public float PlaylistPanelHoveringAlpha { get; private set; } = 1f;

        public Color PlaylistPanelTitleColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color PlaylistPanelDescriptionColor { get; private set; } = new Color(255, 255, 255, 127);

        public Color PlaylistPanelByColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color PlaylistPanelCreatorColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color LeaderboardScoreColorEven { get; private set; } = new Color(24, 30, 37, 255);

        public Color LeaderboardScoreColorOdd { get; private set; } = new Color(24, 30, 37, 255);

        public Color LeaderboardScoreHoverColor { get; private set; } = new Color(54, 78, 103, 255);

        public int LeaderboardScoresGap { get; private set; } = 10;

        public Color LeaderboardScoreRankColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color LeaderboardScoreRatingColor { get; private set; } = new Color(233, 183, 54, 255);

        public Color LeaderboardScoreAccuracyColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color LeaderboardScoreUsernameSelfColor { get; private set; } = new Color(81, 197, 249, 255);

        public Color LeaderboardScoreUsernameOtherColor { get; private set; } = new Color(251, 255, 182, 255);

        public Color LeaderboardTitleColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color LeaderboardRankingTitleColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color LeaderboardDropdownColor { get; private set; } = new Color(57, 139, 208, 255);

        public Color GameModeTextColor { get; private set; } = Color.White;

        public Color LeaderboardStatusTextColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color PersonalBestTitleColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color NoPersonalBestColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color PersonalBestTrophyColor { get; private set; } = new Color(233, 183, 54, 255);

        public Color PersonalBestRankColor { get; private set; } = new Color(255, 255, 255, 255);
        public Color PersonalBestOfRankColor { get; private set; } = new Color(217, 227, 244, 255);
        public Color PersonalBestHeaderLeftColor { get; private set; } = new Color(57, 139, 208, 255);
        public Color PersonalBestHeaderRightColor { get; private set; } = new Color(24, 30, 37, 255);
        public Color ModifiersRankedHeaderLeftColor { get; private set; } = new Color(57, 139, 208, 255);
        public Color ModifiersRankedHeaderRightColor { get; private set; } = new Color(24, 30, 37, 255);
        public Color ModifiersRankedLeftTextColor { get; private set; } = new Color(255, 255, 255, 255);
        public Color ModifiersRankedRightTextColor { get; private set; } = new Color(255, 255, 255, 255);
        public Color ModifiersUnrankedHeaderLeftColor { get; private set; } = new Color(57, 139, 208, 255);
        public Color ModifiersUnrankedHeaderRightColor { get; private set; } = new Color(24, 30, 37, 255);
        public Color ModifiersUnrankedLeftTextColor { get; private set; } = new Color(255, 255, 255, 255);
        public Color ModifiersUnrankedRightTextColor { get; private set; } = new Color(255, 255, 255, 255);
        public Color ModifiersSliderBackgroundColor { get; private set; } = new Color(39, 48, 56, 255);
        public Color ModifiersSliderThumbColor { get; private set; } = new Color(255, 255, 255, 255);

        public Color GameMode1KColor { get; private set; } = new Color(0, 210, 200);
        public Color GameMode2KColor { get; private set; } = new Color(51, 189, 232);
        public Color GameMode3KColor { get; private set; } = new Color(0, 176, 255);
        public Color GameMode4KColor { get; private set; } = new Color(5, 135, 229);
        public Color GameMode5KColor { get; private set; } = new Color(57, 85, 227);
        public Color GameMode6KColor { get; private set; } = new Color(106, 79, 224);
        public Color GameMode7KColor { get; private set; } = new Color(155, 81, 224);
        public Color GameMode8KColor { get; private set; } = new Color(185, 106, 232);
        public Color GameMode9KColor { get; private set; } = new Color(208, 132, 238);
        public Color GameMode10KColor { get; private set; } = new Color(232, 154, 244);

        public Texture2D? SelectFilterPanelRight { get; private set; }

        public Texture2D? SelectFilterPanelLeft { get; private set; }

        public Texture2D? MapsetBannerMask { get; private set; }

        public Texture2D? PlaylistBannerMask { get; private set; }

        public int PlaylistPanelBannerSize { get; private set; } = 80;

        public float RankedStatusPosOffsetX { get; private set; } = -10;

        public float GameModePosOffsetX { get; private set; } = 124;
        public float RankedStatusPosOffsetY { get; private set; } = 20;
        public float GameModePosOffsetY { get; private set; } = -20;

        // Margins
        public int MapsetPanelMarginLeft { get; private set; } = 18;
        public int MapsetPanelMarginRight { get; private set; } = 10;
        public int? DifficultyPanelMarginLeft { get; private set; }
        public int? DifficultyPanelMarginRight { get; private set; }
        public int? PlaylistPanelMarginLeft { get; private set; }
        public int? PlaylistPanelMarginRight { get; private set; }

        // BPM and Length overlay position offsets
        public float MapsetBpmOverlayOffsetX { get; private set; } = 10f;
        public float MapsetBpmOverlayOffsetY { get; private set; } = 10f;
        public float MapsetLengthOverlayOffsetX { get; private set; } = 8f;
        public float MapsetLengthOverlayOffsetY { get; private set; } = 10f;
        public Color MapsetBpmOverlayColor { get; private set; } = new Color(26, 31, 36, 255);
        public Color MapsetLengthOverlayColor { get; private set; } = new Color(26, 31, 36, 255);
        public Color DifficultyOverlayColor { get; private set; } = new Color(26, 31, 36, 240);
        public Color MapsetBpmTextColor { get; private set; } = Color.White;
        public Color MapsetLengthTextColor { get; private set; } = Color.White;
        public Color DifficultyInfoIconsColor { get; private set; } = Color.White;
        public Color DifficultyInfoValuesColor { get; private set; } = new Color(200, 200, 200, 255);

        // Mapset info background color (used for BPM and Length overlays)

        #region MAPSET

        public Texture2D? MapsetSelected { get; private set; }

        public Texture2D? MapsetDeselected { get; private set; }

        public Texture2D? MapsetHovered { get; private set; }

        public Texture2D? PlaylistDeselected { get; private set; }

        public Texture2D? PlaylistHovered { get; private set; }
        public Texture2D? PlaylistOtherGameIcon { get; private set; }

        public Texture2D? LeaderboardAvatarMask { get; private set; }

        #endregion

        #region GAME_MODE

        public Texture2D? GameMode4K { get; private set; }

        public Texture2D? GameMode7K { get; private set; }

        public Texture2D? GameMode4K7K { get; private set; }

        public Texture2D? GameModeNone { get; private set; }

        public Texture2D? GameModeMask { get; private set; }

        public Texture2D? GameMode1K { get; private set; }
        public Texture2D? GameMode2K { get; private set; }
        public Texture2D? GameMode3K { get; private set; }
        public Texture2D? GameMode5K { get; private set; }
        public Texture2D? GameMode6K { get; private set; }
        public Texture2D? GameMode8K { get; private set; }
        public Texture2D? GameMode9K { get; private set; }
        public Texture2D? GameMode10K { get; private set; }

        public Texture2D? GameModeMixed { get; private set; }
        public Texture2D? ModifierSliderElement { get; private set; }

        #endregion

        #region  RANKED_STATUS

        public Texture2D? StatusNotSubmitted { get; private set; }

        public Texture2D? StatusUnranked { get; private set; }

        public Texture2D? StatusRanked { get; private set; }

        public Texture2D? StatusOsu { get; private set; }

        public Texture2D? StatusStepmania { get; private set; }

        public Texture2D? StatusVarious { get; private set; }

        public Texture2D? StatusNone { get; private set; }

        #endregion

        #region LEADERBOARD

        public Texture2D? LeaderboardPanel { get; private set; }

        public Texture2D? PersonalBestPanel { get; private set; }

        public Texture2D? LeaderboardScoreMask { get; private set; }

        public Texture2D? LeaderboardWarning { get; private set; }

        public Texture2D? LeaderboardInfo { get; private set; }

        public SkinLeaderboardIconPosition LeaderboardIconPosition { get; private set; }

        #endregion

        #region MAPSET_OVERLAYS

        public Texture2D? MapsetBpmBackground { get; private set; }

        public Texture2D? MapsetLengthBackground { get; private set; }

        public Texture2D? DifficultySelected { get; private set; }
        public Texture2D? DifficultyDeselected { get; private set; }
        public Texture2D? DifficultyHovered { get; private set; }
        public string DifficultyPanelSource { get; private set; } = "mapset";

        public Texture2D? ModifierSelectorBackground { get; private set; }
        public Texture2D? ModifierBackground { get; private set; }
        public Texture2D? ModifierBackgroundHovered { get; private set; }
        public Texture2D? TabsPanel { get; private set; }

        #endregion


        public SkinMenuSongSelect(SkinStore store, IniData config) : base(store, config)
        {
        }

        protected override void ReadConfig()
        {
            var ini = Config["SongSelect"];

            var displayMapBackground = ini["DisplayMapBackground"];
            ReadIndividualConfig(displayMapBackground, () => DisplayMapBackground = ConfigHelper.ReadBool(false, displayMapBackground));

            var mapBackgroundBrightness = ini["MapBackgroundBrightness"];
            MapBackgroundBrightness = ConfigHelper.ReadByte(MapBackgroundBrightness, mapBackgroundBrightness);

            var mapsetPanelSongTitleColor = ini["MapsetPanelSongTitleColor"];
            MapsetPanelSongTitleColor = ConfigHelper.ReadColor(MapsetPanelSongTitleColor, mapsetPanelSongTitleColor);

            var mapsetPanelSongArtistColor = ini["MapsetPanelSongArtistColor"];
            MapsetPanelSongArtistColor = ConfigHelper.ReadColor(MapsetPanelSongArtistColor, mapsetPanelSongArtistColor);

            var mapsetPanelCreatorColor = ini["MapsetPanelCreatorColor"];
            MapsetPanelCreatorColor = ConfigHelper.ReadColor(MapsetPanelCreatorColor, mapsetPanelCreatorColor);

            var mapsetPanelByColor = ini["MapsetPanelByColor"];
            MapsetPanelByColor = ConfigHelper.ReadColor(MapsetPanelByColor, mapsetPanelByColor);

            var mapsetPanelBannerSize = ini["MapsetPanelBannerSize"];
            MapsetPanelBannerSize = ConfigHelper.ReadVector2(MapsetPanelBannerSize, mapsetPanelBannerSize) ?? MapsetPanelBannerSize;

            var playlistPanelBannerSize = ini["PlaylistPanelBannerSize"];
            PlaylistPanelBannerSize = ConfigHelper.ReadInt32(PlaylistPanelBannerSize, playlistPanelBannerSize);

            var mapsetPanelHoveringAlpha = ini["MapsetPanelHoveringAlpha"];
            MapsetPanelHoveringAlpha = ConfigHelper.ReadFloat(MapsetPanelHoveringAlpha, mapsetPanelHoveringAlpha);

            var playlistPanelHoveringAlpha = ini["PlaylistPanelHoveringAlpha"];
            PlaylistPanelHoveringAlpha = ConfigHelper.ReadFloat(PlaylistPanelHoveringAlpha, playlistPanelHoveringAlpha);

            var playlistPanelTitleColor = ini["PlaylistPanelTitleColor"];
            PlaylistPanelTitleColor = ConfigHelper.ReadColor(PlaylistPanelTitleColor, playlistPanelTitleColor);

            var playlistPanelDescriptionColor = ini["PlaylistPanelDescriptionColor"];
            PlaylistPanelDescriptionColor = ConfigHelper.ReadColor(PlaylistPanelDescriptionColor, playlistPanelDescriptionColor);

            var playlistPanelByColor = ini["PlaylistPanelByColor"];
            PlaylistPanelByColor = ConfigHelper.ReadColor(PlaylistPanelByColor, playlistPanelByColor);

            var playlistPanelCreatorColor = ini["PlaylistPanelCreatorColor"];
            PlaylistPanelCreatorColor = ConfigHelper.ReadColor(PlaylistPanelCreatorColor, playlistPanelCreatorColor);

            var rankedStatusPosOffsetX = ini["RankedStatusPosOffsetX"];
            RankedStatusPosOffsetX = ConfigHelper.ReadInt32((int)RankedStatusPosOffsetX, rankedStatusPosOffsetX);

            var gameModePosOffsetX = ini["GameModePosOffsetX"];
            GameModePosOffsetX = ConfigHelper.ReadInt32((int)GameModePosOffsetX, gameModePosOffsetX);

            var rankedStatusPosOffsetY = ini["RankedStatusPosOffsetY"];
            RankedStatusPosOffsetY = ConfigHelper.ReadInt32((int)RankedStatusPosOffsetY, rankedStatusPosOffsetY);

            var gameModePosOffsetY = ini["GameModePosOffsetY"];
            GameModePosOffsetY = ConfigHelper.ReadInt32((int)GameModePosOffsetY, gameModePosOffsetY);

            var leaderboardScoreColorEven = ini["LeaderboardScoreColorEven"];
            LeaderboardScoreColorEven = ConfigHelper.ReadColor(LeaderboardScoreColorEven, leaderboardScoreColorEven);

            var leaderboardScoreColorOdd = ini["LeaderboardScoreColorOdd"];
            LeaderboardScoreColorOdd = ConfigHelper.ReadColor(LeaderboardScoreColorOdd, leaderboardScoreColorOdd);

            var leaderboardScoreHoverColor = ini["LeaderboardScoreHoverColor"];
            LeaderboardScoreHoverColor = ConfigHelper.ReadColor(LeaderboardScoreHoverColor, leaderboardScoreHoverColor);

            var leaderboardScoresGap = ini["LeaderboardScoresGap"];
            LeaderboardScoresGap = ConfigHelper.ReadInt32(LeaderboardScoresGap, leaderboardScoresGap);

            var leaderboardScoreRankColor = ini["LeaderboardScoreRankColor"];
            LeaderboardScoreRankColor = ConfigHelper.ReadColor(LeaderboardScoreRankColor, leaderboardScoreRankColor);

            var leaderboardScoreRatingColor = ini["LeaderboardScoreRatingColor"];
            LeaderboardScoreRatingColor = ConfigHelper.ReadColor(LeaderboardScoreRatingColor, leaderboardScoreRatingColor);

            var leaderboardIconPosition = ini["LeaderboardIconPosition"];
            ReadIndividualConfig(leaderboardIconPosition, () =>
            {
                if (Enum.TryParse(leaderboardIconPosition, true, out SkinLeaderboardIconPosition pos))
                    LeaderboardIconPosition = pos;
            });

            var leaderboardScoreAccuracyColor = ini["LeaderboardScoreAccuracyColor"];
            LeaderboardScoreAccuracyColor = ConfigHelper.ReadColor(LeaderboardScoreAccuracyColor, leaderboardScoreAccuracyColor);

            var leaderboardScoreUsernameSelfColor = ini["LeaderboardScoreUsernameSelfColor"];
            LeaderboardScoreUsernameSelfColor = ConfigHelper.ReadColor(LeaderboardScoreUsernameSelfColor, leaderboardScoreUsernameSelfColor);

            var leaderboardScoreUsernameOtherColor = ini["LeaderboardScoreUsernameOtherColor"];
            LeaderboardScoreUsernameOtherColor = ConfigHelper.ReadColor(LeaderboardScoreUsernameOtherColor, leaderboardScoreUsernameOtherColor);

            var leaderboardTitleColor = ini["LeaderboardTitleColor"];
            LeaderboardTitleColor = ConfigHelper.ReadColor(LeaderboardTitleColor, leaderboardTitleColor);

            var leaderboardRankingTitleColor = ini["LeaderboardRankingTitleColor"];
            LeaderboardRankingTitleColor = ConfigHelper.ReadColor(LeaderboardRankingTitleColor, leaderboardRankingTitleColor);

            var leaderboardDropdownColor = ini["LeaderboardDropdownColor"];
            LeaderboardDropdownColor = ConfigHelper.ReadColor(LeaderboardDropdownColor, leaderboardDropdownColor);

            var gameModeTextColor = ini["GameModeTextColor"];
            GameModeTextColor = ConfigHelper.ReadColor(GameModeTextColor, gameModeTextColor);

            var personalBestTitleColor = ini["PersonalBestTitleColor"];
            PersonalBestTitleColor = ConfigHelper.ReadColor(PersonalBestTitleColor, personalBestTitleColor);

            var noPersonalBestColor = ini["NoPersonalBestColor"];
            NoPersonalBestColor = ConfigHelper.ReadColor(NoPersonalBestColor, noPersonalBestColor);

            var leaderboardStatusTextColor = ini["LeaderboardStatusTextColor"];
            LeaderboardStatusTextColor = ConfigHelper.ReadColor(LeaderboardStatusTextColor, leaderboardStatusTextColor);

            var personalBestTrophyColor = ini["PersonalBestTrophyColor"];
            PersonalBestTrophyColor = ConfigHelper.ReadColor(PersonalBestTrophyColor, personalBestTrophyColor);

            var personalBestRankColor = ini["PersonalBestRankColor"];
            PersonalBestRankColor = ConfigHelper.ReadColor(PersonalBestRankColor, personalBestRankColor);

            var personalBestOfRankColor = ini["PersonalBestOfRankColor"];
            PersonalBestOfRankColor = ConfigHelper.ReadColor(PersonalBestOfRankColor, personalBestOfRankColor);

            var personalBestHeaderLeftColor = ini["PersonalBestHeaderLeftColor"];
            PersonalBestHeaderLeftColor = ConfigHelper.ReadColor(PersonalBestHeaderLeftColor, personalBestHeaderLeftColor);

            var personalBestHeaderRightColor = ini["PersonalBestHeaderRightColor"];
            PersonalBestHeaderRightColor = ConfigHelper.ReadColor(PersonalBestHeaderRightColor, personalBestHeaderRightColor);

            var modifiersRankedHeaderLeftColor = ini["ModifiersRankedHeaderLeftColor"];
            ModifiersRankedHeaderLeftColor = ConfigHelper.ReadColor(ModifiersRankedHeaderLeftColor, modifiersRankedHeaderLeftColor);

            var modifiersRankedHeaderRightColor = ini["ModifiersRankedHeaderRightColor"];
            ModifiersRankedHeaderRightColor = ConfigHelper.ReadColor(ModifiersRankedHeaderRightColor, modifiersRankedHeaderRightColor);

            var modifiersRankedLeftTextColor = ini["ModifiersRankedLeftTextColor"];
            ModifiersRankedLeftTextColor = ConfigHelper.ReadColor(ModifiersRankedLeftTextColor, modifiersRankedLeftTextColor);

            var modifiersRankedRightTextColor = ini["ModifiersRankedRightTextColor"];
            ModifiersRankedRightTextColor = ConfigHelper.ReadColor(ModifiersRankedRightTextColor, modifiersRankedRightTextColor);

            var modifiersUnrankedHeaderLeftColor = ini["ModifiersUnrankedHeaderLeftColor"];
            ModifiersUnrankedHeaderLeftColor = ConfigHelper.ReadColor(ModifiersUnrankedHeaderLeftColor, modifiersUnrankedHeaderLeftColor);

            var modifiersUnrankedHeaderRightColor = ini["ModifiersUnrankedHeaderRightColor"];
            ModifiersUnrankedHeaderRightColor = ConfigHelper.ReadColor(ModifiersUnrankedHeaderRightColor, modifiersUnrankedHeaderRightColor);

            var modifiersUnrankedLeftTextColor = ini["ModifiersUnrankedLeftTextColor"];
            ModifiersUnrankedLeftTextColor = ConfigHelper.ReadColor(ModifiersUnrankedLeftTextColor, modifiersUnrankedLeftTextColor);

            var modifiersUnrankedRightTextColor = ini["ModifiersUnrankedRightTextColor"];
            ModifiersUnrankedRightTextColor = ConfigHelper.ReadColor(ModifiersUnrankedRightTextColor, modifiersUnrankedRightTextColor);

            var gameMode1KColor = ini["GameMode1KColor"];
            GameMode1KColor = ConfigHelper.ReadColor(GameMode1KColor, gameMode1KColor);

            var gameMode2KColor = ini["GameMode2KColor"];
            GameMode2KColor = ConfigHelper.ReadColor(GameMode2KColor, gameMode2KColor);

            var gameMode3KColor = ini["GameMode3KColor"];
            GameMode3KColor = ConfigHelper.ReadColor(GameMode3KColor, gameMode3KColor);

            var gameMode4KColor = ini["GameMode4KColor"];
            GameMode4KColor = ConfigHelper.ReadColor(GameMode4KColor, gameMode4KColor);

            var gameMode5KColor = ini["GameMode5KColor"];
            GameMode5KColor = ConfigHelper.ReadColor(GameMode5KColor, gameMode5KColor);

            var gameMode6KColor = ini["GameMode6KColor"];
            GameMode6KColor = ConfigHelper.ReadColor(GameMode6KColor, gameMode6KColor);

            var gameMode7KColor = ini["GameMode7KColor"];
            GameMode7KColor = ConfigHelper.ReadColor(GameMode7KColor, gameMode7KColor);

            var gameMode8KColor = ini["GameMode8KColor"];
            GameMode8KColor = ConfigHelper.ReadColor(GameMode8KColor, gameMode8KColor);

            var gameMode9KColor = ini["GameMode9KColor"];
            GameMode9KColor = ConfigHelper.ReadColor(GameMode9KColor, gameMode9KColor);

            var gameMode10KColor = ini["GameMode10KColor"];
            GameMode10KColor = ConfigHelper.ReadColor(GameMode10KColor, gameMode10KColor);

            // Margins
            var mapsetPanelMarginLeft = ini["MapsetPanelMarginLeft"];
            MapsetPanelMarginLeft = ConfigHelper.ReadInt32(MapsetPanelMarginLeft, mapsetPanelMarginLeft);

            var mapsetPanelMarginRight = ini["MapsetPanelMarginRight"];
            MapsetPanelMarginRight = ConfigHelper.ReadInt32(MapsetPanelMarginRight, mapsetPanelMarginRight);

            var difficultyPanelMarginLeft = ini["DifficultyPanelMarginLeft"];
            ReadIndividualConfig(difficultyPanelMarginLeft, () => DifficultyPanelMarginLeft = ConfigHelper.ReadInt32(0, difficultyPanelMarginLeft));

            var difficultyPanelMarginRight = ini["DifficultyPanelMarginRight"];
            ReadIndividualConfig(difficultyPanelMarginRight, () => DifficultyPanelMarginRight = ConfigHelper.ReadInt32(0, difficultyPanelMarginRight));

            var playlistPanelMarginLeft = ini["PlaylistPanelMarginLeft"];
            ReadIndividualConfig(playlistPanelMarginLeft, () => PlaylistPanelMarginLeft = ConfigHelper.ReadInt32(0, playlistPanelMarginLeft));

            var playlistPanelMarginRight = ini["PlaylistPanelMarginRight"];
            ReadIndividualConfig(playlistPanelMarginRight, () => PlaylistPanelMarginRight = ConfigHelper.ReadInt32(0, playlistPanelMarginRight));

            // BPM and Length overlay offsets (positive = inward from edge)
            var mapsetBpmOverlayOffsetX = ini["MapsetBpmOverlayOffsetX"];
            MapsetBpmOverlayOffsetX = ConfigHelper.ReadFloat(MapsetBpmOverlayOffsetX, mapsetBpmOverlayOffsetX);

            var mapsetBpmOverlayOffsetY = ini["MapsetBpmOverlayOffsetY"];
            MapsetBpmOverlayOffsetY = ConfigHelper.ReadFloat(MapsetBpmOverlayOffsetY, mapsetBpmOverlayOffsetY);

            var mapsetLengthOverlayOffsetX = ini["MapsetLengthOverlayOffsetX"];
            MapsetLengthOverlayOffsetX = ConfigHelper.ReadFloat(MapsetLengthOverlayOffsetX, mapsetLengthOverlayOffsetX);

            var mapsetLengthOverlayOffsetY = ini["MapsetLengthOverlayOffsetY"];
            MapsetLengthOverlayOffsetY = ConfigHelper.ReadFloat(MapsetLengthOverlayOffsetY, mapsetLengthOverlayOffsetY);

            // Mapset info background color for BPM/Length overlays (default: 26, 31, 36, 255)
            var mapsetBpmOverlayColor = ini["MapsetBpmOverlayColor"];
            MapsetBpmOverlayColor = ConfigHelper.ReadColor(MapsetBpmOverlayColor, mapsetBpmOverlayColor);

            var mapsetLengthOverlayColor = ini["MapsetLengthOverlayColor"];
            MapsetLengthOverlayColor = ConfigHelper.ReadColor(MapsetLengthOverlayColor, mapsetLengthOverlayColor);

            var difficultyOverlayColor = ini["DifficultyOverlayColor"];
            DifficultyOverlayColor = ConfigHelper.ReadColor(DifficultyOverlayColor, difficultyOverlayColor);

            var mapsetBpmTextColor = ini["MapsetBpmTextColor"];
            MapsetBpmTextColor = ConfigHelper.ReadColor(MapsetBpmTextColor, mapsetBpmTextColor);

            var mapsetLengthTextColor = ini["MapsetLengthTextColor"];
            MapsetLengthTextColor = ConfigHelper.ReadColor(MapsetLengthTextColor, mapsetLengthTextColor);

            var difficultyInfoIconsColor = ini["DifficultyInfoIconsColor"];
            DifficultyInfoIconsColor = ConfigHelper.ReadColor(DifficultyInfoIconsColor, difficultyInfoIconsColor);

            var difficultyInfoValuesColor = ini["DifficultyInfoValuesColor"];
            DifficultyInfoValuesColor = ConfigHelper.ReadColor(DifficultyInfoValuesColor, difficultyInfoValuesColor);

            var difficultyPanelSource = ini["DifficultyPanelSource"];
            ReadIndividualConfig(difficultyPanelSource, () => DifficultyPanelSource = ConfigHelper.ReadString("mapset", difficultyPanelSource));



            var modifiersSliderBackgroundColor = ini["ModifiersSliderBackgroundColor"];
            ModifiersSliderBackgroundColor = ConfigHelper.ReadColor(ModifiersSliderBackgroundColor, modifiersSliderBackgroundColor);

            var modifiersSliderThumbColor = ini["ModifiersSliderThumbColor"];
            ModifiersSliderThumbColor = ConfigHelper.ReadColor(ModifiersSliderThumbColor, modifiersSliderThumbColor);
        }

        protected override void LoadElements()
        {
            const string folder = "SongSelect";

            MapsetSelected = LoadSkinElement(folder, "mapset-selected.png");
            MapsetDeselected = LoadSkinElement(folder, "mapset-deselected.png");
            MapsetHovered = LoadSkinElement(folder, "mapset-hovered.png");
            PlaylistDeselected = LoadSkinElement(folder, "playlist-deselected.png") ?? UserInterface.PlaylistDeselected;
            PlaylistHovered = LoadSkinElement(folder, "playlist-hovered.png") ?? UserInterface.PlaylistHovered;
            PlaylistOtherGameIcon = LoadSkinElement(folder, "playlist-othergame-icon.png") ?? UserInterface.PlaylistOtherGameIcon;
            GameMode4K = LoadSkinElement(folder, "game-mode-4k.png");
            GameMode7K = LoadSkinElement(folder, "game-mode-7k.png");
            GameMode4K7K = LoadSkinElement(folder, "game-mode-4k7k.png");
            GameModeNone = LoadSkinElement(folder, "game-mode-none.png");
            GameModeMask = LoadSkinElement(folder, "game-mode-mask.png");
            GameMode1K = LoadSkinElement(folder, "game-mode-1k.png");
            GameMode2K = LoadSkinElement(folder, "game-mode-2k.png");
            GameMode3K = LoadSkinElement(folder, "game-mode-3k.png");
            GameMode5K = LoadSkinElement(folder, "game-mode-5k.png");
            GameMode6K = LoadSkinElement(folder, "game-mode-6k.png");
            GameMode8K = LoadSkinElement(folder, "game-mode-8k.png");
            GameMode9K = LoadSkinElement(folder, "game-mode-9k.png");
            GameMode10K = LoadSkinElement(folder, "game-mode-10k.png");
            GameModeMixed = LoadSkinElement(folder, "game-mode-mixed.png");
            StatusNotSubmitted = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-notsubmitted", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-notsubmitted.png");
            StatusUnranked = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-unranked", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-unranked.png");
            StatusRanked = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-ranked", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-ranked.png");
            StatusOsu = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-osu", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-osu.png");
            StatusStepmania = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-sm", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-sm.png");
            StatusVarious = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-various", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-various.png");
            StatusNone = Store.LoadSingleTexture($"{Store.Dir}/{folder}/status-none", "Quaver.Resources/Textures/Skins/Shared/SongSelect/status-none.png");
            LeaderboardPanel = LoadSkinElement(folder, "leaderboard-panel.png");
            PersonalBestPanel = LoadSkinElement(folder, "personalbest-panel.png");
            LeaderboardScoreMask = LoadSkinElement(folder, "leaderboard-score-mask.png") ?? UserInterface.LeaderboardScoreMask;
            LeaderboardAvatarMask = LoadSkinElement(folder, "leaderboard-avatar-mask.png") ?? UserInterface.LeaderboardAvatarMask;
            LeaderboardWarning = LoadSkinElement(folder, "leaderboard-warning.png") ?? UserInterface.LeaderboardWarning;
            LeaderboardInfo = LoadSkinElement(folder, "leaderboard-info.png") ?? UserInterface.LeaderboardInfo;
            SelectFilterPanelRight = LoadSkinElement(folder, "select-filter-panel-right.png") ?? UserInterface.FilterPanelRight;
            SelectFilterPanelLeft = LoadSkinElement(folder, "select-filter-panel-left.png");
            MapsetBannerMask = LoadSkinElement(folder, "mapset-banner-mask.png");
            PlaylistBannerMask = LoadSkinElement(folder, "playlist-banner-mask.png");
            MapsetBpmBackground = LoadSkinElement(folder, "mapset-bpm-background.png");
            MapsetLengthBackground = LoadSkinElement(folder, "mapset-length-background.png");
            DifficultySelected = LoadSkinElement(folder, "difficulty-selected.png");
            DifficultyDeselected = LoadSkinElement(folder, "difficulty-deselected.png");
            DifficultyHovered = LoadSkinElement(folder, "difficulty-hovered.png");
            ModifierSelectorBackground = LoadSkinElement(folder, "modifier-selector-panel.png");
            ModifierBackground = LoadSkinElement(folder, "modifier-background.png") ?? UserInterface.ModifierBackground;
            ModifierBackgroundHovered = LoadSkinElement(folder, "modifier-background-hovered.png") ?? UserInterface.ModifierBackgroundHovered;
            ModifierSliderElement = LoadSkinElement(folder, "modifier-slider-element.png") ?? UserInterface.ModifierSliderElement;
            TabsPanel = LoadSkinElement(folder, "LeftPanel/tabs-panel.png") ?? UserInterface.TabsPanel;
        }
    }
}
