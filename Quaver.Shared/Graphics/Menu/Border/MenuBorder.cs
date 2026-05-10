using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Window;

namespace Quaver.Shared.Graphics.Menu.Border
{
    public class MenuBorder : Sprite
    {
        /// <summary>
        ///     The type of menu border this is, whether a header or footer
        /// </summary>
        private MenuBorderType Type { get; }

        /// <summary>
        /// </summary>
        public static int HEIGHT => SkinManager.Skin?.UserInterfaceVersion >= 2.0f ? 70 : 56;

        /// <summary>
        ///     The line displayed at the top of the footer
        /// </summary>
        public Sprite ForegroundLine { get; private set; } = null!;

        /// <summary>
        ///     The line that animates within <see cref="ForegroundLine"/>
        /// </summary>
        public Sprite AnimatedLine { get; private set; } = null!;

        /// <summary>
        ///     The items that are aligned from left to right of the footer
        /// </summary>
        protected List<Drawable>? LeftAlignedItems { get; set; }

        /// <summary>
        ///     The items that are aligned from right to left of the footer
        /// </summary>
        protected List<Drawable>? RightAlignedItems { get; set; }

        /// <summary>
        /// </summary>
        public MenuBorder(MenuBorderType type, List<Drawable>? leftAligned = null, List<Drawable>? rightAligned = null)
        {
            Type = type;

            LeftAlignedItems = leftAligned;
            RightAlignedItems = rightAligned;

            Size = new ScalableVector2(WindowManager.Width, HEIGHT);

            if (type == MenuBorderType.Footer)
            {
                Image = SkinManager.Skin?.MenuBorder?.BackgroundFooter ?? SkinManager.Skin?.MenuBorder?.Background ?? UserInterface.MenuBorderBackgroundFooter;
            }
            else
            {
                Image = SkinManager.Skin?.MenuBorder?.Background ?? UserInterface.MenuBorderBackground;
            }

            CreateForegroundLine();
            CreateAnimatedLine();

            AlignLeftItems();
            AlignRightItems();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            PerformLineAnimations();
            base.Update(gameTime);
            AlignLeftItems();
            AlignRightItems();
        }

        /// <summary>
        ///     Creates the top line sprite of the footer
        /// </summary>
        private void CreateForegroundLine()
        {
            ForegroundLine = new Sprite
            {
                Parent = this,
                Size = new ScalableVector2(Width, 2),
                Alignment = Type == MenuBorderType.Header ? Alignment.BotLeft : Alignment.TopLeft,
                Tint = SkinManager.Skin.MenuBorder.BackgroundLineColor,
                // Hide both background and animated line when MenuBorderLine = False
                Visible = SkinManager.Skin.MenuBorder.ShowLine
            };
        }

        /// <summary>
        ///     Creates the line that is animated within the top border line
        /// </summary>
        private void CreateAnimatedLine()
        {
            AnimatedLine = new Sprite
            {
                Parent = ForegroundLine,
                Size = new ScalableVector2(150, 2),
                Tint = SkinManager.Skin.MenuBorder.ForegroundLineColor,
            };

            if (Type == MenuBorderType.Header)
                AnimatedLine.X = WindowManager.Width - AnimatedLine.Width;
        }

        /// <summary>
        ///     Aligns the drawables from left to right
        /// </summary>
        protected void AlignLeftItems()
        {
            if (LeftAlignedItems == null || LeftAlignedItems.Count == 0)
                return;

            AlignDrawables(AlignmentDirection.LeftToRight, LeftAlignedItems);
        }

        /// <summary>
        ///     Aligns the drawables from right to left
        /// </summary>
        public void AlignRightItems()
        {
            if (RightAlignedItems == null || RightAlignedItems.Count == 0)
                return;

            AlignDrawables(AlignmentDirection.RightToLeft, RightAlignedItems);
        }

        /// <summary>
        ///     Aligns drawables based on the direction
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="items"></param>
        private void AlignDrawables(AlignmentDirection direction, IReadOnlyList<Drawable> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                item.Parent = this;

                item.Y = (int)(item is IMenuBorderItem borderItem && borderItem.UseCustomPaddingY ? borderItem.CustomPaddingY : 0);

                if (item.Y == 0 && Type == MenuBorderType.Footer)
                    item.Y = 2;

                item.Alignment = direction == AlignmentDirection.LeftToRight ? Alignment.MidLeft : Alignment.MidRight;

                var padding = SkinManager.Skin?.UserInterfaceVersion >= 2.0f ? 20 : 25;
                var spacing = item is IMenuBorderItem b && b.UseCustomPaddingX ? b.CustomPaddingX : (SkinManager.Skin?.UserInterfaceVersion >= 2.0f ? 60 : 40);

                if (i == 0)
                {
                    var p = item is IMenuBorderItem b2 && b2.UseCustomPaddingX ? b2.CustomPaddingX : padding;
                    item.X = (int)(direction == AlignmentDirection.LeftToRight ? p : -p);
                }
                else
                {
                    var prev = items[i - 1];
                    item.X = (int)(direction == AlignmentDirection.LeftToRight
                        ? prev.X + prev.Width + spacing
                        : prev.X - prev.Width - spacing);
                }
            }
        }

        /// <summary>
        /// </summary>
        private void PerformLineAnimations()
        {
            if (AnimatedLine.Animations.Count != 0)
                return;

            if (AnimatedLine.X > WindowManager.Width / 2f)
                AnimatedLine.MoveToX(0, Easing.Linear, 15000);
            else
                AnimatedLine.MoveToX(WindowManager.Width - AnimatedLine.Width, Easing.Linear, 15000);
        }
    }
}
