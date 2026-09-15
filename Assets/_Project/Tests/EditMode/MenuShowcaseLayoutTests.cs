using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public sealed class MenuShowcaseLayoutTests
    {
        private const string s_LayoutPath = "Assets/_Project/Content/Definitions/MenuShowcaseLayout_Default.asset";

        [Test]
        public void ShowcaseLayoutAsset_ExistsAndHasValidAmplitudes()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MenuShowcaseLayoutSO>(s_LayoutPath);
            Assert.IsNotNull(layout, $"MenuShowcaseLayout asset must exist at {s_LayoutPath}");
            Assert.Greater(layout.IdleDriftAmplitude, 0f, "Idle drift amplitude must be positive");
            Assert.Greater(layout.IdleDriftFrequency, 0f, "Idle drift frequency must be positive");
            Assert.Greater(layout.ParallaxFactor, 0f, "Parallax factor must be positive");
        }

        [Test]
        public void ShowcasePattern_HasExactlySevenUniqueAndBoundedPlacements()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MenuShowcaseLayoutSO>(s_LayoutPath);
            Assert.IsNotNull(layout);

            var placements = layout.Placements;
            Assert.IsNotNull(placements);
            Assert.AreEqual(7, placements.Count, "Showcase kite pattern must have exactly 7 placements");

            var occupied = new HashSet<GridPos>();
            for (int i = 0; i < placements.Count; i++)
            {
                GridPos pos = placements[i].Position;
                Assert.IsTrue(pos.IsValid, $"Placement {i} at {pos} must be within 9x9 board bounds");
                Assert.IsTrue(occupied.Add(pos), $"Placement {i} at {pos} conflicts with another placement");
            }
        }

        [Test]
        public void ShowcasePattern_MatchesCanonicalSevenGemKite()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MenuShowcaseLayoutSO>(s_LayoutPath);
            Assert.IsNotNull(layout);

            // Red(4,8), Orange(2,6), Yellow(6,6), Green(4,5), Cyan(2,3), Purple(6,3), Blue(4,2)
            var expected = new Dictionary<GridPos, BallColor>
            {
                { new GridPos(4, 8), BallColor.Red },
                { new GridPos(2, 6), BallColor.Orange },
                { new GridPos(6, 6), BallColor.Yellow },
                { new GridPos(4, 5), BallColor.Green },
                { new GridPos(2, 3), BallColor.Cyan },
                { new GridPos(6, 3), BallColor.Purple },
                { new GridPos(4, 2), BallColor.Blue }
            };

            var placements = layout.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                Assert.IsTrue(expected.TryGetValue(p.Position, out BallColor expectedColor),
                    $"Unexpected showcase placement at {p.Position}");
                Assert.AreEqual(expectedColor, p.Color,
                    $"Placement at {p.Position} has incorrect color");
            }
        }
    }
}
