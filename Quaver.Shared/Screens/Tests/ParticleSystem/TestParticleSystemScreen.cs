using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Objects;
using Quaver.Shared.Screens.Main;
using Wobble.Input;
using Wobble.Screens;

namespace Quaver.Shared.Screens.Tests.ParticleSystem
{
    public sealed class TestParticleSystemScreen : QuaverScreen
    {
        /// <inheritdoc />
        public override QuaverScreenType Type { get; } = QuaverScreenType.ParticleTest;

        public TestParticleSystemScreen() => View = new TestParticleSystemScreenView(this);

        /// <inheritdoc />
        public override void OnFirstUpdate()
        {
            base.OnFirstUpdate();
        }

        /// <inheritdoc />
        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            HandleInput();
            base.Update(gameTime);
        }

        /// <summary>
        ///     Handles input for returning to main menu.
        /// </summary>
        private void HandleInput()
        {
            if (Exiting)
                return;

            if (KeyboardManager.IsUniqueKeyPress(Keys.Escape))
            {
                Exit(() => new MainMenuScreen());
            }
        }

        /// <inheritdoc />
        public override UserClientStatus GetClientStatus()
            => new UserClientStatus(ClientStatus.InMenus, -1, "-1", 1, "", 0);
    }
}
