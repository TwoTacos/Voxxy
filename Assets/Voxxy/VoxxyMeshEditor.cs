using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {

    [CustomEditor(typeof(VoxxyMesh))]
    public class VoxxyMeshEditor : Editor {

        public override void OnInspectorGUI() {

            var voxxyMesh = (VoxxyMesh)target;

            var headerStyle = new GUIStyle();
            headerStyle.fontStyle = FontStyle.Bold;

            var fileLabel = new GUIContent("VOX File", "The VOX file (expored from Magica Voxel or similar) that will be imported into a Unity3d friendly mesh.");
            voxxyMesh.VoxAsset = (DefaultAsset)EditorGUILayout.ObjectField(fileLabel, voxxyMesh.VoxAsset, typeof(DefaultAsset), false);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Shared VOX Import Settings", headerStyle);

            if(voxxyMesh.Assets == null) {
                EditorGUILayout.HelpBox("Select VOX file to unlock settings.", MessageType.Info);
            }
            else {
                var settings = voxxyMesh.Assets.Settings;

                var scaleLabel = new GUIContent("Scale Factor", "The number of unity units (i.e. meters) that each voxel will occupy.");
                settings.ScaleFactor = EditorGUILayout.FloatField(scaleLabel, settings.ScaleFactor);

                var centerLabel = new GUIContent("Center", "The center of the model proportional to each axis.");
                settings.Center = EditorGUILayout.Vector3Field(centerLabel, settings.Center);

                var percentLabel = new GUIContent("Max Occlusion", "The maximum percentage of occlusion allowed when expanding surfaces.");
                settings.MaxPercent = EditorGUILayout.IntSlider(percentLabel, settings.MaxPercent, 0, 100);

                if(settings.ScaleFactor != settings.LastScaleFactor || settings.Center != settings.LastCenter || settings.MaxPercent != settings.LastMaxPercent) {
                    AssetDatabase.SaveAssets();
                }

                if(GUILayout.Button("Apply Import Settings")) {
                    voxxyMesh.Assets.Refresh();
                }

                if(!string.IsNullOrEmpty(settings.Message)) {
                    var messageType = settings.Success ? MessageType.Info : MessageType.Error;
                    EditorGUILayout.HelpBox(settings.Message, messageType);
                }

            }

        }
    }
}
