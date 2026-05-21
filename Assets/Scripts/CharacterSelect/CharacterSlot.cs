using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PongLegends
{
    public class CharacterSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public CharacterDefinition Definition { get; private set; }

        private Image _bg;
        private TextMeshProUGUI _nameText;
        private bool _isSelected;
        private int _index;
        private Action<int> _onClick;

        private static readonly Color IdleColor     = new Color(0.17f, 0.17f, 0.27f);
        private static readonly Color HoverColor    = new Color(0.25f, 0.25f, 0.42f);
        private static readonly Color SelectedColor = new Color(0.28f, 0.26f, 0.07f);

        public void Initialize(CharacterDefinition def, int index, Action<int> onClick)
        {
            Definition = def;
            _index     = index;
            _onClick   = onClick;

            _bg       = GetComponent<Image>();
            _nameText = GetComponentInChildren<TextMeshProUGUI>();

            if (_bg       != null) _bg.color     = IdleColor;
            if (_nameText != null) _nameText.text = def.characterName.ToUpper();

            BuildPaddlePreview();
        }

        public void SetHovered(bool hovered)
        {
            if (_isSelected) return;
            if (_bg != null) _bg.color = hovered ? HoverColor : IdleColor;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_bg != null) _bg.color = selected ? SelectedColor : IdleColor;
        }

        public void OnPointerClick(PointerEventData eventData) => _onClick?.Invoke(_index);
        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        public void OnPointerExit(PointerEventData eventData)  => SetHovered(false);

        private void Update()
        {
            if (_isSelected)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
                transform.localScale = Vector3.one * pulse;
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
            }
        }

        // ── Preview ──────────────────────────────────────────────────────────────

        private void BuildPaddlePreview()
        {
            if (Definition == null) return;

            Sprite white = WhiteSprite();

            // Paddle body — vertical rect in character's color
            // Slot is 245×210; name text sits at y=-80, so we draw in the upper portion
            MakeImg("PaddleBody", white, Definition.paddleColor,  new Vector2(0,  18), new Vector2(22, 88));

            // Accent band at top of paddle
            MakeImg("AccentBand", white, Definition.accentColor, new Vector2(0,  66), new Vector2(34,  9));

            // Per-character visual feature decoration
            BuildFeatureDecoration(white);
        }

        private void BuildFeatureDecoration(Sprite white)
        {
            switch (Definition.visualFeature)
            {
                case VisualFeature.Sunglasses:
                    MakeImg("GlassL", white, Definition.accentColor,    new Vector2(-7, 24), new Vector2(12, 5));
                    MakeImg("GlassR", white, Definition.accentColor,    new Vector2( 7, 24), new Vector2(12, 5));
                    MakeImg("Bridge", white, Definition.accentColor,    new Vector2( 0, 24), new Vector2( 4, 3));
                    break;

                case VisualFeature.Headband:
                    MakeImg("Band", white, Definition.accentColor,      new Vector2(0, 54), new Vector2(38, 11));
                    break;

                case VisualFeature.Electric:
                    MakeImg("BoltHi", white, Definition.accentColor,   new Vector2(-3, 50), new Vector2(5, 18));
                    MakeImg("BoltLo", white, Definition.accentColor,   new Vector2( 3, 34), new Vector2(5, 18));
                    break;

                case VisualFeature.Armor:
                    MakeImg("PlateL", white, Definition.accentColor,   new Vector2(-13, 18), new Vector2(5, 88));
                    MakeImg("PlateR", white, Definition.accentColor,   new Vector2( 13, 18), new Vector2(5, 88));
                    break;

                case VisualFeature.NinjaMask:
                    MakeImg("Mask", white, new Color(0.08f, 0.08f, 0.08f), new Vector2(0, 22), new Vector2(28, 9));
                    break;

                case VisualFeature.PixelBlocks:
                    for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        MakeImg($"Px{r}{c}", white, Definition.accentColor,
                            new Vector2(-8 + c * 8, 8 + r * 8), new Vector2(5, 5));
                    break;

                case VisualFeature.Flames:
                    MakeImg("F1", white, new Color(1f, 0.35f, 0f), new Vector2(-7, -14), new Vector2(9, 18));
                    MakeImg("F2", white, new Color(1f, 0.65f, 0f), new Vector2( 0,  -7), new Vector2(9, 26));
                    MakeImg("F3", white, new Color(1f, 0.35f, 0f), new Vector2( 7, -14), new Vector2(9, 18));
                    break;

                case VisualFeature.IceCrystals:
                    MakeImg("Cx1", white, Definition.accentColor, new Vector2(-9, -10), new Vector2(7, 16));
                    MakeImg("Cx2", white, Definition.accentColor, new Vector2( 0,  -5), new Vector2(7, 22));
                    MakeImg("Cx3", white, Definition.accentColor, new Vector2( 9, -10), new Vector2(7, 16));
                    break;
            }
        }

        private void MakeImg(string goName, Sprite white, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            var rt        = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var img       = go.AddComponent<Image>();
            img.sprite    = white;
            img.color     = color;
        }

        private static Sprite _white;
        private static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return _white;
        }
    }
}
