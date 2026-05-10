using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Skinning;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Wobble.Logging;

namespace Quaver.Shared.Graphics.Form.Dropdowns
{
    public class DropdownItem : ImageButton
    {
        /// <summary>
        ///     The parent dropdown sprite
        /// </summary>
        public Dropdown Dropdown { get; }

        /// <summary>
        ///     The index of <see cref="Dropdown"/> options this item represents
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     The text for the individual item
        /// </summary>
        public SpriteTextPlus Text { get; private set; }

        /// <summary>
        ///     The sprite that lights up when hovered
        /// </summary>
        private Sprite HoverSprite { get; set; }

        /// <summary>
        ///    Optional nine-slice background.
        /// </summary>
        private NineSliceSprite BackgroundNineSlice { get; set; }

        /// <summary>
        ///    Optional nine-slice hover sprite.
        /// </summary>
        private NineSliceSprite HoverNineSlice { get; set; }

        /// <summary>
        ///    Whether the item is using nine-slice scaling.
        /// </summary>
        public bool IsNineSliceEnabled => BackgroundNineSlice != null;


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="index"></param>
        /// <param name="image"></param>
        public DropdownItem(Dropdown dropdown, int index, Texture2D image = null) : base(image ?? WobbleAssets.WhiteBox)
        {
            Dropdown = dropdown;
            Index = index;

            Size = Dropdown.Size;
            Tint = Dropdown.ItemBackgroundColor;

            CreateHoverSprite();

            // Create text
            CreateText();

            // Create check icon
            CheckIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -10,
                Image = UserInterface.CheckSymbol,
                Size = new ScalableVector2(16, 16),
                Visible = false
            };

            Hovered += OnHovered;
            LeftHover += OnHoverLeft;
            Clicked += OnClicked;
        }

        public Sprite CheckIcon { get; private set; }


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (HoverSprite.Image != Image)
                HoverSprite.Image = Image;

            if (IsNineSliceEnabled)
            {
                if (BackgroundNineSlice.Image != Image)
                    BackgroundNineSlice.Image = Image;

                if (HoverNineSlice.Image != Image)
                    HoverNineSlice.Image = Image;
                
                BackgroundNineSlice.Size = Size;
                HoverNineSlice.Size = Size;
            }

            base.Update(gameTime);
        }

        /// <summary>
        ///    Enables nine-slice scaling for this item.
        /// </summary>
        /// <param name="margins"></param>
        public void EnableNineSlice(SliceMargins margins)
        {
            if (IsNineSliceEnabled)
                return;

            BackgroundNineSlice = new NineSliceSprite(Image, margins)
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Size = Size,
                Tint = Tint,
                Alpha = Alpha,
                UsePreviousSpriteBatchOptions = true
            };

            HoverNineSlice = new NineSliceSprite(Image, margins)
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Size = Size,
                Tint = Dropdown.ItemHoverColor,
                Alpha = 0,
                UsePreviousSpriteBatchOptions = true
            };

            // Wobble draws children in the order they are in the list.
            // Since we added NineSlice sprites late, they are at the end of the list (drawn on top).
            // We need to move them to the beginning.
            Children.Remove(BackgroundNineSlice);
            Children.Insert(0, BackgroundNineSlice);

            Children.Remove(HoverNineSlice);
            Children.Insert(1, HoverNineSlice);

            // Hide original background and hover
            Tint = Color.Transparent;
            HoverSprite.Visible = false;
        }

        /// <summary>
        ///     Creates <see cref="HoverSprite"/>
        /// </summary>
        private void CreateHoverSprite()
        {
            HoverSprite = new Sprite
            {
                Parent = this,
                Size = Size,
                Alpha = 0,
                Image = Image,
                Tint = Dropdown.ItemHoverColor
            };
        }

        /// <summary>
        ///     Creates <see cref="Text"/>
        /// </summary>
        private void CreateText()
        {
            Text = new SpriteTextPlus(Dropdown.SelectedText.Font, Dropdown.Options[Index], Dropdown.FontSize)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Dropdown.PaddingX,
                Alpha = 1
            };

            if (Dropdown.MaxWidth != 0)
                Text.TruncateWithEllipsis(Dropdown.MaxWidth);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHovered(object sender, EventArgs e)
        {
            if (!Dropdown.Opened)
                return;

            if (IsNineSliceEnabled)
            {
                HoverNineSlice.ClearAnimations();
                HoverNineSlice.FadeTo(Dropdown.HighlightAlpha, Easing.Linear, 75);
            }
            else
            {
                HoverSprite.ClearAnimations();
                HoverSprite.FadeTo(Dropdown.HighlightAlpha, Easing.Linear, 75);
            }

            SkinManager.Skin?.SoundHover?.CreateChannel()?.Play();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHoverLeft(object sender, EventArgs e)
        {
            if (IsNineSliceEnabled)
            {
                HoverNineSlice.ClearAnimations();
                HoverNineSlice.FadeTo(0f, Easing.Linear, 75);
            }
            else
            {
                HoverSprite.ClearAnimations();
                HoverSprite.FadeTo(0f, Easing.Linear, 75);
            }
        }

        /// <summary>
        ///     If the item is selected
        /// </summary>
        public bool IsSelected { get; private set; }

        /// <summary>
        ///    Sets the selected state of the item and updates the visual representation
        /// </summary>
        /// <param name="selected"></param>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            UpdateCheckVisibility();
        }

        /// <summary>
        ///     Updates the visibility and image of the check icon
        /// </summary>
        public void UpdateCheckVisibility()
        {
            if (CheckIcon == null)
                return;

            if (Dropdown.UseCheckboxIcons)
            {
                CheckIcon.Visible = true;
                CheckIcon.Image = FontAwesome.Get(IsSelected ? FontAwesomeIcon.fa_check : FontAwesomeIcon.fa_check_box_empty);
            }
            else
            {
                CheckIcon.Visible = IsSelected && Dropdown.ShowCheckOnSelection;
                CheckIcon.Image = UserInterface.CheckSymbol;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked(object sender, EventArgs e)
        {
            if (!Dropdown.Opened)
                return;

            Dropdown.SelectItem(this);

            if (Dropdown.CloseOnSelect)
                Dropdown.Close();
        }


        /// <summary>
        ///     Updates the hover color of the item
        /// </summary>
        /// <param name="color"></param>
        public void UpdateHoverColor(Color color)
        {
            if (HoverSprite != null)
                HoverSprite.Tint = color;

            if (HoverNineSlice != null)
                HoverNineSlice.Tint = color;
        }
    }
}