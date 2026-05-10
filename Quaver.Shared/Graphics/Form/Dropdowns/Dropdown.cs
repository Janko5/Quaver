using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.Options.Search;
using Quaver.Shared.Skinning;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Managers;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Helpers;

namespace Quaver.Shared.Graphics.Form.Dropdowns
{
    public class Dropdown : ImageButton
    {
        /// <summary>
        ///     The available options for the dropdown
        /// </summary>
        public List<string> Options { get; }

        /// <summary>
        ///     The index of the selected option in <see cref="Options"/>
        /// </summary>
        public int SelectedIndex { get; set; }

        /// <summary>
        ///     The color of the text elements
        /// </summary>
        public Color HoverColor { get; }

        /// <summary>
        ///     The chevron pointing down on the dropdown
        /// </summary>
        public Sprite Chevron { get; private set; }

        /// <summary>
        ///     Texture for the closed state.
        /// </summary>
        private Texture2D _textureClosed;
        public Texture2D TextureClosed
        {
            get => _textureClosed;
            set
            {
                _textureClosed = value;
                if (!Opened)
                {
                    Image = value;
                    if (HoverSprite != null)
                        HoverSprite.Image = value;
                }
            }
        }

        /// <summary>
        ///     Texture for the open state.
        /// </summary>
        private Texture2D _textureOpen;
        public Texture2D TextureOpen
        {
            get => _textureOpen;
            set
            {
                _textureOpen = value;
                if (Opened)
                {
                    Image = value;
                    if (HoverSprite != null)
                        HoverSprite.Image = value;
                }
            }
        }

        /// <summary>
        ///     The color/tint of the main button's hover sprite.
        /// </summary>
        private Color _mainButtonHoverColor = Color.White;
        public Color MainButtonHoverColor
        {
            get => _mainButtonHoverColor;
            set
            {
                _mainButtonHoverColor = value;
                if (HoverSprite != null)
                    HoverSprite.Tint = value;
            }
        }

        /// <summary>
        ///     Background color for the dropdown items.
        /// </summary>
        private Color _itemBackgroundColor = ColorHelper.HexToColor("#181818");
        public Color ItemBackgroundColor
        {
            get => _itemBackgroundColor;
            set
            {
                _itemBackgroundColor = value;
                if (Items != null)
                {
                    foreach (var item in Items)
                        item.Tint = value;
                }
            }
        }

        /// <summary>
        ///    The tint of the dropdown when it is closed.
        /// </summary>
        public Color ColorClosed { get; set; } = Color.White;

        /// <summary>
        ///    The tint of the dropdown when it is open.
        /// </summary>
        public Color ColorOpen { get; set; } = Color.White;

        /// <summary>
        ///    The tint of the divider line.
        /// </summary>
        public Color DividerLineColor { get; set; } = Color.White;

        /// <summary>
        ///     If the check icon should be shown when selected.
        /// </summary>
        private bool _showCheckOnSelection = true;
        public bool ShowCheckOnSelection
        {
            get => _showCheckOnSelection;
            set
            {
                _showCheckOnSelection = value;
                if (Items != null)
                {
                    foreach (var item in Items)
                        item.UpdateCheckVisibility();
                }
            }
        }

        /// <summary>
        ///     Hover color for the dropdown items.
        /// </summary>
        private Color _itemHoverColor = Color.White;
        public Color ItemHoverColor
        {
            get => _itemHoverColor;
            set
            {
                _itemHoverColor = value;
                // Hover effect logic is inside DropdownItem, so we might need a way to update it.
                // DropdownItem uses Dropdown.ItemHoverColor in CreateHoverSprite and logic.
                // But it's better if DropdownItem just reads the property dynamically or we update DropdownItem to exposing the hover sprite tint.
                // For now, let's just leave it property-based as DropdownItem likely reads it on hover event or we update DropdownItem to be reactive.
                // Wait, DropdownItem reads Dropdown.ItemHoverColor in CreateHoverSprite which is only called in constructor.
                // So updating this property DOES need to propagate to existing items.
                if (Items != null)
                {
                    foreach (var item in Items)
                        item.UpdateHoverColor(value);
                }
            }
        }

        /// <summary>
        ///     The font used for the dropdown
        /// </summary>
        public WobbleFontStore Font { get; }

        /// <summary>
        ///     The text of the selected item
        ///     <see cref="SelectedIndex"/> of <see cref="Options"/>
        /// </summary>
        public SpriteTextPlus SelectedText { get; private set; }

        /// <summary>
        ///     The amount of padding for elements on the x axis
        /// </summary>
        public const int PaddingX = 12;

        /// <summary>
        ///     The size of the dropdown font
        /// </summary>
        public int FontSize { get; }

        /// <summary>
        ///     Holds all of the dropdown items in the container
        ///
        ///     When the dropdown is opened/closed, this will give it a clipping rectangle effect, so that
        ///     the dropdown gradually is opening/closing
        /// </summary>
        public ScrollContainer ItemContainer { get; private set; }

        /// <summary>
        ///     The clickable items in the dropdown
        /// </summary>
        public List<DropdownItem> Items { get; private set; }

        /// <summary>
        ///     If the dropdown is currently open
        /// </summary>
        public bool Opened { get; private set; }

        /// <summary>
        ///     The line that divides the top element from the items
        /// </summary>
        public Sprite DividerLine { get; private set; }

        /// <summary>
        ///     The sprite that lights up when hovered
        /// </summary>
        public Sprite HoverSprite { get; private set; }

        /// <summary>
        ///     Event invoked when an item was selected in the dropdown
        /// </summary>
        public event EventHandler<DropdownClickedEventArgs> ItemSelected;

        /// <summary>
        ///     The alpha of the buttons when it's highlighted
        /// </summary>
        public float HighlightAlpha { get; set; } = 0.45f;

        /// <summary>
        ///     The max width of the dropdown's text
        /// </summary>
        public int MaxWidth { get; }

        /// <summary>
        ///     The duration of the opening/closing animation in milliseconds.
        /// </summary>
        public int AnimationTime { get; set; } = 500;

        /// <summary>
        ///     If the dropdown should close when an item is selected
        /// </summary>
        public bool CloseOnSelect { get; set; } = true;

        /// <summary>
        ///     If true, uses persistent Checkbox icons (Box/CheckedBox) instead of toggleable checkmark.
        /// </summary>
        private bool _useCheckboxIcons;
        public bool UseCheckboxIcons
        {
            get => _useCheckboxIcons;
            set
            {
                _useCheckboxIcons = value;
                if (Items != null)
                {
                    foreach (var item in Items)
                        item.UpdateCheckVisibility();
                }
            }
        }

        /// <summary>
        ///     Invokes the ItemSelected event manually
        /// </summary>
        /// <param name="item"></param>
        public void InvokeItemSelected(DropdownItem item) => ItemSelected?.Invoke(this, new DropdownClickedEventArgs(item));

        /// <summary>
        /// </summary>
        private int MaxHeight { get; }

        /// <summary>
        ///     The height of the dropdown when opened
        /// </summary>
        public int OpenHeight
        {
            get
            {
                var height = (int)Height * Options.Count;

                if (MaxHeight != 0 && height >= MaxHeight)
                    height = MaxHeight;
                return height;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="maxHeight"></param>
        /// <param name="font"></param>
        public Dropdown(List<string> options, ScalableVector2 size, int fontSize, Color? color = null, int selectedIndex = 0,
            int maxWidth = 0, int maxHeight = 0, WobbleFontStore font = null)
            : base(UserInterface.DropdownClosed)
        {
            Font = font ?? FontManager.GetWobbleFont(Fonts.LatoBlack);
            Options = options;
            SelectedIndex = selectedIndex;
            HoverColor = color ?? Colors.MainAccent;
            FontSize = fontSize;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;

            if (Options == null || Options.Count == 0)
                throw new InvalidOperationException("You cannot create a dropdown with zero elements");

            TextureClosed = UserInterface.DropdownClosed;
            TextureOpen = UserInterface.DropdownOpen;

            Size = size;
            Tint = Colors.DarkGray;
            ColorClosed = Tint;
            ColorOpen = Tint;

            CreateHoverSprite();
            CreateChevron();
            CreateSelectedText();
            CreateDividerLine();
            CreateItemContainer();
            CreateItems();

            Hovered += OnHovered;
            LeftHover += OnHoverLeft;
            Clicked += OnClicked;
            ClickedOutside += OnClickedOutside;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            ItemSelected = null;
            base.Destroy();
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
                Tint = MainButtonHoverColor
            };
        }

        /// <summary>
        ///     Creates <see cref="Chevron"/>
        /// </summary>
        private void CreateChevron()
        {
            Chevron = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(Height * 0.45f, Height * 0.45f),
                Tint = HoverColor,
                Image = FontAwesome.Get(FontAwesomeIcon.fa_chevron_arrow_down),
                X = -PaddingX
            };
        }

        /// <summary>
        /// </summary>
        private void CreateSelectedText()
        {
            SelectedText = new SpriteTextPlus(Font, Options[SelectedIndex],
                FontSize)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = PaddingX,
                Tint = HoverColor
            };

            if (MaxWidth != 0)
                SelectedText.TruncateWithEllipsis(MaxWidth);
        }

        /// <summary>
        ///     Creates <see cref="DividerLine"/>
        /// </summary>
        private void CreateDividerLine()
        {
            DividerLine = new Sprite
            {
                Parent = this,
                Size = new ScalableVector2(Width, 2),
                Y = Height,
                Tint = DividerLineColor,
                Alpha = 0,
                Visible = false
            };
        }

        /// <summary>
        ///     Creates <see cref="ItemContainer"/>
        /// </summary>
        private void CreateItemContainer()
        {
            var height = Height * Options.Count;

            ItemContainer = new ScrollContainer(new ScalableVector2(Width, 0),
                new ScalableVector2(Width, height))
            {
                Parent = this,
                Y = DividerLine.Y + DividerLine.Height,
                Scrollbar =
                {
                    Visible = false
                },
                Image = null,
                Tint = Color.Transparent,
                Alpha = 0,
                Visible = false
            };
        }

        /// <summary>
        ///     Creates the items to be used in the dropdown
        /// </summary>
        public void CreateItems()
        {
            Items = new List<DropdownItem>();

            for (var i = 0; i < Options.Count; i++)
            {
                Texture2D image;
                if (i == 0)
                    image = SkinManager.Skin?.DropdownTop ?? SkinManager.Skin?.DropdownMiddle ?? UserInterface.DropdownMiddle;
                else if (i == Options.Count - 1)
                    image = SkinManager.Skin?.DropdownBottom ?? UserInterface.DropdownBottom;
                else
                    image = SkinManager.Skin?.DropdownMiddle ?? UserInterface.DropdownMiddle;

                var item = new DropdownItem(this, i, image)
                {
                    Y = i * Height,
                    IsClickable = false
                };

                // Set initial selection state
                item.SetSelected(i == SelectedIndex);

                Items.Add(item);
                ItemContainer.AddContainedDrawable(item);
            }
        }

        /// <summary>
        ///     Event invoked when the dropdown is opened
        /// </summary>
        public event EventHandler OpenedEvent;

        /// <summary>
        ///     Event invoked when the dropdown is closed
        /// </summary>
        public event EventHandler ClosedEvent;

        /// <summary>
        ///     Opens the dropdown menu
        /// </summary>
        public virtual void Open(int? time = null)
        {
            if (Opened)
                return;

            Opened = true;
            var animTime = time ?? AnimationTime;

            Image = TextureOpen;
            Tint = ColorOpen;
            HoverSprite.Image = TextureOpen;

            DividerLine.Visible = true;
            ItemContainer.Visible = true;

            DividerLine.ClearAnimations();
            Chevron.ClearAnimations();
            ItemContainer.ClearAnimations();

            if (animTime > 0)
            {
                DividerLine.FadeTo(1, Easing.OutQuint, animTime / 2);
                Chevron.Animations.Add(new Animation(AnimationProperty.Rotation, Easing.OutQuint, Chevron.Rotation, MathF.PI, animTime));
                ItemContainer.ChangeHeightTo(OpenHeight, Easing.OutQuint, animTime);
                ItemContainer.FadeTo(1f, Easing.OutQuint, animTime / 2);
            }
            else
            {
                DividerLine.Alpha = 1f;
                Chevron.Rotation = MathF.PI;
                ItemContainer.Height = OpenHeight;
                ItemContainer.Alpha = 1f;
            }

            Items.ForEach(x => x.IsClickable = true);
            OpenedEvent?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Closes the dropdown menu
        /// </summary>
        public virtual void Close(int? time = null)
        {
            if (!Opened)
                return;

            Opened = false;
            var animTime = time ?? AnimationTime;

            Image = TextureClosed;
            Tint = ColorClosed;
            HoverSprite.Image = TextureClosed;

            DividerLine.ClearAnimations();
            Chevron.ClearAnimations();
            ItemContainer.ClearAnimations();

            if (animTime > 0)
            {
                DividerLine.FadeTo(0, Easing.OutQuint, animTime / 2);
                Chevron.Animations.Add(new Animation(AnimationProperty.Rotation, Easing.OutQuint, Chevron.Rotation, 0, animTime));
                ItemContainer.ChangeHeightTo(0, Easing.OutQuint, animTime);
                ItemContainer.FadeTo(0f, Easing.OutQuint, animTime / 2);
            }
            else
            {
                DividerLine.Alpha = 0f;
                DividerLine.Visible = false;
                Chevron.Rotation = 0f;
                ItemContainer.Height = 0;
                ItemContainer.Alpha = 0f;
                ItemContainer.Visible = false;
            }

            Items.ForEach(x => x.IsClickable = false);
            ClosedEvent?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHovered(object sender, EventArgs e)
        {
            HoverSprite.ClearAnimations();
            HoverSprite.FadeTo(HighlightAlpha, Easing.OutQuint, 75);

            SkinManager.Skin?.SoundHover?.CreateChannel()?.Play();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHoverLeft(object sender, EventArgs e)
        {
            HoverSprite.ClearAnimations();
            HoverSprite.FadeTo(0f, Easing.OutQuint, 75);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked(object sender, EventArgs e)
        {
            if (Opened)
                Close();
            else
                Open();
        }

        /// <summary>
        ///     If the dropdown supports multiple items being selected at once
        /// </summary>
        public bool MultiSelection { get; set; } = false;

        /// <summary>
        ///     Selects a new dropdown item to be the new value
        /// </summary>
        public void SelectItem(DropdownItem item, bool invokeEvent = true)
        {
            // Handle Multi-Selection behavior
            if (MultiSelection)
            {
                // Toggle selection
                item.SetSelected(!item.IsSelected);

                if (invokeEvent)
                    ItemSelected?.Invoke(this, new DropdownClickedEventArgs(item));

                return;
            }

            // Already selected (Single Select)
            if (SelectedIndex == item.Index)
                return;

            // Deselect old item
            if (Items != null && SelectedIndex >= 0 && SelectedIndex < Items.Count)
                Items[SelectedIndex].SetSelected(false);

            SelectedText.Text = item.Text.Text;
            SelectedText.Tint = item.Text.Tint;
            SelectedIndex = item.Index;

            // Select new item
            item.SetSelected(true);

            if (invokeEvent)
                ItemSelected?.Invoke(this, new DropdownClickedEventArgs(item));
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void OnClickedOutside(object sender, EventArgs e)
        {
            var mousePoint = MouseManager.CurrentState.Position.ToPoint();

            if (ItemContainer.ScreenRectangle.Contains(mousePoint) || ScreenRectangle.Contains(mousePoint))
                return;

            if (Opened)
                Close();
        }
    }
}