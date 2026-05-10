using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;

namespace Quaver.Shared.Skinning.Menus
{
    public class SkinMusicVisualizer : SkinMenu
    {
        public int MusicVisualizerType { get; private set; } = 3;

        public Color MusicVisualizerColor { get; private set; } = new Color(68, 108, 151, 255);

        public SkinMusicVisualizer(SkinStore store, IniData config) : base(store, config)
        {
        }

        protected override void ReadConfig()
        {
            var ini = Config["MusicVisualizer"];

            var type = ini["MusicVisualizerType"];
            MusicVisualizerType = ConfigHelper.ReadInt32(MusicVisualizerType, type);

            var color = ini["MusicVisualizerColor"];
            MusicVisualizerColor = ConfigHelper.ReadColor(MusicVisualizerColor, color);
        }

        protected override void LoadElements()
        {
        }
    }
}
