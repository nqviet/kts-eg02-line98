using UnityEngine;
using Line98.Core;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Supplies the mesh and material needed to draw a translucent ghost of a ball that is about to
    /// move, so the destination indicator can show <em>which</em> ball lands there.
    /// Implemented by <see cref="BallViewManager"/>, which already owns the per-color materials.
    /// </summary>
    public interface IGhostBallSource
    {
        /// <summary>
        /// Resolves the ball currently at <paramref name="from"/>. Returns false when the cell is
        /// empty or the board has not been populated yet.
        /// </summary>
        bool TryGetGhost(GridPos from, out Mesh mesh, out Material material, out float restHeight);
    }
}
