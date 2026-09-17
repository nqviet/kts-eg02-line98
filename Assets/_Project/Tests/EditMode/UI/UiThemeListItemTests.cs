using Line98.Data;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class UiThemeListItemTests
    {
        private const string s_ItemThemePrefabPath = "Assets/_Project/Content/Prefabs/UI/Widgets/Item_Theme.prefab";
        private const string s_ClassicThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset";

        private GameObject m_Prefab;
        private GameObject m_Instance;
        private UiThemeListItem m_Item;
        private UiThemeSO m_ClassicTheme;

        [SetUp]
        public void SetUp()
        {
            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_ItemThemePrefabPath);
            Assert.IsNotNull(m_Prefab, $"Prefab must exist at {s_ItemThemePrefabPath}");

            m_Instance = Object.Instantiate(m_Prefab);
            m_Item = m_Instance.GetComponent<UiThemeListItem>();
            Assert.IsNotNull(m_Item, "UiThemeListItem component must exist on instantiated prefab");

            m_ClassicTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(s_ClassicThemePath);
            Assert.IsNotNull(m_ClassicTheme, $"Classic theme must exist at {s_ClassicThemePath}");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Instance != null)
            {
                Object.DestroyImmediate(m_Instance);
            }
        }

        [Test]
        public void SetIsActive_WhenTrue_BorderIsVisible()
        {
            m_Item.SetIsActive(true);

            Assert.IsTrue(m_Item.IsActive);
            Assert.IsTrue(m_Item.IsBorderVisible, "Active card must show focus border");
        }

        [Test]
        public void SetIsActive_WhenFalse_BorderIsHidden()
        {
            m_Item.SetIsActive(true);
            m_Item.SetIsActive(false);

            Assert.IsFalse(m_Item.IsActive);
            Assert.IsFalse(m_Item.IsBorderVisible, "Inactive card must hide focus border");
        }

        [Test]
        public void SetIsApplied_DoesNotShowBorder_WhenNotActive()
        {
            m_Item.SetIsActive(false);
            m_Item.SetIsApplied(true);

            Assert.IsTrue(m_Item.IsApplied, "Card must be marked applied");
            Assert.IsFalse(m_Item.IsActive, "Card must not be marked active/focused");
            Assert.IsFalse(m_Item.IsBorderVisible, "Applied card without active preview must not show border outline");
        }

        [Test]
        public void SetIsApplied_PreservesBorder_WhenActive()
        {
            m_Item.SetIsActive(true);
            m_Item.SetIsApplied(true);

            Assert.IsTrue(m_Item.IsApplied);
            Assert.IsTrue(m_Item.IsActive);
            Assert.IsTrue(m_Item.IsBorderVisible, "Card that is both active and applied must show border");
        }

        [Test]
        public void Bind_DefaultAppliedThemeOnOpen_BorderIsHidden()
        {
            var model = new ThemeItemModel(
                partId: "classic",
                displayName: "Classic",
                bundle: null,
                swatches: null);

            // On popup open, applied card is bound with isActive = false, isApplied = true
            m_Item.Bind(model, isActive: false, isApplied: true);

            Assert.IsTrue(m_Item.IsApplied);
            Assert.IsFalse(m_Item.IsActive);
            Assert.IsFalse(m_Item.IsBorderVisible, "Default theme card on popup open must not have border visible");
        }

        [Test]
        public void ApplyTheme_DoesNotForceBorderVisible_WhenCardNotActive()
        {
            m_Item.SetIsActive(false);
            m_Item.SetIsApplied(true);
            m_Item.ApplyTheme(m_ClassicTheme);

            Assert.IsFalse(m_Item.IsBorderVisible, "Applying UI theme must not activate border if card is not active");
        }

        [Test]
        public void SwitchFocus_OnlyFocusedCardShowsBorder()
        {
            var otherInstance = Object.Instantiate(m_Prefab);
            var otherItem = otherInstance.GetComponent<UiThemeListItem>();

            try
            {
                // Initial state: Item A (default) is applied but inactive; Item B is neither
                m_Item.SetIsActive(false);
                m_Item.SetIsApplied(true);

                otherItem.SetIsActive(false);
                otherItem.SetIsApplied(false);

                Assert.IsFalse(m_Item.IsBorderVisible, "Default card must not have border initially");
                Assert.IsFalse(otherItem.IsBorderVisible, "Other card must not have border initially");

                // Preview tap on Item B
                m_Item.SetIsActive(false);
                otherItem.SetIsActive(true);

                Assert.IsFalse(m_Item.IsBorderVisible, "Default card must remain without border when another card is focused");
                Assert.IsTrue(otherItem.IsBorderVisible, "Previewed card must show border");

                // Select clicked on Item B: Item B becomes applied and stays active; Item A becomes unapplied
                m_Item.SetIsActive(false);
                m_Item.SetIsApplied(false);

                otherItem.SetIsActive(true);
                otherItem.SetIsApplied(true);

                Assert.IsFalse(m_Item.IsBorderVisible, "Previous default card must not show border after new card is selected");
                Assert.IsTrue(otherItem.IsBorderVisible, "Newly selected card keeps border while active");
            }
            finally
            {
                Object.DestroyImmediate(otherInstance);
            }
        }
    }
}
