using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PhotoshopFile;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PsdLayoutTool;

namespace PsdLayoutTool
{
    /// <summary>
    /// Photoshop blend modes that can be applied to Unity components
    /// </summary>
    public enum BlendMode
    {
        Normal,
        Multiply,
        Screen,
        Overlay,
        SoftLight,
        HardLight,
        ColorDodge,
        ColorBurn,
        Darken,
        Lighten,
        Difference,
        Exclusion,
        Hue,
        Saturation,
        Color,
        Luminosity
    }
    /// <summary>
    /// Handles all of the importing for a PSD file (exporting textures, creating prefabs, etc).
    /// </summary>
    public static class PsdImporter
    {
        /// <summary>
        /// The current file path to use to save layers as .png files
        /// </summary>
        private static string currentPath;

        /// <summary>
        /// The <see cref="GameObject"/> representing the root PSD layer.  It contains all of the other layers as children GameObjects.
        /// </summary>
        private static GameObject rootPsdGameObject;

        /// <summary>
        /// The <see cref="GameObject"/> representing the current group (folder) we are processing.
        /// </summary>
        private static GameObject currentGroupGameObject;

        /// <summary>
        /// The current depth (Z axis position) that sprites will be placed on.  It is initialized to the MaximumDepth ("back" depth) and it is automatically
        /// decremented as the PSD file is processed, back to front.
        /// </summary>
        private static float currentDepth;

        /// <summary>
        /// The amount that the depth decrements for each layer.  This is automatically calculated from the number of layers in the PSD file and the MaximumDepth.
        /// </summary>
        private static float depthStep;

        /// <summary>
        /// Initializes static members of the <see cref="PsdImporter"/> class.
        /// </summary>
        static PsdImporter()
        {
            MaximumDepth = 10;
            PixelsToUnits = 100;
        }

        /// <summary>
        /// Gets or sets the maximum depth.  This is where along the Z axis the back will be, with the front being at 0.
        /// </summary>
        public static float MaximumDepth { get; set; }

        /// <summary>
        /// Gets or sets the number of pixels per Unity unit value.  Defaults to 100 (which matches Unity's Sprite default).
        /// </summary>
        public static float PixelsToUnits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the Unity 4.6+ UI system or not.
        /// </summary>
        public static bool UseUnityUI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the import process should create <see cref="GameObject"/>s in the scene.
        /// </summary>
        private static bool LayoutInScene { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the import process should create a prefab in the project's assets.
        /// </summary>
        private static bool CreatePrefab { get; set; }

        /// <summary>
        /// Gets or sets the size (in pixels) of the entire PSD canvas.
        /// </summary>
        private static Vector2 CanvasSize { get; set; }

        /// <summary>
        /// Gets or sets the name of the current 
        /// </summary>
        private static string PsdName { get; set; }

        /// <summary>
        /// Gets or sets the Unity 4.6+ UI canvas.
        /// </summary>
        private static GameObject Canvas { get; set; }

        /// <summary>
        /// Gets or sets the current <see cref="PsdFile"/> that is being imported.
        /// </summary>
        ////private static PsdFile CurrentPsdFile { get; set; }

        /// <summary>
        /// Exports each of the art layers in the PSD file as separate textures (.png files) in the project's assets.
        /// </summary>
        /// <param name="assetPath">The path of to the .psd file relative to the project.</param>
        public static void ExportLayersAsTextures(string assetPath)
        {
            LayoutInScene = false;
            CreatePrefab = false;
            Import(assetPath);
        }

        /// <summary>
        /// Lays out sprites in the current scene to match the PSD's layout.  Each layer is exported as Sprite-type textures in the project's assets.
        /// </summary>
        /// <param name="assetPath">The path of to the .psd file relative to the project.</param>
        public static void LayoutInCurrentScene(string assetPath)
        {
            LayoutInScene = true;
            CreatePrefab = false;
            Import(assetPath);
        }

        /// <summary>
        /// Generates a prefab consisting of sprites laid out to match the PSD's layout. Each layer is exported as Sprite-type textures in the project's assets.
        /// </summary>
        /// <param name="assetPath">The path of to the .psd file relative to the project.</param>
        public static void GeneratePrefab(string assetPath)
        {
            LayoutInScene = false;
            CreatePrefab = true;
            Import(assetPath);
        }

        /// <summary>
        /// Imports a Photoshop document (.psd) file at the given path.
        /// </summary>
        /// <param name="asset">The path of to the .psd file relative to the project.</param>
        private static void Import(string asset)
        {
            currentDepth = MaximumDepth;
            string fullPath = Path.Combine(GetFullProjectPath(), asset.Replace('\\', '/'));

            PsdFile psd = new PsdFile(fullPath);
            CanvasSize = new Vector2(psd.Width, psd.Height);

            // Pre-load system fonts for faster lookup during import
            FontManager.PreloadSystemFonts();

            // Apply layer masks
            ApplyLayerMasks(psd.Layers);

            // Apply adjustment layers
            ApplyAdjustmentLayers(psd.Layers);
            
            // Apply smart filters
            ApplySmartFilters(psd.Layers);
            depthStep = psd.Layers.Count != 0 ? MaximumDepth / psd.Layers.Count : 0.1f;

            int lastSlash = asset.LastIndexOf('/');
            string assetPathWithoutFilename = asset.Remove(lastSlash + 1, asset.Length - (lastSlash + 1));
            PsdName = asset.Replace(assetPathWithoutFilename, string.Empty).Replace(".psd", string.Empty);

            currentPath = GetFullProjectPath() + assetPathWithoutFilename + PsdName;
            Directory.CreateDirectory(currentPath);

            if (LayoutInScene || CreatePrefab)
            {
                if (UseUnityUI)
                {
                    CreateUIEventSystem();
                    CreateUICanvas();
                    rootPsdGameObject = Canvas;
                }
                else
                {
                    float x = 0 / PixelsToUnits;
                    float y = 0 / PixelsToUnits;
                    y = (CanvasSize.y / PixelsToUnits) - y;
                    float width = psd.Width / PixelsToUnits;
                    float height = psd.Height / PixelsToUnits;

                    rootPsdGameObject = new GameObject(PsdName);
                    rootPsdGameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
                }

                currentGroupGameObject = rootPsdGameObject;
            }

            List<Layer> tree = BuildLayerTree(psd.Layers);
            ExportTree(tree);

            if (CreatePrefab)
            {
                string prefabPath = asset.Replace(".psd", ".prefab");
#if UNITY_2018_1_OR_NEWER
                // Use the new Prefab API for Unity 2018+
                PrefabUtility.SaveAsPrefabAsset(rootPsdGameObject, prefabPath);
#else
                // Use the legacy Prefab API for older Unity versions
                UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
                PrefabUtility.ReplacePrefab(rootPsdGameObject, prefab);
#endif

                if (!LayoutInScene)
                {
                    // if we are not flagged to layout in the scene, delete the GameObject used to generate the prefab
                    UnityEngine.Object.DestroyImmediate(rootPsdGameObject);
                }
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Constructs a tree collection based on the PSD layer groups from the raw list of layers.
        /// </summary>
        /// <param name="flatLayers">The flat list of all layers.</param>
        /// <returns>The layers reorganized into a tree structure based on the layer groups.</returns>
        private static List<Layer> BuildLayerTree(List<Layer> flatLayers)
        {
            // There is no tree to create if there are no layers
            if (flatLayers == null)
            {
                return null;
            }

            // PSD layers are stored backwards (with End Groups before Start Groups), so we must reverse them
            flatLayers.Reverse();

            List<Layer> tree = new List<Layer>();
            Layer currentGroupLayer = null;
            Stack<Layer> previousLayers = new Stack<Layer>();

            foreach (Layer layer in flatLayers)
            {
                if (IsEndGroup(layer))
                {
                    if (previousLayers.Count > 0)
                    {
                        Layer previousLayer = previousLayers.Pop();
                        previousLayer.Children.Add(currentGroupLayer);
                        currentGroupLayer = previousLayer;
                    }
                    else if (currentGroupLayer != null)
                    {
                        tree.Add(currentGroupLayer);
                        currentGroupLayer = null;
                    }
                }
                else if (IsStartGroup(layer))
                {
                    // push the current layer
                    if (currentGroupLayer != null)
                    {
                        previousLayers.Push(currentGroupLayer);
                    }

                    currentGroupLayer = layer;
                }
                else if (layer.Rect.width != 0 && layer.Rect.height != 0)
                {
                    // It must be a text layer or image layer
                    if (currentGroupLayer != null)
                    {
                        currentGroupLayer.Children.Add(layer);
                    }
                    else
                    {
                        tree.Add(layer);
                    }
                }
            }

            // if there are any dangling layers, add them to the tree
            if (tree.Count == 0 && currentGroupLayer != null && currentGroupLayer.Children.Count > 0)
            {
                tree.Add(currentGroupLayer);
            }

            return tree;
        }

        /// <summary>
        /// Fixes any layer names that would cause problems.
        /// </summary>
        /// <param name="name">The name of the layer</param>
        /// <returns>The fixed layer name</returns>
        private static string MakeNameSafe(string name)
        {
            // replace all special characters with an underscore
            Regex pattern = new Regex("[/:&.<>,$¢;+]");
            string newName = pattern.Replace(name, "_");

            if (name != newName)
            {
                Debug.Log(string.Format("Layer name \"{0}\" was changed to \"{1}\"", name, newName));
            }

            return newName;
        }

        /// <summary>
        /// Returns true if the given <see cref="Layer"/> is marking the start of a layer group.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to check if it's the start of a group</param>
        /// <returns>True if the layer starts a group, otherwise false.</returns>
        private static bool IsStartGroup(Layer layer)
        {
            return layer.IsPixelDataIrrelevant;
        }

        /// <summary>
        /// Returns true if the given <see cref="Layer"/> is marking the end of a layer group.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to check if it's the end of a group.</param>
        /// <returns>True if the layer ends a group, otherwise false.</returns>
        private static bool IsEndGroup(Layer layer)
        {
            return layer.Name.Contains("</Layer set>") ||
                layer.Name.Contains("</Layer group>") ||
                (layer.Name == " copy" && layer.Rect.height == 0);
        }

        /// <summary>
        /// Gets full path to the current Unity project. In the form "C:/Project/".
        /// </summary>
        /// <returns>The full path to the current Unity project.</returns>
        private static string GetFullProjectPath()
        {
            string projectDirectory = Application.dataPath;

            // remove the Assets folder from the end since each imported asset has it already in its local path
            if (projectDirectory.EndsWith("Assets"))
            {
                projectDirectory = projectDirectory.Remove(projectDirectory.Length - "Assets".Length);
            }

            return projectDirectory;
        }

        /// <summary>
        /// Gets the relative path of a full path to an asset.
        /// </summary>
        /// <param name="fullPath">The full path to the asset.</param>
        /// <returns>The relative path to the asset.</returns>
        private static string GetRelativePath(string fullPath)
        {
            return fullPath.Replace(GetFullProjectPath(), string.Empty);
        }

        #region Layer Exporting Methods

        /// <summary>
        /// Processes and saves the layer tree.
        /// </summary>
        /// <param name="tree">The layer tree to export.</param>
        private static void ExportTree(List<Layer> tree)
        {
            // we must go through the tree in reverse order since Unity draws from back to front, but PSDs are stored front to back
            for (int i = tree.Count - 1; i >= 0; i--)
            {
                ExportLayer(tree[i]);
            }
        }

        /// <summary>
        /// Exports a single layer from the tree.
        /// </summary>
        /// <param name="layer">The layer to export.</param>
        private static void ExportLayer(Layer layer)
        {

            layer.Name = MakeNameSafe(layer.Name);
            
            // Skip invisible layers
            if (!layer.Visible)
            {
                return;
            }
            
            if (layer.Children.Count > 0 || layer.Rect.width == 0)
            {
                ExportFolderLayer(layer);
            }
            else
            {
                ExportArtLayer(layer);
            }
        }

        /// <summary>
        /// Exports a <see cref="Layer"/> that is a folder containing child layers.
        /// </summary>
        /// <param name="layer">The layer that is a folder.</param>
        private static void ExportFolderLayer(Layer layer)
        {
            if (layer.Name.ContainsIgnoreCase("|Button"))
            {
                layer.Name = layer.Name.ReplaceIgnoreCase("|Button", string.Empty);

                if (UseUnityUI)
                {
                    CreateUIButton(layer);
                }
                else
                {
                    ////CreateGUIButton(layer);
                }
            }
            else if (layer.Name.ContainsIgnoreCase("|Animation"))
            {
                layer.Name = layer.Name.ReplaceIgnoreCase("|Animation", string.Empty);

                string oldPath = currentPath;
                GameObject oldGroupObject = currentGroupGameObject;

                currentPath = Path.Combine(currentPath, layer.Name.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                Directory.CreateDirectory(currentPath);

                if (UseUnityUI)
                {
                    ////CreateUIAnimation(layer);
                }
                else
                {
                    CreateAnimation(layer);
                }

                currentPath = oldPath;
                currentGroupGameObject = oldGroupObject;
            }
            else
            {
                // it is a "normal" folder layer that contains children layers
                string oldPath = currentPath;
                GameObject oldGroupObject = currentGroupGameObject;

                currentPath = Path.Combine(currentPath, layer.Name);
                Directory.CreateDirectory(currentPath);

                if (LayoutInScene || CreatePrefab)
                {
                    currentGroupGameObject = CreateEmptyObject(layer);
                    currentGroupGameObject.transform.parent = oldGroupObject.transform;
                }

                ExportTree(layer.Children);

                currentPath = oldPath;
                currentGroupGameObject = oldGroupObject;
            }
        }

        /// <summary>
        /// Checks if the string contains the given string, while ignoring any casing.
        /// </summary>
        /// <param name="source">The source string to check.</param>
        /// <param name="toCheck">The string to search for in the source string.</param>
        /// <returns>True if the string contains the search string, otherwise false.</returns>
        private static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Replaces any instance of the given string in this string with the given string.
        /// </summary>
        /// <param name="str">The string to replace sections in.</param>
        /// <param name="oldValue">The string to search for.</param>
        /// <param name="newValue">The string to replace the search string with.</param>
        /// <returns>The replaced string.</returns>
        private static string ReplaceIgnoreCase(this string str, string oldValue, string newValue)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase);
            }

            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        /// <summary>
        /// Exports an art layer as an image file and sprite.  It can also generate text meshes from text layers.
        /// </summary>
        /// <param name="layer">The art layer to export.</param>
        private static void ExportArtLayer(Layer layer)
        {
            if (!layer.IsTextLayer)
            {
                if (LayoutInScene || CreatePrefab)
                {
                    // create a sprite from the layer to lay it out in the scene
                    if (!UseUnityUI)
                    {
                        CreateSpriteGameObject(layer);
                    }
                    else
                    {
                        CreateUIImage(layer);
                    }
                }
                else
                {
                    // it is not being laid out in the scene, so simply save out the .png file
                    CreatePNG(layer);
                }
            }
            else
            {
                // it is a text layer
                if (LayoutInScene || CreatePrefab)
                {
                    // create text mesh
                    if (!UseUnityUI)
                    {
                        CreateTextGameObject(layer);
                    }
                    else
                    {
                        CreateUIText(layer);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the given <see cref="Layer"/> as a PNG on the hard drive.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to save as a PNG.</param>
        /// <returns>The filepath to the created PNG file.</returns>
        private static string CreatePNG(Layer layer)
        {
            string file = string.Empty;

            if (layer.Children.Count == 0 && layer.Rect.width > 0)
            {
                // decode the layer into a texture
                Texture2D texture = ImageDecoder.DecodeImage(layer);

                file = Path.Combine(currentPath, layer.Name + ".png");

                File.WriteAllBytes(file, texture.EncodeToPNG());
            }

            return file;
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to use to create a <see cref="Sprite"/>.</param>
        /// <returns>The created <see cref="Sprite"/> object.</returns>
        private static Sprite CreateSprite(Layer layer)
        {
            return CreateSprite(layer, PsdName);
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to use to create a <see cref="Sprite"/>.</param>
        /// <param name="packingTag">The tag used for Unity's atlas packer.</param>
        /// <returns>The created <see cref="Sprite"/> object.</returns>
        private static Sprite CreateSprite(Layer layer, string packingTag)
        {
            Sprite sprite = null;

            if (layer.Children.Count == 0 && layer.Rect.width > 0)
            {
                string file = CreatePNG(layer);
                sprite = ImportSprite(GetRelativePath(file), packingTag);
            }

            return sprite;
        }

        /// <summary>
        /// Imports the <see cref="Sprite"/> at the given path, relative to the Unity project. For example "Assets/Textures/texture.png".
        /// </summary>
        /// <param name="relativePathToSprite">The path to the sprite, relative to the Unity project "Assets/Textures/texture.png".</param>
        /// <param name="packingTag">The tag to use for Unity's atlas packing.</param>
        /// <returns>The imported image as a <see cref="Sprite"/> object.</returns>
        private static Sprite ImportSprite(string relativePathToSprite, string packingTag)
        {
            AssetDatabase.ImportAsset(relativePathToSprite, ImportAssetOptions.ForceUpdate);

            // change the importer to make the texture a sprite
            TextureImporter textureImporter = AssetImporter.GetAtPath(relativePathToSprite) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.mipmapEnabled = false;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
                textureImporter.maxTextureSize = 2048;
                textureImporter.spritePixelsPerUnit = PixelsToUnits;
                textureImporter.spritePackingTag = packingTag;
            }

            AssetDatabase.ImportAsset(relativePathToSprite, ImportAssetOptions.ForceUpdate);

            Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath(relativePathToSprite, typeof(Sprite));
            return sprite;
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a <see cref="TextMeshPro"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create a <see cref="TextMeshPro"/> from.</param>
        private static void CreateTextGameObject(Layer layer)
        {
            Color color = layer.FillColor;

            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            TextMeshPro textMeshPro = gameObject.AddComponent<TextMeshPro>();
            textMeshPro.text = layer.Text;
            textMeshPro.fontSize = layer.FontSize;
            textMeshPro.color = color;
            textMeshPro.autoSizeTextContainer = true;

            // Apply the correct font from PSD
            TMP_FontAsset fontAsset = FontManager.GetOrCreateTMPFont(layer.FontName, layer.FontSize);
            if (fontAsset != null)
            {
                textMeshPro.font = fontAsset;
            }

            switch (layer.Justification)
            {
                case TextJustification.Left:
                    textMeshPro.alignment = TextAlignmentOptions.Left;
                    break;
                case TextJustification.Right:
                    textMeshPro.alignment = TextAlignmentOptions.Right;
                    break;
                case TextJustification.Center:
                    textMeshPro.alignment = TextAlignmentOptions.Center;
                    break;
            }
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a sprite from the given <see cref="Layer"/>
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create the sprite from.</param>
        /// <returns>The <see cref="SpriteRenderer"/> component attached to the new sprite <see cref="GameObject"/>.</returns>
        private static SpriteRenderer CreateSpriteGameObject(Layer layer)
        {
            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSprite(layer);
            
            // Apply layer opacity
            Color color = spriteRenderer.color;
            color.a = layer.Opacity / 255f;
            spriteRenderer.color = color;
            
            // Apply blend mode
            ApplyBlendMode(spriteRenderer, layer.BlendModeKey);
            
            // Apply layer effects
            LayerEffectsApplicator.ApplyEffectsToSprite(spriteRenderer, layer, PixelsToUnits);
            
            return spriteRenderer;
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a sprite from the given <see cref="Layer"/>
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create the sprite from.</param>
        private static GameObject CreateEmptyObject(Layer layer)
        {
<<<<<<< HEAD
=======
            if (layer == null)
            {
                Debug.LogError("Cannot create empty object: layer is null");
                return null;
            }

            if (layer.PsdFile == null)
            {
                Debug.LogError($"Cannot create empty object for layer '{layer.Name}': PsdFile reference is null");
                return null;
            }

            // Avoid division by zero
            if (PixelsToUnits == 0)
            {
                Debug.LogError("PixelsToUnits cannot be zero. Using default value of 100.");
                PixelsToUnits = 100f;
            }

>>>>>>> 82ff974d9d9cd0c494ac35c7f386e2d4903f9461
            float x = 0 / PixelsToUnits;
            float y = 0 / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.PsdFile.Width / PixelsToUnits;
            float height = layer.PsdFile.Height / PixelsToUnits;

<<<<<<< HEAD
            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;
=======
            GameObject gameObject = new GameObject(layer.Name ?? "EmptyObject");
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            
            if (currentGroupGameObject != null)
            {
                gameObject.transform.parent = currentGroupGameObject.transform;
            }
>>>>>>> 82ff974d9d9cd0c494ac35c7f386e2d4903f9461

            currentDepth -= depthStep;

            return gameObject;
        }

        /// <summary>
        /// Creates a Unity sprite animation from the given <see cref="Layer"/> that is a group layer.  It grabs all of the children art
        /// layers and uses them as the frames of the animation.
        /// </summary>
        /// <param name="layer">The group <see cref="Layer"/> to use to create the sprite animation.</param>
        private static void CreateAnimation(Layer layer)
        {
            float fps = 30;

            string[] args = layer.Name.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string arg in args)
            {
                if (arg.ContainsIgnoreCase("FPS="))
                {
                    layer.Name = layer.Name.Replace("|" + arg, string.Empty);

                    string[] fpsArgs = arg.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!float.TryParse(fpsArgs[1], out fps))
                    {
                        Debug.LogError(string.Format("Unable to parse FPS: \"{0}\"", arg));
                    }
                }
            }

            List<Sprite> frames = new List<Sprite>();

            if (layer.Children == null || layer.Children.Count == 0)
            {
                Debug.LogError($"Cannot create animation for layer '{layer.Name}': no child layers found.");
                return;
            }

            Layer firstChild = layer.Children.First();
            SpriteRenderer spriteRenderer = CreateSpriteGameObject(firstChild);
            spriteRenderer.name = layer.Name;

            foreach (Layer child in layer.Children)
            {
                frames.Add(CreateSprite(child, layer.Name));
            }

            spriteRenderer.sprite = frames[0];

<<<<<<< HEAD
#if UNITY_2018 || UNITY_2020 || UNITY_5
=======
#if UNITY_2018_1_OR_NEWER || UNITY_5
>>>>>>> 82ff974d9d9cd0c494ac35c7f386e2d4903f9461
            // Create Animator Controller with an Animation Clip
            UnityEditor.Animations.AnimatorController controller = new UnityEditor.Animations.AnimatorController();
            controller.AddLayer("Base Layer");

            UnityEditor.Animations.AnimatorControllerLayer controllerLayer = controller.layers[0];
            UnityEditor.Animations.AnimatorState state = controllerLayer.stateMachine.AddState(layer.Name);
            state.motion = CreateSpriteAnimationClip(layer.Name, frames, fps);

            AssetDatabase.CreateAsset(controller, GetRelativePath(currentPath) + "/" + layer.Name + ".controller");
#else // Unity 4
            // Create Animator Controller with an Animation Clip
            UnityEditor.Animations.AnimatorController controller = new UnityEditor.Animations.AnimatorController();
            UnityEditor.Animations.AnimatorControllerLayer controllerLayer = controller.AddLayer("Base Layer");

            UnityEditor.Animations.AnimatorState state = controllerLayer.stateMachine.AddState(layer.Name);
            state.SetAnimationClip(CreateSpriteAnimationClip(layer.Name, frames, fps));

            AssetDatabase.CreateAsset(controller, GetRelativePath(currentPath) + "/" + layer.Name + ".controller");
#endif

            // Add an Animator and assign it the controller
            Animator animator = spriteRenderer.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
        }

        /// <summary>
        /// Creates an <see cref="AnimationClip"/> of a sprite animation using the given <see cref="Sprite"/> frames and frames per second.
        /// </summary>
        /// <param name="name">The name of the animation to create.</param>
        /// <param name="sprites">The list of <see cref="Sprite"/> objects making up the frames of the animation.</param>
        /// <param name="fps">The frames per second for the animation.</param>
        /// <returns>The newly constructed <see cref="AnimationClip"/></returns>
        private static AnimationClip CreateSpriteAnimationClip(string name, IList<Sprite> sprites, float fps)
        {
            float frameLength = 1f / fps;

            AnimationClip clip = new AnimationClip();
            clip.name = name;
            clip.frameRate = fps;
            clip.wrapMode = WrapMode.Loop;

            // The AnimationClipSettings cannot be set in Unity (as of 4.6) and must be editted via SerializedProperty
            // from: http://forum.unity3d.com/threads/can-mecanim-animation-clip-properties-be-edited-in-script.251772/
            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty serializedSettings = serializedClip.FindProperty("m_AnimationClipSettings");
            serializedSettings.FindPropertyRelative("m_LoopTime").boolValue = true;
            serializedClip.ApplyModifiedProperties();

            EditorCurveBinding curveBinding = new EditorCurveBinding();
            curveBinding.type = typeof(SpriteRenderer);
            curveBinding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Count];

            for (int i = 0; i < sprites.Count; i++)
            {
                ObjectReferenceKeyframe kf = new ObjectReferenceKeyframe();
                kf.time = i * frameLength;
                kf.value = sprites[i];
                keyFrames[i] = kf;
            }

#if UNITY_2018_1_OR_NEWER
            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
            // Note: hasMotionCurves is read-only, set via SerializedProperty if needed
#else // Unity 4
            AnimationUtility.SetAnimationType(clip, ModelImporterAnimationType.Generic);
            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
            // Note: hasMotionCurves is read-only, set via SerializedProperty if needed
#endif

            AssetDatabase.CreateAsset(clip, GetRelativePath(currentPath) + "/" + name + ".anim");

            return clip;
        }

        /// <summary>
        /// Converts a Photoshop blend mode key to a BlendMode enum value.
        /// </summary>
        /// <param name="blendModeKey">The Photoshop blend mode key (e.g., "norm", "mult", "scrn").</param>
        /// <returns>The corresponding BlendMode enum value.</returns>
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
                default:
                    Debug.LogWarning($"Unsupported blend mode: {blendModeKey}, defaulting to Normal");
                    return BlendMode.Normal;
            }
        }

        /// <summary>
        /// Applies the appropriate blend mode to a SpriteRenderer by setting its material.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to apply the blend mode to.</param>
        /// <param name="blendModeKey">The Photoshop blend mode key.</param>
        private static void ApplyBlendMode(SpriteRenderer spriteRenderer, string blendModeKey)
        {
            BlendMode blendMode = ConvertBlendModeKey(blendModeKey);
            
            switch (blendMode)
            {
                case BlendMode.Multiply:
                    // For multiply, we can use a simple material with multiply blend
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Multiply");
                    break;
                case BlendMode.Screen:
                    // Screen blend mode
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Screen");
                    break;
                case BlendMode.Overlay:
                    // Overlay blend mode
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Overlay");
                    break;
                case BlendMode.SoftLight:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/SoftLight");
                    break;
                case BlendMode.HardLight:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/HardLight");
                    break;
                case BlendMode.ColorDodge:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/ColorDodge");
                    break;
                case BlendMode.ColorBurn:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/ColorBurn");
                    break;
                case BlendMode.Darken:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Darken");
                    break;
                case BlendMode.Lighten:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Lighten");
                    break;
                case BlendMode.Difference:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Difference");
                    break;
                case BlendMode.Exclusion:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Exclusion");
                    break;
                case BlendMode.Hue:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Hue");
                    break;
                case BlendMode.Saturation:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Saturation");
                    break;
                case BlendMode.Color:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Color");
                    break;
                case BlendMode.Luminosity:
                    spriteRenderer.material = CreateBlendMaterial("Sprites/Luminosity");
                    break;
                case BlendMode.Normal:
                default:
                    spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    break;
            }
        }
        
        /// <summary>
        /// Applies the appropriate blend mode to a UI Image component.
        /// </summary>
        /// <param name="image">The UI Image to apply the blend mode to.</param>
        /// <param name="blendModeKey">The Photoshop blend mode key.</param>
        private static void ApplyBlendModeToUI(Image image, string blendModeKey)
        {
            BlendMode blendMode = ConvertBlendModeKey(blendModeKey);
            
            // For UI elements, blend modes can use the same principles if shaders were designed, but we'll use basic simulation for illustration
            switch (blendMode)
            {
                case BlendMode.Multiply:
                    image.material = CreateBlendMaterial("UI/Multiply");
                    break;
                case BlendMode.Screen:
                    image.material = CreateBlendMaterial("UI/Screen");
                    break;
                case BlendMode.Overlay:
                    image.material = CreateBlendMaterial("UI/Overlay");
                    break;
                case BlendMode.SoftLight:
                    Material softLightUI = CreateBlendMaterial("UI/SoftLight");
                    if (softLightUI != null) {
                        image.material = softLightUI;
                    }
                    break;
                case BlendMode.HardLight:
                    Material hardLightUI = CreateBlendMaterial("UI/HardLight");
                    if (hardLightUI != null) {
                        image.material = hardLightUI;
                    }
                    break;
                // New blend modes - setting to null for now as complex UI shader implementations are not defined
                case BlendMode.ColorDodge:
                case BlendMode.ColorBurn:
                case BlendMode.Darken:
                case BlendMode.Lighten:
                case BlendMode.Difference:
                case BlendMode.Exclusion:
                case BlendMode.Hue:
                case BlendMode.Saturation:
                case BlendMode.Color:
                case BlendMode.Luminosity:
                case BlendMode.Normal:
                default:
                    image.material = null;
                    break;
            }
        }
        
        /// <summary>
        /// Creates or finds a material with the specified shader for blend modes.
        /// </summary>
        /// <param name="shaderName">The name of the shader to use.</param>
        /// <returns>A material with the blend mode shader, or fallback material if not found.</returns>
        private static Material CreateBlendMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                return new Material(shader);
            }
            else
            {
                // Fall back to appropriate default shader based on shader type
                string fallbackShader;
                if (shaderName.StartsWith("UI/"))
                {
                    fallbackShader = "UI/Default";
                }
                else if (shaderName.StartsWith("Sprites/"))
                {
                    fallbackShader = "Sprites/Default";
                }
                else
                {
                    fallbackShader = "Sprites/Default";
                }
                
                Debug.LogWarning($"Shader '{shaderName}' not found. Using fallback shader '{fallbackShader}'.");
                
                Shader fallback = Shader.Find(fallbackShader);
                if (fallback != null)
                {
                    return new Material(fallback);
                }
                else
                {
                    // Last resort: try built-in shader
                    Debug.LogWarning($"Fallback shader '{fallbackShader}' also not found. Using built-in default.");
                    return new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
                }
            }
        }

        #endregion

        #region Unity UI
        /// <summary>
        /// Creates the Unity UI event system game object that handles all input.
        /// </summary>
        private static void CreateUIEventSystem()
        {
            if (!GameObject.Find("EventSystem"))
            {
                GameObject gameObject = new GameObject("EventSystem");
                gameObject.AddComponent<EventSystem>();
                gameObject.AddComponent<StandaloneInputModule>();
                gameObject.AddComponent<TouchInputModule>();
            }
        }

        /// <summary>
        /// Creates a Unity UI <see cref="Canvas"/>.
        /// </summary>
        private static void CreateUICanvas()
        {
            Canvas = new GameObject(PsdName);

            Canvas canvas = Canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform transform = Canvas.GetComponent<RectTransform>();
            Vector2 scaledCanvasSize = new Vector2(CanvasSize.x / PixelsToUnits, CanvasSize.y / PixelsToUnits);
            transform.sizeDelta = scaledCanvasSize;

            CanvasScaler scaler = Canvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = PixelsToUnits;
            scaler.referencePixelsPerUnit = PixelsToUnits;

            Canvas.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// Creates a Unity UI <see cref="UnityEngine.UI.Image"/> <see cref="GameObject"/> with a <see cref="Sprite"/> from a PSD <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to use to create the UI Image.</param>
        /// <returns>The newly constructed Image object.</returns>
        private static Image CreateUIImage(Layer layer)
        {
            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;

            // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
            y = (CanvasSize.y / PixelsToUnits) - y;

            // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
            x = x - ((CanvasSize.x / 2) / PixelsToUnits);
            y = y - ((CanvasSize.y / 2) / PixelsToUnits);

            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            // if the current group object actually has a position (not a normal Photoshop folder layer), then offset the position accordingly
            gameObject.transform.position = new Vector3(gameObject.transform.position.x + currentGroupGameObject.transform.position.x, gameObject.transform.position.y + currentGroupGameObject.transform.position.y, gameObject.transform.position.z);

            currentDepth -= depthStep;

            Image image = gameObject.AddComponent<Image>();
            image.sprite = CreateSprite(layer);
            
            // Apply layer opacity
            Color color = image.color;
            color.a = layer.Opacity / 255f;
            image.color = color;
            
            // Apply blend mode for UI
            ApplyBlendModeToUI(image, layer.BlendModeKey);
            
            // Apply layer effects to UI
            LayerEffectsApplicator.ApplyEffectsToUI(image, layer, PixelsToUnits);

            RectTransform transform = gameObject.GetComponent<RectTransform>();
            transform.sizeDelta = new Vector2(width, height);

            return image;
        }

        /// <summary>
        /// <summary>
        /// Creates a Unity UI <see cref="TextMeshProUGUI"/> <see cref="GameObject"/> with the text from a PSD <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> used to create the <see cref="TextMeshProUGUI"/> from.</param>
        private static void CreateUIText(Layer layer)
        {
            Color color = layer.FillColor;

            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;

            // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
            y = (CanvasSize.y / PixelsToUnits) - y;

            // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
            x = x - ((CanvasSize.x / 2) / PixelsToUnits);
            y = y - ((CanvasSize.y / 2) / PixelsToUnits);

            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            TextMeshProUGUI textMeshPro = gameObject.AddComponent<TextMeshProUGUI>();
            textMeshPro.text = layer.Text;
            textMeshPro.fontSize = layer.FontSize;
            textMeshPro.rectTransform.sizeDelta = new Vector2(width, height);

            textMeshPro.color = color;
            textMeshPro.alignment = TextAlignmentOptions.Center;

            // Apply the correct font from PSD
            TMP_FontAsset fontAsset = FontManager.GetOrCreateTMPFont(layer.FontName, layer.FontSize);
            if (fontAsset != null)
            {
                textMeshPro.font = fontAsset;
            }

            switch (layer.Justification)
            {
                case TextJustification.Left:
                    textMeshPro.alignment = TextAlignmentOptions.Left;
                    break;
                case TextJustification.Right:
                    textMeshPro.alignment = TextAlignmentOptions.Right;
                    break;
                case TextJustification.Center:
                    textMeshPro.alignment = TextAlignmentOptions.Center;
                    break;
            }
        }
        /// <summary>
        /// Creates a <see cref="UnityEngine.UI.Button"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The Layer to create the Button from.</param>
        private static void CreateUIButton(Layer layer)
        {
            // create an empty Image object with a Button behavior attached
            Image image = CreateUIImage(layer);
            Button button = image.gameObject.AddComponent<Button>();

            // look through the children for a clip rect
            ////Rectangle? clipRect = null;
            ////foreach (Layer child in layer.Children)
            ////{
            ////    if (child.Name.ContainsIgnoreCase("|ClipRect"))
            ////    {
            ////        clipRect = child.Rect;
            ////    }
            ////}

            // look through the children for the sprite states
            foreach (Layer child in layer.Children)
            {
                if (child.Name.ContainsIgnoreCase("|Disabled"))
                {
                    child.Name = child.Name.ReplaceIgnoreCase("|Disabled", string.Empty);
                    button.transition = Selectable.Transition.SpriteSwap;

                    SpriteState spriteState = button.spriteState;
                    spriteState.disabledSprite = CreateSprite(child);
                    button.spriteState = spriteState;
                }
                else if (child.Name.ContainsIgnoreCase("|Highlighted"))
                {
                    child.Name = child.Name.ReplaceIgnoreCase("|Highlighted", string.Empty);
                    button.transition = Selectable.Transition.SpriteSwap;

                    SpriteState spriteState = button.spriteState;
                    spriteState.highlightedSprite = CreateSprite(child);
                    button.spriteState = spriteState;
                }
                else if (child.Name.ContainsIgnoreCase("|Pressed"))
                {
                    child.Name = child.Name.ReplaceIgnoreCase("|Pressed", string.Empty);
                    button.transition = Selectable.Transition.SpriteSwap;

                    SpriteState spriteState = button.spriteState;
                    spriteState.pressedSprite = CreateSprite(child);
                    button.spriteState = spriteState;
                }
                else if (child.Name.ContainsIgnoreCase("|Default") ||
                         child.Name.ContainsIgnoreCase("|Enabled") ||
                         child.Name.ContainsIgnoreCase("|Normal") ||
                         child.Name.ContainsIgnoreCase("|Up"))
                {
                    child.Name = child.Name.ReplaceIgnoreCase("|Default", string.Empty);
                    child.Name = child.Name.ReplaceIgnoreCase("|Enabled", string.Empty);
                    child.Name = child.Name.ReplaceIgnoreCase("|Normal", string.Empty);
                    child.Name = child.Name.ReplaceIgnoreCase("|Up", string.Empty);

                    image.sprite = CreateSprite(child);

                    float x = child.Rect.x / PixelsToUnits;
                    float y = child.Rect.y / PixelsToUnits;

                    // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
                    y = (CanvasSize.y / PixelsToUnits) - y;

                    // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
                    x = x - ((CanvasSize.x / 2) / PixelsToUnits);
                    y = y - ((CanvasSize.y / 2) / PixelsToUnits);

                    float width = child.Rect.width / PixelsToUnits;
                    float height = child.Rect.height / PixelsToUnits;

                    image.gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);

                    RectTransform transform = image.gameObject.GetComponent<RectTransform>();
                    transform.sizeDelta = new Vector2(width, height);

                    button.targetGraphic = image;
                }
                else if (child.Name.ContainsIgnoreCase("|Text") && !child.IsTextLayer)
                {
                    child.Name = child.Name.ReplaceIgnoreCase("|Text", string.Empty);

                    GameObject oldGroupObject = currentGroupGameObject;
                    currentGroupGameObject = button.gameObject;

                    // If the "text" is a normal art layer, create an Image object from the "text"
                    CreateUIImage(child);

                    currentGroupGameObject = oldGroupObject;
                }

                if (child.IsTextLayer)
                {
                    // Create a child text game object for UI text
                    GameObject oldGroupObject = currentGroupGameObject;
                    currentGroupGameObject = button.gameObject;
                    
                    CreateUIText(child);
                    
                    currentGroupGameObject = oldGroupObject;
                }
            }
        }
        #endregion
        
        #region Smart Filter Processing

        /// <summary>
        /// Processes smart filters for all layers in the PSD file.
        /// This method pre-processes smart filters to apply effects.
        /// </summary>
        /// <param name="layers">The list of layers to process smart filters for.</param>
        private static void ApplySmartFilters(List<Layer> layers)
        {
            if (layers == null) return;

            foreach (Layer layer in layers)
            {
                ProcessSmartFilters(layer);

                // Process child layers recursively
                if (layer.Children != null && layer.Children.Count > 0)
                {
                    ApplySmartFilters(layer.Children);
                }
            }
        }

        /// <summary>
        /// Applies smart filter effects to the given layer.
        /// </summary>
        /// <param name="layer">The layer to process smart filters for.</param>
        private static void ProcessSmartFilters(Layer layer)
        {
            SmartFilters filters = SmartFilterParser.ParseSmartFilters(layer);
            layer.SmartFilters = filters;

            foreach (var effect in filters.Effects)
            {
                ApplySmartFilterEffect(effect, layer);
            }
        }

        /// <summary>
        /// Applies a single smart filter effect to the layer's texture.
        /// Simplified to demonstrate method structure.
        /// </summary>
        /// <param name="effect">The smart filter effect to apply.</param>
        /// <param name="layer">The layer on which to apply the effect.</param>
        private static void ApplySmartFilterEffect(SmartFilterEffect effect, Layer layer)
        {
            switch (effect.Type)
            {
                case SmartFilterType.GaussianBlur:
                    ApplyGaussianBlur((GaussianBlurEffect)effect, layer);
                    break;
                case SmartFilterType.MotionBlur:
                    ApplyMotionBlur((MotionBlurEffect)effect, layer);
                    break;
                case SmartFilterType.Sharpen:
                    ApplySharpen((SharpenEffect)effect, layer);
                    break;
                case SmartFilterType.Noise:
                    ApplyNoise((NoiseEffect)effect, layer);
                    break;
                case SmartFilterType.Emboss:
                    ApplyEmboss((EmbossEffect)effect, layer);
                    break;
                // Add other cases for different types here
            }
        }

        // Placeholder implementations for applying smart filters
        private static void ApplyGaussianBlur(GaussianBlurEffect effect, Layer layer)
        {
            // Simplified logic for Gaussian blur - demonstrate process
            Debug.Log($"Applying Gaussian Blur with radius {effect.Radius} to layer {layer.Name}");
        }

        private static void ApplyMotionBlur(MotionBlurEffect effect, Layer layer)
        {
            // Simplified logic for motion blur - demonstrate process
            Debug.Log($"Applying Motion Blur with angle {effect.Angle} and distance {effect.Distance} to layer {layer.Name}");
        }

        private static void ApplySharpen(SharpenEffect effect, Layer layer)
        {
            // Simplified logic for sharpen - demonstrate process
            Debug.Log($"Applying Sharpen with amount {effect.Amount} to layer {layer.Name}");
        }

        private static void ApplyNoise(NoiseEffect effect, Layer layer)
        {
            // Simplified logic for noise - demonstrate process
            Debug.Log($"Applying Noise with amount {effect.Amount} to layer {layer.Name}");
        }

        private static void ApplyEmboss(EmbossEffect effect, Layer layer)
        {
            // Simplified logic for emboss - demonstrate process
            Debug.Log($"Applying Emboss with angle {effect.Angle} to layer {layer.Name}");
        }

        #endregion
        
        #region Layer Mask Processing

        /// csummary 03e
        /// Processes adjustment layers for all layers in the PSD file.
        /// This method pre-processes adjustments to apply effects.
        ///  03c/summary 03e
        ///  03cparam name="layers" 03eThe list of layers to process adjustments for. 03c/param 03e
        private static void ApplyAdjustmentLayers(ListcLayere layers)
        {
            if (layers == null) return;

            foreach (Layer layer in layers)
            {
                ProcessAdjustmentLayers(layer);

                // Process child layers recursively
                if (layer.Children != null  26 26 layer.Children.Count  3e 0)
                {
                    ApplyAdjustmentLayers(layer.Children);
                }
            }
        }

        ///  03csummary 03e
        /// Applies adjustment layer effects to the given layer.
        ///  03c/summary 03e
        ///  03cparam name="layer" 03eThe layer to process adjustments for. 03c/param 03e
        private static void ProcessAdjustmentLayers(Layer layer)
        {
            AdjustmentLayerEffects effects = AdjustmentLayerParser.ParseAdjustmentLayers(layer);

            foreach (var effect in effects.Effects)
            {
                ApplyAdjustmentEffect(effect, layer);
            }
        }

        ///  03csummary 03e
        /// Applies a single adjustment layer effect to the layer's texture.
        /// Simplified to demonstrate method structure.
        ///  03c/summary 03e
        ///  03cparam name="effect" 03eThe adjustment layer effect to apply. 03c/param 03e
        ///  03cparam name="layer" 03eThe layer on which to apply the effect. 03c/param 03e
        private static void ApplyAdjustmentEffect(AdjustmentLayerEffect effect, Layer layer)
        {
            switch (effect.Type)
            {
                case AdjustmentLayerType.BrightnessContrast:
                    ApplyBrightnessContrast((BrightnessContrastEffect)effect, layer);
                    break;
                case AdjustmentLayerType.HueSaturation:
                    ApplyHueSaturation((HueSaturationEffect)effect, layer);
                    break;
                case AdjustmentLayerType.ColorBalance:
                    ApplyColorBalance((ColorBalanceEffect)effect, layer);
                    break;
                // Add other cases for different types here
            }
        }

        // Placeholder implementations for applying adjustments
        private static void ApplyBrightnessContrast(BrightnessContrastEffect effect, Layer layer)
        {
            // Simplified logic for brightness/contrast - demonstrate process
        }

        private static void ApplyHueSaturation(HueSaturationEffect effect, Layer layer)
        {
            // Simplified logic for hue/saturation - demonstrate process
        }

        private static void ApplyColorBalance(ColorBalanceEffect effect, Layer layer)
        {
            // Simplified logic for color balance - demonstrate process
        }

        #endregion
        
        /// <summary>
        /// Processes layer masks for all layers in the PSD file.
        /// This method pre-processes masks to ensure they are available for texture creation.
        /// </summary>
        /// <param name="layers">The list of layers to process masks for.</param>
        private static void ApplyLayerMasks(List<Layer> layers)
        {
            if (layers == null) return;
            
            foreach (Layer layer in layers)
            {
                ProcessLayerMask(layer);
                
                // Process child layers recursively
                if (layer.Children != null && layer.Children.Count > 0)
                {
                    ApplyLayerMasks(layer.Children);
                }
            }
        }
        
        /// <summary>
        /// Processes a single layer's mask data.
        /// </summary>
        /// <param name="layer">The layer to process the mask for.</param>
        private static GameObject CreateEmptyObject(Layer layer)
        {
            if (layer == null)
            {
                Debug.LogError("Cannot create empty object: layer is null");
                return null;
            }

            if (layer.PsdFile == null)
            {
                Debug.LogError($"Cannot create empty object for layer '{layer.Name}': PsdFile reference is null");
                return null;
            }

            // Avoid division by zero
            if (PixelsToUnits == 0)
            {
                Debug.LogError("PixelsToUnits cannot be zero. Using default value of 100.");
                PixelsToUnits = 100f;
            }

            float x = 0 / PixelsToUnits;
            float y = 0 / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.PsdFile.Width / PixelsToUnits;
            float height = layer.PsdFile.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name ?? "EmptyObject");
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            
            if (currentGroupGameObject != null)
            {
                gameObject.transform.parent = currentGroupGameObject.transform;
            }

            currentDepth -= depthStep;

            return gameObject;
        }
        
        /// <summary>
        /// Determines if a separate mask texture should be created for the layer.
        /// </summary>
        /// <param name="layer">The layer to check.</param>
        /// <returns>True if a separate mask texture should be created.</returns>
        private static bool ShouldCreateSeparateMaskTexture(Layer layer)
        {
            // Create separate mask textures for layers with complex masks
            // or when specifically requested (e.g., layer name contains "|SeparateMask")
            return layer.Name.ContainsIgnoreCase("|SeparateMask") ||
                   layer.Name.ContainsIgnoreCase("|Mask");
        }
        
        /// <summary>
        /// Creates a separate texture file for the layer's mask.
        /// </summary>
        /// <param name="layer">The layer to create the mask texture for.</param>
        /// <returns>The file path to the created mask texture.</returns>
        private static string CreateSeparateMaskTexture(Layer layer)
        {
            if (layer.MaskData == null || layer.MaskData.ImageData == null)
            {
                return string.Empty;
            }
            
            // Create a grayscale texture from the mask data
            int width = (int)layer.MaskData.Rect.width;
            int height = (int)layer.MaskData.Rect.height;
            
            Texture2D maskTexture = new Texture2D(width, height, TextureFormat.Alpha8, false);
            byte[] maskData = layer.MaskData.ImageData;
            
            // Convert mask data to texture pixels
            byte[] pixels = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                int layerRow = y * width;
                // Flip Y for Unity texture
                int textureRow = (height - 1 - y) * width;
                
                for (int x = 0; x < width; x++)
                {
                    int layerIndex = layerRow + x;
                    int textureIndex = textureRow + x;
                    
                    if (layerIndex < maskData.Length)
                    {
                        pixels[textureIndex] = maskData[layerIndex];
                    }
                    else
                    {
                        pixels[textureIndex] = 255; // Default to fully opaque
                    }
                }
            }
            
            maskTexture.LoadRawTextureData(pixels);
            maskTexture.Apply();
            
            // Save the mask texture as a PNG file
            string maskFile = Path.Combine(currentPath, layer.Name + "_mask.png");
            File.WriteAllBytes(maskFile, maskTexture.EncodeToPNG());
            
            // Import as texture (not sprite)
            string relativePath = GetRelativePath(maskFile);
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            
            TextureImporter textureImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.mipmapEnabled = false;
                textureImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                textureImporter.maxTextureSize = 2048;
            }
            
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            
            return maskFile;
        }
        
        /// <summary>
        /// Creates a UI mask component for clipping UI elements.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the mask to.</param>
        /// <param name="layer">The layer containing mask data.</param>
        private static void CreateUIMask(GameObject gameObject, Layer layer)
        {
            if (layer.MaskData == null) return;
            
            // Add a Mask component for UI clipping
            UnityEngine.UI.Mask mask = gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            
            // Create a mask image if one doesn't exist
            Image maskImage = gameObject.GetComponent<Image>();
            if (maskImage == null)
            {
                maskImage = gameObject.AddComponent<Image>();
            }
            
            // Use a simple white texture for the mask shape
            // In a more advanced implementation, you could create a custom texture from the mask data
            maskImage.color = Color.white;
        }
        
        #endregion
    }
}
