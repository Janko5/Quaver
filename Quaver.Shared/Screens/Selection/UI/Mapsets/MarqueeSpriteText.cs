using System;
using Microsoft.Xna.Framework;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites.Text;

namespace Quaver.Shared.Screens.Selection.UI.Mapsets
{
    public class MarqueeSpriteText : HorizontalClippingContainer
    {
        public SpriteTextPlus TextSprite { get; }

        public bool IsActive { get; set; }

        private double _timer;
        private MarqueeState _state = MarqueeState.WaitingStart;

        private float _scrollX;
        private const float ScrollSpeed = 0.05f; // Pixels per ms

        public MarqueeSpriteText(WobbleFontStore font, string text, int fontSize, float width)
        {
            Size = new ScalableVector2(width, font.Store.LineHeight * (fontSize / (float)font.DefaultSize));

            TextSprite = new SpriteTextPlus(font, text, fontSize)
            {
                Parent = this,
                UsePreviousSpriteBatchOptions = true
            };

            // Recalculate height based on actual measurement
            Size = new ScalableVector2(width, TextSprite.Height);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsActive || TextSprite.Width <= Size.X.Value)
            {
                Reset();
                return;
            }

            var dt = gameTime.ElapsedGameTime.TotalMilliseconds;
            var maxScroll = TextSprite.Width - Size.X.Value;

            switch (_state)
            {
                case MarqueeState.WaitingStart:
                    _timer += dt;
                    if (_timer >= 2000)
                    {
                        _timer = 0;
                        _state = MarqueeState.ScrollingLeft;
                    }
                    break;

                case MarqueeState.ScrollingLeft:
                    _scrollX += (float)(ScrollSpeed * dt);
                    if (_scrollX >= maxScroll)
                    {
                        _scrollX = maxScroll;
                        _state = MarqueeState.WaitingEnd;
                    }
                    TextSprite.X = -_scrollX;
                    break;

                case MarqueeState.WaitingEnd:
                    _timer += dt;
                    if (_timer >= 2000)
                    {
                        _timer = 0;
                        _state = MarqueeState.ScrollingRight;
                    }
                    break;

                case MarqueeState.ScrollingRight:
                    _scrollX -= (float)(ScrollSpeed * 2 * dt); // Return faster
                    if (_scrollX <= 0)
                    {
                        _scrollX = 0;
                        _state = MarqueeState.WaitingStart;
                    }
                    TextSprite.X = -_scrollX;
                    break;
            }
        }

        private void Reset()
        {
            _scrollX = 0;
            _timer = 0;
            _state = MarqueeState.WaitingStart;
            TextSprite.X = 0;
        }

        private enum MarqueeState
        {
            WaitingStart,
            ScrollingLeft,
            WaitingEnd,
            ScrollingRight
        }
    }
}
