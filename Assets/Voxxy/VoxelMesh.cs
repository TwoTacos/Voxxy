using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Voxxy {

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelMesh : MonoBehaviour {

        public string filename;

        public DateTime filedate; // TODO: Use this to automatically reload changed model.

        public string filedateString; // TODO: Remove.

        [Tooltip("The percent of occluded voxels allowed on any given face.  Use 0% to only render visible surface decreasing GPU overdraw.  Use 100% to extend faces as far as necessary to decrease triangles.")]
        [Range(0, 100)]
        public int maximumOcclusionPercent = 50;

        public GameObject CubePrefab;

        private VoxFile vox;

        [ContextMenu("Reimport VOX")]
        public void ImportVox() {
            vox = new VoxFile();
            var file = new FileInfo(filename);
            filedate = file.LastWriteTimeUtc;
            filedateString = filedate.ToString();
            vox.Open(filename);
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
            Debug.Log(String.Format("Created {0} voxels from {1}.", count, filename));
        }

        [ContextMenu("Construct Mesh")]
        public void ConstructMesh() {
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

            foreach(var coord in Coordinate.Solid(Coordinate.zero, vox.Size)) {
                //AddFaceIfClear(model, coord, Coordinate.back);
                //AddFaceIfClear(model, coord, Coordinate.forward);
                //AddFaceIfClear(model, coord, Coordinate.left);
                //AddFaceIfClear(model, coord, Coordinate.right);
                //AddFaceIfClear(model, coord, Coordinate.up);
                //AddFaceIfClear(model, coord, Coordinate.down);
            }

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
            mesh.name = "Vox Model " + filename;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            Debug.Log("Constructed mesh for vox model: " + filename);
        }

        List<Vector3> vertices;
        List<Vector2> uvs;
        List<int> triangles;

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

                            var center = new Vector3(face.Bounds.center.x, face.Bounds.center.y, z);
                            if(from.x == to.x - 1) {
                                center = new Vector3(x, face.Bounds.center.y, face.Bounds.center.x);
                            }
                            if(from.y == to.y - 1) {
                                center = new Vector3(face.Bounds.center.x, y, face.Bounds.center.y);
                            }
                            
                            vertices.Add(center + vertex0);
                            vertices.Add(center + vertex1);
                            vertices.Add(center + vertex2);
                            vertices.Add(center + vertex3);

                            uvs.Add(new Vector2(0, 0));
                            uvs.Add(new Vector2(1, 0));
                            uvs.Add(new Vector2(1, 1));
                            uvs.Add(new Vector2(0, 1));

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
        /// <param name="occludingCoord">The unit coordinate that indicates the direction, no validation is done, take care.</param>
        private void AddFaceIfClear(VoxelModel model, Coordinate coord, Coordinate occludingCoord) {
            if(IsFace(model, coord, occludingCoord)) {

                var index = vertices.Count;

                var angle = Quaternion.LookRotation(occludingCoord);
               
                vertices.Add((Vector3)coord + angle * new Vector3( 0.5f,  0.5f, 0.5f)); // 0
                vertices.Add((Vector3)coord + angle * new Vector3(-0.5f,  0.5f, 0.5f)); // 1
                vertices.Add((Vector3)coord + angle * new Vector3(-0.5f, -0.5f, 0.5f)); // 2
                vertices.Add((Vector3)coord + angle * new Vector3( 0.5f, -0.5f, 0.5f)); // 3

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }
        }

        private bool IsFace(VoxelModel model, Coordinate coord, Coordinate occludingCoord) {
            var voxel = model[coord];
            var occludingVoxel = model[coord + occludingCoord];
            return voxel.type == VoxelType.Visible && !occludingVoxel.IsSolid;
        }

        private bool IsOccludedFace(VoxelModel model, Coordinate coord, Coordinate occludingCoord) {
            var voxel = model[coord];
            var occludingVoxel = model[coord + occludingCoord];
            return voxel.IsSolid && occludingVoxel.IsSolid;
        }

    }
}
