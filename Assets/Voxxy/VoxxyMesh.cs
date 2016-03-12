#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Voxxy {

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxxyMesh : MonoBehaviour {

        public DefaultAsset VoxAsset;

        [SerializeField]
        [HideInInspector]
        private DefaultAsset LastVoxAsset;

        /// <summary>
        /// This method, along with the ExecuteInEditMode class attribute re-imports the model when Unity starts up or the model is enabled.
        /// This ensures that any changes to the VOX file when Unity was not running is automatically reflected in the model.
        /// </summary>
        internal void OnEnable() {
            Refresh();
        }

        /// <summary>
        /// This method, along with the ExecuteInEditMode class attribute re-imports the model whenever any of the import attributes above is changed.
        /// </summary>
        internal void Update() {
            Refresh();
        }

        public void Refresh() {
            if(VoxAsset != LastVoxAsset) {
                if(VoxAsset == null) {
                    SetMeshAndMaterial(null, null);
                }
                else {
                    var importer = VoxImporterEditor.OpenOrCreateImporter(VoxAsset);
                    SetMeshAndMaterial(importer.Mesh, importer.Material);
                }
                LastVoxAsset = VoxAsset;
            }
        }

        private void SetMeshAndMaterial(Mesh mesh, Material material) {
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.GetComponent<MeshRenderer>();
            if(renderer.sharedMaterial == null || material != null) {
                renderer.sharedMaterial = material;
            }
        }
    }
}

#endif

