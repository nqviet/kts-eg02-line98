using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Solves camera distance and board framing procedurally to fit the 9x9 board
    /// across portrait aspect ratios from 9:16 down to 9:22 without edge distortion.
    /// </summary>
    public static class BoardFitSolver
    {
        public static readonly Vector2 DefaultMinViewport = new Vector2(0.05f, 0.16f);
        public static readonly Vector2 DefaultMaxViewport = new Vector2(0.95f, 0.78f);

        /// <summary>
        /// Solves the required camera orthographic size so that all 4 corners of the board
        /// project within the target viewport safe rect under the specified pitch and yaw.
        /// </summary>
        public static float SolveOrthographicSize(
            Camera camera,
            float boardExtentX,
            float boardExtentZ,
            Vector3 center,
            float pitchAngle,
            float yawAngle,
            Vector2 minViewport,
            Vector2 maxViewport,
            float minOrthoSize = 1.0f,
            float maxOrthoSize = 40.0f)
        {
            if (camera == null) return 7.5f;

            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            float low = minOrthoSize;
            float high = maxOrthoSize;
            float optimalSize = high;

            Vector3 halfExtents = new Vector3(boardExtentX * 0.5f, 0f, boardExtentZ * 0.5f);
            Vector3 c0 = center + new Vector3(-halfExtents.x, 0f, -halfExtents.z);
            Vector3 c1 = center + new Vector3( halfExtents.x, 0f, -halfExtents.z);
            Vector3 c2 = center + new Vector3(-halfExtents.x, 0f,  halfExtents.z);
            Vector3 c3 = center + new Vector3( halfExtents.x, 0f,  halfExtents.z);

            Vector3 savedPos = camera.transform.position;
            Quaternion savedRot = camera.transform.rotation;
            bool savedOrtho = camera.orthographic;
            float savedOrthoSize = camera.orthographicSize;

            camera.orthographic = true;
            camera.transform.rotation = rotation;
            camera.transform.position = center + rotation * new Vector3(0f, 0f, -20.0f);

            // Fit size independently of the reserved rectangle's location. CameraRig applies
            // the matching screen-space translation after solving.
            Vector2 viewportHalfSize = (maxViewport - minViewport) * 0.5f;
            minViewport = Vector2.one * 0.5f - viewportHalfSize;
            maxViewport = Vector2.one * 0.5f + viewportHalfSize;

            // 24 binary search iterations yield sub-millimeter precision
            for (int i = 0; i < 24; i++)
            {
                float mid = (low + high) * 0.5f;
                camera.orthographicSize = mid;

                Vector3 vp0 = camera.WorldToViewportPoint(c0);
                Vector3 vp1 = camera.WorldToViewportPoint(c1);
                Vector3 vp2 = camera.WorldToViewportPoint(c2);
                Vector3 vp3 = camera.WorldToViewportPoint(c3);

                bool fitsX = vp0.x >= minViewport.x && vp0.x <= maxViewport.x &&
                             vp1.x >= minViewport.x && vp1.x <= maxViewport.x &&
                             vp2.x >= minViewport.x && vp2.x <= maxViewport.x &&
                             vp3.x >= minViewport.x && vp3.x <= maxViewport.x;

                bool fitsY = vp0.y >= minViewport.y && vp0.y <= maxViewport.y &&
                             vp1.y >= minViewport.y && vp1.y <= maxViewport.y &&
                             vp2.y >= minViewport.y && vp2.y <= maxViewport.y &&
                             vp3.y >= minViewport.y && vp3.y <= maxViewport.y;

                if (fitsX && fitsY)
                {
                    optimalSize = mid;
                    // Try to zoom in more to fill more of the frame
                    high = mid;
                }
                else
                {
                    // Move farther away to fit
                    low = mid;
                }
            }

            camera.transform.position = savedPos;
            camera.transform.rotation = savedRot;
            camera.orthographic = savedOrtho;
            camera.orthographicSize = savedOrthoSize;

            return optimalSize;
        }

        /// <summary>
        /// Solves the required camera distance so that all 4 corners of the board
        /// project within the target viewport safe rect.
        /// </summary>
        public static float SolveCameraDistance(
            Camera camera,
            float boardExtentX,
            float boardExtentZ,
            Vector3 center,
            float pitchAngle,
            float yawAngle,
            Vector2 minViewport,
            Vector2 maxViewport,
            float minDistance = 5f,
            float maxDistance = 60f)
        {
            if (camera == null) return 18.5f;
            if (camera.orthographic) return 18.5f;

            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            float low = minDistance;
            float high = maxDistance;
            float optimalDistance = (low + high) * 0.5f;

            Vector3 halfExtents = new Vector3(boardExtentX * 0.5f, 0f, boardExtentZ * 0.5f);
            Vector3 c0 = center + new Vector3(-halfExtents.x, 0f, -halfExtents.z);
            Vector3 c1 = center + new Vector3( halfExtents.x, 0f, -halfExtents.z);
            Vector3 c2 = center + new Vector3(-halfExtents.x, 0f,  halfExtents.z);
            Vector3 c3 = center + new Vector3( halfExtents.x, 0f,  halfExtents.z);

            // 20 binary search iterations yield sub-millimeter distance precision
            for (int i = 0; i < 20; i++)
            {
                float mid = (low + high) * 0.5f;
                Vector3 camPos = center + rotation * new Vector3(0f, 0f, -mid);

                // Temporarily evaluate projection
                Vector3 savedPos = camera.transform.position;
                Quaternion savedRot = camera.transform.rotation;

                camera.transform.position = camPos;
                camera.transform.rotation = rotation;

                Vector3 vp0 = camera.WorldToViewportPoint(c0);
                Vector3 vp1 = camera.WorldToViewportPoint(c1);
                Vector3 vp2 = camera.WorldToViewportPoint(c2);
                Vector3 vp3 = camera.WorldToViewportPoint(c3);

                camera.transform.position = savedPos;
                camera.transform.rotation = savedRot;

                bool fitsX = vp0.x >= minViewport.x && vp0.x <= maxViewport.x &&
                             vp1.x >= minViewport.x && vp1.x <= maxViewport.x &&
                             vp2.x >= minViewport.x && vp2.x <= maxViewport.x &&
                             vp3.x >= minViewport.x && vp3.x <= maxViewport.x;

                bool fitsY = vp0.y >= minViewport.y && vp0.y <= maxViewport.y &&
                             vp1.y >= minViewport.y && vp1.y <= maxViewport.y &&
                             vp2.y >= minViewport.y && vp2.y <= maxViewport.y &&
                             vp3.y >= minViewport.y && vp3.y <= maxViewport.y;

                if (fitsX && fitsY)
                {
                    optimalDistance = mid;
                    // Try to move closer to fill more of the frame
                    high = mid;
                }
                else
                {
                    // Move farther away to fit
                    low = mid;
                }
            }

            return optimalDistance;
        }
    }
}
