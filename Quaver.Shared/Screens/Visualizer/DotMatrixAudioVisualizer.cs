using System;
using ManagedBass;
using Microsoft.Xna.Framework;
using Quaver.Shared.Audio;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Screens.Visualizer
{
    /// <summary>
    ///     Dot-matrix style audio visualizer.
    ///     Zero child sprites — draws directly via SpriteBatch for maximum performance.
    ///     Grid of square cells filled from bottom up per column, driven by FFT spectrum data.
    /// </summary>
    public class DotMatrixAudioVisualizer : Sprite
    {
        /// <summary>
        ///     Size of each square cell in pixels.
        /// </summary>
        private int CellSize { get; }

        /// <summary>
        ///     Cell + gap step size.
        /// </summary>
        private int Step { get; }

        /// <summary>
        ///     Number of columns in the grid.
        /// </summary>
        private int Columns { get; }

        /// <summary>
        ///     Number of rows in the grid.
        /// </summary>
        private int Rows { get; }

        /// <summary>
        ///     Filled row count per column. Updated every tick.
        /// </summary>
        private readonly int[] filledRows;

        /// <summary>
        ///     Smoothed amplitude per column.
        /// </summary>
        private readonly float[] smoothedAmplitudes;

        /// <summary>
        ///     Precomputed color for drawing dots.
        /// </summary>
        private Color dotColor;

        /// <summary>
        ///     FFT buffer.
        /// </summary>
        private readonly float[] spectrumData = new float[2048];

        /// <summary>
        ///     Timer for update throttle.
        /// </summary>
        private TimeSpan interpolationTimer = TimeSpan.Zero;

        /// <summary>
        ///     Update interval.
        /// </summary>
        private readonly TimeSpan updateInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>
        ///     Smoothing factor. Higher = more responsive.
        /// </summary>
        private const float SmoothingFactor = 0.35f;

        /// <param name="width">Total visualizer width in pixels.</param>
        /// <param name="height">Total visualizer height in pixels.</param>
        /// <param name="cellSize">Size of each dot square.</param>
        /// <param name="gap">Gap between dots.</param>
        public DotMatrixAudioVisualizer(int width, int height, int cellSize = 4, int gap = 2)
        {
            CellSize = cellSize;
            Step = cellSize + gap;
            Columns = width / Step;
            Rows = height / Step;

            Size = new ScalableVector2(width, height);
            Alpha = 0f; // Don't draw the parent sprite itself

            var skinColor = SkinManager.Skin.MusicVisualizer.MusicVisualizerColor;
            dotColor = skinColor * 0.85f; // Apply alpha

            filledRows = new int[Columns];
            smoothedAmplitudes = new float[Columns];
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            if (ConfigManager.DisplayMenuAudioVisualizer != null && ConfigManager.DisplayMenuAudioVisualizer.Value)
            {
                interpolationTimer += gameTime.ElapsedGameTime;

                if (interpolationTimer >= updateInterval)
                {
                    interpolationTimer = TimeSpan.Zero;
                    UpdateAmplitudes();
                }
            }

            base.Update(gameTime);
        }

        /// <inheritdoc />
        public override void Draw(GameTime gameTime)
        {
            if (ConfigManager.DisplayMenuAudioVisualizer != null && !ConfigManager.DisplayMenuAudioVisualizer.Value)
                return;

            // Let parent Sprite handle SpriteBatch state (Begin/End), but skip drawing parent image
            // by calling base.Draw which handles children + SpriteBatch setup
            base.Draw(gameTime);
        }

        /// <inheritdoc />
        public override void DrawToSpriteBatch()
        {
            if (!Visible)
                return;

            var spriteBatch = GameBase.Game.SpriteBatch;
            var pixel = WobbleAssets.WhiteBox;
            var baseX = ScreenRectangle.X;
            var baseY = ScreenRectangle.Y;
            var totalHeight = ScreenRectangle.Height;

            for (var x = 0; x < Columns; x++)
            {
                var filled = filledRows[x];
                if (filled <= 0)
                    continue;

                var pixelX = baseX + x * Step;

                for (var y = 0; y < filled; y++)
                {
                    var pixelY = baseY + totalHeight - (y + 1) * Step;

                    spriteBatch.Draw(pixel,
                        new Rectangle((int)pixelX, (int)pixelY, CellSize, CellSize),
                        dotColor);
                }
            }
        }

        /// <summary>
        ///     Sample FFT data and compute filled row counts per column.
        /// </summary>
        private void UpdateAmplitudes()
        {
            if (AudioEngine.Track == null || AudioEngine.Track.IsDisposed)
                return;

            if (AudioEngine.Track.IsPlaying)
                _ = Bass.ChannelGetData(AudioEngine.Track.Stream, spectrumData, (int)DataFlags.FFT2048);
            else
                Array.Clear(spectrumData);

            for (var x = 0; x < Columns; x++)
            {
                var rawAmplitude = GetAmplitudeForColumn(x);
                smoothedAmplitudes[x] += (rawAmplitude - smoothedAmplitudes[x]) * SmoothingFactor;
                filledRows[x] = (int)(smoothedAmplitudes[x] * Rows);
            }
        }

        /// <summary>
        ///     Maps column index to FFT bins mirrored from center.
        ///     Center columns = low freq (bass), outer columns = high freq.
        ///     Same approach as bar visualizer: Math.Abs(column - center).
        /// </summary>
        private float GetAmplitudeForColumn(int column)
        {
            var center = Columns / 2;
            var spectrumIndex = Math.Abs(column - center);

            if (spectrumIndex >= spectrumData.Length)
                return 0f;

            var value = spectrumData[spectrumIndex];
            return MathHelper.Clamp(value * 5.0f, 0f, 1f);
        }
    }
}
