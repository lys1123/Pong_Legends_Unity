using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PongLegends
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Ball : MonoBehaviour
    {
        public const float WorldHalfWidth  = 6.4f;
        public const float WorldHalfHeight = 3.6f;
        public const float Radius          = 0.1f;
        public const float InitialSpeed    = 4f;
        public const float MaxSpeed        = 6f;
        private const float SpeedBumpPerHit = 1.05f;
        private const float EnglishFactor   = 0.35f;
        private const float KickAngle        = 40f;
        private const float StrongKickSpeed  = 12f;

        public event Action<PaddleSide> OnPaddleHit;
        public event Action<PaddleSide> OnScore;

        [SerializeField] private Paddle playerPaddle;
        [SerializeField] private Paddle aiPaddle;

        private Vector2 _velocity;
        private bool _inPlay;
        private SpriteRenderer _renderer;
        private readonly List<IronShield> _shields = new();

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = SpriteFactory.Square;
            _renderer.color = Color.white;
            _renderer.sortingOrder = 2;
            transform.localScale = new Vector3(Radius * 2f, Radius * 2f, 1f);
        }

        public void SetPaddles(Paddle player, Paddle ai)
        {
            playerPaddle = player;
            aiPaddle = ai;
        }

        public void Serve(PaddleSide direction)
        {
            transform.position = Vector3.zero;
            _renderer.color = Color.white;

            float baseAngle = direction == PaddleSide.Player ? 0f : 180f;
            float spread    = UnityEngine.Random.Range(-30f, 30f);
            float rad       = (baseAngle + spread) * Mathf.Deg2Rad;
            _velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * InitialSpeed;
            _inPlay   = true;
        }

        private void Update()
        {
            if (!_inPlay) return;

            transform.position += (Vector3)(_velocity * Time.deltaTime);

            BounceOffWalls();
            CheckPaddleCollision(playerPaddle, PaddleSide.Player);
            CheckPaddleCollision(aiPaddle,     PaddleSide.AI);
            for (int i = 0; i < _shields.Count; i++)
                if (_shields[i] != null) CheckShieldCollision(_shields[i]);
            CheckScoring();
        }

        private void BounceOffWalls()
        {
            Vector2 pos = transform.position;
            if (pos.y + Radius >= WorldHalfHeight)
            {
                _velocity.y = -Mathf.Abs(_velocity.y);
                transform.position = new Vector3(pos.x, WorldHalfHeight - Radius, 0f);
            }
            else if (pos.y - Radius <= -WorldHalfHeight)
            {
                _velocity.y = Mathf.Abs(_velocity.y);
                transform.position = new Vector3(pos.x, -WorldHalfHeight + Radius, 0f);
            }
        }

        private void CheckPaddleCollision(Paddle paddle, PaddleSide side)
        {
            if (paddle == null) return;
            Bounds b   = paddle.GetBounds();
            Vector2 pos = transform.position;

            bool overlapping =
                pos.x + Radius > b.min.x && pos.x - Radius < b.max.x &&
                pos.y + Radius > b.min.y && pos.y - Radius < b.max.y;

            if (!overlapping) return;

            if (side == PaddleSide.Player && _velocity.x < 0f)
            {
                _velocity.x = Mathf.Abs(_velocity.x);
                KickType kick = paddle.GetKickInput();
                if (kick != KickType.None)
                    ApplyKick(kick, 1f);
                else
                {
                    ApplySpin(pos.y, b);
                    ApplyEnglish(paddle);
                    BumpSpeed();
                }
                OnPaddleHit?.Invoke(PaddleSide.Player);
            }
            else if (side == PaddleSide.AI && _velocity.x > 0f)
            {
                _velocity.x = -Mathf.Abs(_velocity.x);
                ApplySpin(pos.y, b);
                ApplyEnglish(paddle);
                if (_velocity.magnitude > MaxSpeed)
                    _velocity = _velocity.normalized * MaxSpeed;
                else
                    BumpSpeed();
                OnPaddleHit?.Invoke(PaddleSide.AI);
            }
        }

        private void CheckShieldCollision(IronShield shield)
        {
            Bounds b    = shield.GetBounds();
            Vector2 pos = transform.position;

            bool overlapping =
                pos.x + Radius > b.min.x && pos.x - Radius < b.max.x &&
                pos.y + Radius > b.min.y && pos.y - Radius < b.max.y;

            if (!overlapping) return;

            if (shield.Side == PaddleSide.Player && _velocity.x < 0f)
            {
                _velocity.x = Mathf.Abs(_velocity.x);
                ApplySpin(pos.y, b);
                BumpSpeed();
                OnPaddleHit?.Invoke(PaddleSide.Player);
            }
            else if (shield.Side == PaddleSide.AI && _velocity.x > 0f)
            {
                _velocity.x = -Mathf.Abs(_velocity.x);
                ApplySpin(pos.y, b);
                if (_velocity.magnitude > MaxSpeed)
                    _velocity = _velocity.normalized * MaxSpeed;
                else
                    BumpSpeed();
                OnPaddleHit?.Invoke(PaddleSide.AI);
            }
        }

        public void AddShield(IronShield s)    => _shields.Add(s);
        public void RemoveShield(IronShield s) => _shields.Remove(s);

        private void ApplySpin(float ballY, Bounds b)
        {
            float normalized = Mathf.Clamp((ballY - b.center.y) / (b.size.y * 0.5f), -1f, 1f);
            _velocity.y = normalized * 1.5f;
        }

        private void ApplyEnglish(Paddle paddle)
        {
            _velocity.y += paddle.GetVelocity() * EnglishFactor;
        }

        private void ApplyKick(KickType kick, float xSign)
        {
            float speed = Mathf.Min(_velocity.magnitude * SpeedBumpPerHit, MaxSpeed);
            float rad   = KickAngle * Mathf.Deg2Rad;
            switch (kick)
            {
                case KickType.High:
                    _velocity = new Vector2(Mathf.Cos(rad) * xSign,  Mathf.Sin(rad)) * speed;
                    break;
                case KickType.Strong:
                    _velocity = new Vector2(xSign, 0f) * StrongKickSpeed;
                    break;
                case KickType.Low:
                    _velocity = new Vector2(Mathf.Cos(rad) * xSign, -Mathf.Sin(rad)) * speed;
                    break;
            }
        }

        private void BumpSpeed()
        {
            float speed = Mathf.Min(_velocity.magnitude * SpeedBumpPerHit, MaxSpeed);
            _velocity = _velocity.normalized * speed;
        }

        private void CheckScoring()
        {
            float x = transform.position.x;
            if (x - Radius < -WorldHalfWidth)
            {
                _inPlay = false;
                OnScore?.Invoke(PaddleSide.AI);
            }
            else if (x + Radius > WorldHalfWidth)
            {
                _inPlay = false;
                OnScore?.Invoke(PaddleSide.Player);
            }
            // Guardrail: ball crossed behind a paddle's back edge (e.g. teleported there by an ability).
            // Award the point without waiting for the wall so the game never gets stuck.
            else if (playerPaddle != null && x < playerPaddle.GetBounds().min.x)
            {
                _inPlay = false;
                OnScore?.Invoke(PaddleSide.AI);
            }
            else if (aiPaddle != null && x > aiPaddle.GetBounds().max.x)
            {
                _inPlay = false;
                OnScore?.Invoke(PaddleSide.Player);
            }
        }

        // --- Ability API ---

        public Vector2 GetVelocity() => _velocity;
        public void SetVelocity(Vector2 v) => _velocity = v;

        public void MultiplySpeed(float factor)
        {
            _velocity *= factor;
            if (_velocity.magnitude > MaxSpeed)
                _velocity = _velocity.normalized * MaxSpeed;
        }

        public void Teleport(Vector2 pos) =>
            transform.position = new Vector3(pos.x, pos.y, 0f);

        public void SetColor(Color c) => _renderer.color = c;

        public IEnumerator FreezeCoroutine(float duration)
        {
            Vector2 saved = _velocity;
            _velocity = Vector2.zero;
            SetColor(Color.cyan);
            yield return new WaitForSeconds(duration);
            // Only restore if we froze a moving ball — a zero saved means we stacked
            // onto an already-frozen ball and must not wipe the first freeze's restore.
            if (saved.sqrMagnitude > 0.01f)
                _velocity = saved;
            SetColor(Color.white);
        }

        // Slows the ball by `factor` for `duration`, then restores the original speed
        // in whatever direction the ball is currently travelling (it may have bounced).
        public IEnumerator SlowCoroutine(float factor, float duration)
        {
            float savedSpeed = _velocity.magnitude;
            _velocity *= factor;
            SetColor(Color.blue);
            yield return new WaitForSeconds(duration);
            if (_velocity.sqrMagnitude > 0.01f)
                _velocity = _velocity.normalized * savedSpeed;
            SetColor(Color.white);
        }

        public void SetInPlay(bool value) => _inPlay = value;
        public bool IsInPlay() => _inPlay;
    }
}
