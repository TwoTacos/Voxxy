using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Voxxy {

    /// <summary>
    /// Represents a single face of the voxel when translated into mesh space.
    /// </summary>
    public class VoxelFace {

        /// <summary>
        /// Create a new VoxelSpace where the lower-left voxel is in this plane at position (x, y).
        /// </summary>
        public VoxelFace(Voxel[,] plane, Coordinate max) {
            this.plane = plane;
            OcclusionPercent = 0.0f;
            Max = max;
        }

        public bool Create(Coordinate start) {
            Start = start;
            End = Start + Coordinate.one;
            return plane[Start.x, Start.y].type == VoxelType.Visible;
        }

        private Voxel[,] plane;

        private Coordinate Max { get; set; }

        public Coordinate Start { get; private set; }

        public Coordinate End { get; private set; }

        /// <summary>
        /// The percentage of the face mesh that is completely occluded by the model itself.
        /// This is the key performance increase as we are trading off pixel overdraws against fewer triangles.
        /// </summary>
        public float OcclusionPercent { get; private set; }

        public Bounds Bounds {
            get {
                return Coordinate.Aabb(Start, End - Coordinate.one);
            }
        }

        public bool Extend() {
            return ExtendLeft() | ExtendRight() | ExtendUp() | ExtendDown();
        }

        private bool ExtendLeft() {
            if(Start.x - 1 < 0) {
                return false; // no where left to go.
            }
            int solidCount = 0;
            //int occludedCount;
            var x = Start.x - 1;
            for(int y = Start.y; y < End.y; ++y) {
                var voxel = plane[x, y];
                if(voxel.IsSolid) {
                    ++solidCount;
                }
                else {
                    return false; // can't extend over non-solid voxel.
                }
            }
            Start += Coordinate.left;
            return true;
        }

        private bool ExtendRight() {
            if(End.x + 1 > Max.x) {
                return false; // no where left to go.
            }
            int solidCount = 0;
            //int occludedCount;
            var x = End.x;
            for(int y = Start.y; y < End.y; ++y) {
                var voxel = plane[x, y];
                if(voxel.IsSolid) {
                    ++solidCount;
                }
                else {
                    return false; // can't extend over non-solid voxel.
                }
            }
            End += Coordinate.right;
            return true;
        }

        private bool ExtendUp() {
            if(End.y + 1 > Max.y) {
                return false; // no where left to go.
            }
            int solidCount = 0;
            //int occludedCount;
            var y = End.y;
            for(int x = Start.x; x < End.x; ++x) {
                var voxel = plane[x, y];
                if(voxel.IsSolid) {
                    ++solidCount;
                }
                else {
                    return false; // can't extend over non-solid voxel.
                }
            }
            End += Coordinate.up;
            return true;
        }

        private bool ExtendDown() {
            if(Start.y - 1 < 0) {
                return false; // no where left to go.
            }
            int solidCount = 0;
            //int occludedCount;
            var y = Start.y - 1;
            for(int x = Start.x; x < End.x; ++x) {
                var voxel = plane[x, y];
                if(voxel.IsSolid) {
                    ++solidCount;
                }
                else {
                    return false; // can't extend over non-solid voxel.
                }
            }
            Start += Coordinate.down;
            return true;
        }

        public void ClearPlane() {
            for(int x = Start.x; x < End.x; ++x) {
                for(int y = Start.y; y < End.y; ++y) {
                    plane[x, y] = Voxel.empty;
                }
            }
        }
    }
}
