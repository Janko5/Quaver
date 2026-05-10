using System;
using ManagedBass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    ///     Wave/area audio visualizer (type 3).
    ///     Smooth FFT profile rendered into a cached low-resolution texture.
    ///     Line thickness and glow scale with visualizer height.
    /// </summary>
    public class WaveAudioVisualizer : Sprite
    {
        private const int FftBufferSize = 4096;
        private const int FftResultBins = 1024;
        private const int VisualFftBins = 260;
        private const int TargetFrameMs = 33;
        private const int RenderColumnSpacing = 1;

        private readonly float[] fftData = new float[FftBufferSize];
        private readonly float[] lineAmplitudes;
        private readonly float[] shapedAmplitudes;
        private readonly float[] smoothBufferA;
        private readonly float[] smoothBufferB;
        private float adaptiveRangeMin;
        private float adaptiveRangeMax = 1f;

        private Color fillTopColor;
        private Color fillBottomColor;
        private Color glowColor;
        private Color glowHaloColor;

        private readonly TimeSpan redrawInterval = TimeSpan.FromMilliseconds(TargetFrameMs);
        private TimeSpan redrawTimer = TimeSpan.Zero;

        private RenderTarget2D renderTarget;
        private Texture2D fillGradientTexture;
        private readonly Sprite cachedWaveform;
        private Rectangle drawRect;
        private Rectangle glowRect;
        private bool needsRedraw = true;
        private bool redrawQueued;

        // Scaled line dimensions based on height
        private int CoreLineThickness { get; }
        private int HaloPadding { get; }

        /// <param name="width">Total visualizer width in pixels.</param>
        /// <param name="height">Total visualizer height in pixels.</param>
        /// <param name="controlPointSpacing">Unused legacy spacing parameter kept for skin/call-site compatibility.</param>
        public WaveAudioVisualizer(int width, int height, int controlPointSpacing = 8)
        {
            var renderColumns = Math.Max(1, width / RenderColumnSpacing);

            CoreLineThickness = Math.Max(1, height / 130);
            HaloPadding = Math.Max(1, height / 75);

            Size = new ScalableVector2(width, height);
            Alpha = 0f;

            var skinColor = SkinManager.Skin.MusicVisualizer.MusicVisualizerColor;

            fillTopColor = skinColor * 0.32f;
            fillBottomColor = skinColor * 0.78f;
            glowColor = skinColor;
            glowHaloColor = skinColor * 0.28f;

            lineAmplitudes = new float[renderColumns];
            shapedAmplitudes = new float[renderColumns];
            smoothBufferA = new float[renderColumns];
            smoothBufferB = new float[renderColumns];

            renderTarget = new RenderTarget2D(
                GameBase.Game.GraphicsDevice,
                renderColumns,
                Math.Max(1, height),
                false,
                GameBase.Game.GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.None);
            fillGradientTexture = CreateVerticalGradientTexture(Math.Max(1, height));

            cachedWaveform = new Sprite
            {
                Parent = this,
                Image = renderTarget,
                Size = new ScalableVector2(width, height),
                Alpha = 1f
            };
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {
            if (ConfigManager.DisplayMenuAudioVisualizer != null && ConfigManager.DisplayMenuAudioVisualizer.Value)
            {
                if (needsRedraw)
                    QueueRedraw();

                redrawTimer += gameTime.ElapsedGameTime;

                if (redrawTimer >= redrawInterval)
                {
                    redrawTimer = TimeSpan.Zero;
                    UpdateWaveform();
                    needsRedraw = true;
                    QueueRedraw();
                }
            }

            base.Update(gameTime);
        }

        /// <inheritdoc />
        public override void Draw(GameTime gameTime)
        {
            if (ConfigManager.DisplayMenuAudioVisualizer != null && !ConfigManager.DisplayMenuAudioVisualizer.Value)
                return;

            base.Draw(gameTime);
        }

        /// <inheritdoc />
        public override void DrawToSpriteBatch()
        {
            if (!Visible)
                return;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            if (renderTarget != null && !renderTarget.IsDisposed)
                renderTarget.Dispose();

            if (fillGradientTexture != null && !fillGradientTexture.IsDisposed)
                fillGradientTexture.Dispose();

            renderTarget = null;
            fillGradientTexture = null;
            base.Destroy();
        }

        private void QueueRedraw()
        {
            if (redrawQueued || renderTarget == null || renderTarget.IsDisposed)
                return;

            redrawQueued = true;
            GameBase.Game.ScheduledRenderTargetDraws.Add(RedrawRenderTarget);
        }

        private void RedrawRenderTarget()
        {
            redrawQueued = false;

            if (renderTarget == null || renderTarget.IsDisposed)
                return;

            needsRedraw = false;

            var graphicsDevice = GameBase.Game.GraphicsDevice;
            var spriteBatch = GameBase.Game.SpriteBatch;
            var previousTargets = graphicsDevice.GetRenderTargets();
            var batchStarted = false;

            try
            {
                GameBase.Game.TryEndBatch();
                GameBase.DefaultSpriteBatchInUse = false;

                graphicsDevice.SetRenderTarget(renderTarget);
                graphicsDevice.Clear(Color.Transparent);

                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied,
                    SamplerState.PointClamp,
                    null,
                    RasterizerState.CullNone,
                    null,
                    null);
                batchStarted = true;

                var pixel = WobbleAssets.WhiteBox;
                var baseline = renderTarget.Height;
                const int topPadding = 0;
                var amplitudeScale = Math.Max(1, baseline - topPadding);
                var prevWaveTop = baseline;

                for (var x = 0; x < shapedAmplitudes.Length; x++)
                {
                    var amplitude = MathHelper.Clamp(shapedAmplitudes[x], 0f, 1f);
                    var columnHeight = (int)Math.Round(amplitude * amplitudeScale);
                    var waveTop = MathHelper.Clamp(baseline - columnHeight, topPadding, baseline);

                    drawRect.X = x;
                    drawRect.Y = waveTop;
                    drawRect.Width = 1;
                    drawRect.Height = baseline - waveTop;
                    if (drawRect.Height > 0)
                    {
                        if (fillGradientTexture != null && !fillGradientTexture.IsDisposed)
                        {
                            var src = new Rectangle(0, waveTop, 1, drawRect.Height);
                            spriteBatch.Draw(fillGradientTexture, drawRect, src, Color.White);
                        }
                        else
                        {
                        spriteBatch.Draw(pixel, drawRect, fillBottomColor);
                        }
                    }

                    var minTop = Math.Min(waveTop, prevWaveTop);
                    var maxTop = Math.Max(waveTop, prevWaveTop);

                    glowRect.X = x;
                    glowRect.Y = Math.Max(0, minTop - HaloPadding);
                    glowRect.Width = 1;
                    var glowBottom = Math.Min(baseline, maxTop + CoreLineThickness + HaloPadding);
                    glowRect.Height = Math.Max(1, glowBottom - glowRect.Y);
                    spriteBatch.Draw(pixel, glowRect, glowHaloColor);

                    drawRect.Y = Math.Max(0, minTop - CoreLineThickness / 2);
                    var lineBottom = Math.Min(baseline, maxTop + CoreLineThickness / 2 + 1);
                    drawRect.Height = Math.Max(1, lineBottom - drawRect.Y);
                    spriteBatch.Draw(pixel, drawRect, glowColor);

                    prevWaveTop = waveTop;
                }

                spriteBatch.End();
                batchStarted = false;
                cachedWaveform.Image = renderTarget;
            }
            finally
            {
                if (batchStarted)
                    spriteBatch.End();

                if (previousTargets.Length > 0)
                    graphicsDevice.SetRenderTargets(previousTargets);
                else
                    graphicsDevice.SetRenderTarget(null);

                GameBase.DefaultSpriteBatchInUse = false;
            }
        }

        private void UpdateWaveform()
        {
            if (AudioEngine.Track == null || AudioEngine.Track.IsDisposed)
            {
                Array.Clear(lineAmplitudes, 0, lineAmplitudes.Length);
                Array.Clear(shapedAmplitudes, 0, shapedAmplitudes.Length);
                needsRedraw = true;
                QueueRedraw();
                return;
            }

            if (!AudioEngine.Track.IsPlaying)
            {
                // Smooth fade-out instead of instant clear.
                var anyActive = false;
                for (var i = 0; i < lineAmplitudes.Length; i++)
                {
                    lineAmplitudes[i] *= 0.88f;
                    if (lineAmplitudes[i] > 0.001f)
                        anyActive = true;
                    else
                        lineAmplitudes[i] = 0f;
                }

                if (anyActive)
                {
                    ShapeLineProfile();
                    needsRedraw = true;
                    QueueRedraw();
                }
                else
                {
                    Array.Clear(shapedAmplitudes, 0, shapedAmplitudes.Length);
                }

                return;
            }

            _ = Bass.ChannelGetData(AudioEngine.Track.Stream, fftData, (int)DataFlags.FFT2048);

            var usableBins = Math.Min(FftResultBins, VisualFftBins);
            var columns = lineAmplitudes.Length;

            for (var x = 0; x < columns; x++)
            {
                var t = columns > 1 ? x / (columns - 1f) : 0f;

                // Non-linear mapping keeps low/mid details where the profile "shape" is.
                var binPos = 1f + (float)Math.Pow(t, 1.35f) * (usableBins - 2);
                var bin0 = (int)binPos;
                var bin1 = Math.Min(usableBins - 1, bin0 + 1);
                var frac = binPos - bin0;

                var sample = MathHelper.Lerp(fftData[bin0], fftData[bin1], frac);
                var left = fftData[Math.Max(1, bin0 - 2)];
                var right = fftData[Math.Min(usableBins - 1, bin0 + 2)];

                var band = sample * 0.52f + Math.Max(left, right) * 0.48f;
                var amplitude = (float)Math.Sqrt(MathHelper.Clamp(band * 12.5f, 0f, 1f));

                // Reduce low-frequency dominance on left side.
                var lowBandCompensation = 0.72f + 0.28f * SmoothStep01(t / 0.38f);
                amplitude *= lowBandCompensation;

                var edgeEnvelope = SmoothStep01(t / 0.052f) * SmoothStep01((1f - t) / 0.052f);
                amplitude *= edgeEnvelope;

                var smoothing = amplitude > lineAmplitudes[x] ? 0.22f : 0.12f;
                lineAmplitudes[x] += (amplitude - lineAmplitudes[x]) * smoothing;
            }

            ShapeLineProfile();
        }

        private void ShapeLineProfile()
        {
            // Macro shape: broad hills.
            SmoothProfile(lineAmplitudes, smoothBufferA, 6);

            // Micro shape: visible waviness.
            SmoothProfile(lineAmplitudes, smoothBufferB, 3);

            var frameMin = float.MaxValue;
            var frameMax = 0f;
            for (var i = 0; i < lineAmplitudes.Length; i++)
            {
                var t = lineAmplitudes.Length > 1 ? i / (lineAmplitudes.Length - 1f) : 0f;
                var fadeIn = SmoothStep01(t / 0.07f);
                var fadeOut = SmoothStep01((1f - t) / 0.07f);
                var edgeFade = fadeIn * fadeOut;

                var detail = Math.Max(0f, smoothBufferB[i] - smoothBufferA[i]);
                var shaped = smoothBufferA[i] * 0.72f + smoothBufferB[i] * 0.28f + detail * 0.22f;
                shaped = MathHelper.Clamp(shaped * edgeFade, 0f, 1.4f);
                shapedAmplitudes[i] = shaped;
            }

            // Final short smoothing pass removes "teeth" while keeping wave structures.
            SmoothProfile(shapedAmplitudes, smoothBufferA, 2);

            for (var i = 0; i < shapedAmplitudes.Length; i++)
            {
                shapedAmplitudes[i] = smoothBufferA[i];
                frameMin = Math.Min(frameMin, smoothBufferA[i]);
                frameMax = Math.Max(frameMax, smoothBufferA[i]);
            }

            if (frameMax > frameMin + 0.0001f)
            {
                var crest = frameMax - frameMin;
                var targetMin = frameMin + crest * 0.01f;
                var targetMax = frameMin + crest * 1.18f;

                // Floor rises slowly during busy passages, drops quickly during quiet ones.
                var minBlend = targetMin > adaptiveRangeMin ? 0.06f : 0.20f;
                adaptiveRangeMin += (targetMin - adaptiveRangeMin) * minBlend;
                adaptiveRangeMax += (targetMax - adaptiveRangeMax) * 0.15f;

                if (adaptiveRangeMax < adaptiveRangeMin + 0.0001f)
                    adaptiveRangeMax = adaptiveRangeMin + 0.0001f;

                var den = adaptiveRangeMax - adaptiveRangeMin;
                var framePeak = 0f;

                for (var i = 0; i < shapedAmplitudes.Length; i++)
                {
                    var normalized = (shapedAmplitudes[i] - adaptiveRangeMin) / den;
                    normalized = Math.Max(0f, normalized);

                    // Push lower amplitudes down, but keep natural peaks.
                    normalized = (float)Math.Pow(normalized, 1.85f);

                    // Reduce peak reach so only truly strong hits approach top.
                    if (normalized > 0.82f)
                        normalized = 0.82f + (normalized - 0.82f) * 0.60f;

                    shapedAmplitudes[i] = normalized * 0.96f;
                    framePeak = Math.Max(framePeak, shapedAmplitudes[i]);
                }

                // No hard top clipping: only scale down if frame exceeds target.
                // Never scale up, so average hits do not reach max height.
                if (framePeak > 0.0001f)
                {
                    const float targetPeak = 0.985f;
                    var peakScale = framePeak > targetPeak ? targetPeak / framePeak : 1f;

                    for (var i = 0; i < shapedAmplitudes.Length; i++)
                        shapedAmplitudes[i] = MathHelper.Clamp(shapedAmplitudes[i] * peakScale, 0f, targetPeak);
                }
            }
            else
            {
                Array.Clear(shapedAmplitudes, 0, shapedAmplitudes.Length);
            }
        }

        private static float SmoothStep01(float x)
        {
            x = MathHelper.Clamp(x, 0f, 1f);
            return x * x * (3f - 2f * x);
        }

        private Texture2D CreateVerticalGradientTexture(int height)
        {
            var texture = new Texture2D(GameBase.Game.GraphicsDevice, 1, height);
            var pixels = new Color[height];
            var inv = Math.Max(1, height - 1);

            for (var y = 0; y < height; y++)
            {
                var t = y / (float)inv;
                pixels[y] = Color.Lerp(fillTopColor, fillBottomColor, t);
            }

            texture.SetData(pixels);
            return texture;
        }

        private void SmoothProfile(float[] source, float[] destination, int radius)
        {
            if (radius <= 0)
            {
                Array.Copy(source, destination, source.Length);
                return;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var start = Math.Max(0, i - radius);
                var end = Math.Min(source.Length - 1, i + radius);
                var count = end - start + 1;

                var sum = 0f;
                for (var j = start; j <= end; j++)
                    sum += source[j];

                destination[i] = sum / count;
            }
        }

    }
}
