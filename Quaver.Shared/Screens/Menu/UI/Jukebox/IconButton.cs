/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Skinning;
using Wobble.Graphics.Animations;
using Wobble.Graphics.UI.Buttons;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Menu.UI.Jukebox
{
    public class IconButton : ImageButton
    {
        /// <summary>
        /// </summary>
        public bool IsPerformingFadeAnimations { get; set; } = true;

        public IconButton(Texture2D image, EventHandler clickAction = null) : base(image, clickAction)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            var dt = gameTime.ElapsedGameTime.TotalMilliseconds;

            // Failsafe: verify if the mouse is actually over the button.
            // This fixes "sticky hover" issues when Wobble fails to emit LeftHover events.
            var isActuallyHovered = IsHovered && ScreenRectangle.Contains(MouseManager.CurrentState.Position);

            if (IsPerformingFadeAnimations)
                Alpha = MathHelper.Lerp(Alpha, isActuallyHovered ? 0.75f : 1, (float) Math.Min(dt / 60, 1));

            base.Update(gameTime);
        }
    }
}
