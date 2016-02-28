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

        public string dummy;

        public DateTime filedate; // TODO: Use this to automatically reload changed model.

        public string filedateString; // TODO: Remove.

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
                AddFaceIfClear(model, coord, Coordinate.forward);
                AddFaceIfClear(model, coord, Coordinate.left);
                AddFaceIfClear(model, coord, Coordinate.right);
                AddFaceIfClear(model, coord, Coordinate.up);
                AddFaceIfClear(model, coord, Coordinate.down);
            }

            for(int z = 0; z < vox.Size.z; ++z) {
                var min = new Coordinate(0, 0, z);
                var max = new Coordinate(vox.Size.x, vox.Size.y, z);
                AddFaces(model, min, max, Coordinate.back);
            }

            Mesh mesh = new Mesh();
            mesh.name = "Vox Model " + filename;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        }

        List<Vector3> vertices;
        List<Vector2> uvs;
        List<int> triangles;


        private void AddFaces(VoxelModel model, Coordinate from, Coordinate to, Coordinate occludingCoord) {
            // Copy plane slice out of model.
            Voxel[,] plane = new Voxel[to.x, to.y];
            for(short x = from.x; x < to.x; ++x) {
                for(short y = from.y; y < to.y; ++y) {
                    var coord = new Coordinate(x, y, to.z);
                    var voxel = model[coord];
                    var occluding = model[coord + occludingCoord];
                    if(voxel.type == VoxelType.Visible && occluding.type == VoxelType.Visible) {
                        plane[x, y] = Voxel.occluded;
                    }
                    else {
                        plane[x, y] = voxel;
                    }
                }
            }

            var angle = Quaternion.LookRotation(occludingCoord);

            for(short x = from.x; x < to.x; ++x) {
                for(short y = from.y; y < to.y; ++y) {
                    if(plane[x, y].type == VoxelType.Visible) { 
                        var face = new VoxelFace(plane, to);
                        var coord = new Coordinate(x, y, from.z);
                        face.Create(coord);

                        while(face.Extend()) {
                        }

                        var index = vertices.Count;

                        var vertex0 = angle * new Vector3(0.5f, 0.5f, 0.5f);
                        var vertex1 = angle * new Vector3(-0.5f, 0.5f, 0.5f);
                        var vertex2 = angle * new Vector3(-0.5f, -0.5f, 0.5f);
                        var vertex3 = angle * new Vector3(0.5f, -0.5f, 0.5f);

                        vertex0.Scale(face.Bounds.size);
                        vertex1.Scale(face.Bounds.size);
                        vertex2.Scale(face.Bounds.size);
                        vertex3.Scale(face.Bounds.size);

                        vertices.Add(face.Bounds.center + vertex0);
                        vertices.Add(face.Bounds.center + vertex1);
                        vertices.Add(face.Bounds.center + vertex2);
                        vertices.Add(face.Bounds.center + vertex3);

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
            return voxel.type == VoxelType.Visible && !(occludingVoxel.type == VoxelType.Visible);
        }

        private bool IsOccludedFace(VoxelModel model, Coordinate coord, Coordinate occludingCoord) {
            var voxel = model[coord];
            var occludingVoxel = model[coord + occludingCoord];
            return voxel.IsSolid && occludingVoxel.IsSolid;
        }

    }
}
