using Quaver.Shared.Assets;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.UI.Buttons;

namespace Quaver.Shared.Graphics.Menu.Border.Components
{
    /// <summary>
    ///     Logo that goes in the left side of a menu header.
    /// </summary>
    public class MenuBorderLogo : ImageButton, IMenuBorderItem
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public bool UseCustomPaddingY { get; } = true;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public int CustomPaddingY { get; set; } = -2;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public bool UseCustomPaddingX { get; } = true;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public int CustomPaddingX { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public MenuBorderLogo() : base(SkinManager.Skin?.UserInterfaceVersion >= 2.0f ? UserInterface.MenuBorderLogoV2 : UserInterface.Logo)
        {
            if (SkinManager.Skin?.UserInterfaceVersion >= 2.0f)
            {
                CustomPaddingX = 20;
                CustomPaddingY = 0;
                Size = new ScalableVector2(Image.Width, Image.Height);
            }
            else
            {
                CustomPaddingX = 25;
                Size = new ScalableVector2(126, 28);
            }
        }
    }
}
