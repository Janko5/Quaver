using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Skinning;
using Quaver.Shared.Online;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Wobble.Scheduling;
using Quaver.Shared.Modifiers;
using System.Linq;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Components
{
    public class LeaderboardPersonalBestScore : Sprite, ILoadable, IFetchedScoreHandler
    {
        /// <summary>
        ///     The parent leaderboard container
        /// </summary>
        private LeaderboardContainer Container { get; }

        /// <summary>
        /// </summary>
        private LoadingWheel? LoadingWheel { get; set; }

        /// <summary>
        ///     Text that is displayed when there is no personal best.
        /// </summary>
        private SpriteTextPlus? NoPersonalBestScore { get; set; }

        /// <summary>
        ///     The mask for the score. (V2)
        /// </summary>
        public Sprite? Mask { get; private set; }

        /// <summary>
        ///     The displayed score
        /// </summary>
        private DrawableLeaderboardScore? Score { get; set; }

        /// <summary>
        ///    The background color for the PB row. Matches index 1 (Even) used in DrawableLeaderboardScore.
        /// </summary>
        private Color BackgroundColor => SkinManager.Skin.SongSelect.LeaderboardScoreColorEven;

        /// <summary>
        ///     Header container for the personal best score.
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
        private Sprite? Trophy { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? TopCountText { get; set; }

        /// <summary>
        ///     Secondary header container for the personal best score.
        /// </summary>
        private Sprite? SecondaryHeaderContainer { get; set; }

        /// <summary>
        /// </summary>
        private NineSliceSprite? SecondaryHeaderLeft { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? SecondaryHeaderLeftText { get; set; }

        /// <summary>
        /// </summary>
        private NineSliceSprite? SecondaryHeaderRight { get; set; }

        /// <summary>
        /// </summary>
        private DrawableModifier? SecondaryHeaderRightSpeedModifier { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? SecondaryHeaderRightRatingText { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus? SecondaryHeaderRightSeparator { get; set; }

        /// <summary>
        ///     Header margins for the nine slice sprites.
        /// </summary>
        private static SliceMargins HeaderMargins { get; } = new SliceMargins(20, 20, 20, 20);

        /// <summary>
        ///     The height for the headers.
        /// </summary>
        private const float HeaderHeight = 40f;

        /// <summary>
        /// </summary>
        private SpriteTextPlus? SecondaryHeaderRightAccText { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        public LeaderboardPersonalBestScore(LeaderboardContainer container)
        {
            Container = container;
            Size = Container.IsV2 ? new ScalableVector2(725, 140) : new ScalableVector2(Container.Width, 70);

            Image = SkinManager.Skin?.SongSelect?.PersonalBestPanel ?? UserInterface.PersonalBestScorePanel;
            Alpha = 0;

            CreateNoPersonalBestScoreText();
            CreateHeader();
            CreateLoadingWheel();
        }


        /// <summary>
        ///     Creates the loading wheel for the screen
        /// </summary>
        private void CreateLoadingWheel()
        {
            LoadingWheel = new LoadingWheel
            {
                Parent = Mask ?? (Sprite)this,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(30, 30),
                Alpha = 0
            };
        }

        /// <summary>
        ///     Fades the wheel in to make it appear as if it is loading
        /// </summary>
        public void StartLoading()
        {
            UpdateVisibility(null, null, "", true);
        }

        /// <summary>
        ///     Calculates the target score and text for the secondary header on the personal best panel.
        /// </summary>
        private (Database.Scores.Score? targetScore, string targetText) GetTargetScore(System.Collections.Generic.List<Database.Scores.Score>? scores, Database.Scores.Score? pb, int topCount, bool offline)
        {
            if (offline || scores == null || scores.Count == 0 || pb == null || string.IsNullOrEmpty(pb.Name))
                return (null, "");

            var index = scores.FindIndex(x => x.Name == pb?.Name);
            var limit = topCount > 0 ? topCount : scores.Count;

            if (index == -1 || index >= limit) 
            {
                // Not on the board (or at/below the limit) -> show the barrier to entry
                return (scores.LastOrDefault(), $"Top #{scores.Count}");
            }
            
            if (index > 0)
            {
                // On the board, but not #1 -> show the immediate rival above
                return (scores[index - 1], $"Target #{index}");
            }
            
            // #1 -> show the lead over #2 if available
            if (scores.Count > 1) 
            {
                return (scores[1], "Vs #2");
            }
            
            return (null, "");
        }

        /// <summary>
        ///     Fades the wheel out to 0.
        /// </summary>
        public void StopLoading()
        {
            LoadingWheel?.Animations.RemoveAll(x => x.Properties != AnimationProperty.Rotation);
            LoadingWheel?.FadeTo(0, Easing.Linear, 250);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="store"></param>
        public void HandleFetchedScores(Map map, FetchedScoreStore store)
        {
            if (map?.Md5Checksum != MapManager.Selected.Value?.Md5Checksum)
                return;

            var isLocal = ConfigManager.LeaderboardSection.Value == LeaderboardType.Local;
            var rank = store.Scores?.FindIndex(x => x.Name == store.PersonalBest?.Name) ?? -1;
            var topCount = store.Scores?.Count ?? -1;
            var targetTuple = GetTargetScore(store.Scores, store.PersonalBest, topCount, isLocal);
            UpdateVisibility(store.PersonalBest, targetTuple.targetScore, targetTuple.targetText, false, isLocal, rank + 1, topCount);
        }

        private void UpdateVisibility(Database.Scores.Score? pb, Database.Scores.Score? targetScore, string targetText, bool loading, bool offline = false, int rank = -1, int topCount = -1)
        {
            ScheduleUpdate(() =>
            {
                // Show the background mask only when there is no score or we are loading.
                Mask?.FadeTo(pb == null || loading ? 1 : 0, Easing.Linear, 250);

                if (loading)
                {
                    HandleLoadingState();
                }
                else
                {
                    HandleLoadedState(pb, targetScore, targetText, offline, rank, topCount);
                }

                FadeTo(1, Easing.Linear, 250);
            });
        }

        private void HandleLoadingState()
        {
            LoadingWheel?.Animations.RemoveAll(x => x.Properties != AnimationProperty.Rotation);
            LoadingWheel?.FadeTo(1, Easing.Linear, 250);
            NoPersonalBestScore?.FadeTo(0, Easing.Linear, 250);
            HeaderContainer?.FadeTo(0, Easing.Linear, 250);

            if (SecondaryHeaderContainer != null)
                SecondaryHeaderContainer.Visible = false;

            Score?.Destroy();
            Score = null;
        }

        private void HandleLoadedState(Database.Scores.Score? pb, Database.Scores.Score? targetScore, string targetText, bool offline, int rank, int topCount)
        {
            StopLoading();

            if (pb == null)
            {
                NoPersonalBestScore?.ClearAnimations();
                NoPersonalBestScore?.FadeTo(1, Easing.Linear, 250);
                Score?.Destroy();
                Score = null;
            }
            else
            {
                NoPersonalBestScore?.FadeTo(0, Easing.Linear, 250);
            }

            if (HeaderRightText != null)
                HeaderRightText.Text = rank > 0 ? $"#{rank}" : "-";

            if (TopCountText != null)
                TopCountText.Text = $"of #{(topCount > 0 ? topCount : 50)}";

            var isUnified = rank <= 0 || offline || pb == null;
            var isSecondaryUnified = targetScore == null;

            UpdateSkinVisuals(isUnified, isSecondaryUnified);
            UpdateSecondaryHeaderContent(targetScore, targetText, isSecondaryUnified);

            if (HeaderRight != null) HeaderRight.Visible = !isUnified;
            if (HeaderRightText != null) HeaderRightText.Visible = !isUnified;
            if (Trophy != null) Trophy.Visible = !isUnified;
            if (TopCountText != null) TopCountText.Visible = !isUnified;

            UpdateHeaderWidth();
            HeaderContainer?.FadeTo(1, Easing.Linear, 250);

            if (SecondaryHeaderContainer != null)
            {
                SecondaryHeaderContainer.Visible = targetScore != null;
                if (targetScore != null)
                    SecondaryHeaderContainer.FadeTo(1, Easing.Linear, 250);
            }

            UpdatePersonalBestScoreRow(pb, rank);
        }

        private void UpdateSkinVisuals(bool isUnified, bool isSecondaryUnified)
        {
            if (HeaderLeft != null)
            {
                HeaderLeft.Image = isUnified
                    ? (SkinManager.Skin?.Universal?.Header ?? UserInterface.UniversalHeader)
                    : (SkinManager.Skin?.Universal?.HeaderLeft ?? UserInterface.UniversalHeaderLeft);

                HeaderLeft.Tint = isUnified
                    ? SkinManager.Skin.Universal.HeaderColor
                    : SkinManager.Skin.SongSelect.PersonalBestHeaderLeftColor;
            }

            if (SecondaryHeaderLeft != null)
            {
                SecondaryHeaderLeft.Image = isSecondaryUnified
                    ? (SkinManager.Skin?.Universal?.Header ?? UserInterface.UniversalHeader)
                    : (SkinManager.Skin?.Universal?.HeaderLeft ?? UserInterface.UniversalHeaderLeft);

                SecondaryHeaderLeft.Tint = isSecondaryUnified
                    ? SkinManager.Skin.Universal.HeaderColor
                    : SkinManager.Skin.SongSelect.PersonalBestHeaderLeftColor;
            }
        }

        private void UpdateSecondaryHeaderContent(Database.Scores.Score? targetScore, string targetText, bool isSecondaryUnified)
        {
            if (SecondaryHeaderRight != null) SecondaryHeaderRight.Visible = !isSecondaryUnified;
            if (SecondaryHeaderRightSpeedModifier != null) SecondaryHeaderRightSpeedModifier.Visible = !isSecondaryUnified;
            if (SecondaryHeaderRightRatingText != null) SecondaryHeaderRightRatingText.Visible = !isSecondaryUnified;
            if (SecondaryHeaderRightSeparator != null) SecondaryHeaderRightSeparator.Visible = !isSecondaryUnified;
            if (SecondaryHeaderRightAccText != null) SecondaryHeaderRightAccText.Visible = !isSecondaryUnified;

            if (targetScore == null)
                return;

            if (SecondaryHeaderLeftText != null) SecondaryHeaderLeftText.Text = targetText;
            if (SecondaryHeaderRightRatingText != null)
                SecondaryHeaderRightRatingText.Text = StringHelper.RatingToString(targetScore.PerformanceRating);

            if (SecondaryHeaderRightAccText != null)
            {
                var acc = (float)(ConfigManager.LeaderboardSection.Value == LeaderboardType.Local &&
                                  ConfigManager.LeaderboardRankedAccuracy.Value
                    ? targetScore.RankedAccuracy
                    : targetScore.Accuracy);
                SecondaryHeaderRightAccText.Text = StringHelper.AccuracyToString(acc);
            }

            if (SecondaryHeaderRightSpeedModifier != null)
            {
                var mods = ModManager.GetModsList((ModIdentifier)targetScore.Mods);
                var speedMod = mods.Find(x => ModManager.IdentifierToModifier(x)?.Find(m => m.Type == ModType.Speed) != null);
                
                if (speedMod != ModIdentifier.None && speedMod != 0)
                {
                    SecondaryHeaderRightSpeedModifier.Image = ModManager.GetTexture(speedMod);
                    SecondaryHeaderRightSpeedModifier.Visible = true;
                }
                else
                {
                    SecondaryHeaderRightSpeedModifier.Visible = false;
                }
            }
        }

        private void UpdatePersonalBestScoreRow(Database.Scores.Score? pb, int rank)
        {
            if (pb == null || (Score != null && Score.Item == pb))
                return;

            Score?.Destroy();
            int width = Container.ScoresContainer.RequiresScroll ? 680 : 705;

            Score = new DrawableLeaderboardScore(null!, pb, rank > 0 ? rank : 1, true)
            {
                Parent = this,
                Alignment = Mask == null ? Alignment.MidCenter : Alignment.BotCenter,
                Size = Mask == null ? new ScalableVector2(Container.Width, 70) : new ScalableVector2(width, 70),
                Y = Mask == null ? 0 : -10,
                Alpha = 0,
                UsePreviousSpriteBatchOptions = true
            };

            Score.FadeTo(1, Easing.Linear, 250);
            Score.ChildContainer.Size = Score.Size;
            Score.UpdateContent(pb, rank > 0 ? rank : 1);
        }

        /// <summary>
        /// </summary>
        private void CreateNoPersonalBestScoreText()
        {
            if (Container.IsV2)
            {
                int width = Container.ScoresContainer.RequiresScroll ? 680 : 705;

                Mask = new Sprite
                {
                    Parent = this,
                    Alignment = Alignment.BotCenter,
                    Y = -10,
                    Size = new ScalableVector2(width, 70),
                    Image = SkinManager.Skin?.SongSelect?.LeaderboardScoreMask ?? UserInterface.LeaderboardScoreMask,
                    Tint = BackgroundColor,
                    Alpha = 0,
                };
            }

            NoPersonalBestScore = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "No personal best score found.", 20)
            {
                Parent = Mask ?? (Sprite)this,
                Alignment = Alignment.MidCenter,
                Alpha = 0,
                Tint = SkinManager.Skin.SongSelect.NoPersonalBestColor
            };
        }



        /// <summary>
        ///     Creates the header for the personal best panel.
        /// </summary>
        private void CreateHeader()
        {
            if (!Container.IsV2 || Mask == null)
                return;

            CreatePrimaryHeader();
            CreateSecondaryHeader();
            UpdateHeaderWidth();
        }

        private void CreatePrimaryHeader()
        {
            HeaderContainer = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                X = 10,
                Y = 10,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = Color.Transparent,
                Alpha = 0
            };

            HeaderLeft = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderLeft ?? UserInterface.UniversalHeaderLeft, HeaderMargins)
            {
                Parent = HeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = SkinManager.Skin.SongSelect.PersonalBestHeaderLeftColor
            };

            HeaderLeftText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Personal Best", 22)
            {
                Parent = HeaderLeft,
                Alignment = Alignment.MidLeft,
                X = 10,
                Tint = SkinManager.Skin.SongSelect.PersonalBestTitleColor
            };

            HeaderRight = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderRight ?? UserInterface.UniversalHeaderRight, HeaderMargins)
            {
                Parent = HeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(40, HeaderHeight),
                Tint = SkinManager.Skin.SongSelect.PersonalBestHeaderRightColor
            };

            HeaderRightText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "#1", 22)
            {
                Parent = HeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.SongSelect.PersonalBestRankColor
            };

            Trophy = new Sprite
            {
                Parent = HeaderRight,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(22, 20),
                Image = UserInterface.SongSelectTrophy,
                Tint = SkinManager.Skin.SongSelect.PersonalBestTrophyColor
            };

            TopCountText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "of Top 100", 22)
            {
                Parent = HeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.SongSelect.PersonalBestOfRankColor
            };
        }

        private void CreateSecondaryHeader()
        {
            SecondaryHeaderContainer = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                X = -10,
                Y = 10,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = Color.Transparent,
                Alpha = 0
            };

            SecondaryHeaderLeft = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderLeft ?? UserInterface.UniversalHeaderLeft, HeaderMargins)
            {
                Parent = SecondaryHeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(100, HeaderHeight),
                Tint = SkinManager.Skin.SongSelect.PersonalBestHeaderLeftColor
            };

            SecondaryHeaderLeftText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "Top #50", 22)
            {
                Parent = SecondaryHeaderLeft,
                Alignment = Alignment.MidLeft,
                X = 10,
                Tint = SkinManager.Skin.SongSelect.PersonalBestTitleColor
            };

            SecondaryHeaderRight = new NineSliceSprite(SkinManager.Skin?.Universal?.HeaderRight ?? UserInterface.UniversalHeaderRight, HeaderMargins)
            {
                Parent = SecondaryHeaderContainer,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(40, HeaderHeight),
                Tint = SkinManager.Skin.SongSelect.PersonalBestHeaderRightColor
            };

            SecondaryHeaderRightSpeedModifier = new DrawableModifier(ModIdentifier.None)
            {
                Parent = SecondaryHeaderRight,
                Alignment = Alignment.MidLeft,
                X = 10,
                Size = new ScalableVector2(45, 18),
                Visible = false
            };

            SecondaryHeaderRightRatingText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00.00", 22)
            {
                Parent = SecondaryHeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreRatingColor
            };

            SecondaryHeaderRightSeparator = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "|", 22)
            {
                Parent = SecondaryHeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreAccuracyColor
            };

            SecondaryHeaderRightAccText = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), "00.00%", 22)
            {
                Parent = SecondaryHeaderRight,
                Alignment = Alignment.MidLeft,
                Tint = SkinManager.Skin.SongSelect.LeaderboardScoreAccuracyColor
            };
        }

        /// <summary>
        ///     Updates the width of the header components based on their text content.
        /// </summary>
        private void UpdateHeaderWidth()
        {
            if (HeaderLeft == null || HeaderLeftText == null || HeaderRight == null || HeaderRightText == null)
                return;

            HeaderLeft.Width = HeaderLeftText.Width + 20;

            if (HeaderRight.Visible)
            {
                HeaderRight.X = HeaderLeft.Width;

                if (Trophy != null && HeaderRightText != null && TopCountText != null)
                {
                    Trophy.X = 10;
                    HeaderRightText.X = Trophy.X + Trophy.Width + 10;
                    TopCountText.X = HeaderRightText.X + HeaderRightText.Width + 4;

                    HeaderRight.Width = TopCountText.X + TopCountText.Width + 10;
                }
            }

            if (HeaderContainer != null)
            {
                if (HeaderRight.Visible)
                    HeaderContainer.Width = HeaderLeft.Width + HeaderRight.Width;
                else
                    HeaderContainer.Width = HeaderLeft.Width;
            }

            if (SecondaryHeaderLeft != null && SecondaryHeaderLeftText != null && SecondaryHeaderRight != null &&
                SecondaryHeaderRightSpeedModifier != null && SecondaryHeaderRightRatingText != null &&
                SecondaryHeaderRightSeparator != null && SecondaryHeaderRightAccText != null)
            {
                SecondaryHeaderLeft.Width = SecondaryHeaderLeftText.Width + 20;

                if (SecondaryHeaderRight.Visible)
                {
                    SecondaryHeaderRight.X = SecondaryHeaderLeft.Width;
                    
                    float nextX = 10;
                    
                    if (SecondaryHeaderRightSpeedModifier.Visible)
                    {
                        SecondaryHeaderRightSpeedModifier.X = nextX;
                        nextX = SecondaryHeaderRightSpeedModifier.X + SecondaryHeaderRightSpeedModifier.Width + 5;
                    }
                    
                    SecondaryHeaderRightRatingText.X = nextX;
                    nextX = SecondaryHeaderRightRatingText.X + SecondaryHeaderRightRatingText.Width + 5;
                    
                    SecondaryHeaderRightSeparator.X = nextX;
                    nextX = SecondaryHeaderRightSeparator.X + SecondaryHeaderRightSeparator.Width + 5;
                    
                    SecondaryHeaderRightAccText.X = nextX;
                    
                    SecondaryHeaderRight.Width = SecondaryHeaderRightAccText.X + SecondaryHeaderRightAccText.Width + 10;
                }

                if (SecondaryHeaderContainer != null)
                {
                    if (SecondaryHeaderRight.Visible)
                        SecondaryHeaderContainer.Width = SecondaryHeaderLeft.Width + SecondaryHeaderRight.Width;
                    else
                        SecondaryHeaderContainer.Width = SecondaryHeaderLeft.Width;
                }
            }
        }
    }
}
