/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Screens.Gameplay.UI
{
    /// <summary>
    ///     Sprite that displays whether the player hit too fast or too slow.
    /// </summary>
    public class HitTimingIndicator : Sprite
    {
        /// <summary>
        ///     Reference to the gameplay screen.
        /// </summary>
        private GameplayScreen Screen { get; }

        /// <summary>
        ///     Texture shown when the player hits too slow.
        /// </summary>
        private Texture2D SlowTexture { get; }

        /// <summary>
        ///     Texture shown when the player hits too fast.
        /// </summary>
        private Texture2D FastTexture { get; }

        /// <summary>
        ///     The original size of the indicator.
        /// </summary>
        private Vector2 OriginalSize { get; }

        /// <summary>
        ///     The original Y position of the indicator.
        /// </summary>
        public float OriginalPosY { get; set; }

        /// <summary>
        ///     If we are currently animating the indicator.
        /// </summary>
        public bool IsAnimating { get; private set; }

        /// <summary>
        ///     Timer for bumping the indicator.
        /// </summary>
        private readonly CountdownTimer bumpTimer;

        /// <summary>
        ///     Time to bump.
        /// </summary>
        private readonly TimeSpan bumpTime;

        /// <summary>
        ///     Start Y of bumping.
        /// </summary>
        private float bumpY;

        /// <summary>
        ///     Fade time constant (matches judgement fade time).
        /// </summary>
        private const int FadeTime = 240;

        private SkinKeys Skin => SkinManager.Skin.Keys[Screen.Map.Mode];

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="slow">Texture for late hits</param>
        /// <param name="fast">Texture for early hits</param>
        /// <param name="size"></param>
        /// <param name="posY"></param>
        public HitTimingIndicator(GameplayScreen screen, Texture2D slow, Texture2D fast, Vector2 size, float posY)
        {
            Screen = screen;
            SlowTexture = slow;
            FastTexture = fast;
            OriginalPosY = posY;
            OriginalSize = size;
            Size = new ScalableVector2(OriginalSize.X, OriginalSize.Y);
            Y = OriginalPosY;
            Visible = false;
            Image = SlowTexture;

            bumpTime = TimeSpan.FromMilliseconds(Skin.HitTimingBumpTime);
            bumpTimer = new CountdownTimer(bumpTime);
            bumpTimer.TimeRemainingChanged += LerpY;
            bumpTimer.Stopped += LerpY;
        }

        private void LerpY(object sender, EventArgs e)
        {
            var t = 1 - bumpTimer.TimeRemaining / bumpTime;
            Y = EasingFunctions.EaseOutExpo(bumpY, OriginalPosY, (float)t);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            PerformAnimation(gameTime);
            base.Update(gameTime);
            bumpTimer.Update(gameTime);
        }

        /// <summary>
        ///     Performs the hit timing animation based on hit difference.
        ///     Positive hitDifference = early (too fast), Negative = late (too slow).
        /// </summary>
        /// <param name="hitDifference">The time difference in ms (StartTime - CurrentTime)</param>
        public void PerformHitTimingAnimation(int hitDifference)
        {
            // Don't show anything for perfect timing or if below threshold
            var threshold = Skin.HitTimingThreshold;
            if (Math.Abs(hitDifference) <= threshold)
                return;

            // Set the appropriate texture
            if (hitDifference > 0)
                Image = FastTexture;  // Early hit - player was too fast
            else
                Image = SlowTexture;  // Late hit - player was too slow

            Visible = true;
            Alpha = 1;

            bumpY = OriginalPosY + Skin.HitTimingBumpY;
            bumpTimer.Restart();
            IsAnimating = true;

            // Scale based on skin settings
            var scale = Skin.HitTimingScale;
            var (x, y) = new Vector2(Image.Width, Image.Height) * scale;
            Size = new ScalableVector2(x, y);
        }

        /// <summary>
        ///     Performs the fade-out animation.
        /// </summary>
        /// <param name="gameTime"></param>
        private void PerformAnimation(GameTime gameTime)
        {
            if (!IsAnimating)
                return;

            var dt = gameTime.ElapsedGameTime.TotalMilliseconds;

            // Tween the alpha if bump is completed
            if (bumpTimer.State == TimerState.Completed)
            {
                Alpha = MathHelper.Lerp(Alpha, 0, (float)Math.Min(dt / FadeTime, 1));

                if (Alpha <= 0)
                {
                    IsAnimating = false;
                    Visible = false;
                }
            }
        }
    }
}
