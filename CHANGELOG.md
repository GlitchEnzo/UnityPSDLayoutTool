# Changelog

All notable changes to the Unity PSD Layout Tool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2025-08-02

### Added
- **TextMeshPro Integration**: Complete TextMeshPro support for all text rendering
  - Automatic system font detection and import from Windows, macOS, and Linux
  - Font matching by name from PSD text layers to system fonts
  - Automatic TextMeshPro font asset creation using Unity's TMP API
  - Font caching system for improved performance on subsequent imports
  - Support for both TextMeshPro (3D) and TextMeshProUGUI (UI) components
  - Graceful fallback to default TMP fonts when system fonts cannot be found
- **FontManager Class**: New comprehensive font management system
  - Cross-platform system font directory scanning
  - Font file caching and TMP asset caching
  - Automatic font import and TMP asset generation
  - Cache management utilities (clear cache, preload fonts)
  - Error handling and logging for font operations

### Changed
- **Text Rendering**: Upgraded from legacy Unity text components to TextMeshPro
  - `TextMesh` components replaced with `TextMeshPro`
  - `UnityEngine.UI.Text` components replaced with `TextMeshProUGUI`
  - All text properties (font size, color, alignment) now use TextMeshPro APIs
- **PSD Import Process**: Enhanced to include font preloading for faster text processing
- **Text Alignment**: Updated to use TextMeshPro's `TextAlignmentOptions` enum

### Improved
- **Text Quality**: Superior text rendering quality with TextMeshPro's advanced features
- **Performance**: Cached font system reduces font lookup time on subsequent imports
- **Cross-Platform Support**: Consistent font handling across Windows, macOS, and Linux
- **Error Handling**: Better error messages and fallback behavior for font-related issues

### Technical Details
- Added TMPro namespace and dependencies
- Integrated FontManager.GetOrCreateTMPFont() in text creation methods
- System font directories automatically scanned on import
- Font assets created in project's Assets folder with proper naming conventions
- Support for TTF and OTF font file formats

## Previous Versions

### Enhanced Features (Previous Updates)
- Full Photoshop blend mode support with mathematically accurate shaders
- Layer effects support (drop shadows, outer glow, color overlay, gradient overlay, stroke)
- Layer masks with automatic alpha channel integration
- Adjustment layers (Brightness/Contrast, Hue/Saturation, Color Balance)
- Smart filters (Gaussian Blur, Motion Blur, Sharpen, Noise, Emboss)
- Unity UI integration with button state support
- Cross-Unity version compatibility (2018.1+)
- CI/CD pipeline with GitHub Actions
- Package Manager distribution support
