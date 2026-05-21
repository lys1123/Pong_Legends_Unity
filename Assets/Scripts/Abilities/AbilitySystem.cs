using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PongLegends
{
    public class AbilitySystem : MonoBehaviour
    {
        private SessionData _session;
        private Ball        _ball;
        private Paddle      _playerPaddle;
        private Paddle      _aiPaddle;

        // True while the ability effect is still active (projectile on screen / ghost balls alive).
        private readonly bool[] _abilityActive = new bool[2];
        private readonly int[]  _ghostCount    = new int[2];

        private float _aiCooldown;
        private const float AICooldownMin = 3f;
        private const float AICooldownMax = 6f;

        public void Initialize(SessionData session, Ball ball, Paddle playerPaddle, Paddle aiPaddle)
        {
            _session      = session;
            _ball         = ball;
            _playerPaddle = playerPaddle;
            _aiPaddle     = aiPaddle;

            ResetAICooldown();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
                Activate(PaddleSide.Player);

            _aiCooldown -= Time.deltaTime;
            if (_aiCooldown <= 0f)
            {
                Activate(PaddleSide.AI);
                ResetAICooldown();
            }
        }

        private void ResetAICooldown() =>
            _aiCooldown = UnityEngine.Random.Range(AICooldownMin, AICooldownMax);

        // ── Activation ───────────────────────────────────────────────────────────

        private void Activate(PaddleSide side)
        {
            int idx = (int)side;
            if (_abilityActive[idx]) return;   // already in use — do nothing

            _abilityActive[idx] = true;

            CharacterDefinition def = side == PaddleSide.Player
                ? _session.playerCharacter
                : _session.aiCharacter;

            if (def.abilityType == AbilityType.ShadowClone)
            {
                if (!_ball.IsInPlay()) { _abilityActive[idx] = false; return; }
                SpawnClones(side);
                return;
            }

            if (def.abilityType == AbilityType.Uppercut)
            {
                ExecuteUppercut(side);
                return;
            }

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            Paddle oppPaddle = side == PaddleSide.Player ? _aiPaddle     : _playerPaddle;
            float  xDir      = side == PaddleSide.Player ? 1f            : -1f;

            var (onHitBall, onHitPaddle) = BuildEffect(def.abilityType);
            float speed                  = ProjectileSpeed(def.abilityType);

            SpawnProjectile(ownPaddle, oppPaddle, xDir, def.accentColor,
                            onHitBall, onHitPaddle, speed, def.abilityType,
                            () => _abilityActive[idx] = false);
        }

        // ── Effect factory ───────────────────────────────────────────────────────

        private (Action<Ball>, Action<Paddle>) BuildEffect(AbilityType type)
        {
            switch (type)
            {
                case AbilityType.CoolWave:
                    return (
                        ball   => StartCoroutine(ball.SlowCoroutine(0.5f, 2f)),
                        paddle => StartCoroutine(paddle.SlowCoroutine(2f))
                    );

                case AbilityType.LightningBolt:
                    return (
                        ball => ball.Teleport(new Vector2(
                            ball.transform.position.x,
                            UnityEngine.Random.Range(-Ball.WorldHalfHeight + 0.3f, Ball.WorldHalfHeight - 0.3f))),
                        paddle => paddle.TeleportRandomY()
                    );

                case AbilityType.IronShell:
                    return (
                        ball =>
                        {
                            Vector2 v = ball.GetVelocity();
                            ball.SetVelocity(new Vector2(-v.x, v.y));
                        },
                        paddle => StartCoroutine(paddle.ShrinkCoroutine(0.6f, 3f))
                    );

                case AbilityType.GlitchBomb:
                    return (
                        ball =>
                        {
                            float spd = ball.GetVelocity().magnitude;
                            float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                            ball.SetVelocity(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd);
                        },
                        paddle => StartCoroutine(paddle.InvertControlsCoroutine(2f))
                    );

                case AbilityType.Fireball:
                    return (
                        ball   => ball.MultiplySpeed(1.5f),
                        paddle => StartCoroutine(paddle.ShrinkCoroutine(0.6f, 2f))
                    );

                case AbilityType.IceShot:
                    return (
                        ball   => StartCoroutine(ball.FreezeCoroutine(1.5f)),
                        paddle => StartCoroutine(paddle.FreezeCoroutine(1.5f))
                    );

                default:
                    return (null, null);
            }
        }

        private static float ProjectileSpeed(AbilityType type) => type switch
        {
            AbilityType.CoolWave      => 3f,
            AbilityType.LightningBolt => 9f,
            AbilityType.IronShell     => 3f,
            AbilityType.GlitchBomb    => 5f,
            AbilityType.Fireball      => 7f,
            AbilityType.IceShot       => 5f,
            _                         => 5f
        };

        // ── Uppercut ─────────────────────────────────────────────────────────────

        private void ExecuteUppercut(PaddleSide side)
        {
            int idx = (int)side;

            if (!_ball.IsInPlay()) { _abilityActive[idx] = false; return; }

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            float  xDir      = side == PaddleSide.Player ? 1f : -1f;

            // Ball must be within striking range: 2 units horizontally, paddle half-height + 1 vertically
            Vector2 ballPos  = _ball.transform.position;
            float   paddleHalfH = ownPaddle.GetBounds().size.y * 0.5f;
            bool    inRange  = Mathf.Abs(ballPos.x - ownPaddle.transform.position.x) < 2f
                            && Mathf.Abs(ballPos.y - ownPaddle.transform.position.y) < paddleHalfH + 1f;

            if (!inRange) { _abilityActive[idx] = false; return; }

            // Launch at 70° upward toward the opponent at 10× current speed
            float   launchSpeed = Mathf.Max(_ball.GetVelocity().magnitude, Ball.InitialSpeed) * 3f;
            float   rad         = 60f * Mathf.Deg2Rad;
            _ball.SetVelocity(new Vector2(xDir * Mathf.Cos(rad), Mathf.Sin(rad)) * launchSpeed);

            _abilityActive[idx] = false; // instant hit — unlocks immediately
        }

        // ── Shadow Clone ─────────────────────────────────────────────────────────

        private void SpawnClones(PaddleSide side)
        {
            int     idx   = (int)side;
            Vector2 vel   = _ball.GetVelocity();
            float   speed = vel.magnitude;
            float   angle = Mathf.Atan2(vel.y, vel.x);

            float[] angles = { angle - 15f * Mathf.Deg2Rad, angle, angle + 15f * Mathf.Deg2Rad };
            int realIdx = UnityEngine.Random.Range(0, 3);

            _ghostCount[idx] = 0;
            for (int i = 0; i < 3; i++)
            {
                Vector2 v = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i])) * speed;
                if (i == realIdx)
                    _ball.SetVelocity(v);
                else
                {
                    _ghostCount[idx]++;
                    SpawnGhost(_ball.transform.position, v * 0.9f, idx);
                }
            }
        }

        private void SpawnGhost(Vector3 position, Vector2 velocity, int sideIdx)
        {
            var go = new GameObject("GhostBall");
            go.transform.position   = position;
            go.transform.localScale = new Vector3(Ball.Radius * 2f, Ball.Radius * 2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.Square;
            sr.color        = new Color(1f, 1f, 1f, 0.4f);
            sr.sortingOrder = 2;
            var ghost = go.AddComponent<GhostBall>();
            ghost.velocity = velocity;
            ghost.onDestroyed = () =>
            {
                _ghostCount[sideIdx]--;
                if (_ghostCount[sideIdx] <= 0)
                    _abilityActive[sideIdx] = false;
            };
            Destroy(go, 2f);
        }

        // ── Projectile spawning ──────────────────────────────────────────────────

        private void SpawnProjectile(Paddle from, Paddle target, float xDir, Color color,
                                     Action<Ball> onHitBall, Action<Paddle> onHitPaddle,
                                     float speed, AbilityType type, Action onDestroyed)
        {
            var go = new GameObject("Projectile");
            go.transform.position = from.transform.position;
            BuildProjectileVisual(go, type, color, xDir);
            var proj = go.AddComponent<Projectile>();
            proj.Initialize(_ball, target, new Vector2(xDir * speed, 0f),
                            onHitBall, onHitPaddle, onDestroyed);
        }

        // ── Projectile visuals ───────────────────────────────────────────────────

        private static void BuildProjectileVisual(GameObject root, AbilityType type, Color accent, float xDir)
        {
            Sprite sq = SpriteFactory.Square;
            switch (type)
            {
                case AbilityType.CoolWave:
                    // Golden ki-blast orb: two overlapping squares at 0° and 45° → star shape
                    AddChild(root, sq, accent,                        Vector3.zero, new Vector3(0.28f, 0.28f, 1f), 3,  0f);
                    AddChild(root, sq, accent,                        Vector3.zero, new Vector3(0.28f, 0.28f, 1f), 3, 45f);
                    AddChild(root, sq, new Color(1f, 1f, 0.7f, 0.9f),Vector3.zero, new Vector3(0.12f, 0.12f, 1f), 4,  0f);
                    break;

                case AbilityType.LightningBolt:
                    // Zigzag bolt: two diagonal bars + bright centre flash
                    AddChild(root, sq, accent, new Vector3( 0.07f * xDir,  0.07f, 0f), new Vector3(0.22f, 0.06f, 1f), 3,  45f);
                    AddChild(root, sq, accent, new Vector3(-0.07f * xDir, -0.07f, 0f), new Vector3(0.22f, 0.06f, 1f), 3, -45f);
                    AddChild(root, sq, new Color(1f, 1f, 1f, 0.9f), Vector3.zero,      new Vector3(0.08f, 0.08f, 1f), 4,   0f);
                    break;

                case AbilityType.IronShell:
                    // Cannonball: large body + highlight + dark leading edge
                    AddChild(root, sq, new Color(0.35f, 0.35f, 0.35f, 1f), Vector3.zero,                           new Vector3(0.34f, 0.34f, 1f), 3,  0f);
                    AddChild(root, sq, new Color(0.75f, 0.75f, 0.75f, 1f), new Vector3(-0.07f,  0.07f, 0f),        new Vector3(0.12f, 0.12f, 1f), 4,  0f);
                    AddChild(root, sq, new Color(0.10f, 0.10f, 0.10f, 1f), new Vector3( 0.08f * xDir, 0f, 0f),    new Vector3(0.10f, 0.34f, 1f), 4,  0f);
                    break;

                case AbilityType.GlitchBomb:
                    // Scattered pixel cluster in green/white/black
                    AddChild(root, sq, accent,                         Vector3.zero,                    new Vector3(0.14f, 0.14f, 1f), 3,  0f);
                    AddChild(root, sq, Color.white,                    new Vector3( 0.12f,  0.10f, 0f), new Vector3(0.08f, 0.08f, 1f), 3,  0f);
                    AddChild(root, sq, Color.black,                    new Vector3(-0.10f,  0.12f, 0f), new Vector3(0.07f, 0.07f, 1f), 4,  0f);
                    AddChild(root, sq, accent,                         new Vector3( 0.10f, -0.12f, 0f), new Vector3(0.07f, 0.07f, 1f), 3,  0f);
                    AddChild(root, sq, Color.white,                    new Vector3(-0.12f, -0.10f, 0f), new Vector3(0.06f, 0.06f, 1f), 3, 45f);
                    break;

                case AbilityType.Fireball:
                    // Core + tapering flame trail behind
                    AddChild(root, sq, new Color(1f, 0.35f, 0f, 1f),  Vector3.zero,                               new Vector3(0.26f, 0.26f, 1f), 3, 45f);
                    AddChild(root, sq, new Color(1f, 0.80f, 0f, 1f),  Vector3.zero,                               new Vector3(0.16f, 0.16f, 1f), 4,  0f);
                    AddChild(root, sq, new Color(1f, 0.45f, 0f, 0.8f),new Vector3(-0.20f * xDir,  0f,    0f),     new Vector3(0.16f, 0.20f, 1f), 3,  0f);
                    AddChild(root, sq, new Color(1f, 0.75f, 0f, 0.6f),new Vector3(-0.32f * xDir,  0.05f, 0f),    new Vector3(0.08f, 0.12f, 1f), 3,  0f);
                    AddChild(root, sq, new Color(1f, 0.75f, 0f, 0.6f),new Vector3(-0.32f * xDir, -0.05f, 0f),    new Vector3(0.08f, 0.12f, 1f), 3,  0f);
                    break;

                case AbilityType.IceShot:
                    // Diamond (rotated square) + forward and side spikes
                    AddChild(root, sq, accent,                         Vector3.zero,                             new Vector3(0.24f, 0.24f, 1f), 3, 45f);
                    AddChild(root, sq, new Color(1f, 1f, 1f, 0.8f),   Vector3.zero,                             new Vector3(0.12f, 0.12f, 1f), 4, 45f);
                    AddChild(root, sq, accent,                         new Vector3( 0.18f * xDir, 0f,     0f),  new Vector3(0.18f, 0.05f, 1f), 3,  0f);
                    AddChild(root, sq, accent,                         new Vector3( 0f,           0.16f,  0f),  new Vector3(0.05f, 0.18f, 1f), 3,  0f);
                    AddChild(root, sq, accent,                         new Vector3( 0f,          -0.16f,  0f),  new Vector3(0.05f, 0.18f, 1f), 3,  0f);
                    break;
            }
        }

        private static void AddChild(GameObject parent, Sprite sprite, Color color,
                                     Vector3 localPos, Vector3 localScale, int sortOrder, float zRot = 0f)
        {
            var go = new GameObject("v");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            if (zRot != 0f) go.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
            var sr        = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = color;
            sr.sortingOrder = sortOrder;
        }
    }
}
