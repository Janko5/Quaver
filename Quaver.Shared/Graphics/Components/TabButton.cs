using System;
using System.Collections.Generic;
using Wobble.Window;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.Animations;
using Quaver.Shared.Helpers;
using Quaver.Shared.Modifiers.Mods;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Managers;
using Quaver.API.Enums;
using Quaver.Shared.Graphics.Form.Dropdowns.RightClick;

namespace Quaver.Shared.Graphics.Components
{
    /// <summary>
    ///     A button used for tabs in various screens (e.g. Song Select V2).
    /// </summary>
    public class TabButton : ImageButton
    {
        /// <summary>
        ///    Standard height for a tab button.
        /// </summary>
        public const float StandardHeight = 40f;

        /// <summary>
        ///    Gap between labels and icons.
        /// </summary>
        public const float ElementSpacing = 15f;

        /// <summary>
        ///    Standard width for a mod icon.
        /// </summary>
        public const float ModIconWidth = 60f;

        /// <summary>
        ///    How much mod icons overlap each other.
        /// </summary>
        public const float ModIconOverlap = 25f;

        /// <summary>
        ///     Maximum width for the tab button.
        /// </summary>
        public const float MaxWidth = 548f;

        /// <summary>
        ///     The background sprite.
        /// </summary>
        private NineSliceSprite Background { get; }

        /// <summary>
        ///     The hover overlay sprite.
        /// </summary>
        private NineSliceSprite HoverOverlay { get; }

        /// <summary>
        ///     The text displayed on the button.
        /// </summary>
        private SpriteTextPlus Label { get; }

        /// <summary>
        ///     Function to determine if the button is active.
        /// </summary>
        private Func<bool> IsActiveFunc { get; }

        /// <summary>
        ///     The icon displaying the current value (e.g. Judgement Window).
        /// </summary>
        private Container ValueIcon { get; }

        /// <summary>
        ///     The container for active mod icons.
        /// </summary>
        private Container ModsContainer { get; }

        /// <summary>
        ///     Tracks all outline sprites so their tint can be updated dynamically.
        /// </summary>
        private readonly List<Sprite> OutlineSprites = new List<Sprite>();

        /// <summary>
        ///     The reset modifiers icon
        /// </summary>
        private ImageButton? ResetIcon { get; set; }

        /// <summary>
        ///     The more button for remaining modifiers
        /// </summary>
        private ImageButton? MoreButton { get; set; }
        private int currentShownModsCount;

        /// <summary>
        ///     The currently active "More" dropdown, used for toggle-close logic.
        /// </summary>
        private RightClickOptions? ActiveMoreDropdown { get; set; }

        /// <summary>
        ///     Stores the original index of TabsPanel to restore it after dropdown is closed.
        /// </summary>
        private int originalTabsPanelIndex = -1;

        /// <summary>
        ///     The order of mods as they appear in the modifier panel.
        /// </summary>
        private static readonly List<ModIdentifier> ModOrderPriority = new List<ModIdentifier>
        {
            ModIdentifier.Mirror,
            ModIdentifier.NoMiss,
            ModIdentifier.Autoplay,
            ModIdentifier.NoFail,
            ModIdentifier.Randomize,
            ModIdentifier.NoSliderVelocity,
            ModIdentifier.NoLongNotes,
            ModIdentifier.FullLN,
            ModIdentifier.Inverse,
            ModIdentifier.Coop,
            ModIdentifier.HeatlthAdjust
        };

        /// <summary>
        ///     The dim overlay shown behind the dropdown. ImageButton to block clicks.
        /// </summary>
        private ImageButton? DimOverlay { get; set; }

        /// <summary>
        ///     Cache for silhouette textures to avoid redundant GPU-CPU transfers.
        /// </summary>
        private static readonly Dictionary<Texture2D, Texture2D> SilhouetteCache = new Dictionary<Texture2D, Texture2D>();

        /// <summary>
        ///     The optional icon displayed on the button.
        /// </summary>
        private Sprite? Icon { get; set; }

        /// <summary>
        ///     Constructor for the tab button.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="onClick"></param>
        /// <param name="isActiveFunc"></param>
        /// <param name="icon"></param>
        /// <param name="hasValueIcon"></param>
        public TabButton(string text, Action onClick, Func<bool> isActiveFunc, Texture2D? icon = null, bool hasValueIcon = false) : base(UserInterface.SquareButton)
        {
            IsActiveFunc = isActiveFunc;

            // Wobble's Sprite/Button renders a WhiteBox if Image is null or if we want to use Nineslice as background.
            // We set Alpha to 0 to hide the base image and use our NineSlice instead.
            Alpha = 0;

            Background = new NineSliceSprite
            {
                Parent = this,
                Image = UserInterface.SquareButton,
                Tint = SkinManager.Skin.Universal.ButtonNotActiveColor,
                Margins = new SliceMargins(19, 19, 0, 0)
            };

            HoverOverlay = new NineSliceSprite
            {
                Parent = this,
                Image = UserInterface.SquareButton,
                Margins = new SliceMargins(19, 19, 0, 0),
                Alpha = 0,
                Tint = SkinManager.Skin.Universal.ButtonHoverColor
            };

            if (hasValueIcon)
            {
                ValueIcon = new Container
                {
                    Parent = this,
                    Size = new ScalableVector2(66, 24),
                    Alignment = Alignment.MidLeft
                };

                ModsContainer = new Container
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    Size = new ScalableVector2(0, StandardHeight)
                };
            }
            else
            {
                ValueIcon = new Container();
                ModsContainer = new Container();
            }

            Label = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), text, 22)
            {
                Parent = this,
                Alignment = hasValueIcon ? Alignment.MidLeft : Alignment.MidCenter,
                X = hasValueIcon ? ElementSpacing : 0,
                Tint = SkinManager.Skin.Universal.ButtonContentColor
            };

            if (icon != null)
            {
                Icon = new Sprite
                {
                    Parent = this,
                    Image = icon,
                    Size = new ScalableVector2(22, 20),
                    Alignment = Alignment.MidCenter,
                    Tint = Label.Tint
                };
            }

            UpdateLayout();

            Clicked += (s, e) => onClick();
            Hovered += (s, e) =>
            {
                if (!IsActiveFunc())
                    HoverOverlay.FadeTo(1f, Easing.OutQuint, 100);
            };
            LeftHover += (s, e) => HoverOverlay.FadeTo(0f, Easing.OutQuint, 100);
        }

        /// <summary>
        ///     Recalculates the layout of the internal elements.
        /// </summary>
        private void UpdateLayout()
        {
            if (Label == null)
                return;

            if (ValueIcon != null && ValueIcon.Parent != null)
            {
                ValueIcon.X = Label.X + Label.Width + ElementSpacing;
                ModsContainer.X = ValueIcon.X + ValueIcon.Width + ElementSpacing;

                if (currentShownModsCount > 0)
                {
                    // Calculate width based on number of active icons and overlap
                    // Added 4px buffer for the right-edge stroke
                    var activeModsCount = currentShownModsCount;
                    var effectiveIconWidth = ModIconWidth - ModIconOverlap;
                    var modsWidth = ModIconWidth + (activeModsCount - 1) * effectiveIconWidth + 4;

                    if (MoreButton != null && MoreButton.Alpha > 0)
                        modsWidth += 5 + (int)MoreButton.Width;

                    // Add width for Reset icon if visible
                    if (ResetIcon != null && ResetIcon.Alpha > 0)
                        modsWidth += ElementSpacing + (int)ResetIcon.Width;

                    ModsContainer.Width = modsWidth;
                    
                    var totalWidth = ModsContainer.X + modsWidth - 4 + ElementSpacing;
                    Size = new ScalableVector2(Math.Min(totalWidth, MaxWidth), StandardHeight);
                }
                else
                {
                    // If only JW icon is shown, use its width for the right margin
                    var totalWidth = ValueIcon.X + ValueIcon.Width + ElementSpacing;
                    Size = new ScalableVector2(Math.Min(totalWidth, MaxWidth), StandardHeight);
                }
            }
            else
            {
                Size = new ScalableVector2(Math.Min(string.IsNullOrEmpty(Label.Text) ? StandardHeight : Label.Width + ElementSpacing * 2, MaxWidth), StandardHeight);
            }

            if (Background != null)
                Background.Size = Size;

            if (HoverOverlay != null)
                HoverOverlay.Size = Size;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            var isActive = IsActiveFunc();

            var bgColor = isActive
                ? SkinManager.Skin.Universal.ButtonActiveColor
                : SkinManager.Skin.Universal.ButtonNotActiveColor;

            Background.Tint = bgColor;

            // Determine final outline color by blending with hover color if needed
            var finalOutlineColor = bgColor;
            if (!isActive && HoverOverlay.Alpha > 0)
            {
                var hoverColor = SkinManager.Skin.Universal.ButtonHoverColor;
                finalOutlineColor = Color.Lerp(bgColor, hoverColor, HoverOverlay.Alpha);
            }

            // Disable hover overlay if the button is active
            if (isActive)
                HoverOverlay.Alpha = 0;

            // Keep outline sprites in sync with the current background/hover color
            foreach (var sprite in OutlineSprites)
                sprite.Tint = finalOutlineColor;

            if (Icon != null)
                Icon.Tint = Label.Tint;

            if (ActiveMoreDropdown != null && !ActiveMoreDropdown.Opened)
                CloseMoreDropdown();

            base.Update(gameTime);
        }

        public override void Destroy()
        {
            CloseMoreDropdown();
            base.Destroy();
        }

        private void CloseMoreDropdown()
        {
            if (ActiveMoreDropdown != null)
            {
                ActiveMoreDropdown.Close();
                ActiveMoreDropdown = null;
            }

            DestroyDimOverlay();

            // Restore TabsPanel to its original Z-order position
            var tabsPanel = this.Parent;
            if (tabsPanel != null && originalTabsPanelIndex != -1)
            {
                var tabsParent = tabsPanel.Parent;
                if (tabsParent != null && tabsParent.Children.Contains(tabsPanel))
                {
                    tabsParent.Children.Remove(tabsPanel);
                    tabsParent.Children.Insert(MathHelper.Clamp(originalTabsPanelIndex, 0, tabsParent.Children.Count), tabsPanel);
                }
            }

            originalTabsPanelIndex = -1;
        }

        /// <summary>
        ///     Updates the texture of the value icon.
        /// </summary>
        /// <param name="id"></param>
        public void UpdateValueIcon(string id)
        {
            if (ValueIcon == null)
                return;

            for (var i = ValueIcon.Children.Count - 1; i >= 0; i--)
                ValueIcon.Children[i].Destroy();

            ValueIcon.Children.Clear();

            try
            {
                var texture = TextureManager.Load(id);
                if (texture != null)
                {
                    new Sprite
                    {
                        Parent = ValueIcon,
                        Size = new ScalableVector2(66, 24),
                        Image = texture,
                        Alignment = Alignment.MidLeft
                    };
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error loading texture for {id}: {e.Message}", LogType.Runtime);
            }

            UpdateLayout();
        }

        /// <summary>
        ///     Updates the displayed mod icons.
        /// </summary>
        /// <param name="mods"></param>
        public void UpdateModIcons(List<IGameplayModifier> mods)
        {
            if (ModsContainer == null)
                return;

            // Clear tracked outline sprites before destroying children
            OutlineSprites.Clear();

            currentShownModsCount = 0;
            MoreButton = null;
            ResetIcon = null;

            OutlineSprites.Clear();

            var oldChildren = ModsContainer.Children.ToList();
            foreach (var child in oldChildren)
                child.Destroy();

            ModsContainer.Children.Clear();

            if (mods == null || mods.Count == 0)
            {
                UpdateLayout();
                return;
            }

            // Filter out mods that shouldn't be shown as icons (like JW mod which has its own icon)
            var filteredMods = mods.FindAll(x => x != null && !(x is ModJudgementWindows));

            if (filteredMods.Count == 0)
            {
                UpdateLayout();
                return;
            }

            var unrankedMods = filteredMods.Where(x => !x.Ranked() || x is ModNoSliderVelocities || x is ModNoLongNotes || x is ModFullLN)
                .OrderBy(x =>
                {
                    var index = ModOrderPriority.IndexOf(x.ModIdentifier);
                    return index == -1 ? int.MaxValue : index;
                }).ToList();

            var rankedMods = filteredMods.Where(x => x.Ranked() && !(x is ModNoSliderVelocities || x is ModNoLongNotes || x is ModFullLN))
                .OrderBy(x =>
                {
                    if (x is ModSpeed) return -1;
                    var index = ModOrderPriority.IndexOf(x.ModIdentifier);
                    return index == -1 ? int.MaxValue : index;
                }).ToList();

            List<IGameplayModifier> modsToShow;
            List<IGameplayModifier> remainingMods;

            if (rankedMods.Count > 0 && unrankedMods.Count > 0)
            {
                modsToShow = rankedMods.Take(3).ToList();
                remainingMods = rankedMods.Skip(3).Concat(unrankedMods).ToList();
            }
            else if (rankedMods.Count > 0)
            {
                if (rankedMods.Count > 3)
                {
                    modsToShow = rankedMods.Take(3).ToList();
                    remainingMods = rankedMods.Skip(3).ToList();
                }
                else
                {
                    modsToShow = rankedMods;
                    remainingMods = new List<IGameplayModifier>();
                }
            }
            else
            {
                if (unrankedMods.Count > 3)
                {
                    modsToShow = unrankedMods.Take(3).ToList();
                    remainingMods = unrankedMods.Skip(3).ToList();
                }
                else
                {
                    modsToShow = unrankedMods;
                    remainingMods = new List<IGameplayModifier>();
                }
            }

            currentShownModsCount = modsToShow.Count;

            var effectiveIconWidth = ModIconWidth - ModIconOverlap;

            // Add in reverse order so the first mod is drawn on top
            for (var i = modsToShow.Count - 1; i >= 0; i--)
            {
                var mod = modsToShow[i];
                try
                {
                    var texture = ModManager.GetTexture(mod.ModIdentifier);
                    if (texture != null)
                    {
                        AddIconWithOutline(ModsContainer, texture, new ScalableVector2(ModIconWidth, 24), i * effectiveIconWidth);
                    }
                }
                catch
                {
                    // Ignore mods with missing textures
                }
            }

            // If we have remaining mods, show More button!
            if (remainingMods.Count > 0)
            {
                var moreImage = SkinManager.Skin?.More ?? UserInterface.More ?? Wobble.Assets.WobbleAssets.WhiteBox;
                var moreHoverImage = SkinManager.Skin?.MoreHover ?? UserInterface.MoreHover ?? moreImage;

                var moreX = (modsToShow.Count - 1) * effectiveIconWidth + ModIconWidth + 5;
                MoreButton = new ImageButton(moreImage)
                {
                    Parent = ModsContainer,
                    Size = new ScalableVector2(ModIconWidth, 24),
                    X = moreX,
                    Alignment = Alignment.MidLeft,
                    Alpha = 1.0f,
                    Tint = Color.White,
                    Depth = -1
                };

                MoreButton.Hovered += (s, e) => MoreButton.Image = moreHoverImage;
                MoreButton.LeftHover += (s, e) => MoreButton.Image = moreImage;

                // Add text to the button showing how many mods are hidden
                new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), $"+{remainingMods.Count}", 18)
                {
                    Parent = MoreButton,
                    Alignment = Alignment.MidCenter,
                    Tint = Color.White,
                    UsePreviousSpriteBatchOptions = true
                };

                MoreButton.Clicked += (sender, args) =>
                {
                    if (ActiveMoreDropdown != null)
                    {
                        CloseMoreDropdown();
                        return;
                    }

                    var optionsDict = new Dictionary<string, Color>();
                    foreach (var mod in remainingMods)
                        optionsDict[mod.Name] = mod.ModColor;

                    float maxNameWidth = 0;
                    var font = FontManager.GetWobbleFont(Fonts.InterBold);
                    var originalFontSize = font.FontSize;
                    font.FontSize = 22;

                    foreach (var mod in remainingMods)
                    {
                        var nameWidth = font.Store.MeasureString(mod.Name).X;
                        if (nameWidth > maxNameWidth)
                            maxNameWidth = nameWidth;
                    }

                    font.FontSize = originalFontSize;

                    // Margin (15) + Icon (ModIconWidth) + Spacing (10) + Text + Spacing (20) + Checkmark (16) + Margin (15)
                    var dropdownWidth = 15 + ModIconWidth + 10 + maxNameWidth + 51;
                    if (dropdownWidth < 220)
                        dropdownWidth = 220;

                    // 1. Create dim overlay
                    var game = (QuaverGame)GameBase.Game;
                    var screenView = game?.CurrentScreen?.View;
                    if (screenView != null)
                    {
                        DimOverlay = new ImageButton(WobbleAssets.WhiteBox)
                        {
                            Parent = screenView.Container,
                            Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                            Tint = Color.Black,
                            Alpha = 0f
                        };
                        DimOverlay.FadeTo(0.85f, Easing.Linear, 200);
                        DimOverlay.Clicked += (s, e) => CloseMoreDropdown();
                    }

                    // 2. Create Dropdown
                    var rco = new RightClickOptions(optionsDict, new ScalableVector2(dropdownWidth, 40), 22);
                    rco.ShowCheckOnSelection = true;
                    ActiveMoreDropdown = rco;

                    if (remainingMods.Count <= 1)
                        rco.DividerLine.Visible = false;

                    for (var i = 0; i < rco.Items.Count; i++)
                    {
                        var item = rco.Items[i];
                        var mod = remainingMods[i];
                        item.SetSelected(true);
                        
                        if (remainingMods.Count <= 1)
                        {
                            item.Image = SkinManager.Skin?.DropdownClose ?? UserInterface.DropdownClosed;
                        }
                        else
                        {
                            if (i == 0)
                                item.Image = SkinManager.Skin?.DropdownOpen ?? UserInterface.DropdownOpen;
                            else if (i == rco.Items.Count - 1)
                                item.Image = SkinManager.Skin?.DropdownBottom ?? UserInterface.DropdownBottom;
                            else
                                item.Image = SkinManager.Skin?.DropdownMiddle ?? UserInterface.DropdownMiddle;
                        }

                        // Use NineSlice scaling for modern V2 look
                        item.EnableNineSlice(new SliceMargins(20, 0));

                        var texture = ModManager.GetTexture(mod.ModIdentifier);
                        if (texture != null)
                        {
                            new Sprite
                            {
                                Parent = item,
                                Image = texture,
                                Size = new ScalableVector2(ModIconWidth, 24),
                                Alignment = Alignment.MidLeft,
                                X = 15
                            };
                        }
                        item.Text.X = 15 + ModIconWidth + 10;
                    }

                    game?.CurrentScreen?.ActivateRightClickOptions(rco);

                    var targetX = MathHelper.Clamp(AbsolutePosition.X + Width - rco.Width, 0, WindowManager.Width - rco.Width);
                    var targetY = MathHelper.Clamp(AbsolutePosition.Y + Height + 10, 0, WindowManager.Height - rco.OpenHeight - 60);
                    rco.Position = new ScalableVector2(targetX, targetY);

                    // 4. Manage Z-Order AFTER overlay and dropdown are positioned
                    var tabsPanel = this.Parent;
                    if (tabsPanel != null)
                    {
                        var tabsParent = tabsPanel.Parent;
                        if (tabsParent != null)
                        {
                            originalTabsPanelIndex = tabsParent.Children.IndexOf(tabsPanel);
                            tabsParent.Children.Remove(tabsPanel);
                            tabsParent.Children.Add(tabsPanel);
                        }
                    }

                    if (remainingMods.Count <= 1)
                    {
                        rco.DividerLine.Visible = false;
                        rco.DividerLine.Alpha = 0f;
                        rco.DividerLine.ClearAnimations();
                        rco.DividerLine.Size = new ScalableVector2(0, 0);

                        if (rco.Items.Count > 0)
                        {
                            rco.Items[0].Image = SkinManager.Skin?.DropdownClose ?? UserInterface.DropdownClosed;
                        }
                    }

                    rco.ClosedEvent += (s, ev) => 
                    {
                        rco.Visible = false;
                        foreach (var item in rco.Items)
                            item.Visible = false;
                        rco.Parent = null;
                        rco.Destroy();
                        DestroyDimOverlay();
                        if (ActiveMoreDropdown == rco)
                            ActiveMoreDropdown = null;
                    };

                    rco.ItemSelected += (s, ev) =>
                    {
                        var modToRemove = remainingMods.FirstOrDefault(m => m.Name == ev.Text);
                        if (modToRemove != null)
                        {
                            ModManager.RemoveMod(modToRemove.ModIdentifier);
                            SkinManager.Skin?.SoundClick.CreateChannel().Play();
                        }
                        
                        if (ActiveMoreDropdown == rco)
                        {
                            CloseMoreDropdown();
                        }
                        else
                        {
                            rco.Destroy();
                        }
                    };
                };
            }
            else
            {
                MoreButton = null;
            }

            // Add Reset button if there are mods
            if (filteredMods.Count > 0)
            {
                var resetX = (modsToShow.Count - 1) * effectiveIconWidth + ModIconWidth + ElementSpacing;
                if (MoreButton != null)
                    resetX += (int)MoreButton.Width + 5;

                ResetIcon = new ImageButton(TextureManager.Load("Quaver.Resources/Textures/UI/SongSelect/LeftPanel/icon-reset.png"))
                {
                    Parent = ModsContainer,
                    Size = new ScalableVector2(20, 20),
                    X = resetX,
                    Alignment = Alignment.MidLeft,
                    Alpha = 1.0f,
                    Tint = Color.White,
                    Depth = -1
                };

                ResetIcon.Hovered += (s, e) => ResetIcon.Tint = Color.LightGray;
                ResetIcon.LeftHover += (s, e) => ResetIcon.Tint = Color.White;
                ResetIcon.Clicked += (s, e) =>
                {
                    ModManager.RemoveAllMods();
                    SkinManager.Skin?.SoundClick.CreateChannel().Play();
                };
            }
            else
            {
                ResetIcon = null;
            }

            UpdateLayout();
        }

        /// <summary>
        ///     Adds an icon with a high-quality consolidated outline.
        /// </summary>
        private void AddIconWithOutline(Drawable parent, Texture2D texture, ScalableVector2 size, float x, float y = 0)
        {
            const int radius = 4;
            const int margin = radius + 2; // Space for the outline and AA
            
            var silhouette = GetDilatedSilhouette(texture, radius);
            var tint = Background?.Tint ?? new Color(6, 16, 25, 255);

            var container = new Container
            {
                Parent = parent,
                Size = size,
                X = x,
                Y = y,
                Alignment = Alignment.MidLeft
            };

            var outlineSprite = new Sprite
            {
                Parent = container,
                Size = new ScalableVector2(size.X.Value + margin * 2, size.Y.Value + margin * 2, size.X.Scale, size.Y.Scale),
                Image = silhouette,
                Tint = tint,
                X = -margin,
                Y = -margin
            };
            OutlineSprites.Add(outlineSprite);

            // Main Icon on top
            new Sprite
            {
                Parent = container,
                Size = size,
                Image = texture
            };
        }

        /// <summary>
        ///     Creates a high-quality anti-aliased silhouette from a texture and caches it.
        /// </summary>
        private Texture2D GetDilatedSilhouette(Texture2D original, int radius)
        {
            if (SilhouetteCache.TryGetValue(original, out var cached))
                return cached;

            try
            {
                int w = original.Width;
                int h = original.Height;
                int margin = radius + 2; 
                int targetW = w + margin * 2;
                int targetH = h + margin * 2;

                var sourceData = new Color[w * h];
                original.GetData(sourceData);

                var filledPixels = new List<Point>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (sourceData[y * w + x].A > 128)
                            filledPixels.Add(new Point(x, y));

                var targetData = new Color[targetW * targetH];

                if (filledPixels.Count > 0)
                {
                    for (int ty = 0; ty < targetH; ty++)
                    {
                        for (int tx = 0; tx < targetW; tx++)
                        {
                            float sx = tx - margin;
                            float sy = ty - margin;
                            float minDistSq = float.MaxValue;

                            foreach (var p in filledPixels)
                            {
                                float dx = p.X - sx;
                                float dy = p.Y - sy;
                                
                                if (Math.Abs(dx) > radius + 1 || Math.Abs(dy) > radius + 1) continue;

                                float distSq = dx * dx + dy * dy;
                                if (distSq < minDistSq) minDistSq = distSq;
                                if (minDistSq < 0.1f) break; 
                            }

                            float dist = (float)Math.Sqrt(minDistSq);
                            float alpha = MathHelper.Clamp(radius + 0.5f - dist, 0, 1);
                            targetData[ty * targetW + tx] = new Color((byte)255, (byte)255, (byte)255, (byte)(alpha * 255));
                        }
                    }
                }

                var result = new Texture2D(GameBase.Game.GraphicsDevice, targetW, targetH);
                result.SetData(targetData);
                SilhouetteCache[original] = result;
                return result;
            }
            catch
            {
                return original;
            }
        }

        /// <summary>
        ///     Destroys the dim overlay if it exists.
        /// </summary>
        private void DestroyDimOverlay()
        {
            if (originalTabsPanelIndex != -1)
            {
                var tabsPanel = this.Parent;
                if (tabsPanel != null)
                {
                    var tabsParent = tabsPanel.Parent;
                    if (tabsParent != null)
                    {
                        tabsParent.Children.Remove(tabsPanel);
                        int insertIndex = MathHelper.Clamp(originalTabsPanelIndex, 0, tabsParent.Children.Count);
                        tabsParent.Children.Insert(insertIndex, tabsPanel);
                    }
                }
                originalTabsPanelIndex = -1;
            }

            if (DimOverlay == null)
                return;

            var overlay = DimOverlay;
            DimOverlay = null;
            overlay.ClearAnimations();
            overlay.FadeTo(0f, Easing.Linear, 150);
            overlay.Wait(160);
            overlay.ScheduleUpdate(() =>
            {
                overlay.Parent = null;
                overlay.Destroy();
            });
        }
    }
}
