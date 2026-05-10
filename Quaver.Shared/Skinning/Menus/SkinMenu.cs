using System;
using System.IO;
using IniFileParser.Model;
using Microsoft.Xna.Framework.Graphics;
using Wobble.Assets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
namespace Quaver.Shared.Skinning.Menus
{
    public abstract class SkinMenu
    {
        protected SkinStore Store { get; }

        protected IniData Config { get; }

        public SkinMenu(SkinStore store, IniData config)
        {
            Store = store;
            Config = config;

            LoadAll();
        }

        private void LoadAll()
        {
            if (Config != null)
                ReadConfig();

            LoadElements();
        }

        protected abstract void ReadConfig();

        protected abstract void LoadElements();

        protected void ReadIndividualConfig(string value, Action load)
        {
            if (value == null)
                return;

            load?.Invoke();
        }

        public Texture2D LoadSkinElement(string folder, string file)
        {
            try
            {
                var path = $"{Store.Dir}/{folder}/{file}";
                return File.Exists(path) ? AssetLoader.LoadTexture2DFromFile(path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Texture2D LoadAndResizeSkinElement(string folder, string file, int width, int height)
        {
            try
            {
                var path = $"{Store.Dir}/{folder}/{file}";
                if (!File.Exists(path))
                    return null;

                using (var image = Image.Load<Rgba32>(path))
                {
                    if (image.Width == width && image.Height == height)
                        return AssetLoader.LoadTexture2DFromFile(path);

                    image.Mutate(x => x.Resize(width, height));

                    using (var ms = new MemoryStream())
                    {
                        image.SaveAsPng(ms);
                        ms.Position = 0;
                        return AssetLoader.LoadTexture2D(ms);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
