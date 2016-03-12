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
            bool voxChanged = allChanges.Any(e => e.EndsWith(".vox", StringComparison.InvariantCultureIgnoreCase));
            if(voxChanged) {
                foreach(var change in allChanges) {
                    var voxAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(change);
                    var importer = VoxImporterEditor.OpenOrCreateImporter(voxAsset);
                    importer.Reimport();
                }
            }

            //VoxxySharedAssets.RemoveDeletedAssets(deletedAssets);
        }
    }
}
