namespace PsdLayoutTool
{
    using System;
    using System.Reflection;
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
        /// The selected TextureImporter as a <see cref="SerializedObject"/>.
        /// </summary>
        private SerializedObject serializedTarget;

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
            serializedTarget = new SerializedObject(target);

            // use reflection to get the default Inspector
            Type t = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name.ToLower().Contains("textureimporterinspector"))
                    {
                        t = type;
                        break;
                    }
                }
            }

            nativeEditor = CreateEditor(serializedTarget.targetObject, t);

            guiStyle = new GUIStyle();
            guiStyle.richText = true;
            guiStyle.fontSize = 14;
            guiStyle.normal.textColor = Color.white;
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
                string assetPath = ((TextureImporter)serializedTarget.targetObject).assetPath;

                if (assetPath.EndsWith(".psd"))
                {
                    GUILayout.Label("<b>PSD Layout Tool</b>", guiStyle, GUILayout.Height(23));

                    GUIContent maximumDepthLabel = new GUIContent("Maximum Depth", "The Z value of the far back plane. The PSD will be laid out from here to 0.");
                    PsdImporter.MaximumDepth = EditorGUILayout.FloatField(maximumDepthLabel, PsdImporter.MaximumDepth);

                    GUIContent pixelsToUnitsLabel = new GUIContent("Pixels to Unity Units", "The scale of the Sprite objects, in the number of pixels to Unity world units.");
                    PsdImporter.PixelsToUnits = EditorGUILayout.FloatField(pixelsToUnitsLabel, PsdImporter.PixelsToUnits);

                    // draw our custom buttons for PSD files
                    if (GUILayout.Button("Export Layers as Textures"))
                    {
                        PsdImporter.ExportLayersAsTextures(assetPath);
                    }

                    if (GUILayout.Button("Layout in Current Scene"))
                    {
                        PsdImporter.LayoutInCurrentScene(assetPath);
                    }

                    if (GUILayout.Button("Generate Prefab"))
                    {
                        PsdImporter.GeneratePrefab(assetPath);
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