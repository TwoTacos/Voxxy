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

            EditorGUILayout.LabelField("VOX Import Settings");

            EditorGUILayout.LabelField("Model", headerStyle);

            var fillVoidsLabel = new GUIContent("Fill Voids", "Some VOX files have hollow centers to reduce the size of the VOX file, these cause larger than necessary meshes during import.  Filling voids can dramatically increase import time, so only select when necessary.");
            importer.FillVoids = EditorGUILayout.Toggle(fillVoidsLabel, importer.FillVoids);

            // TODO: Model import option to fill in empty spaces - trade speed for mesh size.

            EditorGUILayout.LabelField("Mesh", headerStyle);

            var scaleLabel = new GUIContent("Scale Factor", "The number of unity units (i.e. meters) that each voxel will occupy.");
            importer.ScaleFactor = EditorGUILayout.FloatField(scaleLabel, importer.ScaleFactor);

            // TODO: Other typical model import options.

            var centerLabel = new GUIContent("Center", "The center of the model proportional to each axis.  This is proportional to the length of each side of the VOX extents - which may be larger than the model.");
            importer.Center = EditorGUILayout.Vector3Field(centerLabel, importer.Center);

            var percentLabel = new GUIContent("Max Occlusion", "The maximum percentage of occlusion allowed when expanding surfaces.  0% will never expand the surface and will minimize overdraw but will create many more triangles.  100% will expand surfaces behind other surfaces whenever it can, reducing triangles and increasing overdraw.  40% is generally a good balance.");
            importer.MaxPercent = EditorGUILayout.IntSlider(percentLabel, importer.MaxPercent, 0, 100);

            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            GUILayout.Label("");
            if(GUILayout.Button("Reimport")) {
                importer.Reimport();
            }
            GUILayout.Label("");
            if(!importer.HaveImportSettingsChanged()) {
                GUI.enabled = false;
            }
            if(GUILayout.Button("Revert")) {
                importer.Revert();
            }
            if(GUILayout.Button("Apply")) {
                // TODO: voxxyMesh.Assets.Refresh();
                importer.Reimport();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;


            EditorGUILayout.Separator();

            if(!string.IsNullOrEmpty(importer.Message)) {
                var messageType = importer.Success ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(importer.Message, messageType);
            }

        }

        private VoxImporter OpenOrCreateImporter(DefaultAsset voxAsset) {
            var voxAssetPath = AssetDatabase.GetAssetPath(voxAsset);
            var voxFileInfo = new FileInfo(voxAssetPath);
            var settingsPath = voxAssetPath.Replace(voxFileInfo.Extension, "Settings.asset");
            var settingsInfo = new FileInfo(settingsPath);
            if(!settingsInfo.Exists) {
                var importer = ScriptableObject.CreateInstance<VoxImporter>();
                importer.VoxAssetPath = voxAssetPath;
                // TODO: settings.hideFlags = HideFlags.HideInHierarchy;
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

