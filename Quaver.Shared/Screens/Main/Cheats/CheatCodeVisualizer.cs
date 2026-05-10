using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Helpers.Input;
using Quaver.Shared.Screens.Visualizer;
using Wobble;

namespace Quaver.Shared.Screens.Main.Cheats
{
    /// <summary>
    ///     Cheat code to access the visualizer test screen.
    ///     Type "visualizer~" to activate.
    /// </summary>
    public class CheatCodeVisualizer : CheatCode
    {
        public override Keys[] Combination { get; } = new[]
        {
            Keys.V,
            Keys.I,
            Keys.S,
            Keys.U,
            Keys.A,
            Keys.L,
            Keys.I,
            Keys.Z,
            Keys.E,
            Keys.R,
            Keys.OemTilde
        };

        protected override void OnActivated()
        {
            var game = (QuaverGame)GameBase.Game;
            game.CurrentScreen.Exit(() => new VisualizerTestScreen());
        }
    }
}
