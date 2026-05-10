using IniFileParser.Model;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;

namespace Quaver.Shared.Skinning.Menus
{
    public class SkinMenuVolumeController : SkinMenu
    {
        /// <summary>
        ///     Background color of the volume controller panel
        /// </summary>
        public Color VolumeControllerBackgroundColor { get; private set; } = new Color(40,48,56,255);
        
        /// <summary>
        ///     Color of the volume controller header
        /// </summary>
        public Color VolumeControllerHeaderColor { get; private set; } = new Color(57,139,208,255);

        /// <summary>
        ///     Color of the left panel containing the icon in the switch row
        /// </summary>
        public Color VolumeLeftPanelWithIconColor { get; private set; } = new Color(57,139,208,255);

        /// <summary>
        ///     Color of the right panel spanning the main area in the switch row
        /// </summary>
        public Color VolumeRightPanelColor { get; private set; } = new Color(24,30,37,255);

        /// <summary>
        ///     Color of the icon when the switch row is NOT hovered
        /// </summary>
        public Color VolumeIconsColor { get; private set; } = new Color(255,255,255,255);

        /// <summary>
        ///     Color of the icon when the switch row IS hovered
        /// </summary>
        public Color VolumeHoverColor { get; private set; } = new Color(81,197,249,255);

        /// <summary>
        ///     Background color of the volume slider
        /// </summary>
        public Color VolumeSliderBackgroundColor { get; private set; } = new Color(40,48,56,255);

        /// <summary>
        ///     Active color of the volume slider
        /// </summary>
        public Color VolumeSliderActiveColor { get; private set; } = new Color(217,227,244,255);

        /// <summary>
        ///     Color of the titles in the volume controller
        /// </summary>
        public Color VolumeControllerTitlesColor { get; private set; } = Color.White;

        /// <summary>
        ///     Color of the percentages in the volume controller
        /// </summary>
        public Color VolumeControllerPercentsColor { get; private set; } = Color.White;

        /// <summary>
        /// </summary>
        /// <param name="store"></param>
        /// <param name="config"></param>
        public SkinMenuVolumeController(SkinStore store, IniData config) : base(store, config)
        {
        }

        /// <summary>
        /// </summary>
        protected override void ReadConfig()
        {
            var section = Config["VolumeController"];
            if (section == null)
                return;

            VolumeControllerBackgroundColor = ConfigHelper.ReadColor(VolumeControllerBackgroundColor, section["VolumeControllerBackgroundColor"]);
            VolumeControllerHeaderColor = ConfigHelper.ReadColor(VolumeControllerHeaderColor, section["VolumeControllerHeaderColor"]);
            VolumeLeftPanelWithIconColor = ConfigHelper.ReadColor(VolumeLeftPanelWithIconColor, section["VolumeLeftPanelWithIconColor"]);
            VolumeRightPanelColor = ConfigHelper.ReadColor(VolumeRightPanelColor, section["VolumeRightPanelColor"]);
            VolumeIconsColor = ConfigHelper.ReadColor(VolumeIconsColor, section["VolumeIconsColor"]);
            VolumeHoverColor = ConfigHelper.ReadColor(VolumeHoverColor, section["VolumeHoverColor"]);
            VolumeSliderBackgroundColor = ConfigHelper.ReadColor(VolumeSliderBackgroundColor, section["VolumeSliderBackgroundColor"]);
            VolumeSliderActiveColor = ConfigHelper.ReadColor(VolumeSliderActiveColor, section["VolumeSliderActiveColor"]);
            VolumeControllerTitlesColor = ConfigHelper.ReadColor(VolumeControllerTitlesColor, section["VolumeControllerTitlesColor"]);
            VolumeControllerPercentsColor = ConfigHelper.ReadColor(VolumeControllerPercentsColor, section["VolumeControllerPercentsColor"]);
        }

        /// <summary>
        /// </summary>
        protected override void LoadElements()
        {
        }
    }
}
