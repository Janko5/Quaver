using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Wobble;
using Wobble.Graphics;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    /// <summary>
    ///     A container that clips its children to its bounds using ScissorRectangle.
    ///     Used for the "reveal" animation when expanding difficulty lists.
    /// </summary>
    public class ClippingContainer : Container
    {
        /// <summary>
        ///     Cached RasterizerState for scissor clipping to avoid per-frame allocations.
        /// </summary>
        private static readonly RasterizerState ScissorRasterizer = new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None
        };

        /// <summary>
        ///     The current animated height used for clipping.
        ///     This controls how much of the content is visible.
        /// </summary>
        public float ClipHeight { get; set; }

        /// <summary>
        ///     The full height of the content (used for layout calculation).
        /// </summary>
        public float FullContentHeight { get; set; }

        public ClippingContainer()
        {
            UsePreviousSpriteBatchOptions = true;
        }

        /// <summary>
        ///     If true, clipping is enabled. If false, draws children normally.
        /// </summary>
        public bool EnableClipping { get; set; } = true;

        /// <inheritdoc />
        /// <summary>
        ///     Override Draw to apply scissor clipping to all children.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            // If clipping is disabled, draw normally
            if (!EnableClipping)
            {
                base.Draw(gameTime);
                return;
            }

            // If ClipHeight is 0 or less, don't draw anything
            if (ClipHeight <= 0)
                return;

            var game = GameBase.Game;
            if (game?.GraphicsDevice == null)
                return;

            // Calculate the screen rectangle for scissoring
            var screenRect = ScreenRectangle;

            // Create a scissor rectangle based on the ClipHeight
            // ClipHeight controls how much is visible from the TOP
            // All coordinates must be scaled to actual screen pixels
            var scissorRect = new Rectangle(
                (int)(screenRect.X * WindowManager.ScreenScale.X),
                (int)(screenRect.Y * WindowManager.ScreenScale.Y),
                (int)(screenRect.Width * WindowManager.ScreenScale.X),
                (int)(ClipHeight * WindowManager.ScreenScale.Y) // Scale ClipHeight to actual screen pixels
            );

            // Clamp to screen bounds
            var viewport = game.GraphicsDevice.Viewport;
            scissorRect.X = MathHelper.Clamp(scissorRect.X, 0, viewport.Width);
            scissorRect.Y = MathHelper.Clamp(scissorRect.Y, 0, viewport.Height);
            scissorRect.Width = MathHelper.Clamp(scissorRect.Width, 0, viewport.Width - scissorRect.X);
            scissorRect.Height = MathHelper.Clamp(scissorRect.Height, 0, viewport.Height - scissorRect.Y);

            // Ensure minimum size
            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            // Store old scissor rectangle and rasterizer state
            var spriteBatch = GameBase.Game.SpriteBatch;
            var oldScissorRect = game.GraphicsDevice.ScissorRectangle;
            var oldRasterizerState = game.GraphicsDevice.RasterizerState;

            // CRITICAL FIX: Intersect our desired clip rect with the existing parent scissor rect (from ScrollContainer)
            // This prevents the expanded list from drawing outside the MapsetScrollContainer bounds
            if (oldRasterizerState.ScissorTestEnable)
            {
                scissorRect = Rectangle.Intersect(scissorRect, oldScissorRect);
            }

            // Ensure minimum size after intersection
            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            // End current batch to change state
            try
            {
                spriteBatch.End();
            }
            catch
            {
                // SpriteBatch.End() may throw if not currently active.
                // This is expected during certain initialization sequences.
            }

            // Set up scissor test using cached RasterizerState
            game.GraphicsDevice.ScissorRectangle = scissorRect;

            // Begin new batch with scissor enabled
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                ScissorRasterizer,
                null,
                WindowManager.Scale);

            // Draw all children (this is where the clipping happens)
            foreach (var child in Children)
            {
                if (child.Visible)
                    child.Draw(gameTime);
            }

            // End scissored batch
            spriteBatch.End();

            // Restore previous state
            game.GraphicsDevice.ScissorRectangle = oldScissorRect;

            // Restart spritebatch with previously active rasterizer state
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                oldRasterizerState,
                null,
                WindowManager.Scale);
        }

        /// <inheritdoc />
        /// <summary>
        ///     No-op since we handle everything in Draw().
        /// </summary>
        public override void DrawToSpriteBatch()
        {
            // Do nothing - all rendering is handled in Draw()
        }
    }
}
