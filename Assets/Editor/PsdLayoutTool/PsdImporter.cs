﻿namespace PsdLayoutTool
{
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
    using System.Collections;
    /// <summary>
    /// Handles all of the importing for a PSD file (exporting textures, creating prefabs, etc).
    /// </summary>
    public static class PsdImporter
    {

        ///attention：
        ///string const as psd layer name keyword!
        public const string BTN_HEAD = "btn_";              //button normal image keyword
        public const string BTN_TAIL_HIGH = "_highlight";   //button highilght image keyward
        public const string BTN_TAIL_DIS = "_disable";      //buttno disable image keyword

        //public const string TEXT_HEAD = "text_";            //text keyword

        public const string PUBLIC_IMG_HEAD = "public_";    //public images that more than one UI may use

        //sliced image type, image name like aaa_330_400,image name end with width and height,
        //Image will layout as sizeDelta=(330,440),rather than current image reak size

        private const string PUBLIC_IMG_PATH =  @"\public_images";//public images relative path

        /// <summary>
        /// The current file path to use to save layers as .png files
        /// </summary>
        private static string currentPath;

        public const string currentImgPathRoot = "export_image/";//all images output root dictionary 
        //{
        //    get { return currentPath; }
        //    set { currentPath = value; }

        private const string TEST_FONT_NAME = "FandoHei";
        //}
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
            textFont = TEST_FONT_NAME;//.otf";//yanru测试字体

            MaximumDepth = 1;
            PixelsToUnits = 1;
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
        public static bool UseUnityUI
        {
            get { return _useUnityUI; }
            set { _useUnityUI = value; }
        }

        private static bool _useUnityUI = true;

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
        private static GameObject canvasObj { get; set; }

        /// <summary>
        /// full ui size
        /// </summary>
        public static Vector2 ScreenResolution = new Vector2(1280, 760);


        private static string _textFont = "";

        /// <summary>
        /// force use font
        /// </summary>
        /// 
        public static string textFont
        {
            get
            {
                if (_textFont == "")
                {
                    _textFont = "Arial.ttf";
                    //Debug.Log(Time.time + ",force set font=" + _textFont);
                }
                return _textFont;
            }
            set
            {
                _textFont = value;
            }
        }

        private static bool _fullScreenUI = true;
        public static bool fullScreenUI
        {
            get { return _fullScreenUI; }
            set { _fullScreenUI = value; }
        }
         
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

        private static Dictionary<GameObject, Layer> _layerDic;

        /// <summary>
        /// Imports a Photoshop document (.psd) file at the given path.
        /// </summary>
        /// <param name="asset">The path of to the .psd file relative to the project.</param>
        private static void Import(string asset)
        {
            _layerDic = new Dictionary<GameObject, Layer>();
            currentDepth = MaximumDepth;
            string fullPath = Path.Combine(GetFullProjectPath(), asset.Replace('\\', '/'));

            PsdFile psd = new PsdFile(fullPath);
            CanvasSize = ScreenResolution;// new Vector2(psd.Width, psd.Height);
            Debug.Log(Time.time + "update canvasSize as UI size=" + CanvasSize);
            // Set the depth step based on the layer count.  If there are no layers, default to 0.1f.
            depthStep = psd.Layers.Count != 0 ? MaximumDepth / psd.Layers.Count : 0.1f;

            int lastSlash = asset.LastIndexOf('/');
            string assetPathWithoutFilename = asset.Remove(lastSlash + 1, asset.Length - (lastSlash + 1));
            PsdName = asset.Replace(assetPathWithoutFilename, string.Empty).Replace(".psd", string.Empty);

            currentPath = GetFullProjectPath() + "Assets/" + currentImgPathRoot;//output relative dictionay
            currentPath = Path.Combine(currentPath, PsdName);
            createDic(currentPath);

            if (LayoutInScene || CreatePrefab)
            {
                if (UseUnityUI)
                {
                    CreateUIEventSystem();
                    CreateUICanvas();
                }

                //create ui Root
                rootPsdGameObject = CreateObj(PsdName);
                updateParent(rootPsdGameObject, canvasObj);

                //if (fullScreenUI)
                {
                    RectTransform rectRoot = rootPsdGameObject.GetComponent<RectTransform>();
                    rectRoot.anchorMin = new Vector2(0, 0);
                    rectRoot.anchorMax = new Vector2(1, 1);
                    rectRoot.offsetMin = Vector2.zero;
                    rectRoot.offsetMax = Vector2.zero;
                }
                Vector3 rootPos = Vector3.zero;
                updateRectPosition(rootPsdGameObject, rootPos, true);

                currentGroupGameObject = rootPsdGameObject;
            }

            List<Layer> tree = BuildLayerTree(psd.Layers);
            ExportTree(tree);

            if (CreatePrefab)
            {
                UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab(asset.Replace(".psd", ".prefab"));
                PrefabUtility.ReplacePrefab(rootPsdGameObject, prefab);

                //if (!LayoutInScene)
                //{
                //    // if we are not flagged to layout in the scene, delete the GameObject used to generate the prefab
                //    UnityEngine.Object.DestroyImmediate(rootPsdGameObject);
                //}
            }

            //all ui items created, update components
            if (rootPsdGameObject== null)
            {
                return;
            }
                Debug.Log(Time.time + ",dealUI=" + rootPsdGameObject.name + ",finish");
            

            int childCount = rootPsdGameObject.transform.childCount;
            for (int index = 0; index < childCount; index++)
            {
                Transform tran = rootPsdGameObject.transform.GetChild(index);
                tran.position += new Vector3(ScreenResolution.x / 2f, ScreenResolution.y / 2f, 0);
            }

            //刷新文本啊
            List<GameObject> keyList = new List<GameObject>(_layerDic.Keys);
            for(int index=0;index< keyList.Count;index ++)
            {
                keyList[index].GetComponent<Text>().color = _layerDic[keyList[index]].FillColor;
            }

            Dictionary<Transform, bool> _dealDic = new Dictionary<Transform, bool>(); //flag if item will be deleted

            List<Transform> btnList = new List<Transform>();
            List<Transform> deleteList = new List<Transform>();

            Transform[] allChild = rootPsdGameObject.GetComponentsInChildren<Transform>();
            for (int index = 0; index < allChild.Length; index++)
            {
                Transform tran = allChild[index];
                if (tran.name.IndexOf(BTN_HEAD) == 0)
                {
                    Button button = tran.gameObject.AddComponent<Button>();
                    tran.GetComponent<Image>().raycastTarget = true;
                    button.transition = Selectable.Transition.SpriteSwap;
                    btnList.Add(tran);
                }
            }

            for (int btnIndex = 0; btnIndex < btnList.Count; btnIndex++)
            {
                string btnName = btnList[btnIndex].name;
                for (int index = 0; index < allChild.Length; index++)
                {
                    Transform tran = allChild[index];

                    //update image sprite deltaSize and PNG attribute
                    Image image = tran.GetComponent<Image>();
                    if (image != null && image.sprite != null)
                    {
                        string spriteName = image.sprite.name;

                        //match str end with number_number,will used as sliced Image
                        string str1 = spriteName; // "4343434";// "testrewer_4_3";
                        Regex reg = new Regex(@"\d+[_]\d+$");
                        Match match = reg.Match(str1);
                        if (match.ToString() != "")
                        {
                            string[] size = reg.Match(str1).ToString().Split('_');
                            int width = Convert.ToInt32(size[0]);
                            int height = Convert.ToInt32(size[0]);
                            image.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                            image.type = Image.Type.Sliced;

                            if (image.sprite.border == Vector4.zero)
                            {
                                Debug.LogError(Time.time + "need to set png=" + image.sprite.name + ",slice border");
                            }
                        }
                    }

                    if (allChild[index].name.IndexOf(btnName) == 0)
                    {
                        if (allChild[index].name.Contains(BTN_TAIL_HIGH))//button highlight image
                        {
                            SpriteState sprite = btnList[btnIndex].GetComponent<Button>().spriteState;
                            sprite.pressedSprite = allChild[index].GetComponent<Image>().sprite;
                            btnList[btnIndex].GetComponent<Button>().spriteState = sprite;
                            deleteList.Add(tran);
                        }
                        if (allChild[index].name.Contains(BTN_TAIL_DIS))//button disable image 
                        {
                            SpriteState sprite = btnList[btnIndex].GetComponent<Button>().spriteState;
                            sprite.disabledSprite = allChild[index].GetComponent<Image>().sprite;
                            btnList[btnIndex].GetComponent<Button>().spriteState = sprite;
                            deleteList.Add(tran);
                        }
                    }
                }
            }

            //delete no use items

            for (int index = 0; index < deleteList.Count; index++)
            {
                destroyItem(deleteList[index]);
            }

            AssetDatabase.Refresh();
        }


        //TODO testButton
        public static void TestClick()
        {
            if (canvasObj == null)
                return;

            for (int index = 0; index < canvasObj.transform.childCount; index++)
            {
                GameObject.Destroy(canvasObj.transform.GetChild(index));
            }
        }


        private static void destroyItem(Transform child)
        {
            if (child != null)
                GameObject.DestroyImmediate(child.gameObject);
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
            updateLayerName(layer, MakeNameSafe(layer.Name));
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
            //Debug.Log(Time.time + "read layerName=" + layer.Name+",hasBtn?"+(layer.Name.IndexOf(BTN_HEAD))+",has?"+ (layer.Name.ContainsIgnoreCase(BTN_HEAD)));
            if (layer.Name.ContainsIgnoreCase(BTN_HEAD))
            {
                updateLayerName(layer, layer.Name.ReplaceIgnoreCase(BTN_HEAD, string.Empty));

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
                updateLayerName(layer, layer.Name.ReplaceIgnoreCase("|Animation", string.Empty));

                string oldPath = currentPath;
                GameObject oldGroupObject = currentGroupGameObject;

                //currentPath = Path.Combine(currentPath, layer.Name.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                createDic(currentPath);

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

                //       currentPath = Path.Combine(currentPath, layer.Name);
                createDic(currentPath);

                if (LayoutInScene || CreatePrefab)
                {
                    currentGroupGameObject = CreateObj(layer.Name);

#if UNITY_5
                    updateParent(currentGroupGameObject, oldGroupObject);
#else
                    currentGroupGameObject.transform.parent = oldGroupObject.transform;
#endif

                }

                ExportTree(layer.Children);

                currentPath = oldPath;
                currentGroupGameObject = oldGroupObject;
            }
        }

        private static void createDic(string path)
        {
            Directory.CreateDirectory(path);
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
                    //if(createdNameList.Contains(layer.Name))
                    //{
                    //    layer.Name = layer.Name + "_new_" + createdNameList.Count;
                    //}
                    // it is not being laid out in the scene, so simply save out the .png file
                    CreatePNG(layer);
                    //createdNameList.Add(layer.Name);
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

                string writePath = currentPath;
                string layerName = trimSpecialHead(layer.Name);

                if (layerName.Contains(PUBLIC_IMG_HEAD))//common images
                {
                    int length = writePath.Length - 1;
                    if (writePath.LastIndexOf(@"\") != -1)
                        length = writePath.LastIndexOf(@"\");

                    writePath = writePath.Substring(0, length);
                    writePath += PUBLIC_IMG_PATH;
                }

                //output path not exist, create one
                if (!Directory.Exists(writePath))
                {
                    Directory.CreateDirectory(writePath);
                }

                file = Path.Combine(writePath, layerName + ".png");

                File.WriteAllBytes(file, texture.EncodeToPNG());
            }

            return file;
        }

        private  static string trimSpecialHead(string str)
        {
            if (str.IndexOf(BTN_HEAD) == 0)
                return str.Replace(BTN_HEAD, "");

            return str;
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
        /// Creates a <see cref="GameObject"/> with a <see cref="TextMesh"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> to create a <see cref="TextMesh"/> from.</param>
        private static void CreateTextGameObject(Layer layer)
        {
            Color color = layer.FillColor;

            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;

            GameObject gameObject = CreateObj(layer.Name);
            updateRectPosition(gameObject, new Vector3(x + (width / 2), y - (height / 2), currentDepth));
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            Font font = getFontInfo();

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
        /// <returns>The <see cref="SpriteRenderer"/> component attached to the new sprite <see cref="GameObject"/>.</returns>
        private static SpriteRenderer CreateSpriteGameObject(Layer layer)
        {
            float x = layer.Rect.x / PixelsToUnits;
            float y = layer.Rect.y / PixelsToUnits;
            y = (CanvasSize.y / PixelsToUnits) - y;
            float width = layer.Rect.width / PixelsToUnits;
            float height = layer.Rect.height / PixelsToUnits;
            GameObject gameObject = CreateObj(layer.Name);
            updateRectPosition(gameObject, new Vector3(x + (width / 2), y - (height / 2), currentDepth));
            gameObject.transform.parent = currentGroupGameObject.transform;

            currentDepth -= depthStep;

            SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSprite(layer);
            return spriteRenderer;
        }

        private static void updateRectPosition(GameObject rect, Vector3 position, bool isRoot = false)
        {
            rect.GetComponent<RectTransform>().anchoredPosition3D = position;
            //showLog(",update rect=" + rect.name + ",position=" + position + ",isRoot?" + isRoot +
            //    ",parentisRoot?" + ((rect.transform.parent == rootPsdGameObject.transform)));
        }

        private static GameObject CreateObj(string objName)
        {
            GameObject obj = new GameObject(objName);
            obj.AddComponent<RectTransform>();
            return obj;
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
                    updateLayerName(layer, layer.Name.Replace("|" + arg, string.Empty));

                    string[] fpsArgs = arg.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!float.TryParse(fpsArgs[1], out fps))
                    {
                        Debug.LogError(string.Format("Unable to parse FPS: \"{0}\"", arg));
                    }
                }
            }

            List<Sprite> frames = new List<Sprite>();

            Layer firstChild = layer.Children.First();
            SpriteRenderer spriteRenderer = CreateSpriteGameObject(firstChild);
            spriteRenderer.name = layer.Name;

            foreach (Layer child in layer.Children)
            {
                frames.Add(CreateSprite(child, layer.Name));
            }

            spriteRenderer.sprite = frames[0];

#if UNITY_5
            // Create Animator Controller with an Animation Clip
            UnityEditor.Animations.AnimatorController controller = new UnityEditor.Animations.AnimatorController();
            controller.AddLayer("Base Layer");

            UnityEditor.Animations.AnimatorControllerLayer controllerLayer = controller.layers[0];
            UnityEditor.Animations.AnimatorState state = controllerLayer.stateMachine.AddState(layer.Name);
            state.motion = CreateSpriteAnimationClip(layer.Name, frames, fps);

            AssetDatabase.CreateAsset(controller, GetRelativePath(currentPath) + "/" + layer.Name + ".controller");
#else // Unity 4
            // Create Animator Controller with an Animation Clip
            AnimatorController controller = new AnimatorController();
            AnimatorControllerLayer controllerLayer = controller.AddLayer("Base Layer");

            State state = controllerLayer.stateMachine.AddState(layer.Name);
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

#if UNITY_5
            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
#else // Unity 4
            AnimationUtility.SetAnimationType(clip, ModelImporterAnimationType.Generic);
            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);

            clip.ValidateIfRetargetable(true);
#endif

            AssetDatabase.CreateAsset(clip, GetRelativePath(currentPath) + "/" + name + ".anim");

            return clip;
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
                GameObject gameObject = CreateObj("EventSystem");
                gameObject.AddComponent<EventSystem>();
                gameObject.AddComponent<StandaloneInputModule>();
                //gameObject.AddComponent<TouchInputModule>();
            }
        }

        /// <summary>
        /// Creates a Unity UI <see cref="canvasObj"/>.
        /// </summary>
        private static void CreateUICanvas()
        {
            //CanvasComRoot;
            //Canvas = CreateObj(PsdName);
            if (GameObject.Find("Canvas") != null)
            {
                canvasObj = GameObject.Find("Canvas");
            }
            else
            {
                canvasObj = CreateObj("Canvas");
            }

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
                canvas = canvasObj.AddComponent<Canvas>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ScreenResolution;

#if UNITY_5
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

#else
            canvas.renderMode = RenderMode.WorldSpace;
#endif

            RectTransform transform = canvasObj.GetComponent<RectTransform>();
            updateRectSize(ref transform, CanvasSize.x / PixelsToUnits, CanvasSize.y / PixelsToUnits);

            scaler.dynamicPixelsPerUnit = PixelsToUnits;
            scaler.referencePixelsPerUnit = PixelsToUnits;

            GraphicRaycaster racaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (racaster == null)
                racaster = canvasObj.AddComponent<GraphicRaycaster>();
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

            GameObject gameObject = CreateObj(layer.Name);
            updateRectPosition(gameObject, new Vector3(x + (width / 2), y - (height / 2), currentDepth));

#if UNITY_5
            updateParent(gameObject, currentGroupGameObject);
#else
            gameObject.transform.parent = currentGroupGameObject.transform;
#endif

            // if the current group object actually has a position (not a normal Photoshop folder layer), then offset the position accordingly
            updateRectPosition(gameObject, new Vector3(gameObject.transform.position.x + currentGroupGameObject.transform.position.x,
                gameObject.transform.position.y + currentGroupGameObject.transform.position.y, gameObject.transform.position.z));

            currentDepth -= depthStep;

            Image image = gameObject.AddComponent<Image>();
            image.sprite = CreateSprite(layer);
            image.raycastTarget = false; //can not click Image by yanru 2016-06-16 19:26:55

            RectTransform transform = gameObject.GetComponent<RectTransform>();
            updateRectSize(ref transform, width, height);
          
            return image;
        }

        private static void updateRectSize(ref RectTransform transform, float width, float height)
        {
            //Debug.Log(Time.time + ",update rect size tran="+transform.name+", with = " + width + ",height=" + height + ",PixelsToUnits=" + PixelsToUnits + ",pre=" + (width * PixelsToUnits));
            transform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Creates a Unity UI <see cref="UnityEngine.UI.Text"/> <see cref="GameObject"/> with the text from a PSD <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> used to create the <see cref="UnityEngine.UI.Text"/> from.</param>
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

            GameObject gameObject = CreateObj(layer.Name);
            updateRectPosition(gameObject, new Vector3(x + (width / 2), y - (height / 2), currentDepth));

#if UNITY_5
            updateParent(gameObject, currentGroupGameObject);
#else
            gameObject.transform.parent = currentGroupGameObject.transform;
#endif 

            currentDepth -= depthStep;

            Font font = getFontInfo();

            Text textUI = gameObject.GetComponent<Text>();
            if (textUI == null)
            {
                textUI = gameObject.AddComponent<Text>();
            }

            //showLog("update text=" + gameObject.name + ",set text=" + layer.Text);

            textUI.text = layer.Text;
            textUI.font = font;
            textUI.verticalOverflow = VerticalWrapMode.Overflow;
            textUI.rectTransform.sizeDelta = new Vector2(width, height);
            textUI.raycastTarget = false;//can not  click text by yanru 2016-06-16 19:27:41

            float fontSize = layer.FontSize / PixelsToUnits;
            float ceiling = Mathf.Ceil(fontSize);
            if (fontSize < ceiling)
            {
                // Unity UI Text doesn't support floating point font sizes, so we have to round to the next size and scale everything else
                float scaleFactor = ceiling / fontSize;
                textUI.fontSize = (int)ceiling;
                textUI.rectTransform.sizeDelta *= scaleFactor;

                //Debug.Log(Time.time + "set txt=" + textUI.name + ",scale=" + textUI.rectTransform.localScale / scaleFactor + ",factor=" + scaleFactor);
                updateRectScale(textUI, textUI.rectTransform.localScale / scaleFactor);
            }
            else
            {
                textUI.fontSize = (int)fontSize;
            }

            textUI.color = color;
            textUI.alignment = TextAnchor.MiddleCenter;


            Debug.Log(Time.time + ",update txt=" + textUI.name + ",color=" + layer.FillColor + ",color=" + color);

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
            _layerDic.Add(gameObject, layer);
        }

        private static Font getFontInfo()
        {
            Font font = null;
            
            if (textFont.Contains(TEST_FONT_NAME))
            {
                font = Resources.Load<Font>(textFont);
            }
            else
            {
                font = Resources.GetBuiltinResource<Font>(textFont);
            }
            return font;
        }

        private static void updateParent(GameObject gameObject, GameObject father)
        {
            gameObject.transform.SetParent(father.transform);
            gameObject.transform.localScale = Vector3.one;
        }

        private static void updateRectScale(Text textUI, Vector3 newScale)
        {
            textUI.rectTransform.localScale = newScale;
        }

        /// <summary>
        /// Creates a <see cref="UnityEngine.UI.Button"/> from the given <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The Layer to create the Button from.</param>
        private static void CreateUIButton(Layer layer)
        {
            // create an empty Image object with a Button behavior attached
            Image image = CreateUIImage(layer);

            
            /**
            Button button = image.gameObject.AddComponent<Button>();
             
            // look through the children for the sprite states
            foreach (Layer child in layer.Children)
            {
                if (child.Name.ContainsIgnoreCase("|Disabled"))
                {
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Disabled", string.Empty));
                    button.transition = Selectable.Transition.SpriteSwap;

                    SpriteState spriteState = button.spriteState;
                    spriteState.disabledSprite = CreateSprite(child);
                    button.spriteState = spriteState;
                }
                else if (child.Name.ContainsIgnoreCase("|Highlighted"))
                {
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Highlighted", string.Empty));
                    button.transition = Selectable.Transition.SpriteSwap;

                    SpriteState spriteState = button.spriteState;
                    spriteState.highlightedSprite = CreateSprite(child);
                    button.spriteState = spriteState;
                }
                else if (child.Name.ContainsIgnoreCase("|Pressed"))
                {
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Pressed", string.Empty));
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
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Default", string.Empty));
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Enabled", string.Empty));
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Normal", string.Empty));
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Up", string.Empty));

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

                    Debug.Log(Time.time + ",canvasSize=" + CanvasSize + ",child rect.x=" + child.Rect.width + ",height=" + child.Rect.height);
                    updateRectPosition(image.gameObject, new Vector3(x + (width / 2), y - (height / 2), currentDepth));

                    RectTransform transform = image.GetComponent<RectTransform>();
                    updateRectSize(ref transform, width, height);

                    button.targetGraphic = image;
                }
                else if (child.Name.ContainsIgnoreCase("|Text") && !child.IsTextLayer)
                {
                    updateLayerName(child, child.Name.ReplaceIgnoreCase("|Text", string.Empty));

                    GameObject oldGroupObject = currentGroupGameObject;
                    currentGroupGameObject = button.gameObject;

                    // If the "text" is a normal art layer, create an Image object from the "text"
                    CreateUIImage(child);

                    currentGroupGameObject = oldGroupObject;
                }

                if (child.IsTextLayer)
                {

                    // TODO: Create a child text game object
                }
            }
            **/
        }
         
        private static void updateLayerName(Layer child, string newName)
        {
            string layerInfo = "";
            child.Name = newName;
             
        }

        private static void  showLog(string str)
        {
            Debug.Log(Time.time + ":"+str);
        }

        #endregion
    }
}