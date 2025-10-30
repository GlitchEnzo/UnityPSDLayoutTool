using System;
using System.Collections.Generic;
using UnityEngine;
using PhotoshopFile;

namespace PsdLayoutTool
{
    /// <summary>
    /// Types of adjustment layers supported by the PSD importer
    /// </summary>
    public enum AdjustmentLayerType
    {
        BrightnessContrast,
        HueSaturation,
        ColorBalance,
        Curves,
        Levels,
        PhotoFilter,
        ChannelMixer,
        ColorLookup,
        Vibrance,
        Exposure
    }

    /// <summary>
    /// Base class for all adjustment layer effects
    /// </summary>
    [Serializable]
    public abstract class AdjustmentLayerEffect
    {
        public AdjustmentLayerType Type { get; protected set; }
        public bool Enabled { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
    }

    /// <summary>
    /// Brightness/Contrast adjustment layer effect
    /// </summary>
    [Serializable]
    public class BrightnessContrastEffect : AdjustmentLayerEffect
    {
        public float Brightness { get; set; } = 0f; // -100 to 100
        public float Contrast { get; set; } = 0f;   // -100 to 100
        public bool UseLegacy { get; set; } = false;

        public BrightnessContrastEffect()
        {
            Type = AdjustmentLayerType.BrightnessContrast;
        }
    }

    /// <summary>
    /// Hue/Saturation adjustment layer effect
    /// </summary>
    [Serializable]
    public class HueSaturationEffect : AdjustmentLayerEffect
    {
        public float Hue { get; set; } = 0f;        // -180 to 180
        public float Saturation { get; set; } = 0f; // -100 to 100
        public float Lightness { get; set; } = 0f;  // -100 to 100
        public bool Colorize { get; set; } = false;

        public HueSaturationEffect()
        {
            Type = AdjustmentLayerType.HueSaturation;
        }
    }

    /// <summary>
    /// Color Balance adjustment layer effect
    /// </summary>
    [Serializable]
    public class ColorBalanceEffect : AdjustmentLayerEffect
    {
        public float CyanRed { get; set; } = 0f;        // -100 to 100
        public float MagentaGreen { get; set; } = 0f;   // -100 to 100
        public float YellowBlue { get; set; } = 0f;     // -100 to 100
        public bool PreserveLuminosity { get; set; } = true;

        public ColorBalanceEffect()
        {
            Type = AdjustmentLayerType.ColorBalance;
        }
    }

    /// <summary>
    /// Curves adjustment layer effect
    /// </summary>
    [Serializable]
    public class CurvesEffect : AdjustmentLayerEffect
    {
        [Serializable]
        public class CurvePoint
        {
            public float Input { get; set; }
            public float Output { get; set; }

            public CurvePoint(float input, float output)
            {
                Input = input;
                Output = output;
            }
        }

        public List<CurvePoint> RGBCurve { get; set; } = new List<CurvePoint>();
        public List<CurvePoint> RedCurve { get; set; } = new List<CurvePoint>();
        public List<CurvePoint> GreenCurve { get; set; } = new List<CurvePoint>();
        public List<CurvePoint> BlueCurve { get; set; } = new List<CurvePoint>();

        public CurvesEffect()
        {
            Type = AdjustmentLayerType.Curves;
            // Initialize with default linear curve
            RGBCurve.Add(new CurvePoint(0f, 0f));
            RGBCurve.Add(new CurvePoint(1f, 1f));
        }
    }

    /// <summary>
    /// Levels adjustment layer effect
    /// </summary>
    [Serializable]
    public class LevelsEffect : AdjustmentLayerEffect
    {
        public float InputBlack { get; set; } = 0f;   // 0 to 255
        public float InputWhite { get; set; } = 255f; // 0 to 255
        public float InputGamma { get; set; } = 1f;   // 0.1 to 9.99
        public float OutputBlack { get; set; } = 0f;  // 0 to 255
        public float OutputWhite { get; set; } = 255f;// 0 to 255

        public LevelsEffect()
        {
            Type = AdjustmentLayerType.Levels;
        }
    }

    /// <summary>
    /// Photo Filter adjustment layer effect
    /// </summary>
    [Serializable]
    public class PhotoFilterEffect : AdjustmentLayerEffect
    {
        public Color FilterColor { get; set; } = Color.white;
        public float Density { get; set; } = 25f; // 0 to 100
        public bool PreserveLuminosity { get; set; } = true;

        public PhotoFilterEffect()
        {
            Type = AdjustmentLayerType.PhotoFilter;
        }
    }

    /// <summary>
    /// Vibrance adjustment layer effect
    /// </summary>
    [Serializable]
    public class VibranceEffect : AdjustmentLayerEffect
    {
        public float Vibrance { get; set; } = 0f;   // -100 to 100
        public float Saturation { get; set; } = 0f; // -100 to 100

        public VibranceEffect()
        {
            Type = AdjustmentLayerType.Vibrance;
        }
    }

    /// <summary>
    /// Collection of adjustment layer effects for a layer
    /// </summary>
    [Serializable]
    public class AdjustmentLayerEffects
    {
        public List<AdjustmentLayerEffect> Effects { get; set; } = new List<AdjustmentLayerEffect>();

        public void AddEffect(AdjustmentLayerEffect effect)
        {
            Effects.Add(effect);
        }

        public T GetEffect<T>() where T : AdjustmentLayerEffect
        {
            foreach (var effect in Effects)
            {
                if (effect is T)
                    return effect as T;
            }
            return null;
        }

        public bool HasEffect<T>() where T : AdjustmentLayerEffect
        {
            return GetEffect<T>() != null;
        }
    }

    /// <summary>
    /// Parser for Photoshop adjustment layer data
    /// </summary>
    public static class AdjustmentLayerParser
    {
        /// <summary>
        /// Parses adjustment layer effects from a PSD layer
        /// </summary>
        /// <param name="layer">The PSD layer to parse</param>
        /// <returns>Collection of adjustment layer effects</returns>
        public static AdjustmentLayerEffects ParseAdjustmentLayers(Layer layer)
        {
            var effects = new AdjustmentLayerEffects();

            if (layer.AdjustmentInfo == null) return effects;

            foreach (var adjustmentInfo in layer.AdjustmentInfo)
            {
                try
                {
                    var effect = ParseAdjustmentInfo(adjustmentInfo);
                    if (effect != null)
                    {
                        effects.AddEffect(effect);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse adjustment layer '{adjustmentInfo.Key}': {ex.Message}");
                }
            }

            // Check for heuristic-based adjustment detection based on layer name
            ParseHeuristicAdjustments(layer, effects);

            return effects;
        }

        /// <summary>
        /// Parses a single adjustment info block
        /// </summary>
        private static AdjustmentLayerEffect ParseAdjustmentInfo(AdjustmentLayerInfo info)
        {
            switch (info.Key)
            {
                case "brit": // Brightness/Contrast
                    return ParseBrightnessContrast(info);
                case "hue ": // Hue/Saturation
                case "hue2": // Hue/Saturation v2
                    return ParseHueSaturation(info);
                case "blnc": // Color Balance
                    return ParseColorBalance(info);
                case "curv": // Curves
                    return ParseCurves(info);
                case "levl": // Levels
                    return ParseLevels(info);
                case "phfl": // Photo Filter
                    return ParsePhotoFilter(info);
                case "vibA": // Vibrance
                    return ParseVibrance(info);
                default:
                    Debug.LogWarning($"Unsupported adjustment layer type: {info.Key}");
                    return null;
            }
        }

        /// <summary>
        /// Parses brightness/contrast adjustment data
        /// </summary>
        private static BrightnessContrastEffect ParseBrightnessContrast(AdjustmentLayerInfo info)
        {
            var effect = new BrightnessContrastEffect();
            
            // Simplified parsing - in a full implementation, you would parse the binary data
            // For now, use default values
            effect.Brightness = 0f;
            effect.Contrast = 0f;
            
            return effect;
        }

        /// <summary>
        /// Parses hue/saturation adjustment data
        /// </summary>
        private static HueSaturationEffect ParseHueSaturation(AdjustmentLayerInfo info)
        {
            var effect = new HueSaturationEffect();
            
            // Simplified parsing - in a full implementation, you would parse the binary data
            effect.Hue = 0f;
            effect.Saturation = 0f;
            effect.Lightness = 0f;
            
            return effect;
        }

        /// <summary>
        /// Parses color balance adjustment data
        /// </summary>
        private static ColorBalanceEffect ParseColorBalance(AdjustmentLayerInfo info)
        {
            var effect = new ColorBalanceEffect();
            
            // Simplified parsing
            effect.CyanRed = 0f;
            effect.MagentaGreen = 0f;
            effect.YellowBlue = 0f;
            
            return effect;
        }

        /// <summary>
        /// Parses curves adjustment data
        /// </summary>
        private static CurvesEffect ParseCurves(AdjustmentLayerInfo info)
        {
            var effect = new CurvesEffect();
            
            // Simplified parsing - would need to parse curve points from binary data
            return effect;
        }

        /// <summary>
        /// Parses levels adjustment data
        /// </summary>
        private static LevelsEffect ParseLevels(AdjustmentLayerInfo info)
        {
            var effect = new LevelsEffect();
            
            // Simplified parsing
            return effect;
        }

        /// <summary>
        /// Parses photo filter adjustment data
        /// </summary>
        private static PhotoFilterEffect ParsePhotoFilter(AdjustmentLayerInfo info)
        {
            var effect = new PhotoFilterEffect();
            
            // Simplified parsing
            return effect;
        }

        /// <summary>
        /// Parses vibrance adjustment data
        /// </summary>
        private static VibranceEffect ParseVibrance(AdjustmentLayerInfo info)
        {
            var effect = new VibranceEffect();
            
            // Simplified parsing
            return effect;
        }

        /// <summary>
        /// Performs heuristic detection of adjustment layers based on layer names
        /// </summary>
        private static void ParseHeuristicAdjustments(Layer layer, AdjustmentLayerEffects effects)
        {
            string layerName = layer.Name.ToLower();

            // Check for common adjustment layer naming patterns
            if (layerName.Contains("brightness") || layerName.Contains("contrast"))
            {
                if (!effects.HasEffect<BrightnessContrastEffect>())
                {
                    var effect = new BrightnessContrastEffect();
                    
                    // Try to extract values from layer name
                    if (layerName.Contains("bright+"))
                    {
                        effect.Brightness = 20f;
                    }
                    else if (layerName.Contains("bright-"))
                    {
                        effect.Brightness = -20f;
                    }
                    
                    if (layerName.Contains("contrast+"))
                    {
                        effect.Contrast = 20f;
                    }
                    else if (layerName.Contains("contrast-"))
                    {
                        effect.Contrast = -20f;
                    }
                    
                    effects.AddEffect(effect);
                }
            }

            if (layerName.Contains("saturation") || layerName.Contains("hue"))
            {
                if (!effects.HasEffect<HueSaturationEffect>())
                {
                    var effect = new HueSaturationEffect();
                    
                    if (layerName.Contains("saturate+"))
                    {
                        effect.Saturation = 25f;
                    }
                    else if (layerName.Contains("desaturate") || layerName.Contains("saturate-"))
                    {
                        effect.Saturation = -25f;
                    }
                    
                    effects.AddEffect(effect);
                }
            }

            if (layerName.Contains("warm") || layerName.Contains("cool") || layerName.Contains("tint"))
            {
                if (!effects.HasEffect<ColorBalanceEffect>())
                {
                    var effect = new ColorBalanceEffect();
                    
                    if (layerName.Contains("warm"))
                    {
                        effect.CyanRed = 15f;
                        effect.YellowBlue = 10f;
                    }
                    else if (layerName.Contains("cool"))
                    {
                        effect.CyanRed = -15f;
                        effect.YellowBlue = -10f;
                    }
                    
                    effects.AddEffect(effect);
                }
            }
        }
    }
}
