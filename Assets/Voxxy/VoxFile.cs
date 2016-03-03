using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Voxxy {

    /// <summary>
    /// Represents a VOX file, typically exported from Magica Voxel.
    /// </summary>
    public class VoxFile {

        public VoxFile() {
            Voxels = new Dictionary<Vector3, int>();
            Palette = new Color[256];
            LoadDefaultPalette();
        }

        public Vector3 Size { get; private set; }

        /// <summary>
        /// The collection of voxels in the file.  
        /// This is collection of unique Vector3 indicating the location along with their associated color index.
        /// Use the color index as an offset into the Palette to determine the final color.
        /// </summary>
        public Dictionary<Vector3, int> Voxels { get; private set; }

        /// <summary>
        /// The collection of 256 colors which is the palette for this model.
        /// A VOX file has a limited palette.
        /// </summary>
        public Color[] Palette { get; private set; }

        public int Version { get; private set; }

        public void Open(string path) {
            byte[] bytes = File.ReadAllBytes(path);
            using(MemoryStream stream = new MemoryStream(bytes)) {
                using(BinaryReader reader = new BinaryReader(stream)) {
                    var header = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if(header != "VOX ") {
                        throw new FormatException("Invalid VOX file, 'VOX' header not found.");
                    }
                    Version = reader.ReadInt32();
                    ReadMainChunk(reader);
                }
            }
        }

        private void ReadMainChunk(BinaryReader reader) {
            var header = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if(header != "MAIN") {
                throw new FormatException("Invalid VOX file, 'MAIN' chunk not found.");
            }
            int chunkSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();
            reader.ReadBytes(chunkSize); // discard as nothing but children should be present.
            while(reader.BaseStream.Position < 12 + chunkSize + childrenSize) {
                var childType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if(childType == "SIZE") {
                    ReadSizeChunk(reader);
                }
                else if(childType == "XYZI") {
                    ReadVoxelChunk(reader);
                }
                else if(childType  == "RGBA") {
                    ReadPalleteChunk(reader);
                }
                else {
                    ReadUnknownChunk(reader);
                }
            }
        }

        private void ReadSizeChunk(BinaryReader reader) {
            int chunkSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();

            var x = reader.ReadInt32();
            var z = reader.ReadInt32(); // invert z & y dimensions as Unity has Y up and Magica is Z up.
            var y = reader.ReadInt32();
            Size = new Vector3(x, y, z); 
            if(chunkSize < 12) {
                Debug.Log("Possible file corruption, Size chunk is larger than the expected.");
                reader.ReadBytes(chunkSize - 12);
            }

            if(childrenSize > 0) {
                Debug.Log("Possible file corruption, Size chunk should not have children.");
                reader.ReadBytes(childrenSize);
            }
        }


        private void ReadUnknownChunk(BinaryReader reader) {
            // Note: not a problem as older VOX files have chunks not defined here.
            int chunkSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();
            reader.ReadBytes(chunkSize + childrenSize);
        }

        private void ReadPalleteChunk(BinaryReader reader) {
            int chunkSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();

            for(int i = 0; i < 256; ++i) {
                var r = reader.ReadByte() / 255.0f;
                var g = reader.ReadByte() / 255.0f;
                var b = reader.ReadByte() / 255.0f;
                var a = reader.ReadByte() / 255.0f;
                if(i < 255) {
                    Palette[i + 1] = new Color(r, g, b, a);
                }
            }
            if(chunkSize < 1024) {
                Debug.Log("Possible file corruption, Palette chunk is larger than the expected contents.");
                reader.ReadBytes(chunkSize - 1024);
            }
            if(childrenSize > 0) {
                Debug.Log("Possible file corruption, Palette chunk should not have children.");
                reader.ReadBytes(childrenSize);
            }
        }

        private void ReadVoxelChunk(BinaryReader reader) {
            int chunkSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();
            int numVoxels = reader.ReadInt32();

            for(int i = 0; i < numVoxels; ++i) {
                var x = (float)(int)reader.ReadByte();
                var z = (float)(int)reader.ReadByte();
                var y = (float)(int)reader.ReadByte();
                var color = (int)reader.ReadByte();
                Voxels.Add(new Vector3(x, y, z), color);
            }
            if(chunkSize < 4 * numVoxels) {
                Debug.Log("Possible file corruption, Voxel chunk is larger than the voxel contents.");
                reader.ReadBytes(chunkSize - 4 * numVoxels);
            }
            if(childrenSize > 0) {
                Debug.Log("Possible file corruption, Voxel chunk should not have children.");
                reader.ReadBytes(childrenSize);
            }
        }

        /// <summary>
        /// VOX files are allowed to not have a palette and then the default pallete is assumed.
        /// </summary>
        /// <remarks>
        /// Colors from http://voxel.codeplex.com/SourceControl/latest#MV Importer/MV Importer/mv_vox.h
        /// </remarks>
        private void LoadDefaultPalette() {
            int i = 0;
            for(var r = 1.0f; r >= 0.0f; r -= 0.2f) {
                for(var g = 1.0f; g >= 0.0f; g -= 0.2f) {
                    for(var b = 1.0f; b >= 0.0f; b -= 0.2f) {
                        Palette[i++] = new Color(r, g, b);
                    }
                }
            }
            --i;  // The last was black and we don't include it yet.
            float[] wackyScale = { 0.933333f, 0.866667f, 0.733333f, 0.666667f, 0.533333f, 0.466667f, 0.333333f, 0.266667f, 0.133333f, 0.066667f };
            foreach(var r in wackyScale) {
                Palette[i++] = new Color(r, 0, 0);
            }
            foreach(var g in wackyScale) {
                Palette[i++] = new Color(0, g, 0);
            }
            foreach(var b in wackyScale) {
                Palette[i++] = new Color(0, 0, b);
            }
            foreach(var w in wackyScale) {
                Palette[i++] = new Color(w, w, w);
            }
            Palette[i] = new Color(0, 0, 0);
        }

    }

}
