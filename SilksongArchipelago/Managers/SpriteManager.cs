using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SilksongArchipelago.Managers
{
    /// <summary>
    /// Manages custom textures and sprites, such as the official Archipelago icon
    /// displayed for out-of-world items in in-game shops.
    /// </summary>
    public static class SpriteManager
    {
        private static Sprite? _apIconSprite;

        public static Sprite? ArchipelagoIconSprite
        {
            get
            {
                if (_apIconSprite == null)
                {
                    _apIconSprite = LoadEmbeddedSprite("SilksongArchipelago.Resources.ap_icon.png");
                }
                return _apIconSprite;
            }
        }

        private static Sprite? LoadEmbeddedSprite(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    SilksongArchipelagoPlugin.Log.LogWarning($"Embedded resource '{resourceName}' not found.");
                    return null;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(texture, buffer))
                {
                    return Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        600.0f
                    );
                }
            }
            catch (Exception ex)
            {
                SilksongArchipelagoPlugin.Log.LogError($"Failed to load embedded sprite '{resourceName}': {ex.Message}");
            }

            return null;
        }
    }
}
