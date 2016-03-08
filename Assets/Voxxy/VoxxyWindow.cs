//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEditor;
//using System.IO;

//namespace Voxxy {

//    //public class VoxxyWindow : EditorWindow {

//    //    bool groupEnabled;
//    //    bool myBool = true;
//    //    float myFloat = 1.23f;

//    //    // Add menu named "My Window" to the Window menu
//    //    [MenuItem("Window/Voxxy VOX Inspector")]
//    //    static void Init() {
//    //        // Get existing open window or if none, make a new one:
//    //        var window = EditorWindow.GetWindow<VoxxyWindow>();
//    //        window.Show();
//    //    }

//    //    internal void OnEnable() {
//    //        titleContent.text = "VOX Inspector";
//    //        titleContent.tooltip = "Use to manage the model import settings for VOX files.";
//    //    }

//    //    internal void Update() {
//    //        Repaint();
//    //    }

//    //    internal void SelectionChanged() {
//    //        Debug.Log("Selection");
//    //        Show();
//    //        Focus();
//    //    }

//    //    VoxFileSettings vox = new VoxFileSettings();

//    //    void OnGUI() {
//    //        Selection.selectionChanged = SelectionChanged;



//    //        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
//    //        var display = Selection.assetGUIDs.Any() ? Selection.assetGUIDs[0].ToString() : "Nothing selected.";

//    //        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
//    //        myBool = EditorGUILayout.Toggle("Toggle", myBool);
//    //        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
//    //        EditorGUILayout.EndToggleGroup();

//    //        if(Selection.assetGUIDs.Any()) {

//    //            var assetGuid = Selection.assetGUIDs.First();
//    //            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//    //            if(assetPath.EndsWith("vox")) {

//    //                EditorGUILayout.HelpBox("Huzzah.", MessageType.Info);

//    //                var path = assetPath.Replace(".vox", ".asset");
//    //                var settings = AssetDatabase.LoadAssetAtPath<VoxFileSettings>(path);

//    //                if(settings == null) {
//    //                    EditorGUILayout.HelpBox("No settings found, adding them.", MessageType.Warning);
//    //                    settings = ScriptableObject.CreateInstance<VoxFileSettings>();
//    //                    settings.name = "Import Settings";

//    //                    var mesh = new Mesh();
//    //                    var texture = new Texture2D(2, 2);
//    //                    var material = new Material(Shader.Find("Standard"));
//    //                    material.name = "RootMaterial";
//    //                    material.SetTexture("_MainTex", texture);

//    //                    AssetDatabase.CreateAsset(settings, path);
//    //                    AssetDatabase.SaveAssets();
//    //                }
//    //                else {
//    //                    EditorGUILayout.HelpBox("Settings found, path is: " + settings.name, MessageType.Info);
//    //                }
//    //            }
//    //            else {
//    //                EditorGUILayout.HelpBox("Select a VOX file to edit its import settings.", MessageType.Info);
//    //            }

//    //        }
//    //    }

//    //}

//    [Serializable]
//    public class VoxFileSettings : ScriptableObject {

//        public string voxAssetGuid;

//        [Tooltip("The VOX file (exported from Magica Voxel or similar) that will be imported into a Unity3d friendly mesh.")]
//        public DefaultAsset voxFile;

//        [Tooltip("The center of the model as a propotion of each side.  Values below 0 and above 1 can be used to move the center outside of the model's volume.  The size of the model is determined by the VOX file and not by the area filled by voxels.")]
//        public Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);

//        // TODO: Change this to option of voxels/meter or meters/voxel.
//        [Tooltip("The number of unity units (i.e. meters) that each voxel will occupy.")]
//        public float voxelSize = 1.0f;

//        [Tooltip("The percent of occluded voxels allowed on any given face.  0% will only render visible surface decreasing GPU overdraw.  Use 100% to extend faces into the model to decrease triangles.  Default of 40% is good for most situations.")]
//        [Range(0, 100)]
//        public int maximumOcclusionPercent = 40;

       
//        [HideInInspector]
//        public bool success;

//        [HideInInspector]
//        public string message;
//    }

//    //[CustomEditor(typeof(VoxFileSettings))]
//    //[CanEditMultipleObjects]
//    //public class VoxImportSettingsEditor : Editor {
//    //    private SerializedProperty voxFile;
//    //    private SerializedProperty center;
//    //    private SerializedProperty voxelSize;
//    //    private SerializedProperty maximumOcclusionPercent;
//    //    private SerializedProperty message;
//    //    private SerializedProperty success;

//    //    internal void OnEnable() {
//    //        //var obj = serializedObject.targetObject as VoxImportSettings;
//    //        var fileCount = serializedObject.FindProperty("files").arraySize;
//    //        for(int i = 0; i < fileCount; ++i) {
//    //            serializedObject.FindProperty("files").GetArrayElementAtIndex(i);
//    //        }
//    //        //voxFile = serializedObject.FindProperty("voxFile");
//    //        //center = serializedObject.FindProperty("center");
//    //        //voxelSize = serializedObject.FindProperty("voxelSize");
//    //        //maximumOcclusionPercent = serializedObject.FindProperty("maximumOcclusionPercent");
//    //        //message = serializedObject.FindProperty("message");
//    //        //success = serializedObject.FindProperty("success");
//    //    }

//    //    public override void OnInspectorGUI() {
//    //        //serializedObject.Update();
//    //        //EditorGUILayout.PropertyField(voxFile);
//    //        //EditorGUILayout.PropertyField(center);
//    //        //EditorGUILayout.PropertyField(voxelSize);
//    //        //EditorGUILayout.PropertyField(maximumOcclusionPercent);
//    //        //if(!string.IsNullOrEmpty(message.stringValue)) {
//    //        //    var messageType = success.boolValue ? MessageType.Info : MessageType.Error;
//    //        //    EditorGUILayout.HelpBox(message.stringValue, messageType);
//    //        //}
//    //        //serializedObject.ApplyModifiedProperties();
//    //    }
//    //}


//    public static class CustomAssetUtility {
//        public static void CreateAsset<T>() where T : ScriptableObject {
//            T asset = ScriptableObject.CreateInstance<T>();

//            //string path = AssetDatabase.GetAssetPath(Selection.activeObject);
//            //if(path == "") {
//            //    path = "Assets";
//            //}
//            //else if(Path.GetExtension(path) != "") {
//            //    path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
//            //}

//            string path = "Assets/Voxxy/RobotDriller.vox";

//            var meshPath = path.Replace(".vox", ".asset");

//            AssetDatabase.CreateAsset(new Mesh(), meshPath);
//            asset.hideFlags = HideFlags.HideInHierarchy;
//            AssetDatabase.AddObjectToAsset(asset, meshPath);
//            var texture = new Texture2D(2, 2);
//            var material = new Material(Shader.Find("Standard"));
//            material.name = "RootMaterial";
//            material.SetTexture("_MainTex", texture);
//            AssetDatabase.AddObjectToAsset(texture, meshPath);
//            AssetDatabase.AddObjectToAsset(material, meshPath);

//            AssetDatabase.SaveAssets();
//            EditorUtility.FocusProjectWindow();
//            Selection.activeObject = asset;
//        }
//    }

//    public class CreateDialoguePage {
//        [MenuItem("Assets/Create/VOX Import Settings")]
//        public static void CreateAsset() {
//            CustomAssetUtility.CreateAsset<VoxFileSettings>();
//        }
//    }
//}