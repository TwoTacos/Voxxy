using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxxy {
    public class VoxelModel {

        public VoxelModel(Coordinate size) {
            Size = size;
            voxels = new Voxel[size.x, size.y, size.z];
        }

        public Coordinate Size { get; private set; }

        private Voxel[,,] voxels;

        public Voxel this[Coordinate index] {
            get {
                if(Contains(index)) {
                    return voxels[index.x, index.y, index.z];
                }
                else {
                    return Voxel.outside;
                }
            }
            set {
                voxels[index.x, index.y, index.z] = value;
            }
        }

        public Voxel this[short x, short y, short z] {
            get {
                if(Contains(new Coordinate(x, y, z))) {
                    return voxels[x, y, z];
                }
                else {
                    return Voxel.outside;
                }
            }
            set {
                voxels[x, y, z] = value;
            }
        }

        public bool Contains(Coordinate coord) {
            return coord.x >= 0 && coord.y >= 0 && coord.z >= 0 && coord.x < Size.x && coord.y < Size.y && coord.z < Size.z;
        }

        public void Fill(Voxel value) {
            foreach(var coord in Coordinate.Solid(Coordinate.zero, Size)) {
                this[coord] = value;
            }
        }

        public void Flood(Voxel fromVoxel, Voxel toVoxel) {
            var start = Coordinate.zero;
            Coordinate end = Size;
            // Mark as empty items on the periphery that aren't colored.
            foreach(var coord in Coordinate.Shell(start, end)) {
                if(this[coord] == fromVoxel) {
                    this[coord] = toVoxel;
                }
            }
            // Progressively move inward marking NoInfomration as Empty if they have an empty neighbor.
            do {
                start = start + Coordinate.one;
                end = end - Coordinate.one;
                foreach(var coord in Coordinate.Shell(start, end)) {
                    if(this[coord] == fromVoxel) {
                        foreach(var neighbor in Coordinate.VonNeumanNeighborhood(coord)) {
                            if(this[neighbor] == toVoxel) {
                                this[coord] = toVoxel;
                            }
                        }
                    }
                }
            } while(start.x < end.x && start.y < end.y && start.z < end.z);
        }

        internal void Replace(Voxel fromVoxel, Voxel toVoxel) {
            foreach(var coord in Coordinate.Solid(Coordinate.zero, Size)) {
                if(this[coord] == fromVoxel) {
                    this[coord] = toVoxel;
                }
            }
        }
    }
}
