# Test PSD Creation Guide for Unity PSD Layout Tool

This guide will help you create a comprehensive PSD file to test all features of the Unity PSD Layout Tool, including the new TextMeshPro integration.

## Canvas Setup
- **Size**: 1920x1080 pixels (standard game resolution)
- **Color Mode**: RGB Color
- **Background**: Transparent or solid color

## Layer Structure Overview

```
📁 UI_Test_Scene (1920x1080)
├── 📁 Header_Section
│   ├── 🖼️ Header_Background (blend: Normal, opacity: 100%)
│   ├── 📝 Game_Title (TextLayer: "Epic Adventure", Arial Bold, 48px, white)
│   └── 📝 Subtitle (TextLayer: "Press Start to Begin", Arial, 24px, #CCCCCC)
├── 📁 Button_Examples|Button
│   ├── 🖼️ Button_Background|Default (solid blue rectangle)
│   ├── 🖼️ Button_Highlight|Highlighted (lighter blue with glow)
│   ├── 🖼️ Button_Press|Pressed (darker blue, slightly smaller)
│   ├── 🖼️ Button_Disabled|Disabled (gray, 50% opacity)
│   └── 📝 Button_Label|Text (TextLayer: "START GAME", Arial Bold, 20px, white)
├── 📁 Animation_Test|Animation|FPS=24
│   ├── 🖼️ Frame_01 (red circle)
│   ├── 🖼️ Frame_02 (orange circle, rotated 45°)
│   ├── 🖼️ Frame_03 (yellow circle, rotated 90°)
│   └── 🖼️ Frame_04 (green circle, rotated 135°)
├── 📁 Blend_Mode_Tests
│   ├── 🖼️ Base_Layer (white rectangle, blend: Normal)
│   ├── 🖼️ Multiply_Test (red circle, blend: Multiply)
│   ├── 🖼️ Screen_Test (blue square, blend: Screen)
│   ├── 🖼️ Overlay_Test (gradient, blend: Overlay)
│   └── 🖼️ Soft_Light_Test (texture, blend: Soft Light)
├── 📁 Text_Showcase
│   ├── 📝 Arial_Text (TextLayer: "Arial Font Test", Arial, 32px, black)
│   ├── 📝 Times_Text (TextLayer: "Times New Roman", Times New Roman, 28px, #333333)
│   ├── 📝 Helvetica_Text (TextLayer: "Helvetica Sample", Helvetica, 30px, #666666)
│   ├── 📝 Left_Aligned (TextLayer: "Left Aligned Text", Arial, 24px, left justify)
│   ├── 📝 Center_Aligned (TextLayer: "Center Aligned", Arial, 24px, center justify)
│   ├── 📝 Right_Aligned (TextLayer: "Right Aligned", Arial, 24px, right justify)
│   └── 📝 Multiline_Text (TextLayer: "Line 1\nLine 2\nLine 3", Arial, 20px)
├── 📁 Layer_Effects_Demo
│   ├── 🖼️ Drop_Shadow (rectangle with drop shadow effect)
│   ├── 🖼️ Outer_Glow (circle with outer glow effect)
│   ├── 🖼️ Color_Overlay (star with color overlay effect)
│   ├── 🖼️ Gradient_Overlay (triangle with gradient overlay)
│   └── 🖼️ Stroke_Effect (square with stroke effect)
├── 📁 Opacity_Tests
│   ├── 🖼️ Full_Opacity (red square, opacity: 100%)
│   ├── 🖼️ Half_Opacity (blue square, opacity: 50%)
│   ├── 🖼️ Quarter_Opacity (green square, opacity: 25%)
│   └── 🖼️ Barely_Visible (yellow square, opacity: 10%)
├── 📁 Mask_Examples
│   ├── 🖼️ Base_Image (colorful gradient)
│   ├── 🎭 Circular_Mask (circle mask applied to base)
│   └── 🖼️ Complex_Shape (star-shaped mask)
├── 📁 Smart_Filter_Tests
│   ├── 🖼️ Gaussian_Blur (image with Gaussian blur filter)
│   ├── 🖼️ Motion_Blur (image with motion blur filter)
│   ├── 🖼️ Sharpen_Filter (image with sharpen filter)
│   └── 🖼️ Noise_Filter (image with noise filter)
├── 📁 Adjustment_Layers
│   ├── 🔧 Brightness_Contrast (adjustment layer affecting layers below)
│   ├── 🔧 Hue_Saturation (hue/saturation adjustment)
│   └── 🔧 Color_Balance (color balance adjustment)
├── 📁 Special_Cases
│   ├── 🖼️ Invisible_Layer (layer with visibility turned off)
│   ├── 🖼️ Zero_Size_Layer (layer with 0 width or height)
│   ├── 📁 Empty_Group (folder with no children)
│   └── 🖼️ Unicode_名前 (layer with Unicode characters in name)
└── 📁 Background_Elements
    ├── 🖼️ Sky_Gradient (large gradient background)
    ├── 🖼️ Ground_Texture (textured ground element)
    └── 🖼️ Decorative_Border (border frame around entire canvas)
```

## Step-by-Step Creation Instructions

### 1. Canvas Setup
1. Create new document: 1920x1080px, 72 DPI, RGB Color
2. Set background to transparent or solid color

### 2. Header Section
```
Group: "Header_Section"
├── Rectangle: 1920x150px at top, gradient blue to dark blue
├── Text: "Epic Adventure" - Arial Bold, 48px, white, centered
└── Text: "Press Start to Begin" - Arial Regular, 24px, #CCCCCC, centered below title
```

### 3. Button Examples (Unity UI Button Test)
```
Group: "Button_Examples|Button"
├── Rectangle: 200x60px, solid blue (#4A90E2) - name: "Button_Background|Default"
├── Rectangle: 200x60px, lighter blue (#6BA6F0) + outer glow - name: "Button_Highlight|Highlighted"  
├── Rectangle: 190x55px, darker blue (#3A7BC8) - name: "Button_Press|Pressed"
├── Rectangle: 200x60px, gray (#808080), 50% opacity - name: "Button_Disabled|Disabled"
└── Text: "START GAME" - Arial Bold, 20px, white, centered - name: "Button_Label|Text"
```

### 4. Animation Test
```
Group: "Animation_Test|Animation|FPS=24"
├── Circle: 50px diameter, red (#FF0000) - name: "Frame_01"
├── Circle: 50px diameter, orange (#FF8000), rotated 45° - name: "Frame_02"
├── Circle: 50px diameter, yellow (#FFFF00), rotated 90° - name: "Frame_03"
└── Circle: 50px diameter, green (#00FF00), rotated 135° - name: "Frame_04"
```

### 5. Blend Mode Tests
```
Group: "Blend_Mode_Tests"
├── Rectangle: 300x200px, white background - Blend: Normal
├── Circle: 100px, red (#FF0000) - Blend: Multiply
├── Square: 80x80px, blue (#0000FF) - Blend: Screen
├── Rectangle: 150x100px, gradient - Blend: Overlay
└── Rectangle: 120x120px, texture/pattern - Blend: Soft Light
```

### 6. Text Showcase (Font Testing)
```
Group: "Text_Showcase"
├── Text: "Arial Font Test" - Arial, 32px, black
├── Text: "Times New Roman Sample" - Times New Roman, 28px, #333333
├── Text: "Helvetica Sample" - Helvetica, 30px, #666666
├── Text: "Left Aligned Text" - Arial, 24px, left alignment
├── Text: "Center Aligned Text" - Arial, 24px, center alignment
├── Text: "Right Aligned Text" - Arial, 24px, right alignment
└── Text: "Line 1\nLine 2\nLine 3" - Arial, 20px, multiline
```

### 7. Layer Effects Demo
```
Group: "Layer_Effects_Demo"
├── Rectangle with Drop Shadow: X offset: 5px, Y offset: 5px, blur: 10px
├── Circle with Outer Glow: color: yellow, size: 15px, opacity: 75%
├── Star with Color Overlay: color: red, blend mode: multiply
├── Triangle with Gradient Overlay: linear gradient, angle: 45°
└── Square with Stroke: color: black, size: 3px, position: outside
```

### 8. Opacity Tests
```
Group: "Opacity_Tests"
├── Red square - Opacity: 100%
├── Blue square - Opacity: 50%
├── Green square - Opacity: 25%
└── Yellow square - Opacity: 10%
```

### 9. Layer Masks
```
Group: "Mask_Examples"
├── Colorful gradient rectangle
├── Same rectangle with circular mask applied
└── Same rectangle with star-shaped mask
```

### 10. Smart Filters (if supported in your Photoshop version)
```
Group: "Smart_Filter_Tests"
├── Image with Gaussian Blur: radius 5px
├── Image with Motion Blur: angle 0°, distance 10px
├── Image with Sharpen filter
└── Image with Add Noise filter
```

## Important Naming Conventions

### Special Tags for Unity Features:
- `|Button` - Creates Unity UI Button component
- `|Animation` - Creates sprite animation
- `|FPS=24` - Sets animation frame rate
- `|Default`, `|Normal`, `|Up` - Button normal state
- `|Highlighted` - Button highlighted state  
- `|Pressed` - Button pressed state
- `|Disabled` - Button disabled state
- `|Text` - Button text element (when not using text layers)

### Layer Organization:
- Use Groups/Folders to organize related elements
- Name layers descriptively
- Avoid special characters that might cause issues (/, :, &, ., <, >, $, ¢, ;, +)
- Test Unicode characters in layer names

## Testing Checklist

When testing your PSD with the Unity PSD Layout Tool:

### ✅ TextMeshPro Integration
- [ ] Text layers import as TextMeshPro components
- [ ] Font matching works for system fonts (Arial, Times New Roman, Helvetica)
- [ ] Font sizes are preserved
- [ ] Text colors are maintained
- [ ] Text alignment (left, center, right) works correctly
- [ ] Multiline text imports properly

### ✅ Layer Processing
- [ ] Groups become GameObjects
- [ ] Layer hierarchy is preserved
- [ ] Layer positioning is accurate
- [ ] Layer opacity is applied
- [ ] Invisible layers are skipped

### ✅ Blend Modes
- [ ] Normal blend mode works
- [ ] Multiply blend mode applies correctly
- [ ] Screen blend mode applies correctly
- [ ] Overlay blend mode applies correctly
- [ ] Other blend modes fall back gracefully

### ✅ Special Features
- [ ] Button tags create UI Button components
- [ ] Animation tags create sprite animations
- [ ] Layer effects are processed
- [ ] Masks are applied correctly

### ✅ Unity UI vs Sprite Mode
- [ ] Test both "Use Unity UI" enabled and disabled
- [ ] Verify GameObjects vs UI elements are created appropriately
- [ ] Check positioning differences between modes

## File Export Settings

When saving your test PSD:
- **Format**: Photoshop (.psd)
- **Compatibility**: Maximize compatibility enabled
- **Layers**: Preserve all layers and groups
- **Compression**: None or minimal

## Sample Usage in Unity

1. Import the PSD into Unity's Assets folder
2. Select the PSD file in Project window
3. In Inspector, configure:
   - Maximum Depth: 10
   - Pixels to Unity Units: 100
   - Use Unity UI: Test both enabled/disabled
4. Click "Layout in Current Scene" or "Generate Prefab"
5. Verify all features work as expected

This comprehensive test PSD will help validate all features of the Unity PSD Layout Tool including the new TextMeshPro integration!
