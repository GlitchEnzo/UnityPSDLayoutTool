namespace PsdLayoutTool
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Text.RegularExpressions;
    using PhotoshopFile;
    using UnityEditor;
    using UnityEngine;

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
            depthStep = psd.Layers.Count != 0 ? currentDepth / psd.Layers.Count : 0.1f;

            int lastSlash = asset.LastIndexOf('/');
            string assetPathWithoutFilename = asset.Remove(lastSlash + 1, asset.Length - (lastSlash + 1));
            PsdName = asset.Replace(assetPathWithoutFilename, string.Empty).Replace(".psd", string.Empty);

            currentPath = GetFullProjectPath() + "Assets";
            currentPath = Path.Combine(currentPath, PsdName);
            Directory.CreateDirectory(currentPath);

            if (LayoutInScene || CreatePrefab)
            {
                rootPsdGameObject = new GameObject(PsdName);
                currentGroupGameObject = rootPsdGameObject;
            }

            List<Layer> tree = BuildLayerTree(psd.Layers);
            ExportTree(tree);

            if (CreatePrefab)
            {
                Object prefab = PrefabUtility.CreateEmptyPrefab(asset.Replace(".psd", ".prefab"));
                PrefabUtility.ReplacePrefab(rootPsdGameObject, prefab);

                if (!LayoutInScene)
                {
                    // if we are not flagged to layout in the scene, delete the GameObject used to generate the prefab
                    Object.DestroyImmediate(rootPsdGameObject);
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

            // PSD layers are stored backwards (front to back), so we must reverse them
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
            Regex pattern = new Regex("[/:&.<>,$¢;]");
            string newName = pattern.Replace(name, "_");

            if (name != newName)
            {
                Debug.Log(string.Format("{0} was changed to {1}", name, newName));
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

        /// <summary>
        /// Exports an art layer as an image file and sprite.  It can also generate text meshes from text layers.
        /// </summary>
        /// <param name="layer">The art layer to export.</param>
        private static void ExportArtLayer(Layer layer)
        {
            if (!layer.IsTextLayer)
            {
                // decode the layer into an image (must be a bitmap so we can set pixels later)
                Bitmap image = new Bitmap(ImageDecoder.DecodeImage(layer));

                // the image decoder doesn't handle transparency in colors, so we have to do it manually
                if (layer.Opacity != 255)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            System.Drawing.Color color = image.GetPixel(x, y);
                            image.SetPixel(x, y, System.Drawing.Color.FromArgb(layer.Opacity, color));
                        }
                    }
                }

                string name = MakeNameSafe(layer.Name);
                string file = Path.Combine(currentPath, name + ".png");
                image.Save(file, ImageFormat.Png);

                if (LayoutInScene || CreatePrefab)
                {
                    Sprite sprite = ImportSprite(GetRelativePath(file));
                    CreateSpriteGameObject(name, layer.Rect, sprite);
                }
            }
            else
            {
                if (LayoutInScene || CreatePrefab)
                {
                    // create text mesh
                    ////string name = MakeNameSafe(layer.Name);
                    UnityEngine.Color color = new UnityEngine.Color(layer.FillColor.R, layer.FillColor.G, layer.FillColor.B, layer.FillColor.A);
                    CreateTextGameObject(layer.Name, layer.Rect, layer.Text, layer.FontSize, layer.Justification, color);
                }
            }
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
                textureImporter.spritePixelsPerUnit = PixelsToUnits;
                textureImporter.spritePackingTag = PsdName;
            }
            AssetDatabase.ImportAsset(relativePathToSprite, ImportAssetOptions.ForceUpdate);

            Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath(relativePathToSprite, typeof(Sprite));
            return sprite;
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> with a <see cref="TextMesh"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="name">The name of the text object to create.</param>
        /// <param name="rect">The <see cref="Rectangle"/> representing the size of the text area.</param>
        /// <param name="text">The actual text in the text area.</param>
        /// <param name="fontSize">The point size of the font.</param>
        /// <param name="justification">The justification of the text.</param>
        /// <param name="fillColor">The color used to fill the text.</param>
        private static void CreateTextGameObject(string name, Rectangle rect, string text, float fontSize, TextJustification justification, UnityEngine.Color fillColor)
        {
            float x = rect.X / PixelsToUnits;
            float y = rect.Y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = rect.Width / PixelsToUnits;
            float height = rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            UnityEngine.Font font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = font.material;

            TextMesh textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = font;
            textMesh.fontSize = 0;
            textMesh.characterSize = fontSize / PixelsToUnits;
            textMesh.color = fillColor;
            textMesh.anchor = TextAnchor.MiddleCenter;

            switch (justification)
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
        /// <param name="name">The name of the sprite object to create.</param>
        /// <param name="rect">The <see cref="Rectangle"/> representing the size of the sprite.</param>
        /// <param name="sprite">The <see cref="Sprite"/> image to use.</param>
        private static void CreateSpriteGameObject(string name, Rectangle rect, Sprite sprite)
        {
            float x = rect.X / PixelsToUnits;
            float y = rect.Y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = rect.Width / PixelsToUnits;
            float height = rect.Height / PixelsToUnits;

            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = new Vector3(x + (width / 2), y - (height / 2), currentDepth);
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
        }
        #endregion
    }
}