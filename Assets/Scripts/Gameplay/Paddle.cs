using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PongLegends
{
    public enum PaddleMode { Player, AI }
    public enum KickType   { None, High, Strong, Low }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Paddle : MonoBehaviour
    {
        private const float PaddleWidth    = 0.25f;
        private const float PaddleHeight   = 1.2f;
        private const float PlayerSpeed    = 4.5f;
        private const float AISpeed        = 3.5f;
        private const float AIDeadZone     = 0.15f;

        [SerializeField] private PaddleMode mode;
        [SerializeField] private Ball ball;

        private SpriteRenderer _renderer;
        private float _baseHeight;
        private bool _isFrozen;
        private float _speedMultiplier = 1f;
        private bool _controlsInverted;
        private float _currentVelocity;

        // Flame/electric animation children
        private GameObject[] _animatedChildren;
        private float _animTimer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = SpriteFactory.Square;
            _renderer.sortingOrder = 1;
        }

        public void Initialize(CharacterDefinition def, PaddleMode paddleMode, Ball ballRef)
        {
            mode = paddleMode;
            ball = ballRef;

            _baseHeight = PaddleHeight * def.paddleHeightMultiplier;
            transform.localScale = new Vector3(PaddleWidth, _baseHeight, 1f);
            _renderer.color = def.paddleColor;

            BuildVisualFeature(def);
        }

        private void Update()
        {
            if (_isFrozen)
            {
                _currentVelocity = 0f;
                return;
            }

            float move = (mode == PaddleMode.Player ? PlayerInput() : AIInput()) * _speedMultiplier;
            _currentVelocity = move;
            Vector3 pos = transform.position;
            float halfH = transform.localScale.y * 0.5f;
            pos.y = Mathf.Clamp(pos.y + move * Time.deltaTime,
                                 -Ball.WorldHalfHeight + halfH,
                                  Ball.WorldHalfHeight - halfH);
            transform.position = pos;

            AnimateFeature();
        }

        private float PlayerInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float v = 0f;
            if (kb.upArrowKey.isPressed)   v += 1f;
            if (kb.downArrowKey.isPressed) v -= 1f;
            return v * PlayerSpeed * (_controlsInverted ? -1f : 1f);
        }

        private float AIInput()
        {
            if (ball == null) return 0f;
            float diff = ball.transform.position.y - transform.position.y;
            if (Mathf.Abs(diff) < AIDeadZone) return 0f;
            return Mathf.Sign(diff) * AISpeed;
        }

        public Bounds GetBounds() => _renderer.bounds;
        public float GetVelocity() => _currentVelocity;

        public KickType GetKickInput()
        {
            if (mode != PaddleMode.Player) return KickType.None;
            var kb = Keyboard.current;
            if (kb == null) return KickType.None;
            if (kb.aKey.isPressed) return KickType.High;
            if (kb.sKey.isPressed) return KickType.Strong;
            if (kb.dKey.isPressed) return KickType.Low;
            return KickType.None;
        }

        // --- Ability API ---

        public IEnumerator FreezeCoroutine(float duration)
        {
            _isFrozen = true;
            Color saved = _renderer.color;
            _renderer.color = Color.cyan;
            yield return new WaitForSeconds(duration);
            _renderer.color = saved;
            _isFrozen = false;
        }

        // Shrinks (or grows) the paddle by multiplier for duration, then restores.
        public IEnumerator ShrinkCoroutine(float multiplier, float duration)
        {
            Color saved = _renderer.color;
            _renderer.color = new Color(1f, 0.27f, 0f);
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(s.x, _baseHeight * multiplier, s.z);
            yield return new WaitForSeconds(duration);
            transform.localScale = new Vector3(s.x, _baseHeight, s.z);
            _renderer.color = saved;
        }

        // Slows the paddle to 50% movement speed for duration.
        public IEnumerator SlowCoroutine(float duration)
        {
            Color saved = _renderer.color;
            _renderer.color = Color.blue;
            _speedMultiplier = 0.5f;
            yield return new WaitForSeconds(duration);
            _speedMultiplier = 1f;
            _renderer.color = saved;
        }

        // Inverts player controls for duration (has no effect on AI).
        public IEnumerator InvertControlsCoroutine(float duration)
        {
            Color saved = _renderer.color;
            _renderer.color = Color.green;
            _controlsInverted = true;
            yield return new WaitForSeconds(duration);
            _controlsInverted = false;
            _renderer.color = saved;
        }

        private const float TeleportMinDistance = 1.5f;

        // Teleports the paddle to a random Y position guaranteed to be at least
        // TeleportMinDistance away from its current position.
        public void TeleportRandomY()
        {
            float halfH    = transform.localScale.y * 0.5f;
            float lo       = -Ball.WorldHalfHeight + halfH;
            float hi       =  Ball.WorldHalfHeight - halfH;
            float currentY = transform.position.y;

            float belowLen = Mathf.Max(0f, currentY - TeleportMinDistance - lo);
            float aboveLen = Mathf.Max(0f, hi - (currentY + TeleportMinDistance));
            float total    = belowLen + aboveLen;

            float y;
            if (total <= 0f)
            {
                y = currentY > 0f ? lo : hi;
            }
            else
            {
                float r = UnityEngine.Random.Range(0f, total);
                y = r < belowLen ? lo + r : currentY + TeleportMinDistance + (r - belowLen);
            }

            transform.position = new Vector3(transform.position.x, y, 0f);
        }

        // --- Visual Features ---

        private void BuildVisualFeature(CharacterDefinition def)
        {
            Color accent = def.accentColor;
            Color paddle = def.paddleColor;

            switch (def.visualFeature)
            {
                case VisualFeature.Sunglasses:
                    BuildSunglasses(accent);
                    break;
                case VisualFeature.Headband:
                    BuildHeadband(accent);
                    break;
                case VisualFeature.Electric:
                    BuildElectric(accent);
                    break;
                case VisualFeature.Armor:
                    BuildArmor(accent);
                    break;
                case VisualFeature.NinjaMask:
                    BuildNinjaMask(accent);
                    break;
                case VisualFeature.PixelBlocks:
                    BuildPixelBlocks(accent);
                    break;
                case VisualFeature.Flames:
                    BuildFlames(accent);
                    break;
                case VisualFeature.IceCrystals:
                    BuildIceCrystals(accent);
                    break;
            }
        }

        private void BuildSunglasses(Color accent)
        {
            // Two lens quads and a bridge, near top of paddle
            // Paddle is 1 unit tall in local space (scaled via localScale.y externally)
            float lensW = 0.6f, lensH = 0.2f, gap = 0.05f;
            float topY = 0.3f;
            SpriteFactory.CreateQuad("LensL", accent, transform, new Vector3(-lensW * 0.5f - gap * 0.5f, topY, -0.1f), new Vector3(lensW, lensH, 1f));
            SpriteFactory.CreateQuad("LensR", accent, transform, new Vector3( lensW * 0.5f + gap * 0.5f, topY, -0.1f), new Vector3(lensW, lensH, 1f));
            SpriteFactory.CreateQuad("Bridge", accent, transform, new Vector3(0f, topY, -0.1f), new Vector3(gap, lensH * 0.4f, 1f));
        }

        private void BuildHeadband(Color accent)
        {
            // Red strip at top, black belt at center
            SpriteFactory.CreateQuad("Headband", accent, transform, new Vector3(0f, 0.38f, -0.1f), new Vector3(1.1f, 0.12f, 1f));
            SpriteFactory.CreateQuad("Belt", Color.black, transform, new Vector3(0f, 0f, -0.1f), new Vector3(1.1f, 0.08f, 1f));
        }

        private void BuildElectric(Color accent)
        {
            // Small spark dots that are randomized in AnimateFeature
            _animatedChildren = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                float yPos = Mathf.Lerp(-0.35f, 0.35f, i / 3f);
                _animatedChildren[i] = SpriteFactory.CreateQuad($"Spark{i}", accent, transform,
                    new Vector3(0f, yPos, -0.1f), new Vector3(0.15f, 0.15f, 1f));
            }
        }

        private void BuildArmor(Color accent)
        {
            // Darker edge plates
            SpriteFactory.CreateQuad("ArmorTop",    accent, transform, new Vector3(0f,  0.42f, -0.1f), new Vector3(1.2f, 0.1f, 1f));
            SpriteFactory.CreateQuad("ArmorBottom", accent, transform, new Vector3(0f, -0.42f, -0.1f), new Vector3(1.2f, 0.1f, 1f));
            SpriteFactory.CreateQuad("ArmorMid",    accent, transform, new Vector3(0f,  0f,    -0.1f), new Vector3(1.2f, 0.06f, 1f));
        }

        private void BuildNinjaMask(Color accent)
        {
            // Eye slit across the upper paddle
            SpriteFactory.CreateQuad("EyeSlit", accent, transform, new Vector3(0f, 0.22f, -0.1f), new Vector3(1.1f, 0.08f, 1f));
        }

        private void BuildPixelBlocks(Color accent)
        {
            // Alternating blocks along the paddle height
            int blocks = 6;
            for (int i = 0; i < blocks; i++)
            {
                float y = Mathf.Lerp(-0.42f, 0.42f, i / (float)(blocks - 1));
                Color c = i % 2 == 0 ? accent : Color.black;
                SpriteFactory.CreateQuad($"Pixel{i}", c, transform, new Vector3(0f, y, -0.1f), new Vector3(1.1f, 0.12f, 1f));
            }
        }

        private void BuildFlames(Color accent)
        {
            // Triangle-like quads on top/bottom that animate
            _animatedChildren = new GameObject[4];
            _animatedChildren[0] = SpriteFactory.CreateQuad("FlameT1", accent, transform, new Vector3(-0.2f, 0.5f, -0.1f), new Vector3(0.15f, 0.2f, 1f));
            _animatedChildren[1] = SpriteFactory.CreateQuad("FlameT2", accent, transform, new Vector3( 0.2f, 0.5f, -0.1f), new Vector3(0.15f, 0.2f, 1f));
            _animatedChildren[2] = SpriteFactory.CreateQuad("FlameB1", accent, transform, new Vector3(-0.2f,-0.5f, -0.1f), new Vector3(0.15f, 0.2f, 1f));
            _animatedChildren[3] = SpriteFactory.CreateQuad("FlameB2", accent, transform, new Vector3( 0.2f,-0.5f, -0.1f), new Vector3(0.15f, 0.2f, 1f));
        }

        private void BuildIceCrystals(Color accent)
        {
            // Diamond star shapes (4 quads per star) on each side
            Vector3[] centers = { new Vector3(0f, 0.35f, -0.1f), new Vector3(0f, -0.35f, -0.1f) };
            foreach (var c in centers)
            {
                SpriteFactory.CreateQuad("IceH", accent, transform, c, new Vector3(0.4f, 0.1f, 1f));
                SpriteFactory.CreateQuad("IceV", accent, transform, c, new Vector3(0.1f, 0.4f, 1f));
            }
        }

        private void AnimateFeature()
        {
            if (_animatedChildren == null) return;
            _animTimer += Time.deltaTime;
            if (_animTimer < 0.08f) return;
            _animTimer = 0f;

            foreach (var child in _animatedChildren)
            {
                if (child == null) continue;
                Vector3 s = child.transform.localScale;
                float flicker = UnityEngine.Random.Range(0.6f, 1.2f);
                child.transform.localScale = new Vector3(s.x, Mathf.Abs(s.y) * flicker, s.z);
            }
        }
    }
}
