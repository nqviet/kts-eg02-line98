using UnityEngine;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Governs the 3D orthogonal / fixed-angle camera rig according to CameraProfileSO.
    /// Manages the three-tier hierarchy:
    ///   CameraRig (look-at center and orientation)
    ///     └── CamShakeRoot (CamShake additive displacement)
    ///          └── Camera (actual Orthographic/Perspective camera at -distance)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour, ITickable
    {
        [SerializeField] private CameraProfileSO m_Profile;
        [SerializeField] private Camera m_Camera;
        [SerializeField] private CamShake m_CamShake;
        [SerializeField] private Transform m_ShakeRoot;

        private float m_SolvedDistance = 18.5f;
        private float m_SolvedOrthographicSize = 7.5f;
        private int m_LastScreenWidth;
        private int m_LastScreenHeight;
        private Vector3 m_BoardCenter = Vector3.zero;
        private float m_BoardExtentX = 9.0f;
        private float m_BoardExtentZ = 9.0f;
        private Vector2 m_MinViewport = BoardFitSolver.DefaultMinViewport;
        private Vector2 m_MaxViewport = BoardFitSolver.DefaultMaxViewport;
        private Renderer m_Backdrop;

        public void SetBackdrop(Renderer backdrop)
        {
            m_Backdrop = backdrop;
            if (m_Backdrop == null) return;
            m_Backdrop.transform.SetParent(m_Camera.transform, false);
            UpdateBackdrop();
        }

        private void UpdateBackdrop()
        {
            if (m_Backdrop == null) return;
            const float depth = 70f;
            float height = m_Camera.orthographic ? m_Camera.orthographicSize * 2f
                : 2f * depth * Mathf.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            Texture texture = m_Backdrop.sharedMaterial.mainTexture;
            float aspect = texture != null ? (float)texture.width / texture.height : 9f / 16f;
            height = Mathf.Max(height, height * m_Camera.aspect / aspect);
            m_Backdrop.transform.localPosition = new Vector3(0f, 0f, depth);
            m_Backdrop.transform.localRotation = Quaternion.identity;
            m_Backdrop.transform.localScale = new Vector3(height * aspect, height, 1f);
        }

        public Camera Camera => m_Camera;
        public CamShake CamShake => m_CamShake;
        public Vector3 YawRightAxis => m_Camera != null ? m_Camera.transform.right : Vector3.right;
        public float SolvedDistance => m_SolvedDistance;
        public float SolvedOrthographicSize => m_SolvedOrthographicSize;
        public bool IsOrthographic => m_Camera != null && m_Camera.orthographic;
        public Vector2 MinViewport => m_MinViewport;
        public Vector2 MaxViewport => m_MaxViewport;

        public void SetTargetViewport(Vector2 minViewport, Vector2 maxViewport)
        {
            m_MinViewport = new Vector2(Mathf.Clamp01(minViewport.x), Mathf.Clamp01(minViewport.y));
            m_MaxViewport = new Vector2(Mathf.Clamp01(maxViewport.x), Mathf.Clamp01(maxViewport.y));
            if (m_MaxViewport.x <= m_MinViewport.x || m_MaxViewport.y <= m_MinViewport.y)
            {
                m_MinViewport = BoardFitSolver.DefaultMinViewport;
                m_MaxViewport = BoardFitSolver.DefaultMaxViewport;
            }
            RefitCamera();
        }

        public void Initialize(CameraProfileSO profile, Vector3 boardCenter, float boardSize = 9.0f, float boardDepth = 0f)
        {
            m_Profile = profile;
            m_BoardCenter = boardCenter;
            m_BoardExtentX = boardSize;
            m_BoardExtentZ = boardDepth > 0f ? boardDepth : boardSize;

            EnsureRigHierarchy();
            ApplyProfile();
            RefitCamera();
        }

        private void EnsureRigHierarchy()
        {
            if (m_ShakeRoot == null)
            {
                var shakeChild = transform.Find("CamShakeRoot");
                if (shakeChild == null)
                {
                    var go = new GameObject("CamShakeRoot");
                    go.transform.SetParent(transform, false);
                    m_ShakeRoot = go.transform;
                }
                else
                {
                    m_ShakeRoot = shakeChild;
                }
            }

            if (m_CamShake == null)
            {
                m_CamShake = m_ShakeRoot.GetComponent<CamShake>();
                if (m_CamShake == null)
                {
                    m_CamShake = m_ShakeRoot.gameObject.AddComponent<CamShake>();
                }
            }

            if (m_Camera == null)
            {
                m_Camera = GetComponentInChildren<Camera>();
                if (m_Camera == null)
                {
                    var camGo = new GameObject("MainCamera");
                    camGo.transform.SetParent(m_ShakeRoot, false);
                    camGo.tag = "MainCamera";
                    m_Camera = camGo.AddComponent<Camera>();
                }
                else
                {
                    m_Camera.transform.SetParent(m_ShakeRoot, false);
                }
            }
        }

        public void ApplyProfile()
        {
            if (m_Camera == null) return;

            bool isOrtho = m_Profile == null || m_Profile.IsOrthographic;
            float orthoSize = m_Profile != null ? m_Profile.OrthographicSize : 7.5f;
            float fov = m_Profile != null ? m_Profile.FieldOfView : 28.0f;
            float pitch = m_Profile != null ? m_Profile.PitchAngle : 58.0f;
            float yaw = m_Profile != null ? m_Profile.YawAngle : 0.0f;

            m_Camera.orthographic = isOrtho;
            if (isOrtho)
            {
                m_Camera.orthographicSize = orthoSize;
            }
            else
            {
                m_Camera.fieldOfView = fov;
            }

            m_Camera.nearClipPlane = 0.3f;
            m_Camera.farClipPlane = 100f;

            transform.position = m_BoardCenter;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void RefitCamera()
        {
            if (m_Camera == null) return;

            float pitch = m_Profile != null ? m_Profile.PitchAngle : 58.0f;
            float yaw = m_Profile != null ? m_Profile.YawAngle : 0.0f;

            if (m_Camera.orthographic)
            {
                m_SolvedOrthographicSize = BoardFitSolver.SolveOrthographicSize(
                    m_Camera,
                    m_BoardExtentX,
                    m_BoardExtentZ,
                    m_BoardCenter,
                    pitch,
                    yaw,
                    m_MinViewport,
                    m_MaxViewport,
                    minOrthoSize: 2f,
                    maxOrthoSize: 30f);

                m_Camera.orthographicSize = m_SolvedOrthographicSize;
                m_SolvedDistance = m_Profile != null ? m_Profile.Distance : 18.5f;
            }
            else
            {
                m_SolvedDistance = BoardFitSolver.SolveCameraDistance(
                    m_Camera,
                    m_BoardExtentX,
                    m_BoardExtentZ,
                    m_BoardCenter,
                    pitch,
                    yaw,
                    m_MinViewport,
                    m_MaxViewport,
                    minDistance: 8f,
                    maxDistance: 40f);
            }

            // Center the board inside the HUD's reserved rectangle, including on tall screens.
            Vector2 viewportCenter = (m_MinViewport + m_MaxViewport) * 0.5f;
            Vector3 framingOffset = m_Camera.orthographic
                ? new Vector3((0.5f - viewportCenter.x) * 2f * m_SolvedOrthographicSize * m_Camera.aspect,
                    (0.5f - viewportCenter.y) * 2f * m_SolvedOrthographicSize, 0f)
                : Vector3.zero;
            m_Camera.transform.localPosition = new Vector3(0f, 0f, -m_SolvedDistance) + framingOffset;
            m_Camera.transform.localRotation = Quaternion.identity;

            UpdateBackdrop();

            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;
        }

        public void Tick(float dt)
        {
            if (Screen.width != m_LastScreenWidth || Screen.height != m_LastScreenHeight)
            {
                RefitCamera();
            }

            if (m_CamShake != null)
            {
                m_CamShake.Tick(dt);
            }
        }
    }
}
