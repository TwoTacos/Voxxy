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
            textureIndexes = new List<int>();
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

            var existingTextureIndex = MatchExistingTexture(texture);
            if(existingTextureIndex >= 0) {
                textureIndexes.Add(existingTextureIndex);
            }
            else {
                textures.Add(texture);
                textureIndexes.Add(textures.Count - 1);
            }
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

        public int VertexCount {
            get {
                return vertices.Count;
            }
        }

        public int TriangleCount {
            get {
                return triangles.Count / 3;
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

            atlas = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Point;

            var rects = atlas.PackTextures(textures.ToArray(), 2);
            for(int i = 0; i < vertices.Count / 4; ++i) {
                var textureIndex = textureIndexes[i];
                var rect = rects[textureIndex];
                if(IsQuadHorizontal(i)) {
                    // top & bottom need to rotate 180
                    uvs.Add(new Vector2(rect.xMax, rect.yMin));
                    uvs.Add(new Vector2(rect.xMin, rect.yMin));
                    uvs.Add(new Vector2(rect.xMin, rect.yMax));
                    uvs.Add(new Vector2(rect.xMax, rect.yMax));
                }
                else {
                    uvs.Add(new Vector2(rect.xMin, rect.yMax));
                    uvs.Add(new Vector2(rect.xMax, rect.yMax));
                    uvs.Add(new Vector2(rect.xMax, rect.yMin));
                    uvs.Add(new Vector2(rect.xMin, rect.yMin));
                }
            }
            //for(int i = 0; i < textures.Count; ++i) {
            //    var rect = rects[i];
            //    var epsilon = 0f; // slight adjustment to avoid the exact border of the texture.
            //    if(IsQuadHorizontal(i)) {
            //        // top & bottom need to rotate 180
            //        uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMin + epsilon));
            //        uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMin + epsilon));
            //        uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMax - epsilon));
            //        uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMax - epsilon));
            //    }
            //    else {
            //        uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMax - epsilon));
            //        uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMax - epsilon));
            //        uvs.Add(new Vector2(rect.xMax - epsilon, rect.yMin + epsilon));
            //        uvs.Add(new Vector2(rect.xMin + epsilon, rect.yMin + epsilon));
            //    }
            //}
            // Inefficient, but seldom called...
            int[] xSearch = { 1, -1, 0, 0, 1, -1, 1, -1 };
            int[] ySearch = { 0, 0, 1, -1, 1, 1, -1, -1 };
            var pixels = atlas.GetPixels();
            var transparent = new Color(0, 0, 0, 0);
            for(int x = 0; x < atlas.width; ++x) {
                for(int y = 0; y < atlas.height; ++y) {
                    var pixel = GetPixel(pixels, atlas.width, atlas.height, x, y);
                    if(pixel == transparent) {
                        for(int n = 0; n < xSearch.Length; ++n) {
                            var neighbor = GetPixel(pixels, atlas.width, atlas.height, x + xSearch[n], y + ySearch[n]);
                            if(neighbor != transparent) {
                                atlas.SetPixel(x, y, neighbor);
                                break;
                            }
                        }
                    }
                }
            }
            atlas.Apply();

        }

        private bool IsQuadHorizontal(int i) {
            var y1 = vertices[4 * i].y;
            var y2 = vertices[4 * i + 1].y;
            var y3 = vertices[4 * i + 2].y;
            var y4 = vertices[4 * i + 3].y;
            var delta = Mathf.Abs(y1 - y2) + Mathf.Abs(y1 - y3) + Mathf.Abs(y1 - y4);
            return delta < 0.01;
        }

        private static Color GetPixel(Color[] pixels, int width, int height, int x, int y) {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            return pixels[width * y + x];
        }

        private int MatchExistingTexture(Texture2D texture) {
            for(int i = 0; i < textures.Count; ++i) {
                if(TextureEquals(texture, textures[i])) {
                    return i;
                }
            }
            return -1;
        }

        private static bool TextureEquals(Texture2D lhs, Texture2D rhs) {
            if(lhs.width == rhs.width && lhs.height == rhs.height) {
                var lhp = lhs.GetPixels32();
                var rhp = rhs.GetPixels32();
                for(int i = 0; i < lhp.Length; ++i) {
                    if(!lhp[i].Equals(rhp[i])) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<int> triangles;
        private List<int> textureIndexes;
        private List<Texture2D> textures;

    }
}
