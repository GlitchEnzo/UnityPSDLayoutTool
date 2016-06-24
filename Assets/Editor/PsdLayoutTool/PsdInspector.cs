﻿namespace PsdLayoutTool
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A custom Inspector to allow PSD files to be turned into prefabs and separate textures per layer.
    /// </summary>
    /// <remarks>
    /// Unity isn't able to draw the custom inspector for a TextureImporter (even if calling the base
    /// method or calling DrawDefaultInspector).  It comes out as just a generic, hard to use mess of GUI
    /// items.  To add in the buttons we want without disrupting the normal GUI for TextureImporter, we have
    /// to do some reflection "magic".
    /// Thanks to DeadNinja: http://forum.unity3d.com/threads/custom-textureimporterinspector.260833/
    /// </remarks>
    [CustomEditor(typeof(TextureImporter))]
    public class PsdInspector : Editor
    {
        /// <summary>
        /// The native Unity editor used to render the <see cref="TextureImporter"/>'s Inspector.
        /// </summary>
        private Editor nativeEditor;

        /// <summary>
        /// The style used to draw the section header text.
        /// </summary>
        private GUIStyle guiStyle;

        /// <summary>
        /// Called by Unity when any Texture file is first clicked on and the Inspector is populated.
        /// </summary>
        public void OnEnable()
        {
            // use reflection to get the default Inspector
            Type type = Type.GetType("UnityEditor.TextureImporterInspector, UnityEditor");

            if(target==null )
            {
                Debug.Log(Time.time + "target is null");
            }
            if (type == null)
            {
                Debug.Log(Time.time + "type is null");
            }

            nativeEditor = CreateEditor(target, type);

            // set up the GUI style for the section headers
            guiStyle = new GUIStyle();
            guiStyle.richText = true;
            guiStyle.fontSize = 14;
            guiStyle.normal.textColor = Color.black;

            if (Application.HasProLicense())
            {
                guiStyle.normal.textColor = Color.white;
            }


            TextureImporter import = (TextureImporter)target;
            if (import.textureType != TextureImporterType.Sprite)
            {
                import.textureType = TextureImporterType.Sprite;
                import.mipmapEnabled = false;
                import.filterMode = FilterMode.Bilinear;
                import.SaveAndReimport();
                import.textureFormat = TextureImporterFormat.AutomaticCompressed;
                Debug.Log(Time.time + " focuse on cur image forse set Image as Sprite type！");
            }
        }

        /// <summary>
        /// Draws the Inspector GUI for the TextureImporter.
        /// Normal Texture files should appear as they normally do, however PSD files will have additional items.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (nativeEditor != null)
            {
                // check if it is a PSD file selected
                string assetPath = ((TextureImporter)target).assetPath;

                if (assetPath.EndsWith(PsdImporter.PSD_TAIL))
                {
                    GUILayout.Label("<b>PSD Layout Tool</b>", guiStyle, GUILayout.Height(23));

                    //All ui depth is the same 1
                    //GUIContent maximumDepthLabel = new GUIContent("Maximum Depth", "The Z value of the far back plane. The PSD will be laid out from here to 0.");
                    //PsdImporter.MaximumDepth = EditorGUILayout.FloatField(maximumDepthLabel, PsdImporter.MaximumDepth);

                    //modify by yanru
                    //GUIContent pixelsToUnitsLabel = new GUIContent("Pixels to Unity Units", "The scale of the Sprite objects, in the number of pixels to Unity world units.");
                    //PsdImporter.PixelsToUnits = EditorGUILayout.FloatField(pixelsToUnitsLabel, PsdImporter.PixelsToUnits);

                    //GUIContent useUnityUILabel = new GUIContent("Use Unity UI", "Create Unity UI elements instead of \"normal\" GameObjects.");
                    //PsdImporter.UseUnityUI = EditorGUILayout.Toggle(useUnityUILabel, PsdImporter.UseUnityUI);

                    //set ui width and height;       
                    GUIContent screenSize = new GUIContent("screenResolution", "set canvas width and height");
                    PsdImporter.ScreenResolution = EditorGUILayout.Vector2Field(screenSize, PsdImporter.ScreenResolution);

                    //set textFont
                    GUIContent fontName = new GUIContent("fontName", "use font on UI");
                    PsdImporter.textFont = EditorGUILayout.TextField(fontName, PsdImporter.textFont);

                    //force use fullScreenUI
                    //GUIContent useFullScreenUI = new GUIContent("useFullScreenUI", "useFullScreenUI");
                    //PsdImporter.fullScreenUI = EditorGUILayout.Toggle(useFullScreenUI, PsdImporter.fullScreenUI);

                    // draw our custom buttons for PSD files
                    //if (GUILayout.Button("Export Layers as Textures"))
                    //{
                    //    PsdImporter.ExportLayersAsTextures(assetPath);
                    //}

                    if (GUILayout.Button("Layout in Current Scene"))
                    {
                        PsdImporter.LayoutInCurrentScene(assetPath);
                    }

                    if (GUILayout.Button("Layout in Current Scene(use image real size)"))
                    {
                        PsdImporter.LayoutInCurrentScene(assetPath, true);
                    }

                    if (GUILayout.Button("Generate Prefab"))
                    {
                        PsdImporter.GeneratePrefab(assetPath);
                    }

                    if (GUILayout.Button("test ClickButton"))
                    {
                        PsdImporter.TestClick();
                    }

                    GUILayout.Space(3);

                    GUILayout.Box(string.Empty, GUILayout.Height(1), GUILayout.MaxWidth(Screen.width - 30));

                    GUILayout.Space(3);

                    GUILayout.Label("<b>Unity Texture Importer Settings</b>", guiStyle, GUILayout.Height(23));

                    // draw the default Inspector for the PSD
                    nativeEditor.OnInspectorGUI();
                }
                else
                {
                    // It is a "normal" Texture, not a PSD
                    nativeEditor.OnInspectorGUI();
                }
            }

            // Unfortunately we cant hide the ImportedObject section because the interal InspectorWindow checks via
            // "if (editor is AssetImporterEditor)" and all flags that this check sets are method local variables
            // so aside from direct patching UnityEditor.dll, reflection cannot be used here.
            // Therefore we just move the ImportedObject section out of view
            ////GUILayout.Space(2048);
        }
    }
}