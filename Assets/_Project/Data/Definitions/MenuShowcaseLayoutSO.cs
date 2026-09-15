using System.Collections.Generic;
using UnityEngine;
using Line98.Core;

namespace Line98.Data
{
    [System.Serializable]
    public struct ShowcasePlacement
    {
        [SerializeField] private BallColor m_Color;
        [SerializeField] private int m_Col;
        [SerializeField] private int m_Row;

        public ShowcasePlacement(BallColor color, int col, int row)
        {
            m_Color = color;
            m_Col = col;
            m_Row = row;
        }

        public BallColor Color => m_Color;
        public int Col => m_Col;
        public int Row => m_Row;
        public GridPos Position => new GridPos(m_Col, m_Row);
    }

    [CreateAssetMenu(fileName = "MenuShowcaseLayout_Default", menuName = "Line98/Definitions/Menu Showcase Layout")]
    public sealed class MenuShowcaseLayoutSO : ScriptableObject
    {
        [SerializeField] private ShowcasePlacement[] m_Placements = new ShowcasePlacement[]
        {
            new ShowcasePlacement(BallColor.Red, 4, 8),
            new ShowcasePlacement(BallColor.Orange, 2, 6),
            new ShowcasePlacement(BallColor.Yellow, 6, 6),
            new ShowcasePlacement(BallColor.Green, 4, 5),
            new ShowcasePlacement(BallColor.Cyan, 2, 3),
            new ShowcasePlacement(BallColor.Purple, 6, 3),
            new ShowcasePlacement(BallColor.Blue, 4, 2)
        };

        [SerializeField] private float m_IdleDriftAmplitude = 0.04f;
        [SerializeField] private float m_IdleDriftFrequency = 1.0f;
        [SerializeField] private float m_ParallaxFactor = 0.05f;

        public IReadOnlyList<ShowcasePlacement> Placements => m_Placements;
        public float IdleDriftAmplitude => m_IdleDriftAmplitude;
        public float IdleDriftFrequency => m_IdleDriftFrequency;
        public float ParallaxFactor => m_ParallaxFactor;

        public void SetPlacements(ShowcasePlacement[] placements)
        {
            m_Placements = placements;
        }
    }
}
