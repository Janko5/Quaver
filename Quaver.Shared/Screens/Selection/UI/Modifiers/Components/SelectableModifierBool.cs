using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Quaver.Shared.Graphics.Components;
using Wobble.Assets;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Selection.UI.Modifiers.Components
{
    public class SelectableModifierBool : SelectableModifier
    {
        private IconButton OnOffButton;

        private ModifierSwitch Switch;

        private Texture2D Texture => ModManager.IsActivated(Mod.ModIdentifier) ? UserInterface.On : UserInterface.Off;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="mod"></param>
        public SelectableModifierBool(int width, IGameplayModifier mod) : base(width, mod)
        {
            ModManager.ModsChanged += OnModsChanged;
        }

        protected override void SetupV1Layout(int width)
        {
            base.SetupV1Layout(width);

            OnOffButton = new IconButton(Texture, (sender, args) =>
            {
                if (!CanActivateMultiplayerMod())
                    return;

                if (ModManager.IsActivated(Mod.ModIdentifier))
                    ModManager.RemoveMod(Mod.ModIdentifier, true);
                else
                    ModManager.AddMod(Mod.ModIdentifier, true);
            })
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                Size = new ScalableVector2(78, 23),
                X = -Padding - 2, // Zachowanie lekkiego przesunięcia V1
                UsePreviousSpriteBatchOptions = true,
            };
        }

        protected override void SetupV2Layout()
        {
            base.SetupV2Layout();

            Switch = new ModifierSwitch(ModManager.IsActivated(Mod.ModIdentifier), isOn =>
            {
                if (!CanActivateMultiplayerMod())
                    return;

                if (isOn)
                    ModManager.AddMod(Mod.ModIdentifier, true);
                else
                    ModManager.RemoveMod(Mod.ModIdentifier, true);
            })
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -Padding,
                UsePreviousSpriteBatchOptions = true,
            };
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (IsV2 && Switch != null)
            {
                Switch.Visible = true;
                Switch.Alpha = CanActivateMultiplayerMod() ? 1f : 0.60f;
            }
            else if (OnOffButton != null)
            {
                OnOffButton.Visible = true;
                OnOffButton.IsPerformingFadeAnimations = CanActivateMultiplayerMod();

                if (!OnOffButton.IsPerformingFadeAnimations)
                    OnOffButton.Alpha = Name.Alpha;
            }

            base.Update(gameTime);
        }


        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            ModManager.ModsChanged -= OnModsChanged;
            base.Destroy();
        }

        private void OnModsChanged(object sender, ModsChangedEventArgs e)
        {
            ScheduleUpdate(() =>
            {
                if (IsV2)
                {
                    if (Switch.IsOn != ModManager.IsActivated(Mod.ModIdentifier))
                        Switch.Toggle();
                }
                else
                {
                    OnOffButton.Image = Texture;
                }
            });
        }
    }
}