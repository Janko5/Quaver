using System.Linq;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Scores;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.Leaderboard.Components
{
    public class DrawableLeaderboardScore : PoolableSprite<Score>
    {
        /// <summary>
        ///     Whether the V2 layout is active
        /// </summary>
        private static bool IsV2 => (SkinManager.Skin?.UserInterfaceVersion ?? 1.0f) == 2.0f;

        private static int BaseScoreHeight => IsV2 ? 70 : 66;

        public static int ScoreHeight => BaseScoreHeight + SkinManager.Skin.SongSelect.LeaderboardScoresGap;

        /// <summary>
        ///     The width of a score row. 725 (750 if empty PB/no scrollbar) for V2, 560 for V1.
        /// </summary>
        public int GetScoreWidth()
        {
            if (!IsV2)
                return 560;

            if (Container is LeaderboardScoresContainer s)
                return s.RequiresScroll ? 680 : 705;

            return 705;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override int HEIGHT => IsPersonalBest ? BaseScoreHeight : ScoreHeight;

        /// <summary>
        ///     If the score is a personal best score
        /// </summary>
        public bool IsPersonalBest { get; }

        /// <summary>
        ///     The child score container
        /// </summary>
        public DrawableLeaderboardScoreContainer ChildContainer { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <param name="isPersonalBest"></param>
        public DrawableLeaderboardScore(PoolableScrollContainer<Score> container, Score item, int index, bool isPersonalBest) : base(container, item, index)
        {
            IsPersonalBest = isPersonalBest;
            Size = new ScalableVector2(GetScoreWidth(), HEIGHT);
            Alpha = 0;
            Tint = Color.Transparent;

            ChildContainer = new DrawableLeaderboardScoreContainer(this)
            {
                Parent = this,
                UsePreviousSpriteBatchOptions = true
            };

            ModManager.ModsChanged += OnModsChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            ModManager.ModsChanged -= OnModsChanged;

            base.Destroy();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public override void UpdateContent(Score item, int index)
        {
            Item = item;
            Index = index;
            Size = new ScalableVector2(GetScoreWidth(), HEIGHT);

            ChildContainer.UpdateContent(this);
        }

        private void OnModsChanged(object? sender, ModsChangedEventArgs e) => UpdateContent(Item, Index);
    }
}
