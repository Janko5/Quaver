using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using System;

namespace Quaver.Shared.Helpers
{
    public static class AvatarMaskingHelper
    {
        private static readonly BlendState MaskBlendState = new BlendState
        {
            AlphaSourceBlend = Blend.DestinationAlpha,
            AlphaBlendFunction = BlendFunction.Subtract,
            AlphaDestinationBlend = Blend.InverseDestinationAlpha
        };

        /// <summary>
        ///     Performs a blend of a source texture and a mask, returning a new RenderTarget2D.
        ///     The caller is responsible for the lifetime of the returned texture.
        /// </summary>
        public static Texture2D PerformBlend(Texture2D srcTexture, Texture2D srcMask)
        {
            if (srcTexture == null || srcMask == null)
                return null;

            var renderTarget = new RenderTarget2D(GameBase.Game.GraphicsDevice, srcTexture.Width, srcTexture.Height, false,
                GameBase.Game.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

            GameBase.Game.GraphicsDevice.SetRenderTarget(renderTarget);

            // Attempt to end the spritebatch
            _ = GameBase.Game.TryEndBatch();

            GameBase.Game.SpriteBatch.Begin(blendState: MaskBlendState);
            GameBase.Game.SpriteBatch.Draw(srcMask, srcTexture.Bounds, Color.White);
            GameBase.Game.SpriteBatch.Draw(srcTexture, srcTexture.Bounds, Color.White);
            GameBase.Game.SpriteBatch.End();

            GameBase.Game.GraphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }

        /// <summary>
        ///     Performs a blend of a background texture into a mask. The background is scaled uniformly to fit 
        ///     the mask's area (cover) and is centered.
        ///     The caller is responsible for the lifetime of the returned texture.
        /// </summary>
        public static Texture2D PerformBackgroundBlend(Texture2D srcTexture, Texture2D srcMask)
        {
            if (srcTexture == null || srcMask == null)
                return null;

            var renderTarget = new RenderTarget2D(GameBase.Game.GraphicsDevice, srcMask.Width, srcMask.Height, false,
                GameBase.Game.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

            GameBase.Game.GraphicsDevice.SetRenderTarget(renderTarget);

            // Attempt to end the spritebatch
            _ = GameBase.Game.TryEndBatch();

            GameBase.Game.SpriteBatch.Begin(blendState: MaskBlendState);

            // Draw the mask first to set the alpha
            GameBase.Game.SpriteBatch.Draw(srcMask, srcMask.Bounds, Color.White);

            // Calculate scale to fill (cover) the mask area
            float scale = MathHelper.Max((float)srcMask.Width / srcTexture.Width, (float)srcMask.Height / srcTexture.Height);
            int width = (int)(srcTexture.Width * scale);
            int height = (int)(srcTexture.Height * scale);
            int xOffset = (srcMask.Width - width) / 2;
            int yOffset = (srcMask.Height - height) / 2;

            // Draw the texture centered and scaled
            GameBase.Game.SpriteBatch.Draw(srcTexture, new Rectangle(xOffset, yOffset, width, height), Color.White);

            GameBase.Game.SpriteBatch.End();

            GameBase.Game.GraphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }
    }
}
