using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Objects;
using Quaver.Shared.Screens.Main;
using Wobble;
using Wobble.Input;
using Wobble.Screens;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Quaver.Shared.Skinning;
using Quaver.Shared.Assets;
using Quaver.Shared.Audio;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Backgrounds;
using ManagedBass;
using Wobble.Graphics.Animations;
using Wobble.Graphics.UI;
using Wobble.Managers;
using Quaver.Shared.Helpers;
using Wobble.Window;

namespace Quaver.Shared.Screens.Visualizer
{
    public sealed class VisualizerTestScreen : QuaverScreen
    {
        public override QuaverScreenType Type { get; } = QuaverScreenType.VisualizerTest;

        public VisualizerTestScreen()
        {
            View = new VisualizerTestScreenView(this);
        }

        public override void Update(GameTime gameTime)
        {
            HandleInput();
            base.Update(gameTime);
        }

        private void HandleInput()
        {
            if (Exiting)
                return;

            if (KeyboardManager.IsUniqueKeyPress(Keys.Escape))
            {
                Exit(() => new MainMenuScreen());
                return;
            }

            if (KeyboardManager.IsUniqueKeyPress(Keys.D0) || KeyboardManager.IsUniqueKeyPress(Keys.NumPad0))
            {
                ((VisualizerTestScreenView)View).SetVisualizerType(0);
            }
            else if (KeyboardManager.IsUniqueKeyPress(Keys.D1) || KeyboardManager.IsUniqueKeyPress(Keys.NumPad1))
            {
                ((VisualizerTestScreenView)View).SetVisualizerType(1);
            }
            else if (KeyboardManager.IsUniqueKeyPress(Keys.D2) || KeyboardManager.IsUniqueKeyPress(Keys.NumPad2))
            {
                ((VisualizerTestScreenView)View).SetVisualizerType(2);
            }
            else if (KeyboardManager.IsUniqueKeyPress(Keys.D3) || KeyboardManager.IsUniqueKeyPress(Keys.NumPad3))
            {
                ((VisualizerTestScreenView)View).SetVisualizerType(3);
            }
        }

        public override UserClientStatus GetClientStatus() => new UserClientStatus(ClientStatus.InMenus, -1, "-1", 1, "", 0);
    }

    public class VisualizerTestScreenView : ScreenView
    {
        private BackgroundImage Background { get; set; }
        private FullScreenAudioVisualizer Visualizer { get; set; }
        private DotMatrixAudioVisualizer DotMatrixVisualizer { get; set; }
        private WaveAudioVisualizer WaveVisualizer { get; set; }
        private SpriteTextPlus ScreenTitle { get; set; }

        public VisualizerTestScreenView(VisualizerTestScreen screen) : base(screen)
        {
            CreateBackground();
            CreateScreenTitle();
            CreateVisualizer();
        }

        private void CreateBackground() => Background = new BackgroundImage(UserInterface.UniversalBackground, 0, false)
        { Parent = Container };

        private void CreateScreenTitle()
        {
            ScreenTitle = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.LatoBlack),
                "Music Visualizer - [0] Off  [1] Bars  [2] DotMatrix  [3] Wave  [Esc] Exit", 24)
            {
                Parent = Container,
                Alignment = Alignment.TopCenter,
                Y = 40
            };
        }

        private void CreateVisualizer()
        {
            var width = (int)WindowManager.Width;
            var height = (int)WindowManager.Height / 2;
            var numBars = width / (3 + 4);

            Visualizer = new FullScreenAudioVisualizer(width, height, numBars, 3, 4)
            {
                Parent = Container,
                Alignment = Alignment.BotCenter,
                Y = 0
            };
        }

        public void SetVisualizerType(int type)
        {
            if (Visualizer != null)
            {
                Visualizer.Destroy();
                Visualizer = null;
            }

            if (DotMatrixVisualizer != null)
            {
                DotMatrixVisualizer.Destroy();
                DotMatrixVisualizer = null;
            }

            if (WaveVisualizer != null)
            {
                WaveVisualizer.Destroy();
                WaveVisualizer = null;
            }

            var width = (int)WindowManager.Width;
            var height = (int)WindowManager.Height / 2;

            switch (type)
            {
                case 0:
                    // No visualizer (off)
                    break;
                case 1:
                    var numBars = width / (3 + 4);
                    Visualizer = new FullScreenAudioVisualizer(width, height, numBars, 3, 4)
                    {
                        Parent = Container,
                        Alignment = Alignment.BotCenter,
                        Y = 0
                    };
                    break;
                case 2:
                    DotMatrixVisualizer = new DotMatrixAudioVisualizer(width, height, 4, 2)
                    {
                        Parent = Container,
                        Alignment = Alignment.BotCenter,
                        Y = 0
                    };
                    break;
                case 3:
                    WaveVisualizer = new WaveAudioVisualizer(width, height, 8)
                    {
                        Parent = Container,
                        Alignment = Alignment.BotCenter,
                        Y = 0
                    };
                    break;
            }
        }

        public override void Update(GameTime gameTime) => Container?.Update(gameTime);

        public override void Draw(GameTime gameTime)
        {
            GameBase.Game.GraphicsDevice.Clear(ColorHelper.HexToColor("#151515"));
            Container?.Draw(gameTime);
        }

        public override void Destroy()
        {
            Container?.Destroy();
        }
    }

    public class FullScreenAudioVisualizer : Container
    {
        public List<Sprite> Bars { get; }
        public int MaxBarHeight { get; }

        private TimeSpan interpolationTimer = TimeSpan.Zero;
        private readonly TimeSpan barInterpolateInterval = TimeSpan.FromMilliseconds(50);
        private readonly float[] spectrumData = new float[2048];

        public FullScreenAudioVisualizer(int width, int maxHeight, int numBars, int barWidth, int spacing = 5)
        {
            MaxBarHeight = maxHeight;

            Size = new ScalableVector2(width, maxHeight);

            Bars = new List<Sprite>();

            for (var i = 0; i < numBars; i++)
            {
                var bar = new Sprite()
                {
                    Parent = this,
                    Alignment = Alignment.BotLeft,
                    Tint = SkinManager.Skin.MusicVisualizer.MusicVisualizerColor,
                    Width = barWidth,
                    X = barWidth * i + i * spacing,
                    Alpha = 0.8f
                };

                Bars.Add(bar);
            }
        }

        public override void Update(GameTime gameTime)
        {
            interpolationTimer += gameTime.ElapsedGameTime;
            if (interpolationTimer >= barInterpolateInterval)
            {
                interpolationTimer = TimeSpan.Zero;
                InterpolateBars();
            }

            base.Update(gameTime);
        }

        private void InterpolateBars()
        {
            if (AudioEngine.Track == null || AudioEngine.Track.IsDisposed)
                return;

            if (AudioEngine.Track.IsPlaying)
                _ = Bass.ChannelGetData(AudioEngine.Track.Stream, spectrumData, (int)DataFlags.FFT2048);
            else
                Array.Clear(spectrumData);

            var center = Bars.Count / 2;

            for (var i = 0; i < Bars.Count; i++)
            {
                var bar = Bars[i];
                var spectrumIndex = Math.Abs(i - center);
                var targetHeight = MathHelper.Clamp(spectrumData[spectrumIndex] * MaxBarHeight * 1.75f, 0, MaxBarHeight);

                bar.Visible = targetHeight > 1f;

                if (!bar.Visible)
                    continue;

                lock (bar.Animations)
                {
                    bar.Animations.Clear();
                    bar.Animations.Add(new Animation(AnimationProperty.Height, Easing.Linear,
                        bar.Height, targetHeight, (float)barInterpolateInterval.TotalMilliseconds));
                }
            }
        }
    }
}
