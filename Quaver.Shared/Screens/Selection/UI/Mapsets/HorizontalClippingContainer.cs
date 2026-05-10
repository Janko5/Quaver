using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Wobble;
using Wobble.Graphics;
using Wobble.Window;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    /// <summary>
    ///     A container that clips its children to its Size using ScissorRectangle.
    /// </summary>
    public class HorizontalClippingContainer : Container
    {
        private static readonly RasterizerState ScissorRasterizer = new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None
        };

        public HorizontalClippingContainer()
        {
            UsePreviousSpriteBatchOptions = true;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Size.X.Value <= 0 || Size.Y.Value <= 0)
                return;

            var game = GameBase.Game;
            if (game?.GraphicsDevice == null)
                return;

            var screenRect = ScreenRectangle;

            var scissorRect = new Rectangle(
                (int)(screenRect.X * WindowManager.ScreenScale.X),
                (int)(screenRect.Y * WindowManager.ScreenScale.Y),
                (int)(Size.X.Value * WindowManager.ScreenScale.X),
                (int)(Size.Y.Value * WindowManager.ScreenScale.Y)
            );

            var viewport = game.GraphicsDevice.Viewport;
            scissorRect.X = MathHelper.Clamp(scissorRect.X, 0, viewport.Width);
            scissorRect.Y = MathHelper.Clamp(scissorRect.Y, 0, viewport.Height);
            scissorRect.Width = MathHelper.Clamp(scissorRect.Width, 0, viewport.Width - scissorRect.X);
            scissorRect.Height = MathHelper.Clamp(scissorRect.Height, 0, viewport.Height - scissorRect.Y);

            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            var spriteBatch = GameBase.Game.SpriteBatch;
            var oldScissorRect = game.GraphicsDevice.ScissorRectangle;
            var oldRasterizerState = game.GraphicsDevice.RasterizerState;

            // Always intersect with parent's scissor rect for proper nested clipping
            scissorRect = Rectangle.Intersect(scissorRect, oldScissorRect);


            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                return;

            try
            {
                spriteBatch.End();
            }
            catch
            {
                // Ignored
            }

            game.GraphicsDevice.ScissorRectangle = scissorRect;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                ScissorRasterizer,
                null,
                WindowManager.Scale);

            foreach (var child in Children)
            {
                if (child.Visible)
                    child.Draw(gameTime);
            }

            spriteBatch.End();

            game.GraphicsDevice.ScissorRectangle = oldScissorRect;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.LinearClamp,
                null,
                oldRasterizerState,
                null,
                WindowManager.Scale);
        }

        public override void DrawToSpriteBatch()
        {
        }
    }
}
