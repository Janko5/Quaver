using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Graphics;
using Wobble;
using Wobble.Logging;
using Wobble.Graphics.Shaders;

namespace Quaver.Shared.Graphics.Shaders
{
    public static class ShaderManager
    {
        /// <summary>
        ///     Cached effects to avoid reloading them multiple times in parallel.
        ///     Key: resource path, Value: Effect instance.
        /// </summary>
        private static ConcurrentDictionary<string, Effect> Effects { get; } = new ConcurrentDictionary<string, Effect>();

        /// <summary>
        ///     Lock object to synchronize shader loading.
        /// </summary>
        private static object LoadLock { get; } = new object();

        /// <summary>
        ///     Loads an effect from resources or returns it from cache.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Effect? GetEffect(string path)
        {
            if (Effects.TryGetValue(path, out var effect))
                return effect;

            try
            {
                var bytecode = GameBase.Game.Resources.Get(path);
                if (bytecode == null)
                    throw new Exception($"Shader resource not found: {path}");

                lock (LoadLock)
                {
                    if (Effects.TryGetValue(path, out effect))
                        return effect;

                    effect = new Effect(GameBase.Game.GraphicsDevice, bytecode);

                    if (!Effects.TryAdd(path, effect))
                    {
                        effect.Dispose();
                        return Effects[path];
                    }
                }

                return effect;
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
                return null;
            }
        }
        /// <summary>
        ///     Creates a new Shader instance using a shared Effect.
        ///     The returned Shader will not dispose of the underlying Effect.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Shader? CreateShader(string path, Dictionary<string, object>? parameters = null)
        {
            var effect = GetEffect(path);
            if (effect == null)
                return null;

            return new Shader(effect, parameters ?? new Dictionary<string, object>())
            {
                OwnsShaderEffect = false
            };
        }
    }
}
