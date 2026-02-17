using System;
using System.Collections.Generic;
using UnityEngine;
using PhotoshopFile;

namespace PsdLayoutTool
{
    /// <summary>
    /// Types of smart filters supported by the PSD importer
    /// </summary>
    public enum SmartFilterType
    {
        GaussianBlur,
        MotionBlur,
        RadialBlur,
        Sharpen,
        UnsharpMask,
        Noise,
        Distort,
        Emboss,
        FindEdges,
        HighPass,
        Liquify,
        OilPaint,
        Posterize,
        Solarize,
        WaveDistortion
    }

    /// <summary>
    /// Base class for all smart filter effects
    /// </summary>
    [Serializable]
    public abstract class SmartFilterEffect
    {
        public SmartFilterType Type { get; protected set; }
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
    }

    /// <summary>
    /// Gaussian Blur smart filter effect
    /// </summary>
    [Serializable]
    public class GaussianBlurEffect : SmartFilterEffect
    {
        public float Radius { get; set; } = 1.0f; // 0.1 to 250.0

        public GaussianBlurEffect()
        {
            Type = SmartFilterType.GaussianBlur;
        }
    }

    /// <summary>
    /// Motion Blur smart filter effect
    /// </summary>
    [Serializable]
    public class MotionBlurEffect : SmartFilterEffect
    {
        public float Angle { get; set; } = 0f;     // -360 to 360 degrees
        public float Distance { get; set; } = 1f; // 1 to 999 pixels

        public MotionBlurEffect()
        {
            Type = SmartFilterType.MotionBlur;
        }
    }

    /// <summary>
    /// Radial Blur smart filter effect
    /// </summary>
    [Serializable]
    public class RadialBlurEffect : SmartFilterEffect
    {
        public enum RadialBlurMethod
        {
            Spin,
            Zoom
        }

        public float Amount { get; set; } = 10f; // 1 to 100
        public RadialBlurMethod Method { get; set; } = RadialBlurMethod.Spin;
        public Vector2 Center { get; set; } = new Vector2(0.5f, 0.5f); // Normalized coordinates

        public RadialBlurEffect()
        {
            Type = SmartFilterType.RadialBlur;
        }
    }

    /// <summary>
    /// Sharpen smart filter effect
    /// </summary>
    [Serializable]
    public class SharpenEffect : SmartFilterEffect
    {
        public float Amount { get; set; } = 50f; // 1 to 500

        public SharpenEffect()
        {
            Type = SmartFilterType.Sharpen;
        }
    }

    /// <summary>
    /// Unsharp Mask smart filter effect
    /// </summary>
    [Serializable]
    public class UnsharpMaskEffect : SmartFilterEffect
    {
        public float Amount { get; set; } = 50f;     // 1 to 500%
        public float Radius { get; set; } = 1f;      // 0.1 to 250 pixels
        public float Threshold { get; set; } = 0f;   // 0 to 255 levels

        public UnsharpMaskEffect()
        {
            Type = SmartFilterType.UnsharpMask;
        }
    }

    /// <summary>
    /// Noise smart filter effect
    /// </summary>
    [Serializable]
    public class NoiseEffect : SmartFilterEffect
    {
        public enum NoiseDistribution
        {
            Uniform,
            Gaussian
        }

        public float Amount { get; set; } = 10f;    // 0.1 to 400%
        public NoiseDistribution Distribution { get; set; } = NoiseDistribution.Uniform;
        public bool Monochromatic { get; set; } = false;

        public NoiseEffect()
        {
            Type = SmartFilterType.Noise;
        }
    }

    /// <summary>
    /// Emboss smart filter effect
    /// </summary>
    [Serializable]
    public class EmbossEffect : SmartFilterEffect
    {
        public float Angle { get; set; } = 135f;    // -180 to 180 degrees
        public float Height { get; set; } = 3f;     // 1 to 10 pixels
        public float Amount { get; set; } = 100f;   // 1 to 500%

        public EmbossEffect()
        {
            Type = SmartFilterType.Emboss;
        }
    }

    /// <summary>
    /// High Pass smart filter effect
    /// </summary>
    [Serializable]
    public class HighPassEffect : SmartFilterEffect
    {
        public float Radius { get; set; } = 10f; // 0.1 to 250 pixels

        public HighPassEffect()
        {
            Type = SmartFilterType.HighPass;
        }
    }

    /// <summary>
    /// Wave Distortion smart filter effect
    /// </summary>
    [Serializable]
    public class WaveDistortionEffect : SmartFilterEffect
    {
        public float Amplitude { get; set; } = 10f;    // 1 to 999
        public float Wavelength { get; set; } = 100f;  // 1 to 999
        public float Scale { get; set; } = 100f;       // 1 to 100%
        public bool Horizontal { get; set; } = true;
        public bool Vertical { get; set; } = false;

        public WaveDistortionEffect()
        {
            Type = SmartFilterType.WaveDistortion;
        }
    }

    /// <summary>
    /// Collection of smart filter effects for a layer
    /// </summary>
    [Serializable]
    public class SmartFilters
    {
        public List<SmartFilterEffect> Effects { get; set; } = new List<SmartFilterEffect>();

        public void AddEffect(SmartFilterEffect effect)
        {
            Effects.Add(effect);
        }

        public T GetEffect<T>() where T : SmartFilterEffect
        {
            foreach (var effect in Effects)
            {
                if (effect is T)
                    return effect as T;
            }
            return null;
        }

        public bool HasEffect<T>() where T : SmartFilterEffect
        {
            return GetEffect<T>() != null;
        }
    }

    /// <summary>
    /// Parser for Photoshop smart filter data
    /// </summary>
    public static class SmartFilterParser
    {
        /// <summary>
        /// Parses smart filter effects from a PSD layer
        /// </summary>
        /// <param name="layer">The PSD layer to parse</param>
        /// <returns>Collection of smart filter effects</returns>
        public static SmartFilters ParseSmartFilters(Layer layer)
        {
            var filters = new SmartFilters();

            if (layer.AdjustmentInfo == null) return filters;

            foreach (var adjustmentInfo in layer.AdjustmentInfo)
            {
                try
                {
                    var effect = ParseSmartFilterInfo(adjustmentInfo);
                    if (effect != null)
                    {
                        filters.AddEffect(effect);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse smart filter '{adjustmentInfo.Key}': {ex.Message}");
                }
            }

            // Check for heuristic-based smart filter detection based on layer name
            ParseHeuristicSmartFilters(layer, filters);

            return filters;
        }

        /// <summary>
        /// Parses a single smart filter info block
        /// </summary>
        private static SmartFilterEffect ParseSmartFilterInfo(AdjustmentLayerInfo info)
        {
            switch (info.Key)
            {
                case "GBlr": // Gaussian Blur
                    return ParseGaussianBlur(info);
                case "MBlr": // Motion Blur
                    return ParseMotionBlur(info);
                case "RBlr": // Radial Blur
                    return ParseRadialBlur(info);
                case "Shrp": // Sharpen
                    return ParseSharpen(info);
                case "USMk": // Unsharp Mask
                    return ParseUnsharpMask(info);
                case "Nois": // Add Noise
                    return ParseNoise(info);
                case "Embs": // Emboss
                    return ParseEmboss(info);
                case "HiPs": // High Pass
                    return ParseHighPass(info);
                case "Wave": // Wave
                    return ParseWaveDistortion(info);
                default:
                    Debug.LogWarning($"Unsupported smart filter type: {info.Key}");
                    return null;
            }
        }

        /// <summary>
        /// Parses Gaussian Blur filter data
        /// </summary>
        private static GaussianBlurEffect ParseGaussianBlur(AdjustmentLayerInfo info)
        {
            var effect = new GaussianBlurEffect();
            
            // Simplified parsing - in a full implementation, you would parse the binary data
            effect.Radius = 2.0f; // Default value
            
            return effect;
        }

        /// <summary>
        /// Parses Motion Blur filter data
        /// </summary>
        private static MotionBlurEffect ParseMotionBlur(AdjustmentLayerInfo info)
        {
            var effect = new MotionBlurEffect();
            
            // Simplified parsing
            effect.Angle = 0f;
            effect.Distance = 5f;
            
            return effect;
        }

        /// <summary>
        /// Parses Radial Blur filter data
        /// </summary>
        private static RadialBlurEffect ParseRadialBlur(AdjustmentLayerInfo info)
        {
            var effect = new RadialBlurEffect();
            
            // Simplified parsing
            effect.Amount = 10f;
            effect.Method = RadialBlurEffect.RadialBlurMethod.Spin;
            
            return effect;
        }

        /// <summary>
        /// Parses Sharpen filter data
        /// </summary>
        private static SharpenEffect ParseSharpen(AdjustmentLayerInfo info)
        {
            var effect = new SharpenEffect();
            
            // Simplified parsing
            effect.Amount = 50f;
            
            return effect;
        }

        /// <summary>
        /// Parses Unsharp Mask filter data
        /// </summary>
        private static UnsharpMaskEffect ParseUnsharpMask(AdjustmentLayerInfo info)
        {
            var effect = new UnsharpMaskEffect();
            
            // Simplified parsing
            effect.Amount = 50f;
            effect.Radius = 1f;
            effect.Threshold = 0f;
            
            return effect;
        }

        /// <summary>
        /// Parses Noise filter data
        /// </summary>
        private static NoiseEffect ParseNoise(AdjustmentLayerInfo info)
        {
            var effect = new NoiseEffect();
            
            // Simplified parsing
            effect.Amount = 10f;
            effect.Distribution = NoiseEffect.NoiseDistribution.Uniform;
            
            return effect;
        }

        /// <summary>
        /// Parses Emboss filter data
        /// </summary>
        private static EmbossEffect ParseEmboss(AdjustmentLayerInfo info)
        {
            var effect = new EmbossEffect();
            
            // Simplified parsing
            effect.Angle = 135f;
            effect.Height = 3f;
            effect.Amount = 100f;
            
            return effect;
        }

        /// <summary>
        /// Parses High Pass filter data
        /// </summary>
        private static HighPassEffect ParseHighPass(AdjustmentLayerInfo info)
        {
            var effect = new HighPassEffect();
            
            // Simplified parsing
            effect.Radius = 10f;
            
            return effect;
        }

        /// <summary>
        /// Parses Wave Distortion filter data
        /// </summary>
        private static WaveDistortionEffect ParseWaveDistortion(AdjustmentLayerInfo info)
        {
            var effect = new WaveDistortionEffect();
            
            // Simplified parsing
            effect.Amplitude = 10f;
            effect.Wavelength = 100f;
            effect.Scale = 100f;
            
            return effect;
        }

        /// <summary>
        /// Performs heuristic detection of smart filters based on layer names
        /// </summary>
        private static void ParseHeuristicSmartFilters(Layer layer, SmartFilters filters)
        {
            string layerName = layer.Name.ToLower();

            // Check for common smart filter naming patterns
            if (layerName.Contains("blur") && !filters.HasEffect<GaussianBlurEffect>())
            {
                var effect = new GaussianBlurEffect();
                
                if (layerName.Contains("motion"))
                {
                    // Convert to motion blur
                    var motionEffect = new MotionBlurEffect();
                    motionEffect.Distance = 5f;
                    filters.AddEffect(motionEffect);
                }
                else if (layerName.Contains("radial"))
                {
                    // Convert to radial blur
                    var radialEffect = new RadialBlurEffect();
                    radialEffect.Amount = 10f;
                    filters.AddEffect(radialEffect);
                }
                else
                {
                    // Default Gaussian blur
                    if (layerName.Contains("heavy") || layerName.Contains("strong"))
                    {
                        effect.Radius = 5f;
                    }
                    else if (layerName.Contains("light") || layerName.Contains("subtle"))
                    {
                        effect.Radius = 1f;
                    }
                    else
                    {
                        effect.Radius = 2f;
                    }
                    
                    filters.AddEffect(effect);
                }
            }

            if (layerName.Contains("sharpen") && !filters.HasEffect<SharpenEffect>())
            {
                var effect = new SharpenEffect();
                
                if (layerName.Contains("unsharp"))
                {
                    // Convert to unsharp mask
                    var unsharpEffect = new UnsharpMaskEffect();
                    unsharpEffect.Amount = 50f;
                    unsharpEffect.Radius = 1f;
                    filters.AddEffect(unsharpEffect);
                }
                else
                {
                    effect.Amount = 50f;
                    filters.AddEffect(effect);
                }
            }

            if (layerName.Contains("noise") && !filters.HasEffect<NoiseEffect>())
            {
                var effect = new NoiseEffect();
                
                if (layerName.Contains("heavy"))
                {
                    effect.Amount = 25f;
                }
                else if (layerName.Contains("light"))
                {
                    effect.Amount = 5f;
                }
                else
                {
                    effect.Amount = 10f;
                }
                
                filters.AddEffect(effect);
            }

            if (layerName.Contains("emboss") && !filters.HasEffect<EmbossEffect>())
            {
                var effect = new EmbossEffect();
                effect.Angle = 135f;
                effect.Height = 3f;
                effect.Amount = 100f;
                filters.AddEffect(effect);
            }

            if (layerName.Contains("highpass") && !filters.HasEffect<HighPassEffect>())
            {
                var effect = new HighPassEffect();
                effect.Radius = 10f;
                filters.AddEffect(effect);
            }
        }
    }
}
