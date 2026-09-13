using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Resolution-independent silhouettes for the main HUD controls.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudIconGraphic : UnityEngine.UI.MaskableGraphic
    {
        public enum IconKind { Undo, Restart, Directions, Statistics, Settings }
        [SerializeField] private IconKind m_Kind;
        public IconKind Kind { get => m_Kind; set { m_Kind = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (m_Kind == IconKind.Undo || m_Kind == IconKind.Restart)
            {
                bool undo = m_Kind == IconKind.Undo;
                for (int i = 0; i < 48; i++)
                {
                    float a = Mathf.Lerp(undo ? -100 : 40, undo ? 145 : 335, i / 48f) * Mathf.Deg2Rad;
                    float b = Mathf.Lerp(undo ? -100 : 40, undo ? 145 : 335, (i + 1) / 48f) * Mathf.Deg2Rad;
                    Quad(vh, Polar(a, .43f), Polar(b, .43f), Polar(b, .28f), Polar(a, .28f));
                }
                if (undo) Triangle(vh, new Vector2(-.5f,.30f), new Vector2(-.15f,.52f), new Vector2(-.15f,.05f));
                else Triangle(vh, new Vector2(.44f,.45f), new Vector2(.44f,.08f), new Vector2(.07f,.12f));
            }
            else if (m_Kind == IconKind.Directions)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f;
                    Triangle(vh, Rotate(new Vector2(0,.49f),a), Rotate(new Vector2(-.12f,.30f),a), Rotate(new Vector2(.12f,.30f),a));
                }
            }
            else if (m_Kind == IconKind.Statistics)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = (i-1)*.30f;
                    float top = i == 1 ? .46f : i == 2 ? .2f : .03f;
                    Quad(vh,new Vector2(x-.10f,-.43f),new Vector2(x+.10f,-.43f),new Vector2(x+.10f,top),new Vector2(x-.10f,top));
                }
            }
            else
            {
                for (int i = 0; i < 64; i++)
                {
                    float a = i * Mathf.PI / 32, b = (i+1)*Mathf.PI/32;
                    float ra = i % 8 < 4 ? .48f : .38f;
                    float rb = (i+1) % 8 < 4 ? .48f : .38f;
                    Quad(vh,Polar(a,ra),Polar(b,rb),Polar(b,.19f),Polar(a,.19f));
                }
            }
        }
        private static Vector2 Polar(float a,float r) => new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
        private static Vector2 Rotate(Vector2 p,float a) => new Vector2(p.x*Mathf.Cos(a)-p.y*Mathf.Sin(a),p.x*Mathf.Sin(a)+p.y*Mathf.Cos(a));
        private void Vertex(UnityEngine.UI.VertexHelper vh,Vector2 p)
        {
            Rect r = rectTransform.rect;
            vh.AddVert(new Vector3(r.center.x+p.x*r.width,r.center.y+p.y*r.height,0),color,Vector2.zero);
        }
        private void Triangle(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 c)
        {
            int n=vh.currentVertCount; Vertex(vh,a); Vertex(vh,b); Vertex(vh,c); vh.AddTriangle(n,n+1,n+2);
        }
        private void Quad(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int n=vh.currentVertCount; Vertex(vh,a); Vertex(vh,b); Vertex(vh,c); Vertex(vh,d);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
    }
}
