using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelMesh : MonoBehaviour {

        [Tooltip("The VOX file (expored from Magica Voxel or similar) that will be imported into a Unity3d friendly mesh.")]
        public DefaultAsset voxFile;

        [Tooltip("The center of the model as a propotion of each side.  Values below 0 and above 1 can be used to move the center outside of the model's volume.  The size of the model is determined by the VOX file and not by the area filled by voxels.")]
        public Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);

        // TODO: Change this to option of voxels/meter or meters/voxel.
        [Tooltip("The number of unity units (i.e. meters) that each voxel will occupy.")]
        public float voxelSize = 1.0f;

        [Tooltip("The percent of occluded voxels allowed on any given face.  0% will only render visible surface decreasing GPU overdraw.  Use 100% to extend faces into the model to decrease triangles.  Default of 40% is good for most situations.")]
        [Range(0, 100)]
        public int maximumOcclusionPercent = 40;

        [HideInInspector]
        [SerializeField]
        private Vector3 lastCenter;

        [HideInInspector]
        [SerializeField]
        private float lastVoxelSize;

        [HideInInspector]
        [SerializeField]
        private int lastMaxPercent;

        [HideInInspector]
        public DateTime filedate; 

        [HideInInspector]
        public Texture2D atlas;

        private VoxFile vox;

        /// <summary>
        /// This method, along with the ExecuteInEditMode class attribute re-imports the model when Unity starts up or the model is enabled.
        /// This ensures that any changes to the VOX file when Unity was not running is automatically reflected in the model.
        /// </summary>
        internal void OnEnable() {
            ReimportVox();
        }

        /// <summary>
        /// This method, along with the ExecuteInEditMode class attribute re-imports the model whenever any of the import attributes above is changed.
        /// </summary>
        internal void Update() {
            ReimportVox();
        }

        [ContextMenu("Reimport")]
        public void ReimportVox() {
            if(voxFile == null) {
                ClearModel();
                return;
            }

            var filepath = AssetDatabase.GetAssetPath(voxFile);
            var file = new FileInfo(filepath);
            
            if(filedate != file.LastWriteTimeUtc || center != lastCenter || voxelSize != lastVoxelSize || maximumOcclusionPercent != lastMaxPercent) {
                filedate = file.LastWriteTimeUtc;
                lastCenter = center;
                lastVoxelSize = voxelSize;
                lastMaxPercent = maximumOcclusionPercent;

                vox = new VoxFile();
                vox.Open(filepath);
                ConstructMesh();
            }
        }


        [ContextMenu("Clear Material")]
        public void ClearModel() {
            gameObject.GetComponent<MeshFilter>().sharedMesh = null;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", null);
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

            meshBuilder = new MeshBuilder(voxFile.name + " VOX Model");

            for(int x = 0; x < vox.Size.x; ++x) {
                var min = new Coordinate(x, 0, 0);
                var max = new Coordinate(x + 1, vox.Size.y, vox.Size.z);
                AddFaces(model, min, max, Coordinate.right);
                AddFaces(model, min, max, Coordinate.left);
            }
            for(int y = 0; y < vox.Size.y; ++y) {
                var min = new Coordinate(0, y, 0);
                var max = new Coordinate(vox.Size.x, y + 1, vox.Size.z);
                AddFaces(model, min, max, Coordinate.up);
                AddFaces(model, min, max, Coordinate.down);
            }
            for(int z = 0; z < vox.Size.z; ++z) {
                var min = new Coordinate(0, 0, z);
                var max = new Coordinate(vox.Size.x, vox.Size.y, z + 1);
                AddFaces(model, min, max, Coordinate.back);
                AddFaces(model, min, max, Coordinate.forward);
            }

            UpdateMaterialAndTexture();

            Debug.Log(String.Format("Voxxy constructed mesh for VOX model {0} with {1} vertices and {2} triangles. ", voxFile.name, meshBuilder.VertexCount, meshBuilder.TriangleCount));
        }

        private void UpdateMaterialAndTexture() {
            gameObject.GetComponent<MeshFilter>().sharedMesh = meshBuilder.Mesh;
            atlas = meshBuilder.Atlas;
            var renderer = gameObject.GetComponent<MeshRenderer>();
            var materialName = voxFile.name + " Material";
            if(materialName != renderer.sharedMaterial.name) {
                var material = new Material(Shader.Find("Standard"));
                material.name = materialName;
                renderer.sharedMaterial = material;
            }
            renderer.sharedMaterial.SetTexture("_MainTex", atlas);
        }

        private MeshBuilder meshBuilder;


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

            var centerOffset = new Vector3(-(vox.Size.x - 1) * center.x, -(vox.Size.y - 1) * center.y, -(vox.Size.z - 1) * center.z);

            for(var x = from.x; x < to.x; ++x) {
                for(var y = from.y; y < to.y; ++y) {
                    for(var z = from.z; z < to.z; ++z) {
                        var planeX = from.x == to.x - 1 ? z : x;
                        var planeY = from.y == to.y - 1 ? z : y;
                        if(plane[planeX, planeY].type == VoxelType.Visible) {
                            var face = new VoxelFace(plane, planeExtent, maximumOcclusionPercent / 100f);
                            face.Create(new Coordinate(planeX, planeY));

                            while(face.Extend()) {
                            }

                            var xOffset = 0.5f * face.Bounds.size.x;
                            var yOffset = 0.5f * face.Bounds.size.y;
                            var zOffset = 0.5f * face.Bounds.size.z;
                            var vertex0 = angle * new Vector3( xOffset,  yOffset, zOffset);
                            var vertex1 = angle * new Vector3(-xOffset,  yOffset, zOffset);
                            var vertex2 = angle * new Vector3(-xOffset, -yOffset, zOffset);
                            var vertex3 = angle * new Vector3( xOffset, -yOffset, zOffset);

                            var faceCenter = new Vector3(face.Bounds.center.x, face.Bounds.center.y, z);
                            if(from.x == to.x - 1) {
                                faceCenter = new Vector3(x, face.Bounds.center.y, face.Bounds.center.x);
                            }
                            if(from.y == to.y - 1) {
                                faceCenter = new Vector3(face.Bounds.center.x, y, face.Bounds.center.y);
                            }
                            
                            vertex0 = voxelSize * (centerOffset + faceCenter + vertex0);
                            vertex1 = voxelSize * (centerOffset + faceCenter + vertex1);
                            vertex2 = voxelSize * (centerOffset + faceCenter + vertex2);
                            vertex3 = voxelSize * (centerOffset + faceCenter + vertex3);

                            var texture = face.GetTexture();
                            meshBuilder.AddQuad(vertex0, vertex1, vertex2, vertex3, texture);

                            face.ClearPlane();
                        }
                    }
                }
            }
        }

    }
}
