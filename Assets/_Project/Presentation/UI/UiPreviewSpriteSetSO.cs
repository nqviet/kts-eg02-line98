using Line98.Core;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Theme-owned sprite mapping used by all preview-tray prefab instances.</summary>
    [CreateAssetMenu(menuName = "Line 98/UI/Preview Sprite Set", fileName = "UiPreviewSpriteSet")]
    public sealed class UiPreviewSpriteSetSO : ScriptableObject
    {
        [SerializeField] private Sprite[] m_Sprites = new Sprite[7];

        public void SetSprites(Sprite red, Sprite orange, Sprite yellow, Sprite green, Sprite cyan, Sprite purple, Sprite blue)
        {
            m_Sprites = new[] { red, orange, yellow, green, cyan, purple, blue };
        }

        public Sprite GetSprite(BallColor color)
        {
            int index = color switch
            {
                BallColor.Red => 0,
                BallColor.Orange => 1,
                BallColor.Yellow => 2,
                BallColor.Green => 3,
                BallColor.Cyan => 4,
                BallColor.Purple => 5,
                BallColor.Blue => 6,
                _ => -1
            };

            return index >= 0 && index < m_Sprites.Length ? m_Sprites[index] : null;
        }
    }
}
