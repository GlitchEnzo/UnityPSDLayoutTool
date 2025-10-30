using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;

namespace PsdLayoutTool
{
    /// <summary>
    /// Manages font imports and TextMeshPro font asset creation for PSD imports
    /// </summary>
    public static class FontManager
    {
        #region Constants
        
        /// <summary>
        /// Common system font directories on different platforms
        /// </summary>
        private static readonly string[] SystemFontDirectories = new string[]
        {
            // Windows
            @"C:\Windows\Fonts",
            @"C:\WINDOWS\Fonts",
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            
            // macOS
            "/System/Library/Fonts",
            "/Library/Fonts",
            "~/Library/Fonts",
            
            // Linux
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            "~/.fonts",
            "~/.local/share/fonts"
        };

        /// <summary>
        /// Common font file extensions
        /// </summary>
        private static readonly string[] FontExtensions = { ".ttf", ".otf", ".ttc" };

        /// <summary>
        /// Cache of found fonts to avoid repeated filesystem searches
        /// </summary>
        private static Dictionary<string, string> fontCache = new Dictionary<string, string>();

        /// <summary>
        /// Cache of created TextMeshPro font assets
        /// </summary>
        private static Dictionary<string, TMP_FontAsset> tmpFontCache = new Dictionary<string, TMP_FontAsset>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets or creates a TextMeshPro font asset for the specified font name
        /// </summary>
        /// <param name="fontName">The font name from the PSD layer</param>
        /// <param name="fontSize">The font size for optimal atlas generation</param>
        /// <returns>A TextMeshPro font asset, or null if the font cannot be found/created</returns>
        public static TMP_FontAsset GetOrCreateTMPFont(string fontName, float fontSize = 36f)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                Debug.LogWarning("FontManager: Font name is null or empty, using default font");
                return GetDefaultTMPFont();
            }

            // Check if we already have this font cached
            string cacheKey = $"{fontName}_{fontSize}";
            if (tmpFontCache.ContainsKey(cacheKey))
            {
                return tmpFontCache[cacheKey];
            }

            // Try to find the font file
            string fontPath = FindFontFile(fontName);
            if (string.IsNullOrEmpty(fontPath))
            {
                Debug.LogWarning($"FontManager: Could not find font '{fontName}', using default font");
                TMP_FontAsset defaultFont = GetDefaultTMPFont();
                tmpFontCache[cacheKey] = defaultFont;
                return defaultFont;
            }

            // Import and create TextMeshPro font asset
            TMP_FontAsset tmpFont = CreateTMPFontAsset(fontPath, fontName, fontSize);
            if (tmpFont != null)
            {
                tmpFontCache[cacheKey] = tmpFont;
                Debug.Log($"FontManager: Successfully created TextMeshPro font asset for '{fontName}'");
            }
            else
            {
                Debug.LogWarning($"FontManager: Failed to create TextMeshPro font asset for '{fontName}', using default font");
                TMP_FontAsset defaultFont = GetDefaultTMPFont();
                tmpFontCache[cacheKey] = defaultFont;
                return defaultFont;
            }

            return tmpFont;
        }

        /// <summary>
        /// Clears the font caches (useful for testing or when fonts change)
        /// </summary>
        public static void ClearCache()
        {
            fontCache.Clear();
            tmpFontCache.Clear();
        }

        /// <summary>
        /// Pre-loads fonts found in system directories into cache
        /// </summary>
        public static void PreloadSystemFonts()
        {
            Debug.Log("FontManager: Pre-loading system fonts...");
            int fontsFound = 0;

            foreach (string directory in SystemFontDirectories)
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(directory);
                if (Directory.Exists(expandedPath))
                {
                    foreach (string extension in FontExtensions)
                    {
                        try
                        {
                            string[] fontFiles = Directory.GetFiles(expandedPath, "*" + extension, SearchOption.TopDirectoryOnly);
                            foreach (string fontFile in fontFiles)
                            {
                                string fontName = Path.GetFileNameWithoutExtension(fontFile);
                                if (!fontCache.ContainsKey(fontName.ToLower()))
                                {
                                    fontCache[fontName.ToLower()] = fontFile;
                                    fontsFound++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"FontManager: Error accessing font directory '{expandedPath}': {ex.Message}");
                        }
                    }
                }
            }

            Debug.Log($"FontManager: Pre-loaded {fontsFound} system fonts");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds a font file by name in system directories
        /// </summary>
        /// <param name="fontName">The name of the font to find</param>
        /// <returns>The full path to the font file, or null if not found</returns>
        private static string FindFontFile(string fontName)
        {
            // Check cache first
            string lowerFontName = fontName.ToLower();
            if (fontCache.ContainsKey(lowerFontName))
            {
                return fontCache[lowerFontName];
            }

            // Search system font directories
            foreach (string directory in SystemFontDirectories)
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(directory);
                if (!Directory.Exists(expandedPath)) continue;

                foreach (string extension in FontExtensions)
                {
                    // Try exact match first
                    string exactPath = Path.Combine(expandedPath, fontName + extension);
                    if (File.Exists(exactPath))
                    {
                        fontCache[lowerFontName] = exactPath;
                        return exactPath;
                    }

                    // Try case-insensitive search
                    try
                    {
                        string[] files = Directory.GetFiles(expandedPath, "*" + extension);
                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file);
                            if (string.Equals(fileName, fontName, StringComparison.OrdinalIgnoreCase))
                            {
                                fontCache[lowerFontName] = file;
                                return file;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"FontManager: Error searching directory '{expandedPath}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a TextMeshPro font asset from a font file
        /// </summary>
        /// <param name="fontPath">Path to the font file</param>
        /// <param name="fontName">Name of the font</param>
        /// <param name="fontSize">Size for atlas generation</param>
        /// <returns>The created TextMeshPro font asset</returns>
        private static TMP_FontAsset CreateTMPFontAsset(string fontPath, string fontName, float fontSize)
        {
            try
            {
                // First, import the font file into Unity
                string unityFontPath = ImportFontToUnity(fontPath, fontName);
                if (string.IsNullOrEmpty(unityFontPath))
                {
                    return null;
                }

                // Load the imported font
                Font unityFont = AssetDatabase.LoadAssetAtPath<Font>(unityFontPath);
                if (unityFont == null)
                {
                    Debug.LogError($"FontManager: Failed to load imported font at '{unityFontPath}'");
                    return null;
                }

                // Create TextMeshPro font asset
                string tmpFontPath = $"Assets/PSD Layout Tool/Fonts/TMP_{fontName}.asset";
                EnsureDirectoryExists(Path.GetDirectoryName(tmpFontPath));

                // Check if TMP font already exists
                TMP_FontAsset existingTmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(tmpFontPath);
                if (existingTmpFont != null)
                {
                    return existingTmpFont;
                }

                // Create the TextMeshPro font asset
                TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(unityFont, (int)fontSize, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (tmpFont != null)
                {
                    tmpFont.name = $"TMP_{fontName}";
                    AssetDatabase.CreateAsset(tmpFont, tmpFontPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    Debug.Log($"FontManager: Created TextMeshPro font asset at '{tmpFontPath}'");
                    return tmpFont;
                }
                else
                {
                    Debug.LogError($"FontManager: Failed to create TextMeshPro font asset from font '{fontName}'");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FontManager: Exception creating TextMeshPro font asset for '{fontName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Imports a font file into Unity's Assets folder
        /// </summary>
        /// <param name="fontPath">Path to the external font file</param>
        /// <param name="fontName">Name of the font</param>
        /// <returns>The Unity asset path of the imported font</returns>
        private static string ImportFontToUnity(string fontPath, string fontName)
        {
            try
            {
                string targetPath = $"Assets/PSD Layout Tool/Fonts/{fontName}{Path.GetExtension(fontPath)}";
                EnsureDirectoryExists(Path.GetDirectoryName(targetPath));

                // Check if font already exists
                if (File.Exists(targetPath))
                {
                    return targetPath;
                }

                // Copy the font file
                File.Copy(fontPath, targetPath, true);
                AssetDatabase.ImportAsset(targetPath);
                AssetDatabase.Refresh();

                Debug.Log($"FontManager: Imported font '{fontName}' to '{targetPath}'");
                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FontManager: Failed to import font '{fontName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the default TextMeshPro font asset
        /// </summary>
        /// <returns>The default TextMeshPro font asset</returns>
        private static TMP_FontAsset GetDefaultTMPFont()
        {
            // Try to get the default TextMeshPro font
            TMP_FontAsset defaultFont = Resources.GetBuiltinResource<TMP_FontAsset>("LiberationSans SDF");
            if (defaultFont == null)
            {
                // Fallback to any available TextMeshPro font
                string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (fontGuids.Length > 0)
                {
                    string fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                    defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                }
            }

            return defaultFont;
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">The directory path to ensure exists</param>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        #endregion
    }
}
