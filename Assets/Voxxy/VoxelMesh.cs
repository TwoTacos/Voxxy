﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Voxxy {

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelMesh : MonoBehaviour {

        [Tooltip("The VOX file (expored from Magica Voxel or similar) that will be imported into a Unity3d friendly mesh.")]
        public DefaultAsset voxFile;

        [Tooltip("The center of the model as a propotion of each side.  Values below 0 and above 1 can be used to move the center outside of the model's volume.  The size of the model is determined by the VOX file and not by the area filled by voxels.")]
        public Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("The number of unity units (i.e. meters) that each voxel will occupy.")]
        public float voxelSize = 1.0f;

        public DateTime filedate; // TODO: Use this to automatically reload changed model.

        public string filedateString; // TODO: Remove.

        [Tooltip("The percent of occluded voxels allowed on any given face.  0% will only render visible surface decreasing GPU overdraw.  Use 100% to extend faces into the model to decrease triangles.  Default of 40% is good for most situations.")]
        [Range(0, 100)]
        public int maximumOcclusionPercent = 40;

        public GameObject CubePrefab;

        private VoxFile vox;

        public void ImportVox() {
            Debug.Log("Name: " + voxFile.name);
            Debug.Log("ToString: " + voxFile.ToString());
            Debug.Log("pathToVox: " + AssetDatabase.GetAssetPath(voxFile));
            var filepath = AssetDatabase.GetAssetPath(voxFile);
            vox = new VoxFile();
            var file = new FileInfo(filepath);
            filedate = file.LastWriteTimeUtc;
            filedateString = filedate.ToString();
            vox.Open(filepath);
        }

        [ContextMenu("Construct Cubes")]
        public void ConstructCubes() {
            var model = new VoxelModel(vox.Size);

            model.Fill(Voxel.unknown);
            // Copy model into volume
            foreach(var voxel in vox.Voxels) {
                model[(Coordinate)voxel.Key] = new Voxel(VoxelType.Visible, voxel.Value);
            }
            model.Flood(Voxel.unknown, Voxel.empty);
            model.Replace(Voxel.unknown, Voxel.occluded);
            // Finally, render them.
            int count = 0;
            foreach(var coord in Coordinate.Solid(Coordinate.zero, vox.Size)) {
                var voxel = model[coord];
                if(voxel != Voxel.empty && voxel != Voxel.occluded) {
                    ++count; 
                    var go = Instantiate(CubePrefab, coord, Quaternion.identity) as GameObject;
                    go.transform.SetParent(transform);
                }
                if(count > 1000) {
                    break;
                }
            }
            Debug.Log(String.Format("Created {0} voxels from {1}.", count, voxFile.name));
        }

        [ContextMenu("Construct Mesh")]
        public void ConstructMesh() {
            ImportVox();

            var model = new VoxelModel(vox.Size);

            model.Fill(Voxel.unknown);
            // Copy model into volume
            foreach(var voxel in vox.Voxels) {
                model[(Coordinate)voxel.Key] = new Voxel(VoxelType.Visible, voxel.Value);
            }
            model.Flood(Voxel.unknown, Voxel.empty);
            model.Replace(Voxel.unknown, Voxel.occluded);

            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            triangles = new List<int>();

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

            Mesh mesh = new Mesh();
            mesh.name = voxFile.name + "VOX Model";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            Debug.Log("Constructed mesh for vox model: " + voxFile.name);
        }

        List<Vector3> vertices;
        List<Vector2> uvs;
        List<int> triangles;

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

                            var index = vertices.Count;

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
                            
                            vertices.Add(voxelSize * (centerOffset + faceCenter + vertex0));
                            vertices.Add(voxelSize * (centerOffset + faceCenter + vertex1));
                            vertices.Add(voxelSize * (centerOffset + faceCenter + vertex2));
                            vertices.Add(voxelSize * (centerOffset + faceCenter + vertex3));

                            uvs.Add(new Vector2(0, 1));
                            uvs.Add(new Vector2(1, 1));
                            uvs.Add(new Vector2(1, 0));
                            uvs.Add(new Vector2(0, 0));

                            triangles.Add(index);
                            triangles.Add(index + 1);
                            triangles.Add(index + 2);
                            triangles.Add(index);
                            triangles.Add(index + 2);
                            triangles.Add(index + 3);

                            face.ClearPlane();
                        }
                    }
                }
            }
        }

    }
}
