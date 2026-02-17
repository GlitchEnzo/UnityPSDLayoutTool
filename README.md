# Unity PSD Layout Tool

[![CI - Quality & Compatibility](https://github.com/gmoyle/UnityPSDLayoutTool/actions/workflows/ci.yml/badge.svg)](https://github.com/gmoyle/UnityPSDLayoutTool/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Unity 2018.1+](https://img.shields.io/badge/Unity-2018.1%2B-blue.svg)](https://unity3d.com)

A lightweight Photoshop Unity import tool for rapid prototyping and scene building with cross-Unity version compatibility.

## 🎯 Features

* **Multi-Unity Support**: Works with Unity 2018.1+ (including 2019, 2020, 2021, 2022+)
* **TextMeshPro Integration**: Automatically converts PSD text layers to TextMeshPro and TextMeshProUGUI components with font matching and import.
* **Layout PSD layers as Unity Sprites** with proper positioning and pivot handling
  * Create Sprite animations using layer sets as animation frames
  * **Full Photoshop Blend Mode Support**: Automatically applies all 16 Photoshop blend modes to Unity materials with mathematically accurate shaders
  * **Layer Opacity Support**: Preserves Photoshop layer opacity settings in Unity
  * **Invisible Layer Filtering**: Skips invisible layers during import to match Photoshop visibility
  * **Layer Effects Support**: Drop shadows, outer glow, color overlay, gradient overlay, and stroke effects
  * **Layer Masks**: Full support for layer masks with automatic alpha channel integration
  * **Adjustment Layers**: Support for Brightness/Contrast, Hue/Saturation, and Color Balance adjustments
  * **Smart Filters**: Support for Gaussian Blur, Motion Blur, Sharpen, Noise, and Emboss filters
* **Unity UI Integration** - Generate Unity UI elements instead of standard GameObjects
  * Create Button objects with multiple states from layer groups
  * **UI Text Support**: Creates proper UI Text components from Photoshop text layers
* **Flexible Output Options**:
  * Generate prefabs for reusable assets
  * Layout directly in current scene
  * Export individual layers as PNG textures
* **Enhanced Positioning**: Improved pivot and canvas-based positioning system
* **Package Manager Ready**: Distributed as a proper Unity package

## 📦 Installation

### Option 1: Unity Package Manager (Recommended)
1. Open Unity Package Manager
2. Click "+" → "Add package from git URL"
3. Enter: `https://github.com/gmoyle/UnityPSDLayoutTool.git`

### Option 2: Download Release
1. Go to [Releases](https://github.com/gmoyle/UnityPSDLayoutTool/releases)
2. Download the latest `.unitypackage` file
3. Import into your Unity project

### Option 3: Manual Installation
Simply copy the `Assets/PSD Layout Tool` folder into your project's Assets directory.

How to Use
==========
The PSD Layout Tool is implemented as a Unity Custom Inspector.  If you select a PSD file that you have in your project (Assets folder) special buttons will appear above the default importer settings.

![](screenshots/inspector.png?raw=true)

* **Maximum Depth**
  * The maximum depth value (Z position) to use when laying the layers out.  The front-most layer (minimum depth) is always 0.
* **Pixels to Unity Units**
  * The scale to use when generating Unity Sprites, in pixels to Unity world units (meters).
* **Use Unity UI**
  * Check to generate Unity 4.6+ UI elements instead of "normal" GameObjects.
* **Export Layers as Textures**
  * Creates a .png image file for each layer in the PSD file, using the same folder structure.
* **Layout in Current Scene**
  * Creates a Unity 4.3+ Sprite object for each layer in the PSD file.  It is laid out to match the PSD's layout and folder structure.
* **Generate Prefab**
  * Identical to the previous option, but it generates a .prefab file instead of putting the objects in the scene.

Special Tags
==========
Layers can have special tags applied to them that flags them to have the layout tool perform special operations on them.

### Group Layer Tags ###

|        Tag        | Description |
| ----------------- | ----------- |
|  &#124;Animation  |  Creates a Sprite animation using all of the children layers as frames |
|  &#124;FPS=##     |  The number of frames per second to use for a Sprite animation.  Defaults to 30 if not present  | 
|  &#124;Button     |  Creates a Button object using any tagged children layers as the button states |

### Art Layer Tags ###

|        Tag          | Description |
| -----------------   | ----------- |
|  &#124;Disabled     |  Represents the disabled state of a button     |
|  &#124;Highlighted  |  Represents the highlighted state of a button  | 
|  &#124;Pressed      |  Represents the pressed state of a button  | 
|  &#124;Default      |  Represents the default/enabled/normal/up state of a button  | 
|  &#124;Enabled      |  Represents the default/enabled/normal/up state of a button  |
|  &#124;Normal       |  Represents the default/enabled/normal/up state of a button  |
|  &#124;Up           |  Represents the default/enabled/normal/up state of a button  |
|  &#124;Text         |  Represents a **texture** that is the text of a button (normal text layers import without this tag)  |

## TextMeshPro Integration

The Unity PSD Layout Tool now provides **full TextMeshPro integration** for superior text rendering quality and performance:

### Automatic Font Import
- **System Font Detection**: Automatically scans Windows, macOS, and Linux system font directories
- **Font Matching**: Matches PSD text layer fonts with system-installed TTF/OTF fonts by name
- **Automatic Import**: Imports matched fonts into Unity's Assets folder if not already present
- **TMP Asset Creation**: Automatically creates TextMeshPro font assets using Unity's TextMeshPro API
- **Font Caching**: Caches font files and TMP assets for improved performance on subsequent imports

### Text Component Creation
- **3D Text**: Creates `TextMeshPro` components for sprite-based layouts
- **UI Text**: Creates `TextMeshProUGUI` components for Unity UI layouts
- **Property Preservation**: Maintains text content, font size, color, and alignment from PSD layers
- **Fallback Support**: Uses default TMP fonts when system fonts cannot be found or imported

### Supported Font Properties
- **Font Family**: Matches font family names from PSD text layers
- **Font Size**: Preserves original font size from Photoshop
- **Text Color**: Maintains text fill color from PSD layers
- **Text Alignment**: Supports Left, Center, and Right text justification
- **Multi-line Text**: Handles multi-line text content properly

### Font Management
- **Preloading**: System fonts are preloaded during PSD import for faster processing
- **Cache Management**: Built-in cache clearing and font reloading capabilities
- **Error Handling**: Graceful fallback to default fonts when font import fails
- **Cross-Platform**: Works consistently across Windows, macOS, and Linux development environments

### Usage Notes
- **Automatic Operation**: Font matching and import happens automatically during PSD import
- **No Configuration**: No manual setup required - fonts are detected and imported transparently
- **Performance**: First import may take longer due to font asset creation, subsequent imports are faster
- **Font Requirements**: System fonts must be installed and accessible to be imported

## Photoshop Compatibility

Smart Objects are supported, and do not need to be flattened/rasterized in Photoshop before importing.

## Blend Mode Support

The Unity PSD Layout Tool provides **complete Photoshop blend mode compatibility** with mathematically accurate shader implementations. All 16 standard Photoshop blend modes are supported and automatically applied during import.

## Supported Blend Modes

### Basic Blend Modes
| Blend Mode | Photoshop Key | Description | Implementation |
|------------|---------------|-------------|----------------|
| **Normal** | `norm` | Standard alpha blending | Unity default material |
| **Multiply** | `mult` | Darkens by multiplying colors | Custom multiply shader |
| **Screen** | `scrn` | Lightens by inverting, multiplying, and inverting again | Custom screen shader |
| **Overlay** | `over` | Combines multiply and screen based on base color | Custom overlay shader |

### Light Blend Modes
| Blend Mode | Photoshop Key | Description | Implementation |
|------------|---------------|-------------|----------------|
| **Soft Light** | `sLit` | Softer version of overlay | Custom soft light shader |
| **Hard Light** | `hLit` | Combines multiply and screen based on blend color | Custom hard light shader |
| **Color Dodge** | `cDdg` | Brightens base color by decreasing contrast | Custom color dodge shader |
| **Color Burn** | `cBrn` | Darkens base color by increasing contrast | Custom color burn shader |

### Comparative Blend Modes
| Blend Mode | Photoshop Key | Description | Implementation |
|------------|---------------|-------------|----------------|
| **Darken** | `dark` | Selects the darker of base and blend colors | Custom darken shader |
| **Lighten** | `lite` | Selects the lighter of base and blend colors | Custom lighten shader |
| **Difference** | `diff` | Subtracts darker color from lighter color | Custom difference shader |
| **Exclusion** | `smud` | Similar to difference but with lower contrast | Custom exclusion shader |

### HSV-Based Blend Modes
| Blend Mode | Photoshop Key | Description | Implementation |
|------------|---------------|-------------|----------------|
| **Hue** | `hue ` | Uses hue from blend, saturation/value from base | HSV color space conversion shader |
| **Saturation** | `sat ` | Uses saturation from blend, hue/value from base | HSV color space conversion shader |
| **Color** | `colr` | Uses hue/saturation from blend, value from base | HSV color space conversion shader |
| **Luminosity** | `lum ` | Uses value from blend, hue/saturation from base | HSV color space conversion shader |

## Technical Implementation

### Sprite Renderers
For sprite-based layouts, blend modes are applied by:
1. **Automatic Detection**: The tool reads the blend mode key from each PSD layer
2. **Material Creation**: Custom materials are created using the appropriate blend mode shader
3. **Shader Assignment**: The material is automatically assigned to the sprite renderer

### UI Elements
For Unity UI layouts, blend modes are handled with:
- **Limited Support**: Basic blend modes (Multiply, Screen, Overlay, Soft Light, Hard Light) work with UI materials
- **Fallback Behavior**: Advanced blend modes fall back to normal rendering for UI elements due to UI rendering pipeline limitations

### Shader Quality
- **Mathematical Accuracy**: All shaders implement the exact mathematical formulas used by Photoshop
- **Color Space Conversion**: HSV-based blend modes include proper RGB↔HSV conversion functions
- **Performance Optimized**: Shaders are optimized for real-time rendering while maintaining accuracy
- **Unity Compatible**: All shaders follow Unity's sprite shader conventions and support standard features

## Usage Notes

### Automatic Application
Blend modes are automatically applied during import - no manual configuration required:
```
1. Import your PSD file into Unity
2. Select the PSD file in the Project window
3. Use "Layout in Current Scene" or "Generate Prefab" 
4. Blend modes are automatically detected and applied
```

### Performance Considerations
- **Shader Compilation**: First use of each blend mode may cause a brief shader compilation
- **Draw Calls**: Each unique blend mode creates a separate draw call
- **Mobile Performance**: Complex blend modes (HSV-based) may impact performance on low-end mobile devices

### Troubleshooting
- **Missing Shaders**: If blend mode shaders are missing, the tool falls back to Unity's default sprite material
- **Warning Messages**: Check the Console for any blend mode application warnings
- **Unexpected Results**: Ensure your PSD layers use standard Photoshop blend modes (custom blend modes are not supported)

![](screenshots/photoshop.jpg?raw=true)
![](screenshots/photoshop.jpg?raw=true)

## 🔧 Development & Contributing

### CI/CD Pipeline
This repository uses GitHub Actions for continuous integration:

- **Quality & Compatibility CI**: Runs on every push/PR - validates package structure, code syntax, and Unity compatibility
- **Unity Testing**: Available but disabled until Unity license secrets are configured (optional)
- **Release Automation**: Creates packages and GitHub releases on version tags

### Running Locally
1. Clone the repository
2. Open with Unity 2018.1 or newer
3. Make your changes
4. Test with sample PSD files

### Setting Up CI/CD
For full Unity testing capabilities, see [GitHub Secrets Setup](.github/SETUP_SECRETS.md).

### Creating a Release
1. Update version in `Assets/PSD Layout Tool/package.json`
2. Commit and push changes
3. Create a new tag: `git tag v1.0.4 && git push origin v1.0.4`
4. GitHub Actions will automatically create the release

## 📋 Version Compatibility

| Unity Version | Status | Notes |
|---------------|-----------|-------|
| 2022.x | ✅ Tested | Full support |
| 2021.x | ✅ Tested | Full support |
| 2020.x | ✅ Tested | Full support |
| 2019.x | ✅ Tested | Full support |
| 2018.1+ | ✅ Tested | Full support |
| 2017.x | ⚠️ Limited | May work but not tested |
| Unity 5 | ⚠️ Legacy | Legacy support only |
| Unity 4 | ⚠️ Legacy | Legacy support only |

## 🐛 Issues & Support

- **Bug Reports**: Please use [GitHub Issues](https://github.com/gmoyle/UnityPSDLayoutTool/issues)
- **Feature Requests**: Submit via GitHub Issues with the "enhancement" label
- **Questions**: Check existing issues first, then create a new one

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 Acknowledgments

- Original work by [GlitchEnzo](https://github.com/GlitchEnzo/UnityPSDLayoutTool)
- Enhanced with Unity 2020+ compatibility and improved error handling
- CI/CD pipeline and package management improvements
>>>>>>> 82ff974d9d9cd0c494ac35c7f386e2d4903f9461
