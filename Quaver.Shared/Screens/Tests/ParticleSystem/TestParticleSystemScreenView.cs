using Microsoft.Xna.Framework;
using Quaver.Shared.Graphics.Backgrounds;
using Wobble;
using Wobble.Graphics;
using Wobble.Screens;

namespace Quaver.Shared.Screens.Tests.ParticleSystem
{
    public class TestParticleSystemScreenView : ScreenView
    {
        /// <summary>
        ///     The particle system being tested.
        /// </summary>
        private BackgroundParticleSystem ParticleSystem { get; }

        /// <summary>
        ///     Dark navy background color: RGB(8, 10, 18)
        /// </summary>
        private static readonly Color BackgroundColor = new Color(8, 10, 18);

        public TestParticleSystemScreenView(TestParticleSystemScreen screen) : base(screen)
        {
            // Create particle system with 300 particles
            ParticleSystem = new BackgroundParticleSystem(300)
            {
                Parent = Container
            };
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            Container?.Update(gameTime);
        }

        /// <inheritdoc />
        public override void Draw(GameTime gameTime)
        {
            // Clear to dark navy background
            GameBase.Game.GraphicsDevice.Clear(BackgroundColor);
            Container?.Draw(gameTime);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            Container?.Destroy();
        }
    }
}
