using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {
    public class VoxxyAssetPostProcessor : AssetPostprocessor {

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            var allChanges = importedAssets.Union(deletedAssets).Union(movedAssets);
            bool voxChanged = allChanges.Any(e => e.EndsWith("vox", StringComparison.InvariantCultureIgnoreCase));
            if(voxChanged) {
                var allActiveMeshes = UnityEngine.Object.FindObjectsOfType<VoxelMesh>();
                foreach(var activeMesh in allActiveMeshes) {
                    activeMesh.ReimportVox();
                }
            }
        }
    }
}
