using UnityEngine;

namespace PongLegends
{
    public class IronShield : MonoBehaviour
    {
        private Vector3    _size;
        private PaddleSide _side;
        private Ball       _ball;

        public PaddleSide Side => _side;

        public void Initialize(Ball ball, PaddleSide side, Vector3 position,
                               float width, float height, Color bodyColor, Color accentColor)
        {
            _ball = ball;
            _side = side;
            _size = new Vector3(width, height, 0.1f);
            transform.position = position;

            float midH   = height * 0.5f;
            float taperH = height * 0.25f;
            float taperW = width  * 0.6f;
            float taperY = midH * 0.5f + taperH * 0.5f; // center of taper section

            // Center body (middle 50%)
            SpriteFactory.CreateQuad("ShieldMid", bodyColor, transform,
                new Vector3(0f, 0f, 0f), new Vector3(width, midH, 1f), 2);

            // Tapered top and bottom (25% each, ~60% width)
            SpriteFactory.CreateQuad("TaperTop", bodyColor, transform,
                new Vector3(0f,  taperY, 0f), new Vector3(taperW, taperH, 1f), 2);
            SpriteFactory.CreateQuad("TaperBot", bodyColor, transform,
                new Vector3(0f, -taperY, 0f), new Vector3(taperW, taperH, 1f), 2);

            // Accent: vertical ridge down the center body
            SpriteFactory.CreateQuad("Ridge", accentColor, transform,
                new Vector3(0f, 0f, 0f), new Vector3(width * 0.14f, midH, 1f), 3);

            // Accent: seam bands where taper meets body
            float seamY = midH * 0.5f;
            SpriteFactory.CreateQuad("SeamTop", accentColor, transform,
                new Vector3(0f,  seamY, 0f), new Vector3(width, height * 0.025f, 1f), 3);
            SpriteFactory.CreateQuad("SeamBot", accentColor, transform,
                new Vector3(0f, -seamY, 0f), new Vector3(width, height * 0.025f, 1f), 3);

            ball.AddShield(this);
        }

        private void OnDestroy()
        {
            if (_ball != null)
                _ball.RemoveShield(this);
        }

        // Full bounding box used for ball collision (taper is cosmetic)
        public Bounds GetBounds() => new Bounds(transform.position, _size);
    }
}
