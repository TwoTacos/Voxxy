using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxxyMesh : MonoBehaviour {

        //public DefaultAsset VoxAsset;

        //public VoxxySharedAssets Assets {
        //    get {
        //        if(assets == null) {
        //            assets = VoxxySharedAssets.GetAssetsForModel(VoxAsset);
        //        }
        //        return assets;
        //    }
        //}
        //private VoxxySharedAssets assets;

        //private void ClearModel() {
        //    gameObject.GetComponent<MeshFilter>().sharedMesh = null;
        //    if(gameObject.GetComponent<MeshRenderer>().sharedMaterial != null) {
        //        gameObject.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", null);
        //    }

        //    // on faile
        //    {
        //        gameObject.GetComponent<MeshFilter>().sharedMesh = null;
        //        gameObject.GetComponent<MeshRenderer>().sharedMaterial = null;
        //    }

        //}


        ///// <summary>
        ///// This method, along with the ExecuteInEditMode class attribute re-imports the model when Unity starts up or the model is enabled.
        ///// This ensures that any changes to the VOX file when Unity was not running is automatically reflected in the model.
        ///// </summary>
        //internal void OnEnable() {
        //    Refresh();
        //}

        ///// <summary>
        ///// This method, along with the ExecuteInEditMode class attribute re-imports the model whenever any of the import attributes above is changed.
        ///// </summary>
        //internal void Update() {
        //    Refresh();
        //}

        //[ContextMenu("Reimport")]
        //public void Reimport() {
        //    if(Assets != null) {
        //        Assets.Reimport();
        //        Bind();
        //    }
        //}

        //[ContextMenu("Refresh")] 
        //public void Refresh() {
        //    if(Assets != null) {
        //        Assets.Refresh();
        //        Bind();
        //    }
        //}

        //private void Bind() {
        //    gameObject.GetComponent<MeshFilter>().sharedMesh = assets.Mesh;
        //    gameObject.GetComponent<MeshRenderer>().sharedMaterial = assets.Material;
        //}

    }
}
