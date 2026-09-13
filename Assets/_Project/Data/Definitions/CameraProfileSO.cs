using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "CameraProfile_Default", menuName = "Line98/Definitions/Camera Profile")]
    public class CameraProfileSO : ScriptableObject
    {
        [SerializeField] private bool m_IsOrthographic = true;
        [SerializeField] private float m_OrthographicSize = 7.5f;
        [SerializeField] private float m_FieldOfView = 28.0f;
        [SerializeField] private float m_PitchAngle = 58.0f;
        [SerializeField] private float m_YawAngle = 0.0f;
        [SerializeField] private float m_Distance = 18.5f;
        [SerializeField] private float m_ParallaxFactor = 0.05f;

        public bool IsOrthographic => m_IsOrthographic;
        public float OrthographicSize => m_OrthographicSize;
        public float FieldOfView => m_FieldOfView;
        public float PitchAngle => m_PitchAngle;
        public float YawAngle => m_YawAngle;
        public float Distance => m_Distance;
        public float ParallaxFactor => m_ParallaxFactor;
    }
}
