namespace PsdLayoutTool
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using PhotoshopFile;
    using UnityEditor;
    using UnityEngine;

#if !(UNITY_4_3 || UNITY_4_5)
    // if we are using Unity 4.6 or higher, allow using Unity UI
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
#endif

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

            // Set the depth step based on the layer count.  If there are no layers, default to 0.1f.
            depthStep = psd.Layers.Count != 0 ? MaximumDepth / psd.Layers.Count : 0.1f;

            int lastSlash = asset.LastIndexOf('/');
            string assetPathWithoutFilename = asset.Remove(lastSlash + 1, asset.Length - (lastSlash + 1));
            PsdName = asset.Replace(assetPathWithoutFilename, string.Empty).Replace(".psd", string.Empty);

            currentPath = GetFullProjectPath() + "Assets";
            currentPath = Path.Combine(currentPath, PsdName);
            Directory.CreateDirectory(currentPath);

            if (LayoutInScene || CreatePrefab)
            {
#if UNITY_4_3 || UNITY_4_5
				rootPsdGameObject = new GameObject(PsdName);
				currentGroupGameObject = rootPsdGameObject;
#else // Unity 4.6+
                if (UseUnityUI)
                {
                    CreateUIEventSystem();
                    CreateUICanvas();
                    rootPsdGameObject = Canvas;
                }
                else
                {
                    rootPsdGameObject = new GameObject(PsdName);
                }

                currentGroupGameObject = rootPsdGameObject;
#endif
            }

            List<Layer> tree = BuildLayerTree(psd.Layers);
            ExportTree(tree);

            if (CreatePrefab)
            {
                UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab(asset.Replace(".psd", ".prefab"));
                PrefabUtility.ReplacePrefab(rootPsdGameObject, prefab);

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
                else if (layer.Rect.Width != 0 && layer.Rect.Height != 0)
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
                (layer.Name == " copy" && layer.Rect.Height == 0);
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
            if (layer.Children.Count > 0 || layer.Rect.Width == 0)
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
#if !(UNITY_4_3 || UNITY_4_5)
                if (UseUnityUI)
                {
                    CreateButton(layer);
                }
#endif
            }
            else
            {
                string oldPath = currentPath;
                GameObject oldGroupObject = currentGroupGameObject;

                currentPath = Path.Combine(currentPath, layer.Name);
                Directory.CreateDirectory(currentPath);

                if (LayoutInScene || CreatePrefab)
                {
                    currentGroupGameObject = new GameObject(layer.Name);
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
                    Sprite sprite = CreateSprite(layer);
                    if (!UseUnityUI)
                    {
                        CreateSpriteGameObject(layer, sprite);
                    }
                    else
                    {
#if !(UNITY_4_3 || UNITY_4_5)
                        CreateUIImage(layer, sprite);
#endif
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
#if !(UNITY_4_3 || UNITY_4_5)
                        CreateUIText(layer);
#endif
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
            // decode the layer into an image (must be a bitmap so we can set pixels later)
            Bitmap image = new Bitmap(ImageDecoder.DecodeImage(layer));

            // the image decoder doesn't handle transparency in colors, so we have to do it manually
            ////if (layer.Opacity != 255)
            ////{
            ////    for (int x = 0; x < image.Width; x++)
            ////    {
            ////        for (int y = 0; y < image.Height; y++)
            ////        {
            ////            System.Drawing.Color color = image.GetPixel(x, y);
            ////            image.SetPixel(x, y, System.Drawing.Color.FromArgb(layer.Opacity, color));
            ////        }
            ////    }
            ////}

            string file = Path.Combine(currentPath, layer.Name + ".png");
            image.Save(file, ImageFormat.Png);

            return file;
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to use to create a <see cref="Sprite"/>.</param>
        /// <returns>The created <see cref="Sprite"/> object.</returns>
        private static Sprite CreateSprite(Layer layer)
        {
            string file = CreatePNG(layer);
            return ImportSprite(GetRelativePath(file));
        }

        /// <summary>
        /// Imports the <see cref="Sprite"/> at the given path, relative to the Unity project "Assets/Textures/texture.tga".
        /// </summary>
        /// <param name="relativePathToSprite">The path to the sprite, relative to the Unity project "Assets/Textures/texture.tga".</param>
        /// <returns>The imported image as a <see cref="Sprite"/> object.</returns>
        private static Sprite ImportSprite(string relativePathToSprite)
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

#if UNITY_4_3 || UNITY_4_5
                textureImporter.spritePixelsToUnits = PixelsToUnits;
#else // Unity 4.6+
                textureImporter.spritePixelsPerUnit = PixelsToUnits;
#endif

                textureImporter.spritePackingTag = PsdName;
            }

            AssetDatabase.ImportAsset(relativePathToSprite, ImportAssetOptions.ForceUpdate);

            Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath(relativePathToSprite, typeof(Sprite));
            return sprite;
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a <see cref="TextMesh"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create a <see cref="TextMesh"/> from.</param>
        private static void CreateTextGameObject(Layer layer)
        {
            UnityEngine.Color color = new UnityEngine.Color(layer.FillColor.R, layer.FillColor.G, layer.FillColor.B, layer.FillColor.A);

            float x = layer.Rect.X / PixelsToUnits;
            float y = layer.Rect.Y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.Width / PixelsToUnits;
            float height = layer.Rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            UnityEngine.Font font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = font.material;

            TextMesh textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = layer.Text;
            textMesh.font = font;
            textMesh.fontSize = 0;
            textMesh.characterSize = layer.FontSize / PixelsToUnits;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;

            switch (layer.Justification)
            {
                case TextJustification.Left:
                    textMesh.alignment = TextAlignment.Left;
                    break;
                case TextJustification.Right:
                    textMesh.alignment = TextAlignment.Right;
                    break;
                case TextJustification.Center:
                    textMesh.alignment = TextAlignment.Center;
                    break;
            }
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a sprite from the given <see cref="Layer"/>
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create the sprite from.</param>
        /// <param name="sprite">The Sprite object to use to create the GameObject.</param>
        private static void CreateSpriteGameObject(Layer layer, Sprite sprite)
        {
            float x = layer.Rect.X / PixelsToUnits;
            float y = layer.Rect.Y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.Width / PixelsToUnits;
            float height = layer.Rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
        }

        #endregion

        #region Unity 4.6+ UI
        // only allow Unity UI creation in Unity 4.6 or higher
#if !(UNITY_4_3 || UNITY_4_5)
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
        /// <param name="sprite">The <see cref="Sprite"/> image to use.</param>
        /// <returns>The newly constructed Image object.</returns>
        private static UnityEngine.UI.Image CreateUIImage(Layer layer, Sprite sprite)
        {
            float x = layer.Rect.X / PixelsToUnits;
            float y = layer.Rect.Y / PixelsToUnits;

            // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
            y = (CanvasSize.y / PixelsToUnits) - y;

            // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
            x = x - ((CanvasSize.x / 2) / PixelsToUnits);
            y = y - ((CanvasSize.y / 2) / PixelsToUnits);

            float width = layer.Rect.Width / PixelsToUnits;
            float height = layer.Rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            // if the current group object actually has a position (not a normal Photoshop folder layer), then offset the position accordingly
            gameObject.transform.position = new Vector3(gameObject.transform.position.x + currentGroupGameObject.transform.position.x, gameObject.transform.position.y + currentGroupGameObject.transform.position.y, gameObject.transform.position.z);

            currentDepth -= depthStep;

            UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;

            RectTransform transform = gameObject.GetComponent<RectTransform>();
            transform.sizeDelta = new Vector2(width, height);

            return image;
        }

        /// <summary>
        /// Creates a Unity UI <see cref="UnityEngine.UI.Text"/> <see cref="GameObject"/> with the text from a PSD <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> used to create the <see cref="UnityEngine.UI.Text"/> from.</param>
        private static void CreateUIText(Layer layer)
        {
            UnityEngine.Color color = new UnityEngine.Color(layer.FillColor.R, layer.FillColor.G, layer.FillColor.B, layer.FillColor.A);

            float x = layer.Rect.X / PixelsToUnits;
            float y = layer.Rect.Y / PixelsToUnits;

            // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
            y = (CanvasSize.y / PixelsToUnits) - y;

            // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
            x = x - ((CanvasSize.x / 2) / PixelsToUnits);
            y = y - ((CanvasSize.y / 2) / PixelsToUnits);

            float width = layer.Rect.Width / PixelsToUnits;
            float height = layer.Rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(layer.Name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            UnityEngine.Font font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");

            Text textUI = gameObject.AddComponent<Text>();
            textUI.text = layer.Text;
            textUI.font = font;
            textUI.rectTransform.sizeDelta = new Vector2(width, height);

            float fontSize = layer.FontSize / PixelsToUnits;
            float ceiling = Mathf.Ceil(fontSize);
            if (fontSize < ceiling)
            {
                // Unity UI Text doesn't support floating point font sizes, so we have to round to the next size and scale everything else
                float scaleFactor = ceiling / fontSize;
                textUI.fontSize = (int)ceiling;
                textUI.rectTransform.sizeDelta *= scaleFactor;
                textUI.rectTransform.localScale /= scaleFactor;
            }
            else
            {
                textUI.fontSize = (int)fontSize;
            }

            textUI.color = color;
            textUI.alignment = TextAnchor.MiddleCenter;

            switch (layer.Justification)
            {
                case TextJustification.Left:
                    textUI.alignment = TextAnchor.MiddleLeft;
                    break;
                case TextJustification.Right:
                    textUI.alignment = TextAnchor.MiddleRight;
                    break;
                case TextJustification.Center:
                    textUI.alignment = TextAnchor.MiddleCenter;
                    break;
            }
        }

        /// <summary>
        /// Creates a <see cref="UnityEngine.UI.Button"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The Layer to create the Button from.</param>
        private static void CreateButton(Layer layer)
        {
            // create an empty Image object with a Button behavior attached
            UnityEngine.UI.Image image = CreateUIImage(layer, null);
            Button button = image.gameObject.AddComponent<Button>();

            // look through the children for a clip rect
            Rectangle clipRect;
            foreach (Layer child in layer.Children)
            {
                if (child.Name.ContainsIgnoreCase("|ClipRect"))
                {
                    clipRect = child.Rect;
                }
            }

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

                    float x = child.Rect.X / PixelsToUnits;
                    float y = child.Rect.Y / PixelsToUnits;

                    // Photoshop increase Y while going down. Unity increases Y while going up.  So, we need to reverse the Y position.
                    y = (CanvasSize.y / PixelsToUnits) - y;

                    // Photoshop uses the upper left corner as the pivot (0,0).  Unity defaults to use the center as (0,0), so we must offset the positions.
                    x = x - ((CanvasSize.x / 2) / PixelsToUnits);
                    y = y - ((CanvasSize.y / 2) / PixelsToUnits);

                    float width = child.Rect.Width / PixelsToUnits;
                    float height = child.Rect.Height / PixelsToUnits;

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
                    CreateUIImage(child, CreateSprite(child));

                    currentGroupGameObject = oldGroupObject;
                }

                if (child.IsTextLayer)
                {
                    Debug.Log("Button Text!");
                }
            }
        }
#endif
        #endregion
    }
}