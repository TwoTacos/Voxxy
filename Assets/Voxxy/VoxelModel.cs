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

        /// <summary>
        /// Indicates if the indicate coordinate is within the bounds of this model.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public bool Contains(Coordinate coord) {
            return coord.x >= 0 && coord.y >= 0 && coord.z >= 0 && coord.x < Size.x && coord.y < Size.y && coord.z < Size.z;
        }

        /// <summary>
        /// Fills the entire model with the given voxel.
        /// </summary>
        public void Fill(Voxel value) {
            foreach(var coord in Coordinate.Solid(Coordinate.zero, Size)) {
                this[coord] = value;
            }
        }

        /// <summary>
        /// Flood fill the model changing all voxels that are connected of the source type and replacing with the target type.
        /// This starts at the start coordinate and extends until no more are found. 
        /// If the start coordinate does not match the source voxel, then no changes are made.
        /// </summary>
        public void Flood(Coordinate start, Voxel source, Voxel target) {
            var toVisit = new Queue<Coordinate>();
            toVisit.Enqueue(start);
            while(toVisit.Any()) {
                var coord = toVisit.Dequeue();
                var voxel = this[coord];
                if(voxel == source) {
                    this[coord] = target;
                    var neighbors = coord.VonNeumanNeighbors().Where(e => this.Contains(e));
                    foreach(var neighbor in neighbors) {
                        toVisit.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Replace the voxel of the specified type and color with another everywhere in the model.
        /// </summary>
        public void Replace(Voxel source, Voxel target) {
            foreach(var coord in Coordinate.Solid(Coordinate.zero, Size)) {
                if(this[coord] == source) {
                    this[coord] = target;
                }
            }
        }
    }
}
