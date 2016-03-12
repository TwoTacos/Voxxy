#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {
    public class VoxImporter : ScriptableObject {

        [HideInInspector]
        public string VoxAssetPath;

        [HideInInspector]
        public bool FillVoids = false;

        [HideInInspector]
        public bool OptimizeMesh = true;

        [HideInInspector]
        public float ScaleFactor = 0.125f;

        [HideInInspector]
        public Vector3 Center = new Vector3(0.5f, 0.5f, 0.5f);

        [HideInInspector]
        public int MaxPercent = 40;

        [HideInInspector]
        public bool LastFillVoids = false;
        [HideInInspector]
        public bool LastOptimizeMesh = true;
        [HideInInspector]
        public float LastScaleFactor = 0f;
        [HideInInspector]
        public Vector3 LastCenter = new Vector3(0.5f, 0.5f, 0.5f);
        [HideInInspector]
        public int LastMaxPercent = 0;
        [HideInInspector]
        public bool Success;
        [HideInInspector]
        public string Message;
        [HideInInspector]
        public DateTime FileDate;

        [HideInInspector]
        public bool ImportDefaultPalette = true;
        [HideInInspector]
        public List<Texture2D> Palettes = new List<Texture2D>();

        public bool HaveChanged(FileInfo file) {
            return FileDate != file.LastWriteTimeUtc || HaveImportSettingsChanged();
            
        }

        public bool HaveImportSettingsChanged() {
            return Center != LastCenter || ScaleFactor != LastScaleFactor || MaxPercent != LastMaxPercent || FillVoids != LastFillVoids || OptimizeMesh != LastOptimizeMesh;
        }

        public void Revert() {
            Center = LastCenter;
            ScaleFactor = LastScaleFactor;
            MaxPercent = LastMaxPercent;
            FillVoids = LastFillVoids;
            OptimizeMesh = LastOptimizeMesh;
        }

        private VoxFile vox;

        public void Reimport() {
            var file = new FileInfo(VoxAssetPath);

            FileDate = file.LastWriteTimeUtc;
            LastCenter = Center;
            LastScaleFactor = ScaleFactor;
            LastMaxPercent = MaxPercent;
            LastFillVoids = FillVoids;
            LastOptimizeMesh = OptimizeMesh;

            textureGuid = null;
            meshGuid = null;
            materialGuid = null;

            vox = new VoxFile();
            vox.Open(VoxAssetPath);
            ConstructMesh(GrayscalePalette());
        }

        public Color[] GrayscalePalette() {
            var palette = new Color[256];
            for(int i = 0; i < 256; ++i) {
                var f = i / 255.0f;
                palette[i] = new Color(f, f, f);
            }
            return palette;
        }

        public void ConstructMesh(Color[] palette) {

            var model = new VoxelModel(vox.Size);

            model.Fill(Voxel.unknown);
            // Copy model into volume
            foreach(var voxel in vox.Voxels) {
                var color = palette[voxel.Value];
                model[(Coordinate)voxel.Key] = new Voxel(VoxelType.Visible, color);
            }

            if(FillVoids) {
                foreach(var coord in Coordinate.Shell(Coordinate.zero, vox.Size)) {
                    model.Flood(coord, Voxel.unknown, Voxel.empty);
                }
                model.Replace(Voxel.unknown, Voxel.occluded);
            }

            var voxAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(VoxAssetPath);
            meshBuilder = new MeshBuilder(voxAsset.name + " VOX Model");

            Success = true;

            for(int x = 0; x < vox.Size.x && Success == true; ++x) {
                var min = new Coordinate(x, 0, 0);
                var max = new Coordinate(x + 1, vox.Size.y, vox.Size.z);
                AddFaces(model, min, max, Coordinate.right);
                AddFaces(model, min, max, Coordinate.left);
            }
            for(int y = 0; y < vox.Size.y && Success == true; ++y) {
                var min = new Coordinate(0, y, 0);
                var max = new Coordinate(vox.Size.x, y + 1, vox.Size.z);
                AddFaces(model, min, max, Coordinate.up);
                AddFaces(model, min, max, Coordinate.down);
            }
            for(int z = 0; z < vox.Size.z && Success == true; ++z) {
                var min = new Coordinate(0, 0, z);
                var max = new Coordinate(vox.Size.x, vox.Size.y, z + 1);
                AddFaces(model, min, max, Coordinate.back);
                AddFaces(model, min, max, Coordinate.forward);
            }

            if(Success == true) {
                UpdateMaterialAndTexture();
                Message = String.Format("Voxxy model {0}: {1} vertices, {2} triangles. ", voxAsset.name, meshBuilder.VertexCount, meshBuilder.TriangleCount);
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

            var centerOffset = new Vector3(-vox.Size.x * Center.x + 0.5f, -vox.Size.y * Center.y + 0.5f, -vox.Size.z * Center.z + 0.5f);

            for(var x = from.x; x < to.x; ++x) {
                for(var y = from.y; y < to.y; ++y) {
                    for(var z = from.z; z < to.z; ++z) {
                        var planeX = from.x == to.x - 1 ? z : x;
                        var planeY = from.y == to.y - 1 ? z : y;
                        if(plane[planeX, planeY].type == VoxelType.Visible) {
                            var face = new VoxelFace(plane, planeExtent, MaxPercent / 100f);
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

                            vertex0 = ScaleFactor * (centerOffset + faceCenter + vertex0);
                            vertex1 = ScaleFactor * (centerOffset + faceCenter + vertex1);
                            vertex2 = ScaleFactor * (centerOffset + faceCenter + vertex2);
                            vertex3 = ScaleFactor * (centerOffset + faceCenter + vertex3);

                            // Switch texture for specific direction since we go through the block the same way for each and need to flip.
                            var texture = face.GetTexture(occludingDirection == Coordinate.forward || occludingDirection == Coordinate.left, occludingDirection == Coordinate.down);
                            meshBuilder.AddQuad(vertex0, vertex1, vertex2, vertex3, texture);

                            if(meshBuilder.VertexCount > 65000) {
                                Message = "VOX model contains more then 65,000 vertices which cannot be stored in a single Unity Mesh.  Try dividing VOX file into simpler shapes and importing as multiple meshes.";
                                Success = false;
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
            CreateOrUpdateMesh();
            Texture2D atlas = CreateOrUpdateTexture(0);
            CreateOrUpdateMaterial(atlas);

            for(int i = 0; i < Palettes.Count; ++i) {
                CreateOrUpdateTexture(i + 1);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateOrUpdateMesh() {
            var voxFileInfo = new FileInfo(VoxAssetPath);
            CreateSubAssetFolder(VoxAssetPath, "Meshes");
            var meshPath = VoxAssetPath.Replace(voxFileInfo.Name, "Meshes/" + voxFileInfo.Name);
            meshPath = meshPath.Replace(voxFileInfo.Extension, "Mesh.asset");
            if(!string.IsNullOrEmpty(meshGuid)) {
                meshPath = AssetDatabase.GUIDToAssetPath(meshGuid);
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh == null) {
                mesh = meshBuilder.Mesh;
                AssetDatabase.CreateAsset(mesh, meshPath);
                meshGuid = AssetDatabase.AssetPathToGUID(meshPath);
            }
            else {
                mesh.triangles = null;
                mesh.uv = null;
                mesh.vertices = meshBuilder.Mesh.vertices;
                mesh.uv = meshBuilder.Mesh.uv;
                mesh.triangles = meshBuilder.Mesh.triangles;
                mesh.RecalculateNormals();
            }
            if(OptimizeMesh) {
                mesh.Optimize();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [SerializeField]
        [HideInInspector]
        private string meshGuid;

        public Mesh Mesh {
            get {
                var path = AssetDatabase.GUIDToAssetPath(meshGuid);
                return AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
        }

        private Texture2D CreateOrUpdateTexture(int index) {
            var voxFileInfo = new FileInfo(VoxAssetPath);
            CreateSubAssetFolder(VoxAssetPath, "Textures");
            var pngPath = VoxAssetPath.Replace(voxFileInfo.Name, "Textures/" + voxFileInfo.Name);
            pngPath = pngPath.Replace(voxFileInfo.Extension, String.Format("Albedo{0}.png", index));
            if(!string.IsNullOrEmpty(textureGuid)) {
                pngPath = AssetDatabase.GUIDToAssetPath(textureGuid);
            }

            var pngFileInfo = new FileInfo(pngPath);
            var alreadyExists = pngFileInfo.Exists;

            var newAtlas = meshBuilder.Atlas;
            var palette = vox.Palette;
            if(index > 0) {
                palette = ExtractPalette(Palettes[index - 1]);
            }
            var coloredAtlas = Colorize(newAtlas, palette);
            var atlasPng = coloredAtlas.EncodeToPNG();
            File.WriteAllBytes(pngPath, atlasPng);

            AssetDatabase.ImportAsset(pngPath);

            if(!alreadyExists) {
                // default import settings suitable for voxel models.
                var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if(importer != null) {
                    importer.textureType = TextureImporterType.Image;
                    importer.alphaIsTransparency = false;
                    importer.grayscaleToAlpha = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Point;
                    importer.maxTextureSize = Mathf.Max(newAtlas.width, newAtlas.height);
                    importer.textureFormat = TextureImporterFormat.ARGB32;
                    AssetDatabase.ImportAsset(pngPath);
                    textureGuid = AssetDatabase.AssetPathToGUID(pngPath);
                }
            }

            var existingAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            return existingAtlas;
        }

        /// <summary>
        /// Given a grayscale texture with each gray representing an offset into the provided palette, return an image with the appropriate color.
        /// </summary>
        private Texture2D Colorize(Texture2D grayscale, Color[] palette) {
            var colorized = new Texture2D(grayscale.width, grayscale.height);
            //result.LoadRawTextureData(newAtlas.GetRawTextureData());
            for(int x = 0; x < colorized.width; ++x) {
                for(int y = 0; y < colorized.height; ++y) {
                    var gray = grayscale.GetPixel(x, y);
                    var color = palette[(int)(255 * gray.r)];
                    colorized.SetPixel(x, y, color);
                }
            }
            return colorized;
        }

        private static void MarkTextureReadable(Texture2D texture) {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if(importer != null && importer.textureType != TextureImporterType.Advanced && importer.isReadable == false) {
                importer.textureType = TextureImporterType.Advanced;
                importer.isReadable = true;
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        private Color[] ExtractPalette(Texture2D image) {
            MarkTextureReadable(image);
            var palette = new Color[256];
            if(image.width != 256 && image.height != 1) {
                throw new ArgumentException("The provided image must have 256 colors that map to the palette.");
            }
            for(int i = 0; i < 256; ++i) {
                palette[i] = image.GetPixel(i - 1, 0); // offset issue with VOX palettes.
            }
            return palette;
        }

        [SerializeField]
        [HideInInspector]
        private string textureGuid;

        public Texture2D Texture {
            get {
                var path = AssetDatabase.GUIDToAssetPath(textureGuid);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private void CreateOrUpdateMaterial(Texture2D atlas) {
            var voxFileInfo = new FileInfo(VoxAssetPath);
            CreateSubAssetFolder(VoxAssetPath, "Materials");
            var materialPath = VoxAssetPath.Replace(voxFileInfo.Name, "Materials/" + voxFileInfo.Name);
            materialPath = materialPath.Replace(voxFileInfo.Extension, "Opaque.mat");
            if(!string.IsNullOrEmpty(materialGuid)) {
                materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material == null) {
                material = new Material(Shader.Find("Standard"));
                var voxAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(VoxAssetPath);
                material.name = voxAsset.name + " Material";
                AssetDatabase.CreateAsset(material, materialPath);
                materialGuid = AssetDatabase.AssetPathToGUID(materialPath);
            }
            material.SetTexture("_MainTex", atlas);
        }

        private void CreateSubAssetFolder(string path, string subFolder) {
            var fi = new FileInfo(path);
            var newFolder = Path.Combine(fi.DirectoryName, subFolder);
            var di = new DirectoryInfo(newFolder);
            if(!di.Exists) {
                AssetDatabase.CreateFolder(path, subFolder);
            }
        }

        [SerializeField]
        [HideInInspector]
        private string materialGuid;

        public Material Material {
            get {
                var path = AssetDatabase.GUIDToAssetPath(materialGuid);
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }


    }

}

#endif
