using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Helpers.Input;
using Quaver.Shared.Screens.Tests.ParticleSystem;
using Wobble;

namespace Quaver.Shared.Screens.Main.Cheats
{
    /// <summary>
    ///     Cheat code to access the particle system test screen.
    ///     Type "particle~" to activate.
    /// </summary>
    public class CheatCodeParticle : CheatCode
    {
        public override Keys[] Combination { get; } = new[]
        {
            Keys.P,
            Keys.A,
            Keys.R,
            Keys.T,
            Keys.I,
            Keys.C,
            Keys.L,
            Keys.E,
            Keys.OemTilde
        };

        protected override void OnActivated()
        {
            var game = (QuaverGame)GameBase.Game;
            game.CurrentScreen.Exit(() => new TestParticleSystemScreen());
        }
    }
}
