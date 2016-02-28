using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxxy {
    public struct Voxel {

        public Voxel(VoxelType type, int color = 0) {
            this.type = type;
            this.color = color;
        }

        public int color;

        public VoxelType type;

        /// <summary>
        /// A solid voxel is one that is visible or occluded.
        /// </summary>
        public bool IsSolid {
            get {
                return type == VoxelType.Visible || type == VoxelType.Occluded;
            }
        }

        public readonly static Voxel unknown = new Voxel(VoxelType.Unknown);

        public readonly static Voxel empty = new Voxel(VoxelType.Empty);

        public readonly static Voxel outside = new Voxel(VoxelType.Outside);

        public readonly static Voxel occluded = new Voxel(VoxelType.Occluded);

        /// <summary>
        /// A generic, visible voxel with no color information.
        /// </summary>
        public readonly static Voxel visible = new Voxel(VoxelType.Visible);

        public override bool Equals(object obj) {
            if(obj is Voxel) {
                return this == (Voxel)obj;
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() {
            return type.GetHashCode() ^ color.GetHashCode();
        }

        public static bool operator==(Voxel lhs, Voxel rhs) {
            return lhs.type == rhs.type && lhs.color == rhs.color;
        }

        public static bool operator!=(Voxel lhs, Voxel rhs) {
            return lhs.type != rhs.type && lhs.color != rhs.color;
        }

        public override string ToString() {
            return String.Format("V({0}: {1})", type, color);
        }

    }
}
