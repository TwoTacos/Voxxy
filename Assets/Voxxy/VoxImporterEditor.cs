#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {
    [CustomEditor(typeof(DefaultAsset))]
    public class VoxImporterEditor : Editor {

        public override void OnInspectorGUI() {

            var asset = (DefaultAsset)target;
            var filePath = AssetDatabase.GetAssetPath(asset);
            var fileInfo = new FileInfo(filePath);

            if(fileInfo.Extension.ToLowerInvariant() == ".vox") {
                OnVoxFileGUI(asset);
            }

        }

        public void OnVoxFileGUI(DefaultAsset voxAsset) {
            GUI.enabled = true;

            var importer = OpenOrCreateImporter(voxAsset);

            var headerStyle = new GUIStyle();
            headerStyle.fontStyle = FontStyle.Bold;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("VOX Import Settings");
            if(GUILayout.Button("Reimport")) {
                importer.Reimport();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Model", headerStyle);

            var fillVoidsLabel = new GUIContent("Fill Voids", "Some VOX files have hollow centers to reduce the size of the VOX file, these cause larger than necessary meshes during import.  Filling voids can dramatically increase import time, so only select when necessary.");
            importer.FillVoids = EditorGUILayout.Toggle(fillVoidsLabel, importer.FillVoids);

            EditorGUILayout.LabelField("Mesh", headerStyle);

            var scaleLabel = new GUIContent("Scale Factor", "The number of unity units (i.e. meters) that each voxel will occupy.");
            importer.ScaleFactor = EditorGUILayout.FloatField(scaleLabel, importer.ScaleFactor);

            var optimizeMesh = new GUIContent("Optimize Mesh", "The vertices and indices will be reordered for better GPU performance.");
            importer.OptimizeMesh = EditorGUILayout.Toggle(optimizeMesh, importer.OptimizeMesh);

            var centerLabel = new GUIContent("Center", "The center of the model proportional to each axis.  This is proportional to the length of each side of the VOX extents - which may be larger than the model.");
            importer.Center = EditorGUILayout.Vector3Field(centerLabel, importer.Center);

            var percentLabel = new GUIContent("Max Occlusion", "The maximum percentage of occlusion allowed when expanding surfaces.  0% will never expand the surface and will minimize overdraw but will create many more triangles.  100% will expand surfaces behind other surfaces whenever it can, reducing triangles and increasing overdraw.  40% is generally a good balance.");
            importer.MaxPercent = EditorGUILayout.IntSlider(percentLabel, importer.MaxPercent, 0, 100);

            EditorGUILayout.LabelField("Textures", headerStyle);

            var defaultPaletteLabel = new GUIContent("Import Default Palette", "Import the palette that is inside the VOX file (if one exists) and make it available as palette #0.");
            importer.ImportDefaultPalette = EditorGUILayout.Toggle(defaultPaletteLabel, importer.ImportDefaultPalette);

            for(int i = 0;  i < importer.Palettes.Count(); ++i) {
                var palette = importer.Palettes[i];
                var additionalPaletteLabel = new GUIContent("Import Palette " + (i + 1).ToString(), "Import an additional palette as a 256x1 PNG image which can be substituted in the model as palette #" + (i + 1).ToString() + ". Select the 'none' texture to remove.");
                importer.Palettes[i] = EditorGUILayout.ObjectField(additionalPaletteLabel, palette, typeof(Texture2D), false) as Texture2D;
            }
            var newPaletteLabel = new GUIContent("New Palette", "Add an additional palette (a 256x1 PNG image) which can be substituted for the palette using the Voxxy Mesh component.");
            var newPalette = EditorGUILayout.ObjectField(newPaletteLabel, null, typeof(Texture2D), false) as Texture2D;
            if(newPalette != null) {
                importer.Palettes.Add(newPalette);
            }
            importer.Palettes.Remove(null);

            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            GUILayout.Label("");
            GUILayout.Label("");
            if(!importer.HaveImportSettingsChanged()) {
                GUI.enabled = false;
            }
            if(GUILayout.Button("Revert")) {
                importer.Revert();
            }
            if(GUILayout.Button("Apply")) {
                importer.Reimport();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;


            EditorGUILayout.Separator();

            if(!string.IsNullOrEmpty(importer.Message)) {
                var messageType = importer.Success ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(importer.Message, messageType);
            }

            // Helpful hints that we can fix up.
            var lights = Light.GetLights(LightType.Directional, Int32.MaxValue);
            var badBiasLight = lights.FirstOrDefault(e => e.shadowNormalBias > 0 && e.shadows != LightShadows.None);
            if(badBiasLight != null) { 
                EditorGUILayout.HelpBox(String.Format("Light '{0}' uses shadows and should have a Normal Bias of 0 for use with the sharp edges of voxel shapes.", badBiasLight.name), MessageType.Warning);
                GUILayout.BeginHorizontal();
                GUILayout.Label("");
                if(GUILayout.Button("Fix Lights Normal Bias")) {
                    badBiasLight.shadowNormalBias = 0;
                }
                GUILayout.Label("");
                GUILayout.EndHorizontal();
            }

        }

        public static VoxImporter OpenOrCreateImporter(DefaultAsset voxAsset) {
            var voxAssetPath = AssetDatabase.GetAssetPath(voxAsset);
            if(string.IsNullOrEmpty(voxAssetPath)) {
                return null;
            }
            var voxFileInfo = new FileInfo(voxAssetPath);
            var settingsPath = voxAssetPath.Replace(voxFileInfo.Extension, "Settings.asset");
            var settingsInfo = new FileInfo(settingsPath);
            if(!settingsInfo.Exists) {
                var importer = ScriptableObject.CreateInstance<VoxImporter>();
                importer.VoxAssetPath = voxAssetPath;
                AssetDatabase.CreateAsset(importer, settingsPath);
                AssetDatabase.SaveAssets();
                return importer;
            }
            else {
                var importer = AssetDatabase.LoadAssetAtPath<VoxImporter>(settingsPath);
                importer.VoxAssetPath = voxAssetPath;
                return importer;
            }
        }


    }

}

#endif

