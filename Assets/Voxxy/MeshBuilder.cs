using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Voxxy {

    /// <summary>
    /// A simple quad based mesh builder.  
    /// Used to collect a number of quads along with their textures and to then combine them into a mesh with a unified texture atlas.
    /// </summary>
    public class MeshBuilder {

        public MeshBuilder(string name) {
            Name = name;
            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            triangles = new List<int>();
            textures = new List<Texture2D>();
        }

        public void AddQuad(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft, Texture2D texture) {
            mesh = null;
            atlas = null;

            int index = vertices.Count;

            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomRight);
            vertices.Add(bottomLeft);

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);

            textures.Add(texture);
        }

        public string Name { get; private set; }

        public Mesh Mesh {
            get {
                if(mesh == null) {
                    CalculateMesh();
                }
                return mesh;
            }
        }
        private Mesh mesh = null;

        public Texture2D Atlas {
            get {
                if(mesh == null) {
                    CalculateMesh();
                }
                return atlas;
            }
        }
        private Texture2D atlas = null;

        private void CalculateMesh() {
            CalculateUvs();
            mesh = new Mesh();
            mesh.name = Name;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
        }

        private void CalculateUvs() {
            uvs.Clear();

            var size = 256; // TODO: should this grow dynamically?
            atlas = new Texture2D(size, size, TextureFormat.DXT1, false);
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Point;

            var rects = atlas.PackTextures(textures.ToArray(), 1, size);
            for(int i = 0; i < textures.Count; ++i) {
                var texture = textures[i];
                var rect = rects[i];
                var epsilon = 0.0005f; // slight adjustment to avoid the exact border of the texture.
                if(vertices[4 * i].y == vertices[4 * i + 1].y && vertices[4 * i].y == vertices[4 * i + 2].y) {
                    // top & bottom need to rotate 180
                    uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMin + epsilon));
                    uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMin + epsilon));
                    uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMax - epsilon));
                    uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMax - epsilon));
                }
                else {
                    uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMax - epsilon));
                    uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMax - epsilon));
                    uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMin + epsilon));
                    uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMin + epsilon));
                }
            }

            //uvs.Add(new Vector2(0, 1));
            //uvs.Add(new Vector2(1, 1));
            //uvs.Add(new Vector2(1, 0));
            //uvs.Add(new Vector2(0, 0));
        }

        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<int> triangles;
        private List<Texture2D> textures;

    }
}
