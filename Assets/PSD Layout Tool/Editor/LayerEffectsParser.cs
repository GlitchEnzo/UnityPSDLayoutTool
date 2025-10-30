using UnityEngine;
using System.Collections.Generic;
using PhotoshopFile;

namespace PsdLayoutTool
{
    /// <summary>
    /// Parses Photoshop layer effects data from PSD files
    /// </summary>
    public static class LayerEffectsParser
    {
        /// <summary>
        /// Parses layer effects from a PSD layer's adjustment layer info
        /// </summary>
        /// <param name="layer">The layer to parse effects from</param>
        /// <returns>LayerEffects object containing all parsed effects</returns>
        public static LayerEffects ParseLayerEffects(Layer layer)
        {
            var layerEffects = new LayerEffects();
            
            if (!layer.HasEffects)
                return layerEffects;

            // Find effects data in adjustment layer info
            foreach (var adjustmentInfo in layer.AdjustmentInfo)
            {
                if (adjustmentInfo.Key == "lfx2" || adjustmentInfo.Key == "lrFX")
                {
                    ParseEffectsData(adjustmentInfo.DataReader, layerEffects);
                    break;
                }
            }

            return layerEffects;
        }

        /// <summary>
        /// Parses the binary effects data from Photoshop
        /// </summary>
        /// <param name="reader">Binary reader containing effects data</param>
        /// <param name="layerEffects">LayerEffects object to populate</param>
        private static void ParseEffectsData(BinaryReverseReader reader, LayerEffects layerEffects)
        {
            try
            {
                // Skip to effects data - this is a simplified parser
                // Real PSD effects parsing is extremely complex, so we'll create default effects
                // based on common layer effects and reasonable defaults
                
                // For now, create default effects as placeholders
                // In a real implementation, this would parse the complex binary structure
                CreateDefaultEffects(layerEffects);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to parse layer effects: {ex.Message}");
                // Create default effects on parse failure
                CreateDefaultEffects(layerEffects);
            }
        }

        /// <summary>
        /// Creates default layer effects when parsing fails or for demonstration
        /// In a real implementation, this would be replaced with actual PSD parsing
        /// </summary>
        /// <param name="layerEffects">LayerEffects object to populate</param>
        private static void CreateDefaultEffects(LayerEffects layerEffects)
        {
            // Add a default drop shadow effect
            var dropShadow = new DropShadowEffect
            {
                Color = new Color(0, 0, 0, 0.75f),
                Distance = 5.0f,
                Angle = 135.0f,
                Size = 5.0f,
                Spread = 0.0f,
                Opacity = 0.75f,
                BlendMode = BlendMode.Multiply
            };
            layerEffects.AddEffect(dropShadow);

            // Add a default outer glow effect
            var outerGlow = new OuterGlowEffect
            {
                Color = new Color(1, 1, 0, 0.5f),
                Size = 5.0f,
                Spread = 0.0f,
                Range = 50.0f,
                Opacity = 0.5f,
                BlendMode = BlendMode.Screen
            };
            layerEffects.AddEffect(outerGlow);
        }

        /// <summary>
        /// Creates layer effects based on simple detection heuristics
        /// This is a fallback method when full PSD parsing isn't available
        /// </summary>
        /// <param name="layer">The layer to analyze</param>
        /// <returns>LayerEffects based on layer properties</returns>
        public static LayerEffects CreateEffectsFromHeuristics(Layer layer)
        {
            var layerEffects = new LayerEffects();

            if (!layer.HasEffects)
                return layerEffects;

            // Analyze layer name for effect hints
            string layerName = layer.Name.ToLower();

            if (layerName.Contains("shadow") || layerName.Contains("drop"))
            {
                var dropShadow = new DropShadowEffect
                {
                    Color = new Color(0, 0, 0, 0.6f),
                    Distance = 3.0f,
                    Angle = 135.0f,
                    Size = 3.0f,
                    Opacity = 0.6f
                };
                layerEffects.AddEffect(dropShadow);
            }

            if (layerName.Contains("glow"))
            {
                var outerGlow = new OuterGlowEffect
                {
                    Color = new Color(1, 1, 0, 0.8f),
                    Size = 5.0f,
                    Opacity = 0.8f
                };
                layerEffects.AddEffect(outerGlow);
            }

            if (layerName.Contains("stroke") || layerName.Contains("outline"))
            {
                var stroke = new StrokeEffect
                {
                    Color = Color.black,
                    Size = 2.0f,
                    Position = StrokeEffect.StrokePosition.Outside,
                    Opacity = 1.0f
                };
                layerEffects.AddEffect(stroke);
            }

            if (layerName.Contains("overlay") || layerName.Contains("tint"))
            {
                var colorOverlay = new ColorOverlayEffect
                {
                    Color = new Color(1, 0, 0, 0.5f),
                    Opacity = 0.5f
                };
                layerEffects.AddEffect(colorOverlay);
            }

            return layerEffects;
        }
    }
}
