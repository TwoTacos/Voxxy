using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Voxxy {
    public class MeshBuilder {

        public MeshBuilder() {
            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            triangles = new List<int>();
        }

        public void AddQuad(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft) {
            int index = vertices.Count;

            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomRight);
            vertices.Add(bottomLeft);

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
        }

        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<int> triangles;

        public Mesh ToMesh(string name) {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
