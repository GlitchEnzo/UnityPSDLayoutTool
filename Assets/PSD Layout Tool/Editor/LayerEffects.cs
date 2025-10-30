using UnityEngine;
using System;
using System.Collections.Generic;

namespace PsdLayoutTool
{
    /// <summary>
    /// Represents the different types of layer effects that can be applied
    /// </summary>
    public enum LayerEffectType
    {
        DropShadow,
        InnerShadow,
        OuterGlow,
        InnerGlow,
        Stroke,
        ColorOverlay,
        GradientOverlay,
        PatternOverlay,
        Bevel,
        Emboss,
        Satin
    }

    /// <summary>
    /// Base class for all layer effects
    /// </summary>
    [Serializable]
    public abstract class LayerEffect
    {
        public LayerEffectType Type { get; protected set; }
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
    }

    /// <summary>
    /// Drop shadow effect settings
    /// </summary>
    [Serializable]
    public class DropShadowEffect : LayerEffect
    {
        public Color Color { get; set; } = Color.black;
        public float Distance { get; set; } = 5.0f;
        public float Angle { get; set; } = 135.0f;
        public float Spread { get; set; } = 0.0f;
        public float Size { get; set; } = 5.0f;

        public DropShadowEffect()
        {
            Type = LayerEffectType.DropShadow;
        }
    }

    /// <summary>
    /// Inner shadow effect settings
    /// </summary>
    [Serializable]
    public class InnerShadowEffect : LayerEffect
    {
        public Color Color { get; set; } = Color.black;
        public float Distance { get; set; } = 5.0f;
        public float Angle { get; set; } = 135.0f;
        public float Choke { get; set; } = 0.0f;
        public float Size { get; set; } = 5.0f;

        public InnerShadowEffect()
        {
            Type = LayerEffectType.InnerShadow;
        }
    }

    /// <summary>
    /// Outer glow effect settings
    /// </summary>
    [Serializable]
    public class OuterGlowEffect : LayerEffect
    {
        public Color Color { get; set; } = Color.yellow;
        public float Spread { get; set; } = 0.0f;
        public float Size { get; set; } = 5.0f;
        public float Range { get; set; } = 50.0f;

        public OuterGlowEffect()
        {
            Type = LayerEffectType.OuterGlow;
        }
    }

    /// <summary>
    /// Inner glow effect settings
    /// </summary>
    [Serializable]
    public class InnerGlowEffect : LayerEffect
    {
        public Color Color { get; set; } = Color.yellow;
        public float Choke { get; set; } = 0.0f;
        public float Size { get; set; } = 5.0f;
        public float Range { get; set; } = 50.0f;
        public bool Source { get; set; } = false; // false = Edge, true = Center

        public InnerGlowEffect()
        {
            Type = LayerEffectType.InnerGlow;
        }
    }

    /// <summary>
    /// Stroke effect settings
    /// </summary>
    [Serializable]
    public class StrokeEffect : LayerEffect
    {
        public enum StrokePosition
        {
            Outside,
            Inside,
            Center
        }

        public Color Color { get; set; } = Color.red;
        public float Size { get; set; } = 3.0f;
        public StrokePosition Position { get; set; } = StrokePosition.Outside;

        public StrokeEffect()
        {
            Type = LayerEffectType.Stroke;
        }
    }

    /// <summary>
    /// Color overlay effect settings
    /// </summary>
    [Serializable]
    public class ColorOverlayEffect : LayerEffect
    {
        public Color Color { get; set; } = Color.red;

        public ColorOverlayEffect()
        {
            Type = LayerEffectType.ColorOverlay;
        }
    }

    /// <summary>
    /// Gradient overlay effect settings
    /// </summary>
    [Serializable]
    public class GradientOverlayEffect : LayerEffect
    {
        public Gradient Gradient { get; set; }
        public float Angle { get; set; } = 90.0f;
        public float Scale { get; set; } = 100.0f;
        public bool Reverse { get; set; } = false;

        public GradientOverlayEffect()
        {
            Type = LayerEffectType.GradientOverlay;
            
            // Create a default gradient (black to white)
            Gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.black, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.white, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            Gradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    /// <summary>
    /// Container for all layer effects applied to a layer
    /// </summary>
    [Serializable]
    public class LayerEffects
    {
        public List<LayerEffect> Effects { get; set; }

        public LayerEffects()
        {
            Effects = new List<LayerEffect>();
        }

        public void AddEffect(LayerEffect effect)
        {
            Effects.Add(effect);
        }

        public T GetEffect<T>() where T : LayerEffect
        {
            foreach (var effect in Effects)
            {
                if (effect is T)
                    return effect as T;
            }
            return null;
        }

        public List<T> GetEffects<T>() where T : LayerEffect
        {
            List<T> results = new List<T>();
            foreach (var effect in Effects)
            {
                if (effect is T)
                    results.Add(effect as T);
            }
            return results;
        }

        public bool HasEffect<T>() where T : LayerEffect
        {
            return GetEffect<T>() != null;
        }

        public bool HasAnyEffects => Effects.Count > 0;
    }
}
