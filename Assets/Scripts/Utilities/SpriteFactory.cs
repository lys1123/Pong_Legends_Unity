using UnityEngine;

namespace PongLegends
{
    public static class SpriteFactory
    {
        private static Sprite _square;

        public static Sprite Square
        {
            get
            {
                if (_square != null) return _square;
                Texture2D tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                // pixelsPerUnit=1: 1 pixel = 1 world unit, so scale via transform.localScale
                _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return _square;
            }
        }

        public static GameObject CreateQuad(string name, Color color, Transform parent,
                                            Vector3 localPos, Vector3 localScale, int sortOrder = 1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = sortOrder;
            return go;
        }
    }
}
