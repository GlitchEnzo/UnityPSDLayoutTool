# Blend Mode Implementation Guide

This document provides technical details about the blend mode implementation in the Unity PSD Layout Tool.

## Architecture Overview

The blend mode system consists of three main components:

1. **Blend Mode Enum** (`BlendMode`) - Defines all supported blend modes
2. **Shader Files** - Custom Unity shaders implementing each blend mode
3. **Integration Code** - Automatic detection and application during import

## Shader Implementation Details

### Basic Mathematical Blend Modes

#### Multiply (`SpriteMultiply.shader`)
```hlsl
// Formula: result = base * blend
fixed3 multiply = base * blend;
```

#### Screen (`SpriteScreen.shader`)
```hlsl
// Formula: result = 1 - (1 - base) * (1 - blend)  
fixed3 screen = 1.0 - (1.0 - base) * (1.0 - blend);
```

#### Overlay (`SpriteOverlay.shader`)
```hlsl
// Formula: if base < 0.5: result = 2 * base * blend
//          else: result = 1 - 2 * (1 - base) * (1 - blend)
fixed3 overlay = lerp(
    1.0 - 2.0 * (1.0 - base) * (1.0 - blend),  // base >= 0.5
    2.0 * base * blend,                        // base < 0.5
    step(base, 0.5)
);
```

### Advanced Mathematical Blend Modes

#### Color Dodge (`SpriteColorDodge.shader`)
```hlsl
// Formula: result = base / (1 - blend) [clamped to prevent division by zero]
fixed3 colorDodge = base / max(1.0 - blend, 0.001);
```

#### Color Burn (`SpriteColorBurn.shader`)
```hlsl
// Formula: result = 1 - (1 - base) / blend [clamped to prevent division by zero]
fixed3 colorBurn = 1.0 - (1.0 - base) / max(blend, 0.001);
```

#### Difference (`SpriteDifference.shader`)
```hlsl
// Formula: result = |base - blend|
fixed3 difference = abs(base - blend);
```

#### Exclusion (`SpriteExclusion.shader`)
```hlsl
// Formula: result = base + blend - 2 * base * blend
fixed3 exclusion = base + blend - 2.0 * base * blend;
```

### Comparative Blend Modes

#### Darken (`SpriteDarken.shader`)
```hlsl
// Formula: result = min(base, blend)
fixed3 darken = min(base, blend);
```

#### Lighten (`SpriteLighten.shader`)
```hlsl
// Formula: result = max(base, blend)
fixed3 lighten = max(base, blend);
```

### HSV-Based Blend Modes

These blend modes require color space conversion between RGB and HSV:

#### RGB to HSV Conversion
```hlsl
fixed3 rgb2hsv(fixed3 c)
{
    fixed4 K = fixed4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    fixed4 p = lerp(fixed4(c.bg, K.wz), fixed4(c.gb, K.xy), step(c.b, c.g));
    fixed4 q = lerp(fixed4(p.xyw, c.r), fixed4(c.r, p.yzx), step(p.x, c.r));

    fixed d = q.x - min(q.w, q.y);
    fixed e = 1.0e-10;
    return fixed3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
```

#### HSV to RGB Conversion
```hlsl
fixed3 hsv2rgb(fixed3 c)
{
    fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
```

#### Hue Blend Mode (`SpriteHue.shader`)
```hlsl
// Uses hue from blend, saturation and value from base
fixed3 baseHSV = rgb2hsv(base);
fixed3 blendHSV = rgb2hsv(blend);
fixed3 resultHSV = fixed3(blendHSV.x, baseHSV.y, baseHSV.z);
fixed3 hue = hsv2rgb(resultHSV);
```

#### Saturation Blend Mode (`SpriteSaturation.shader`)
```hlsl
// Uses saturation from blend, hue and value from base
fixed3 baseHSV = rgb2hsv(base);
fixed3 blendHSV = rgb2hsv(blend);
fixed3 resultHSV = fixed3(baseHSV.x, blendHSV.y, baseHSV.z);
fixed3 saturation = hsv2rgb(resultHSV);
```

#### Color Blend Mode (`SpriteColor.shader`)
```hlsl
// Uses hue and saturation from blend, value from base
fixed3 baseHSV = rgb2hsv(base);
fixed3 blendHSV = rgb2hsv(blend);
fixed3 resultHSV = fixed3(blendHSV.x, blendHSV.y, baseHSV.z);
fixed3 color = hsv2rgb(resultHSV);
```

#### Luminosity Blend Mode (`SpriteLuminosity.shader`)
```hlsl
// Uses value (luminosity) from blend, hue and saturation from base
fixed3 baseHSV = rgb2hsv(base);
fixed3 blendHSV = rgb2hsv(blend);
fixed3 resultHSV = fixed3(baseHSV.x, baseHSV.y, blendHSV.z);
fixed3 luminosity = hsv2rgb(resultHSV);
```

## Integration Code

### Blend Mode Detection

The `ConvertBlendModeKey` method maps Photoshop blend mode keys to the internal enum:

```csharp
private static BlendMode ConvertBlendModeKey(string blendModeKey)
{
    switch (blendModeKey)
    {
        case "norm": return BlendMode.Normal;
        case "mult": return BlendMode.Multiply;
        case "scrn": return BlendMode.Screen;
        case "over": return BlendMode.Overlay;
        case "sLit": return BlendMode.SoftLight;
        case "hLit": return BlendMode.HardLight;
        case "cDdg": return BlendMode.ColorDodge;
        case "cBrn": return BlendMode.ColorBurn;
        case "dark": return BlendMode.Darken;
        case "lite": return BlendMode.Lighten;
        case "diff": return BlendMode.Difference;
        case "smud": return BlendMode.Exclusion;
        case "hue ": return BlendMode.Hue;
        case "sat ": return BlendMode.Saturation;
        case "colr": return BlendMode.Color;
        case "lum ": return BlendMode.Luminosity;
        default: return BlendMode.Normal;
    }
}
```

### Material Application

The `ApplyBlendMode` method creates and assigns materials:

```csharp
private static void ApplyBlendMode(SpriteRenderer spriteRenderer, string blendModeKey)
{
    BlendMode blendMode = ConvertBlendModeKey(blendModeKey);
    
    switch (blendMode)
    {
        case BlendMode.Multiply:
            spriteRenderer.material = CreateBlendMaterial("Sprites/Multiply");
            break;
        case BlendMode.ColorDodge:
            spriteRenderer.material = CreateBlendMaterial("Sprites/ColorDodge");
            break;
        // ... other cases
    }
}
```

### Material Creation

The `CreateBlendMaterial` method handles shader loading and fallback:

```csharp
private static Material CreateBlendMaterial(string shaderName)
{
    Shader shader = Shader.Find(shaderName);
    if (shader != null)
    {
        return new Material(shader);
    }
    else
    {
        Debug.LogWarning($"Shader '{shaderName}' not found. Using default material.");
        return new Material(Shader.Find("Sprites/Default"));
    }
}
```

## Performance Considerations

### Shader Compilation
- First use of each blend mode triggers shader compilation
- Consider pre-warming shaders in build process for production

### Draw Call Optimization
- Each unique material creates a separate draw call
- Consider sprite atlasing for objects with same blend modes
- Group objects with identical blend modes when possible

### Mobile Performance
- HSV-based blend modes are more computationally expensive
- Consider LOD systems for complex blend mode hierarchies
- Test performance on target mobile devices

## Extending the System

### Adding New Blend Modes

1. **Add to Enum**: Update the `BlendMode` enum
2. **Create Shader**: Implement the mathematical formula in a new shader file
3. **Update Mapping**: Add the Photoshop key mapping in `ConvertBlendModeKey`
4. **Update Application**: Add the case in `ApplyBlendMode` method

### Custom Blend Mode Example

```hlsl
// Example: Custom "Negate" blend mode
Shader "Sprites/Negate"
{
    // ... standard sprite shader structure ...
    
    fixed4 frag(v2f IN) : SV_Target
    {
        fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
        
        // Custom negate formula: result = 1 - (base * blend)
        fixed3 base = fixed3(0.5, 0.5, 0.5); // Assuming middle gray as base
        fixed3 blend = c.rgb;
        fixed3 negate = 1.0 - (base * blend);
        
        c.rgb = negate;
        c.rgb *= c.a;
        return c;
    }
}
```

## Troubleshooting

### Common Issues

1. **Shader Not Found**: Ensure shader files are in the correct directory
2. **Incorrect Results**: Verify Photoshop blend mode keys match expected values
3. **Performance Issues**: Monitor draw calls and shader complexity

### Debugging Tips

- Use Unity's Frame Debugger to inspect material assignments
- Check Console for blend mode application warnings
- Compare results with Photoshop using identical test images
- Profile shader performance using Unity Profiler

## References

- [Photoshop Blend Mode Mathematics](https://en.wikipedia.org/wiki/Blend_modes)
- [Unity Shader Documentation](https://docs.unity3d.com/Manual/ShadersOverview.html)
- [HSV Color Space](https://en.wikipedia.org/wiki/HSL_and_HSV)
