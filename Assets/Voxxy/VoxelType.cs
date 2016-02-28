using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxxy {

    /// <summary>
    /// The type of the voxel, which along with its color indicates how it is to be rendered.
    /// </summary>
    public enum VoxelType {
        /// <summary>
        /// The type of the voxel is not known, typically an initialization state.
        /// </summary>
        Unknown,

        /// <summary>
        /// The voxel contains nothing, consider it air or a void space.
        /// </summary>
        Empty,

        /// <summary>
        /// The voxel exists and is visible from some vantage point around the model.
        /// </summary>
        Visible,

        /// <summary>
        /// The voxel exists, but is occluded from view, such as most interior voxels.
        /// An occluded voxel may still have a color.
        /// </summary>
        Occluded,

        /// <summary>
        /// The voxel is empty as it is outside of the models bounds. 
        /// Used to simplify logic when requesting voxels outside of a model's bounds.
        /// </summary>
        Outside,

    }
}
