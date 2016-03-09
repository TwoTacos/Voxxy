using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {

    /// <summary>
    /// Responsible for converting the VOX file into a set of shared assets in the project.
    /// </summary>
    public class VoxxySharedAssets {

        private VoxxySharedAssets() {

        }

        public static VoxxySharedAssets GetAssetsForModel(DefaultAsset voxAsset) {
            if(voxAsset == null) {
                return null;
            }
            var filepath = AssetDatabase.GetAssetPath(voxAsset);
            if(!sharedAssets.ContainsKey(filepath)) {
                if(filepath == null) {
                    Debug.LogWarning("Could not find VOX asset: " + voxAsset.name);
                    return null;
                }
                var file = new FileInfo(filepath);
                if(!file.Exists) {
                    Debug.LogWarning("Could not find VOX file: " + file.FullName);
                    return null;
                }
                if(file.Extension.ToLowerInvariant() != ".vox") {
                    Debug.LogWarning("VOX files must end in 'vox' extension" + file.FullName);
                    return null;
                }

                var newAssets = new VoxxySharedAssets();
                newAssets.Load(voxAsset);
                sharedAssets.Add(filepath, newAssets);
            }
            return sharedAssets[filepath];
        }
        
        internal static void RemoveDeletedAssets(string[] deletedAssets) {
            foreach(var asset in deletedAssets) {
                if(sharedAssets.ContainsKey(asset)) {
                    sharedAssets.Remove(asset);
                }
            }
        }

        private static Dictionary<string, VoxxySharedAssets> sharedAssets = new Dictionary<string, VoxxySharedAssets>();

        public VoxImportSettings Settings { get; private set; }

        private void Load(DefaultAsset voxAsset) {
            filepath = AssetDatabase.GetAssetPath(voxAsset);
            var assetPath = filepath.Replace(".vox", ".asset");
            Settings = AssetDatabase.LoadAssetAtPath<VoxImportSettings>(assetPath);
            if(Settings == null) {
                // First time, need to create.  Wait for end of mesh construction to save.
                Settings = ScriptableObject.CreateInstance<VoxImportSettings>();
                Settings.VoxAsset = voxAsset;
            }
            Reimport();
        }

        public string filepath;
        public DateTime filedate;

        private VoxFile vox;

        public void Reimport() {
            if(Settings.VoxAsset == null) {
                ClearModel();
                return;
            }

            var file = new FileInfo(filepath);

            Settings.FileDate = file.LastWriteTimeUtc;
            Settings.LastCenter = Settings.Center;
            Settings.LastScaleFactor = Settings.ScaleFactor;
            Settings.LastMaxPercent = Settings.MaxPercent;

            vox = new VoxFile();
            vox.Open(filepath);
            ConstructMesh();
        }

        public void Refresh() {
            if(Settings.VoxAsset == null) {
                ClearModel();
                return;
            }

            var filepath = AssetDatabase.GetAssetPath(Settings.VoxAsset);
            var file = new FileInfo(filepath);

            if(Settings.HaveChanged(file)) {
                Reimport();
            }
        }

        private void ClearModel() {
            filedate = DateTime.MinValue;
        }

        public void ConstructMesh() {

            var model = new VoxelModel(vox.Size);

            model.Fill(Voxel.unknown);
            // Copy model into volume
            foreach(var voxel in vox.Voxels) {
                var color = vox.Palette[voxel.Value];
                model[(Coordinate)voxel.Key] = new Voxel(VoxelType.Visible, color);
            }
            foreach(var coord in Coordinate.Shell(Coordinate.zero, vox.Size)) {
                model.Flood(coord, Voxel.unknown, Voxel.empty);
            }
            model.Replace(Voxel.unknown, Voxel.occluded);

            meshBuilder = new MeshBuilder(Settings.VoxAsset.name + " VOX Model");

            Settings.Success = true;

            for(int x = 0; x < vox.Size.x && Settings.Success == true; ++x) {
                var min = new Coordinate(x, 0, 0);
                var max = new Coordinate(x + 1, vox.Size.y, vox.Size.z);
                AddFaces(model, min, max, Coordinate.right);
                AddFaces(model, min, max, Coordinate.left);
            }
            for(int y = 0; y < vox.Size.y && Settings.Success == true; ++y) {
                var min = new Coordinate(0, y, 0);
                var max = new Coordinate(vox.Size.x, y + 1, vox.Size.z);
                AddFaces(model, min, max, Coordinate.up);
                AddFaces(model, min, max, Coordinate.down);
            }
            for(int z = 0; z < vox.Size.z && Settings.Success == true; ++z) {
                var min = new Coordinate(0, 0, z);
                var max = new Coordinate(vox.Size.x, vox.Size.y, z + 1);
                AddFaces(model, min, max, Coordinate.back);
                AddFaces(model, min, max, Coordinate.forward);
            }

            if(Settings.Success == true) {
                UpdateMaterialAndTexture();
                Settings.Message = String.Format("Voxxy model {0}: {1} vertices, {2} triangles. ", Settings.VoxAsset.name, meshBuilder.VertexCount, meshBuilder.TriangleCount);
            }
        }


        /// <summary>
        /// Vertices are mapped to position that would be correct for the 'forward' face (that is, +z direction).
        /// Quaternions are used to rotate this face in the direction of the occluding coordinate.
        ///        _________
        ///       /1       /|0
        ///      /        / |
        ///     /________/  |
        ///     |       |   |
        ///     |  2    |  / 3            y
        ///     |       | /               |  /z
        ///     |_______|/                | /
        ///                               |/_____x
        /// </summary>
        /// <param name="occludingDirection">The unit coordinate that indicates the direction, no validation is done, take care.</param>
        private void AddFaces(VoxelModel model, Coordinate from, Coordinate to, Coordinate occludingDirection) {
            var planeExtentX = from.x == to.x - 1 ? to.z : to.x;
            var planeExtentY = from.y == to.y - 1 ? to.z : to.y;
            var planeExtent = new Coordinate(planeExtentX, planeExtentY);

            // Copy plane slice out of model.
            Voxel[,] plane = new Voxel[planeExtentX, planeExtentY];
            for(var x = from.x; x < to.x; ++x) {
                for(var y = from.y; y < to.y; ++y) {
                    for(var z = from.z; z < to.z; ++z) {
                        var coord = new Coordinate(x, y, z);
                        var voxel = model[coord];
                        var occluding = model[coord + occludingDirection];
                        var planeX = from.x == to.x - 1 ? z : x;
                        var planeY = from.y == to.y - 1 ? z : y;
                        if(voxel.type == VoxelType.Visible && occluding.IsSolid) {
                            plane[planeX, planeY] = Voxel.occluded;
                        }
                        else {
                            plane[planeX, planeY] = voxel;
                        }
                    }
                }
            }

            var angle = Quaternion.LookRotation(occludingDirection);

            var centerOffset = new Vector3(-vox.Size.x * Settings.Center.x + 0.5f, -vox.Size.y * Settings.Center.y + 0.5f, -vox.Size.z * Settings.Center.z + 0.5f);

            for(var x = from.x; x < to.x; ++x) {
                for(var y = from.y; y < to.y; ++y) {
                    for(var z = from.z; z < to.z; ++z) {
                        var planeX = from.x == to.x - 1 ? z : x;
                        var planeY = from.y == to.y - 1 ? z : y;
                        if(plane[planeX, planeY].type == VoxelType.Visible) {
                            var face = new VoxelFace(plane, planeExtent, Settings.MaxPercent / 100f);
                            face.Create(new Coordinate(planeX, planeY));

                            while(face.Extend()) {
                            }

                            var xOffset = 0.5f * face.Bounds.size.x;
                            var yOffset = 0.5f * face.Bounds.size.y;
                            var zOffset = 0.5f * face.Bounds.size.z;
                            var vertex0 = angle * new Vector3(xOffset, yOffset, zOffset);
                            var vertex1 = angle * new Vector3(-xOffset, yOffset, zOffset);
                            var vertex2 = angle * new Vector3(-xOffset, -yOffset, zOffset);
                            var vertex3 = angle * new Vector3(xOffset, -yOffset, zOffset);

                            var faceCenter = new Vector3(face.Bounds.center.x, face.Bounds.center.y, z);
                            if(from.x == to.x - 1) {
                                faceCenter = new Vector3(x, face.Bounds.center.y, face.Bounds.center.x);
                            }
                            if(from.y == to.y - 1) {
                                faceCenter = new Vector3(face.Bounds.center.x, y, face.Bounds.center.y);
                            }

                            vertex0 = Settings.ScaleFactor * (centerOffset + faceCenter + vertex0);
                            vertex1 = Settings.ScaleFactor * (centerOffset + faceCenter + vertex1);
                            vertex2 = Settings.ScaleFactor * (centerOffset + faceCenter + vertex2);
                            vertex3 = Settings.ScaleFactor * (centerOffset + faceCenter + vertex3);

                            // Switch texture for specific direction since we go through the block the same way for each and need to flip.
                            var texture = face.GetTexture(occludingDirection == Coordinate.forward || occludingDirection == Coordinate.left, occludingDirection == Coordinate.down);
                            meshBuilder.AddQuad(vertex0, vertex1, vertex2, vertex3, texture);

                            if(meshBuilder.VertexCount > 65000) {
                                Settings.Message = "VOX model contains more then 65,000 vertices which cannot be stored in a single Unity Mesh.  Try dividing VOX file into simpler shapes and importing as multiple meshes.";
                                Settings.Success = false;
                                return;
                            }

                            face.ClearPlane();
                        }
                    }
                }
            }
        }

        private MeshBuilder meshBuilder;

        private void UpdateMaterialAndTexture() {
            var voxPath = AssetDatabase.GetAssetPath(Settings.VoxAsset);
            var assetPath = voxPath.Replace(".vox", ".asset");

            CreateOrUpdateMesh(assetPath);
            Texture2D existingAtlas = AddOrUpdateTexture(assetPath);
            AddOrUpdateMaterial(assetPath, existingAtlas);
            AddOrUpdateSettings(assetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateOrUpdateMesh(string assetPath) {
            var newMesh = meshBuilder.Mesh;
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if(existingMesh == null) {
                AssetDatabase.CreateAsset(newMesh, assetPath);
                existingMesh = newMesh;
            }
            else {
                existingMesh.triangles = null;
                existingMesh.uv = null;
                existingMesh.vertices = newMesh.vertices;
                existingMesh.uv = newMesh.uv;
                existingMesh.triangles = newMesh.triangles;
                existingMesh.RecalculateNormals();
            }
            Mesh = existingMesh;
        }

        private Texture2D AddOrUpdateTexture(string assetPath) {
            var newAtlas = meshBuilder.Atlas;
            var existingAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if(existingAtlas == null) {
                AssetDatabase.AddObjectToAsset(newAtlas, assetPath);
                existingAtlas = newAtlas;
            }
            else {
                existingAtlas.LoadRawTextureData(newAtlas.GetRawTextureData());
            }

            return existingAtlas;
        }

        private void AddOrUpdateMaterial(string assetPath, Texture2D existingAtlas) {
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if(existingMaterial == null) {
                existingMaterial = new Material(Shader.Find("Standard"));
                existingMaterial.name = Settings.VoxAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(existingMaterial, assetPath);
            }
            existingMaterial.SetTexture("_MainTex", existingAtlas);
            Material = existingMaterial;
        }

        private void AddOrUpdateSettings(string assetPath) {
            var existingSettings = AssetDatabase.LoadAssetAtPath<VoxImportSettings>(assetPath);
            if(existingSettings == null) {
                AssetDatabase.AddObjectToAsset(Settings, assetPath);
            }
            else {
                // Do nothing, we are already editing the live asset version.
            }
        }

        public Mesh Mesh { get; private set; }

        public Material Material { get; private set; }

    }
}
