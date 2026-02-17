using UnityEngine;
using UnityEngine.UI;
using PhotoshopFile;

namespace PsdLayoutTool
{
    /// <summary>
    /// Applies layer effects to Unity GameObjects and components
    /// </summary>
    public static class LayerEffectsApplicator
    {
        /// <summary>
        /// Applies layer effects to a SpriteRenderer component
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to apply effects to</param>
        /// <param name="layer">The PSD layer containing effect data</param>
        /// <param name="pixelsToUnits">The pixels to Unity units conversion factor</param>
        public static void ApplyEffectsToSprite(SpriteRenderer spriteRenderer, Layer layer, float pixelsToUnits)
        {
            if (!layer.HasEffects)
                return;

            // Parse layer effects
            var layerEffects = layer.LayerEffects ?? LayerEffectsParser.ParseLayerEffects(layer);
            layer.LayerEffects = layerEffects;

            if (!layerEffects.HasAnyEffects)
                return;

            GameObject gameObject = spriteRenderer.gameObject;

            // Apply each effect
            foreach (var effect in layerEffects.Effects)
            {
                if (!effect.Enabled)
                    continue;

                switch (effect.Type)
                {
                    case LayerEffectType.DropShadow:
                        ApplyDropShadow(gameObject, effect as DropShadowEffect, pixelsToUnits);
                        break;
                    case LayerEffectType.OuterGlow:
                        ApplyOuterGlow(spriteRenderer, effect as OuterGlowEffect);
                        break;
                    case LayerEffectType.Stroke:
                        ApplyStroke(spriteRenderer, effect as StrokeEffect);
                        break;
                    case LayerEffectType.ColorOverlay:
                        ApplyColorOverlay(spriteRenderer, effect as ColorOverlayEffect);
                        break;
                    case LayerEffectType.GradientOverlay:
                        ApplyGradientOverlay(spriteRenderer, effect as GradientOverlayEffect);
                        break;
                }
            }
        }

        /// <summary>
        /// Applies layer effects to a UI Image component
        /// </summary>
        /// <param name="image">The UI Image to apply effects to</param>
        /// <param name="layer">The PSD layer containing effect data</param>
        /// <param name="pixelsToUnits">The pixels to Unity units conversion factor</param>
        public static void ApplyEffectsToUI(Image image, Layer layer, float pixelsToUnits)
        {
            if (!layer.HasEffects)
                return;

            // Parse layer effects
            var layerEffects = layer.LayerEffects ?? LayerEffectsParser.ParseLayerEffects(layer);
            layer.LayerEffects = layerEffects;

            if (!layerEffects.HasAnyEffects)
                return;

            // For UI elements, we have more limited options
            // Most effects will fall back to color modifications or shadow components
            foreach (var effect in layerEffects.Effects)
            {
                if (!effect.Enabled)
                    continue;

                switch (effect.Type)
                {
                    case LayerEffectType.DropShadow:
                        ApplyDropShadowToUI(image, effect as DropShadowEffect, pixelsToUnits);
                        break;
                    case LayerEffectType.ColorOverlay:
                        ApplyColorOverlayToUI(image, effect as ColorOverlayEffect);
                        break;
                    case LayerEffectType.Stroke:
                        ApplyStrokeToUI(image, effect as StrokeEffect);
                        break;
                }
            }
        }

        #region Sprite Effect Application

        /// <summary>
        /// Applies drop shadow effect to a sprite by creating a shadow GameObject
        /// </summary>
        private static void ApplyDropShadow(GameObject originalObject, DropShadowEffect effect, float pixelsToUnits)
        {
            if (effect == null) return;

            // Create shadow GameObject
            GameObject shadowObject = new GameObject(originalObject.name + "_Shadow");
            shadowObject.transform.SetParent(originalObject.transform.parent);
            shadowObject.transform.SetSiblingIndex(originalObject.transform.GetSiblingIndex());

            // Copy sprite renderer
            SpriteRenderer originalRenderer = originalObject.GetComponent<SpriteRenderer>();
            SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = originalRenderer.sprite;
            shadowRenderer.sortingLayerName = originalRenderer.sortingLayerName;
            shadowRenderer.sortingOrder = originalRenderer.sortingOrder - 1;

            // Apply shadow material
            Material shadowMaterial = CreateShadowMaterial(effect);
            shadowRenderer.material = shadowMaterial;

            // Position shadow
            float angleRad = effect.Angle * Mathf.Deg2Rad;
            float offsetX = Mathf.Cos(angleRad) * effect.Distance / pixelsToUnits;
            float offsetY = Mathf.Sin(angleRad) * effect.Distance / pixelsToUnits;
            
            shadowObject.transform.position = originalObject.transform.position + new Vector3(offsetX, offsetY, 0.01f);
            shadowObject.transform.localScale = originalObject.transform.localScale;
        }

        /// <summary>
        /// Applies outer glow effect by modifying the sprite material
        /// </summary>
        private static void ApplyOuterGlow(SpriteRenderer spriteRenderer, OuterGlowEffect effect)
        {
            if (effect == null) return;

            Material glowMaterial = CreateGlowMaterial(effect);
            spriteRenderer.material = glowMaterial;
        }

        /// <summary>
        /// Applies stroke effect by modifying the sprite material
        /// </summary>
        private static void ApplyStroke(SpriteRenderer spriteRenderer, StrokeEffect effect)
        {
            if (effect == null) return;

            Material strokeMaterial = CreateStrokeMaterial(effect);
            spriteRenderer.material = strokeMaterial;
        }

        /// <summary>
        /// Applies color overlay effect by modifying the sprite material
        /// </summary>
        private static void ApplyColorOverlay(SpriteRenderer spriteRenderer, ColorOverlayEffect effect)
        {
            if (effect == null) return;

            Material overlayMaterial = CreateColorOverlayMaterial(effect);
            spriteRenderer.material = overlayMaterial;
        }

        /// <summary>
        /// Applies gradient overlay effect by modifying the sprite material
        /// </summary>
        private static void ApplyGradientOverlay(SpriteRenderer spriteRenderer, GradientOverlayEffect effect)
        {
            if (effect == null) return;

            Material gradientMaterial = CreateGradientOverlayMaterial(effect);
            spriteRenderer.material = gradientMaterial;
        }

        #endregion

        #region UI Effect Application

        /// <summary>
        /// Applies drop shadow effect to UI element using Shadow component
        /// </summary>
        private static void ApplyDropShadowToUI(Image image, DropShadowEffect effect, float pixelsToUnits)
        {
            if (effect == null) return;

            Shadow shadow = image.gameObject.GetComponent<Shadow>();
            if (shadow == null)
                shadow = image.gameObject.AddComponent<Shadow>();

            shadow.effectColor = effect.Color;
            
            float angleRad = effect.Angle * Mathf.Deg2Rad;
            shadow.effectDistance = new Vector2(
                Mathf.Cos(angleRad) * effect.Distance / pixelsToUnits,
                Mathf.Sin(angleRad) * effect.Distance / pixelsToUnits
            );
        }

        /// <summary>
        /// Applies color overlay effect to UI element by modifying image color
        /// </summary>
        private static void ApplyColorOverlayToUI(Image image, ColorOverlayEffect effect)
        {
            if (effect == null) return;

            // Blend the overlay color with the current image color
            Color currentColor = image.color;
            image.color = Color.Lerp(currentColor, effect.Color, effect.Opacity);
        }

        /// <summary>
        /// Applies stroke effect to UI element using Outline component
        /// </summary>
        private static void ApplyStrokeToUI(Image image, StrokeEffect effect)
        {
            if (effect == null) return;

            Outline outline = image.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();

            outline.effectColor = effect.Color;
            outline.effectDistance = new Vector2(effect.Size, effect.Size);
        }

        #endregion

        #region Material Creation

        /// <summary>
        /// Creates a shadow material for drop shadow effects
        /// </summary>
        private static Material CreateShadowMaterial(DropShadowEffect effect)
        {
            Shader shadowShader = Shader.Find("Sprites/Effects/DropShadow");
            if (shadowShader == null)
            {
                Debug.LogWarning("Drop shadow shader not found, using default sprite shader");
                shadowShader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shadowShader);
            material.SetColor("_ShadowColor", effect.Color);
            material.SetFloat("_ShadowBlur", effect.Size);
            
            return material;
        }

        /// <summary>
        /// Creates a glow material for outer glow effects
        /// </summary>
        private static Material CreateGlowMaterial(OuterGlowEffect effect)
        {
            Shader glowShader = Shader.Find("Sprites/Effects/OuterGlow");
            if (glowShader == null)
            {
                Debug.LogWarning("Outer glow shader not found, using default sprite shader");
                glowShader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(glowShader);
            material.SetColor("_GlowColor", effect.Color);
            material.SetFloat("_GlowSize", effect.Size);
            material.SetFloat("_GlowIntensity", effect.Opacity);
            
            return material;
        }

        /// <summary>
        /// Creates a stroke material for stroke effects
        /// </summary>
        private static Material CreateStrokeMaterial(StrokeEffect effect)
        {
            Shader strokeShader = Shader.Find("Sprites/Effects/Stroke");
            if (strokeShader == null)
            {
                Debug.LogWarning("Stroke shader not found, using default sprite shader");
                strokeShader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(strokeShader);
            material.SetColor("_StrokeColor", effect.Color);
            material.SetFloat("_StrokeWidth", effect.Size);
            
            return material;
        }

        /// <summary>
        /// Creates a color overlay material
        /// </summary>
        private static Material CreateColorOverlayMaterial(ColorOverlayEffect effect)
        {
            Shader overlayShader = Shader.Find("Sprites/Effects/ColorOverlay");
            if (overlayShader == null)
            {
                Debug.LogWarning("Color overlay shader not found, using default sprite shader");
                overlayShader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(overlayShader);
            material.SetColor("_OverlayColor", effect.Color);
            
            return material;
        }

        /// <summary>
        /// Creates a gradient overlay material
        /// </summary>
        private static Material CreateGradientOverlayMaterial(GradientOverlayEffect effect)
        {
            Shader gradientShader = Shader.Find("Sprites/Effects/GradientOverlay");
            if (gradientShader == null)
            {
                Debug.LogWarning("Gradient overlay shader not found, using default sprite shader");
                gradientShader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(gradientShader);
            
            // Create gradient texture
            Texture2D gradientTexture = CreateGradientTexture(effect.Gradient, 256);
            material.SetTexture("_GradientTex", gradientTexture);
            material.SetFloat("_GradientAngle", effect.Angle);
            material.SetFloat("_GradientScale", effect.Scale / 100.0f);
            material.SetFloat("_GradientOpacity", effect.Opacity);
            
            return material;
        }

        /// <summary>
        /// Creates a texture from a Unity Gradient
        /// </summary>
        private static Texture2D CreateGradientTexture(Gradient gradient, int width)
        {
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                Color color = gradient.Evaluate(t);
                texture.SetPixel(x, 0, color);
            }
            
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
