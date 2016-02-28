﻿using System;
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

        private int SolidCount { get; set; }

        private int OccludedCount { get; set; }

        /// <summary>
        /// The percentage of the face mesh that is completely occluded by the model itself.
        /// This is the key performance increase as we are trading off pixel overdraws against fewer triangles.
        /// </summary>
        public float OcclusionPercent {
            get {
                return (float)OccludedCount / SolidCount;
            }
        }

        public Bounds Bounds {
            get {
                return Coordinate.Aabb(Start, End - Coordinate.one);
            }
        }

        public bool Extend() {
            return ExtendRight() | ExtendDown() | ExtendLeft() | ExtendUp();
        }

        private bool ExtendLeft() {
            if(Start.x - 1 < 0) {
                return false; // no where left to go.
            }
            var success = GenericExtend(Start.x - 1, Start.x, Start.y, End.y);
            if(success) {
                Start += Coordinate.left;
            }
            return success;
        }

        private bool GenericExtend(int startX, int endX, int startY, int endY) {
            int solidCount = 0;
            int occludedCount = 0;
            for(int x = startX; x < endX; ++x) {
                for(int y = startY; y < endY; ++y) {
                    var voxel = plane[x, y];
                    if(voxel.IsSolid) {
                        ++solidCount;
                        if(voxel.type == VoxelType.Occluded) {
                            ++occludedCount;
                        }
                    }
                    else {
                        return false; // can't extend over non-solid voxel.
                    }
                }
            }
            if(occludedCount == solidCount) {
                return false; // everything we would have encompassed was occluded.
            }
            var resultOcclusionPercent = (float)(OccludedCount + occludedCount) / (SolidCount + solidCount);
            if(resultOcclusionPercent > 0.5f) {
                return false; // too great a percentage of occluded children.
            }
            SolidCount += solidCount;
            OccludedCount += occludedCount;
            return true;
        }

        private bool ExtendRight() {
            if(End.x + 1 > Max.x) {
                return false; // no where left to go.
            }
            var success = GenericExtend(End.x, End.x + 1, Start.y, End.y);
            if(success) {
                End += Coordinate.right;
            }
            return success;
        }

        private bool ExtendUp() {
            if(End.y + 1 > Max.y) {
                return false; // no where left to go.
            }
            var success = GenericExtend(Start.x, End.x, End.y, End.y + 1);
            if(success) {
                End += Coordinate.up;
            }
            return success;
        }

        private bool ExtendDown() {
            if(Start.y - 1 < 0) {
                return false; // no where left to go.
            }
            var success = GenericExtend(Start.x, End.x, Start.y - 1, Start.y);
            if(success) {
                Start += Coordinate.down;
            }
            return success;
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
