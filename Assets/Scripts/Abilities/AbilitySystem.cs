using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PongLegends
{
    public class AbilitySystem : MonoBehaviour
    {
        private SessionData _session;
        private Ball        _ball;
        private Paddle      _playerPaddle;
        private Paddle      _aiPaddle;
        private NetworkMode _networkMode;

        // True while the ability effect is still active (projectile on screen / ghost balls alive).
        private readonly bool[] _abilityActive = new bool[2];
        private readonly int[]  _ghostCount    = new int[2];

        private float _aiCooldown;
        private bool  _flashActive;
        private const float AICooldownMin = 3f;
        private const float AICooldownMax = 6f;

        private readonly GameObject[] _ironShields       = new GameObject[2];
        private readonly GameObject[] _visualIronShields = new GameObject[2];

        public void Initialize(SessionData session, Ball ball, Paddle playerPaddle, Paddle aiPaddle)
        {
            _session      = session;
            _ball         = ball;
            _playerPaddle = playerPaddle;
            _aiPaddle     = aiPaddle;
            _networkMode  = session.networkMode;

            ResetAICooldown();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                if (_networkMode == NetworkMode.OnlineClient)
                {
                    if (NetworkAbilityBridge.Instance != null)
                        NetworkAbilityBridge.Instance.SendAbilityRequest();
                }
                else
                    Activate(PaddleSide.Player);
            }

            // AI auto-fires only in offline mode; in online mode the "AI" side is a real player
            if (_networkMode == NetworkMode.Offline && !_flashActive)
            {
                _aiCooldown -= Time.deltaTime;
                if (_aiCooldown <= 0f)
                {
                    Activate(PaddleSide.AI);
                    ResetAICooldown();
                }
            }
        }

        private void ResetAICooldown() =>
            _aiCooldown = UnityEngine.Random.Range(AICooldownMin, AICooldownMax);

        // ── Activation ───────────────────────────────────────────────────────────

        private void Activate(PaddleSide side)
        {
            int idx = (int)side;
            CharacterDefinition def = side == PaddleSide.Player
                ? _session.playerCharacter
                : _session.aiCharacter;

            // IronDefense bypasses the cooldown lock — reactivation replaces the existing shield
            if (def.abilityType == AbilityType.IronShell)
            {
                SoundManager.Play("ability_ironshield");
                ExecuteIronDefense(side);
                if (_networkMode == NetworkMode.OnlineHost && NetworkAbilityBridge.Instance != null)
                    NetworkAbilityBridge.Instance.BroadcastSpawnIronShield(
                        side, _ironShields[idx].transform.position);
                return;
            }

            if (_abilityActive[idx]) return;   // already in use — do nothing
            _abilityActive[idx] = true;

            if (def.abilityType == AbilityType.ShadowClone)
            {
                if (!_ball.IsInPlay()) { _abilityActive[idx] = false; return; }
                SoundManager.Play("ability_shadowclone");
                SpawnClones(side);
                return;
            }

            if (def.abilityType == AbilityType.Uppercut)
            {
                ExecuteUppercut(side); // sound plays inside after range check
                return;
            }

            if (def.abilityType == AbilityType.Paparazzi)
            {
                SoundManager.Play("ability_paparazzi");
                ExecutePaparazzi(side);
                return;
            }

            // Projectile abilities
            SoundManager.Play(AbilitySoundName(def.abilityType));

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            Paddle oppPaddle = side == PaddleSide.Player ? _aiPaddle     : _playerPaddle;
            float  xDir      = side == PaddleSide.Player ? 1f            : -1f;

            var (onHitBall, onHitPaddle) = BuildEffect(def.abilityType);
            float speed                  = ProjectileSpeed(def.abilityType);

            Vector2 spawnPos = ownPaddle.transform.position;
            SpawnProjectile(ownPaddle, oppPaddle, xDir, def.accentColor,
                            onHitBall, onHitPaddle, speed, def.abilityType,
                            () => _abilityActive[idx] = false);

            if (_networkMode == NetworkMode.OnlineHost && NetworkAbilityBridge.Instance != null)
                NetworkAbilityBridge.Instance.BroadcastProjectile(
                    def.abilityType, spawnPos, xDir, def.accentColor);
        }

        // Called on host when client requests ability activation via network
        public void ActivateFromNetwork(PaddleSide side) => Activate(side);

        // ── Effect factory ───────────────────────────────────────────────────────

        private (Action<Ball>, Action<Paddle>) BuildEffect(AbilityType type)
        {
            switch (type)
            {
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
                        paddle =>
                        {
                            StartCoroutine(paddle.InvertControlsCoroutine(2f));
                            if (_networkMode == NetworkMode.OnlineHost)
                                BroadcastNetPaddleEffect(paddle, NetworkEffectType.InvertControls, 2f);
                        }
                    );

                case AbilityType.Fireball:
                    return (
                        ball   => ball.MultiplySpeed(1.5f),
                        paddle =>
                        {
                            StartCoroutine(paddle.ShrinkCoroutine(0.6f, 2f));
                            if (_networkMode == NetworkMode.OnlineHost)
                                BroadcastNetPaddleEffect(paddle, NetworkEffectType.Shrink, 2f);
                        }
                    );

                case AbilityType.IceShot:
                    return (
                        ball   => StartCoroutine(ball.FreezeCoroutine(1.5f)),
                        paddle =>
                        {
                            StartCoroutine(paddle.FreezeCoroutine(1.5f));
                            if (_networkMode == NetworkMode.OnlineHost)
                                BroadcastNetPaddleEffect(paddle, NetworkEffectType.Freeze, 1.5f);
                        }
                    );

                default:
                    return (null, null);
            }
        }

        private void BroadcastNetPaddleEffect(Paddle paddle, NetworkEffectType effect, float duration)
        {
            if (NetworkAbilityBridge.Instance == null) return;
            PaddleSide side = paddle == _playerPaddle ? PaddleSide.Player : PaddleSide.AI;
            NetworkAbilityBridge.Instance.BroadcastPaddleEffect(side, effect, duration);
        }

        private static string AbilitySoundName(AbilityType type) => type switch
        {
            AbilityType.LightningBolt => "ability_lightning",
            AbilityType.GlitchBomb    => "ability_glitch",
            AbilityType.Fireball      => "ability_fireball",
            AbilityType.IceShot       => "ability_ice",
            _                         => null
        };

        private static float ProjectileSpeed(AbilityType type) => type switch
        {
            AbilityType.LightningBolt => 9f,
            AbilityType.GlitchBomb    => 6f,
            AbilityType.Fireball      => 7f,
            AbilityType.IceShot       => 7f,
            _                         => 5f
        };

        // ── Paparazzi ────────────────────────────────────────────────────────────

        private void ExecutePaparazzi(PaddleSide side)
        {
            StartCoroutine(RunPaparazzi((int)side));
        }

        private static readonly WaitForSeconds _waitCameraPhase = new(1f);

        private IEnumerator RunPaparazzi(int idx)
        {
            GameObject[] cameras = SpawnCornerCameras();

            yield return _waitCameraPhase;

            foreach (var cam in cameras)
                Destroy(cam);

            _flashActive = true;
            Paddle opponent = idx == (int)PaddleSide.Player ? _aiPaddle : _playerPaddle;
            PaddleSide opponentSide = idx == (int)PaddleSide.Player ? PaddleSide.AI : PaddleSide.Player;
            StartCoroutine(opponent.SilentFreezeCoroutine(0.5f));

            if (_networkMode == NetworkMode.OnlineHost && NetworkAbilityBridge.Instance != null)
            {
                NetworkAbilityBridge.Instance.BroadcastPaparazziFlash();
                NetworkAbilityBridge.Instance.BroadcastPaddleEffect(
                    opponentSide, NetworkEffectType.SilentFreeze, 0.5f);
            }

            yield return StartCoroutine(StrobeFlash(FindAnyObjectByType<Canvas>()));

            _flashActive        = false;
            _abilityActive[idx] = false;
        }

        // Called on client via network broadcast — run the flash visual locally
        public void ClientRunPaparazziFlash()
        {
            StartCoroutine(StrobeFlash(FindAnyObjectByType<Canvas>()));
        }

        private static readonly Color         _flashWhite         = Color.white;
        private static readonly Color         _flashYellow        = new(1f, 0.95f, 0.55f);
        private static readonly Color         _flashClear         = Color.clear;
        private static readonly WaitForSeconds _waitFlashFallback  = new(0.5f);

        private IEnumerator StrobeFlash(Canvas canvas)
        {
            if (canvas == null) { yield return _waitFlashFallback; yield break; }

            var go = new GameObject("FlashOverlay");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();

            // Pattern totals exactly 0.50 s
            (Color col, float dur)[] pattern =
            {
                (_flashWhite,  0.08f),
                (_flashClear,  0.04f),
                (_flashYellow, 0.07f),
                (_flashClear,  0.04f),
                (_flashWhite,  0.08f),
                (_flashClear,  0.04f),
                (_flashYellow, 0.06f),
                (_flashClear,  0.03f),
                (_flashWhite,  0.06f),
            };

            foreach (var (col, dur) in pattern)
            {
                img.color = col;
                yield return new WaitForSeconds(dur);
            }

            Destroy(go);
        }

        private static GameObject[] SpawnCornerCameras()
        {
            Sprite camSprite = Resources.Load<Sprite>("PaparazziCamera");
            if (camSprite == null)
                Debug.LogWarning("Paparazzi: sprite not found — place PaparazziCamera.png in Assets/Resources/ and set Texture Type to Sprite.");

            float cx = Ball.WorldHalfWidth  - 0.5f;
            float cy = Ball.WorldHalfHeight - 0.45f;
            Vector2[] positions = {
                new(-cx,  cy), new( cx,  cy),
                new(-cx, -cy), new( cx, -cy)
            };

            var cameras = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("PaparazziCamera");
                go.transform.position = new Vector3(positions[i].x, positions[i].y, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = camSprite;
                sr.sortingOrder = 3;

                if (camSprite != null)
                {
                    float baseScale = 0.9f / camSprite.bounds.size.x;
                    float xScale    = positions[i].x > 0 ? -baseScale : baseScale;
                    go.transform.localScale = new Vector3(xScale, baseScale, 1f);
                }

                cameras[i] = go;
            }
            return cameras;
        }

        // ── Iron Defense ─────────────────────────────────────────────────────────

        private const float ShieldOffset = 0.6f; // world units in front of the paddle center

        private void ExecuteIronDefense(PaddleSide side)
        {
            int idx = (int)side;

            if (_ironShields[idx] != null)
                Destroy(_ironShields[idx]);

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            float  xDir      = side == PaddleSide.Player ? 1f            : -1f;

            CharacterDefinition def = side == PaddleSide.Player
                ? _session.playerCharacter
                : _session.aiCharacter;

            float   paddleWidth  = ownPaddle.GetBounds().size.x;
            float   paddleHeight = ownPaddle.GetBounds().size.y;
            Vector3 pos          = ownPaddle.transform.position + new Vector3(xDir * ShieldOffset, 0f, 0f);

            var go     = new GameObject("IronShield");
            var shield = go.AddComponent<IronShield>();
            shield.Initialize(_ball, side, pos, paddleWidth, paddleHeight, def.paddleColor, def.accentColor);
            _ironShields[idx] = go;
        }

        private void OnDisable()
        {
            for (int i = 0; i < _ironShields.Length; i++)
            {
                if (_ironShields[i] != null)
                {
                    if (_networkMode == NetworkMode.OnlineHost && NetworkAbilityBridge.Instance != null)
                        NetworkAbilityBridge.Instance.BroadcastDestroyIronShield((PaddleSide)i);
                    Destroy(_ironShields[i]);
                    _ironShields[i] = null;
                }
            }
            for (int i = 0; i < _visualIronShields.Length; i++)
            {
                if (_visualIronShields[i] != null)
                {
                    Destroy(_visualIronShields[i]);
                    _visualIronShields[i] = null;
                }
            }
        }

        // ── Client-side visual methods (called on non-host machines) ─────────────

        public void ClientSpawnProjectileVisual(AbilityType type, Vector3 startPos, float xDir, Color color)
        {
            var go = new GameObject("ProjectileVisual");
            go.transform.position = startPos;
            BuildProjectileVisual(go, type, color, xDir);
            float speed = ProjectileSpeed(type);
            StartCoroutine(MoveProjectileVisual(go, new Vector2(xDir * speed, 0f)));
        }

        private static IEnumerator MoveProjectileVisual(GameObject go, Vector2 velocity)
        {
            float elapsed = 0f;
            while (go != null && elapsed < 5f)
            {
                go.transform.position += (Vector3)(velocity * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        public void ClientSpawnGhostBalls(Vector2[] positions, Vector2[] velocities)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject("GhostBallVisual");
                go.transform.position   = new Vector3(positions[i].x, positions[i].y, 0f);
                go.transform.localScale = new Vector3(Ball.Radius * 2f, Ball.Radius * 2f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = SpriteFactory.Square;
                sr.color        = Color.white;
                sr.sortingOrder = 2;
                var ghost = go.AddComponent<GhostBall>();
                ghost.velocity = velocities[i];
                Destroy(go, 2f);
            }
        }

        public void ClientSpawnIronShieldVisual(PaddleSide side, Vector3 pos)
        {
            int idx = (int)side;
            if (_visualIronShields[idx] != null)
                Destroy(_visualIronShields[idx]);

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            CharacterDefinition def = side == PaddleSide.Player
                ? _session.playerCharacter : _session.aiCharacter;

            var go = new GameObject("IronShieldVisual");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.Square;
            sr.color        = def.accentColor;
            sr.sortingOrder = 3;
            go.transform.localScale = new Vector3(
                ownPaddle.GetBounds().size.x * 0.5f,
                ownPaddle.GetBounds().size.y, 1f);

            _visualIronShields[idx] = go;
        }

        public void ClientDestroyIronShield(PaddleSide side)
        {
            int idx = (int)side;
            if (_visualIronShields[idx] != null)
            {
                Destroy(_visualIronShields[idx]);
                _visualIronShields[idx] = null;
            }
        }

        // ── Uppercut ─────────────────────────────────────────────────────────────

        private void ExecuteUppercut(PaddleSide side)
        {
            int idx = (int)side;

            if (!_ball.IsInPlay()) { _abilityActive[idx] = false; return; }

            Paddle ownPaddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            float  xDir      = side == PaddleSide.Player ? 1f : -1f;

            // Ball must be within kick range: paddle bounds expanded by a small buffer,
            // matching the contact zone where A/S/D kicks would apply.
            const float UppercutBuffer = 0.35f;
            Vector2 ballPos = _ball.transform.position;
            Bounds  pb      = ownPaddle.GetBounds();
            bool    inRange = ballPos.x + Ball.Radius > pb.min.x - UppercutBuffer
                           && ballPos.x - Ball.Radius < pb.max.x + UppercutBuffer
                           && ballPos.y + Ball.Radius > pb.min.y - UppercutBuffer
                           && ballPos.y - Ball.Radius < pb.max.y + UppercutBuffer;

            if (!inRange) { _abilityActive[idx] = false; return; }

            SoundManager.Play("ability_uppercut");

            // Launch at 45° upward at 2× current speed — powerful but readable enough to defend.
            float   launchSpeed = Mathf.Max(_ball.GetVelocity().magnitude, Ball.InitialSpeed) * 2f;
            float   rad         = 45f * Mathf.Deg2Rad;
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
            // Ghosts are visually distinct only to the caster (the player).
            // When the AI casts, all balls look identical so the player is deceived.
            bool ghostsVisibleToCaster = side == PaddleSide.Player;
            var allTransforms = new Transform[3];

            var ghostPositions  = new System.Collections.Generic.List<Vector2>();
            var ghostVelocities = new System.Collections.Generic.List<Vector2>();

            for (int i = 0; i < 3; i++)
            {
                Vector2 v = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i])) * speed;
                if (i == realIdx)
                {
                    _ball.SetVelocity(v);
                    allTransforms[i] = _ball.transform;
                }
                else
                {
                    Vector2 ghostVel = v * 0.9f;
                    _ghostCount[idx]++;
                    allTransforms[i] = SpawnGhost(_ball.transform.position, ghostVel, idx, ghostsVisibleToCaster).transform;
                    ghostPositions.Add(_ball.transform.position);
                    ghostVelocities.Add(ghostVel);
                }
            }

            // When the player uses Shadow Clone, send the AI after a randomly chosen ball.
            // The AI's override auto-clears (Unity null) when a ghost GO is destroyed,
            // falling back to normal ball tracking at that point.
            if (side == PaddleSide.Player)
            {
                int aiPick = UnityEngine.Random.Range(0, 3);
                _aiPaddle.SetTrackingTarget(allTransforms[aiPick]);
            }

            if (_networkMode == NetworkMode.OnlineHost && NetworkAbilityBridge.Instance != null)
                NetworkAbilityBridge.Instance.BroadcastGhostBalls(
                    ghostPositions.ToArray(), ghostVelocities.ToArray());
        }

        private static readonly Color _ghostVisibleColor = new(1f, 1f, 1f, 0.3f);

        private GameObject SpawnGhost(Vector3 position, Vector2 velocity, int sideIdx, bool visibleToCaster)
        {
            var go = new GameObject("GhostBall");
            go.transform.position   = position;
            go.transform.localScale = new Vector3(Ball.Radius * 2f, Ball.Radius * 2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.Square;
            sr.color        = visibleToCaster ? _ghostVisibleColor : Color.white;
            sr.sortingOrder = 2;
            var ghost = go.AddComponent<GhostBall>();
            ghost.velocity = velocity;
            ghost.onDestroyed = () =>
            {
                _ghostCount[sideIdx]--;
                if (_ghostCount[sideIdx] <= 0)
                {
                    _abilityActive[sideIdx] = false;
                    if (sideIdx == (int)PaddleSide.Player)
                        _aiPaddle.SetTrackingTarget(null);
                }
            };
            Destroy(go, 2f);
            return go;
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
