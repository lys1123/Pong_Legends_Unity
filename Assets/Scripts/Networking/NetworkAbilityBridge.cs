using UnityEngine;

namespace PongLegends
{
    // Handles all ability-related network events in both directions:
    //   Client → Host:  SendKickToHost, SendAbilityRequest
    //   Host → Others:  Broadcast* methods called from AbilitySystem after local execution
#if PHOTON_UNITY_NETWORKING
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    public class NetworkAbilityBridge : MonoBehaviour
    {
        public static NetworkAbilityBridge Instance { get; private set; }

        private AbilitySystem _abilitySystem;
        private Paddle        _playerPaddle;
        private Paddle        _aiPaddle;
        private NetworkMode   _mode;

        public void Initialize(AbilitySystem sys, Paddle p1, Paddle p2, NetworkMode mode)
        {
            Instance       = this;
            _abilitySystem = sys;
            _playerPaddle  = p1;
            _aiPaddle      = p2;
            _mode          = mode;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ── Client → Host ───────────────────────────────────────────────────

        // Client presses SPACE — ask the host to execute their ability.
        public void SendAbilityRequest()
        {
            NetworkManager.RaiseOthers(NetEvent.RequestAbility,
                (int)PaddleSide.AI, reliable: true);
        }

        // Client detected a kick key — tell host which kick to apply on contact.
        public void SendKickToHost(KickType kick)
        {
            NetworkManager.RaiseOthers(NetEvent.RequestAbility,
                new object[] { "kick", (int)kick }, reliable: true);
        }

        // ── Host → Others (broadcast after local execution) ─────────────────

        public void BroadcastProjectile(AbilityType type, Vector2 startPos, float xDir, Color color)
        {
            NetworkManager.RaiseOthers(NetEvent.SpawnProjectile,
                new object[] { (int)type, startPos.x, startPos.y, xDir,
                               color.r, color.g, color.b }, reliable: true);
        }

        public void BroadcastGhostBalls(Vector2[] positions, Vector2[] velocities)
        {
            int n = positions.Length;
            var posX = new float[n]; var posY = new float[n];
            var velX = new float[n]; var velY = new float[n];
            for (int i = 0; i < n; i++)
            {
                posX[i] = positions[i].x; posY[i] = positions[i].y;
                velX[i] = velocities[i].x; velY[i] = velocities[i].y;
            }
            NetworkManager.RaiseOthers(NetEvent.SpawnGhostBalls,
                new object[] { posX, posY, velX, velY }, reliable: true);
        }

        public void BroadcastPaparazziFlash()
        {
            NetworkManager.RaiseOthers(NetEvent.PaparazziFlash, null, reliable: true);
        }

        public void BroadcastPaddleEffect(PaddleSide side, NetworkEffectType effect, float duration)
        {
            NetworkManager.RaiseOthers(NetEvent.PaddleEffect,
                new object[] { (int)side, (int)effect, duration }, reliable: true);
        }

        public void BroadcastSpawnIronShield(PaddleSide side, Vector2 pos)
        {
            NetworkManager.RaiseOthers(NetEvent.SpawnIronShield,
                new object[] { (int)side, pos.x, pos.y }, reliable: true);
        }

        public void BroadcastDestroyIronShield(PaddleSide side)
        {
            NetworkManager.RaiseOthers(NetEvent.DestroyIronShield, (int)side, reliable: true);
        }

        // ── Incoming event dispatch ─────────────────────────────────────────

        public void OnNetworkEvent(EventData ev)
        {
            switch (ev.Code)
            {
                case NetEvent.RequestAbility:
                    HandleAbilityRequest(ev.CustomData);
                    break;

                case NetEvent.SpawnProjectile when _mode != NetworkMode.OnlineHost:
                    HandleSpawnProjectile((object[])ev.CustomData);
                    break;

                case NetEvent.SpawnGhostBalls when _mode != NetworkMode.OnlineHost:
                    HandleSpawnGhostBalls((object[])ev.CustomData);
                    break;

                case NetEvent.PaparazziFlash when _mode != NetworkMode.OnlineHost:
                    _abilitySystem?.ClientRunPaparazziFlash();
                    break;

                case NetEvent.PaddleEffect when _mode != NetworkMode.OnlineHost:
                    HandlePaddleEffect((object[])ev.CustomData);
                    break;

                case NetEvent.SpawnIronShield when _mode != NetworkMode.OnlineHost:
                    HandleSpawnIronShield((object[])ev.CustomData);
                    break;

                case NetEvent.DestroyIronShield when _mode != NetworkMode.OnlineHost:
                    _abilitySystem?.ClientDestroyIronShield((PaddleSide)(int)ev.CustomData);
                    break;
            }
        }

        // ── Request handlers (host only) ────────────────────────────────────

        private void HandleAbilityRequest(object data)
        {
            if (_mode != NetworkMode.OnlineHost) return;

            if (data is object[] arr && arr.Length == 2 && (string)arr[0] == "kick")
            {
                // Incoming kick from client — apply to the AI (right) paddle.
                _aiPaddle?.ApplyNetworkKick((KickType)(int)arr[1]);
            }
            else
            {
                // SPACE from client — execute their ability on the host.
                _abilitySystem?.ActivateFromNetwork(PaddleSide.AI);
            }
        }

        // ── Visual-only handlers (client / spectator) ───────────────────────

        private void HandleSpawnProjectile(object[] data)
        {
            var type  = (AbilityType)(int)data[0];
            var pos   = new Vector3((float)data[1], (float)data[2], 0f);
            var xDir  = (float)data[3];
            var color = new Color((float)data[4], (float)data[5], (float)data[6]);
            _abilitySystem?.ClientSpawnProjectileVisual(type, pos, xDir, color);
        }

        private void HandleSpawnGhostBalls(object[] data)
        {
            var posX = (float[])data[0]; var posY = (float[])data[1];
            var velX = (float[])data[2]; var velY = (float[])data[3];
            int n    = posX.Length;
            var positions  = new Vector2[n];
            var velocities = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                positions[i]  = new Vector2(posX[i], posY[i]);
                velocities[i] = new Vector2(velX[i], velY[i]);
            }
            _abilitySystem?.ClientSpawnGhostBalls(positions, velocities);
        }

        private void HandlePaddleEffect(object[] data)
        {
            var side     = (PaddleSide)(int)data[0];
            var effect   = (NetworkEffectType)(int)data[1];
            var duration = (float)data[2];
            Paddle paddle = side == PaddleSide.Player ? _playerPaddle : _aiPaddle;
            if (paddle == null) return;

            switch (effect)
            {
                case NetworkEffectType.Freeze:        _abilitySystem?.StartCoroutine(paddle.FreezeCoroutine(duration));        break;
                case NetworkEffectType.SilentFreeze:  _abilitySystem?.StartCoroutine(paddle.SilentFreezeCoroutine(duration));  break;
                case NetworkEffectType.Shrink:        _abilitySystem?.StartCoroutine(paddle.ShrinkCoroutine(0.6f, duration));  break;
                case NetworkEffectType.Slow:          _abilitySystem?.StartCoroutine(paddle.SlowCoroutine(duration));          break;
                case NetworkEffectType.InvertControls:_abilitySystem?.StartCoroutine(paddle.InvertControlsCoroutine(duration));break;
            }
        }

        private void HandleSpawnIronShield(object[] data)
        {
            var side = (PaddleSide)(int)data[0];
            var pos  = new Vector3((float)data[1], (float)data[2], 0f);
            _abilitySystem?.ClientSpawnIronShieldVisual(side, pos);
        }
    }

#else
    public class NetworkAbilityBridge : UnityEngine.MonoBehaviour
    {
        public static NetworkAbilityBridge Instance { get; private set; }
        public void Initialize(AbilitySystem sys, Paddle p1, Paddle p2, NetworkMode mode)
            => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void SendAbilityRequest()                                                          { }
        public void SendKickToHost(KickType kick)                                                 { }
        public void BroadcastProjectile(AbilityType type, Vector2 startPos, float xDir, Color color) { }
        public void BroadcastGhostBalls(Vector2[] positions, Vector2[] velocities)               { }
        public void BroadcastPaparazziFlash()                                                     { }
        public void BroadcastPaddleEffect(PaddleSide side, NetworkEffectType effect, float dur)  { }
        public void BroadcastSpawnIronShield(PaddleSide side, Vector2 pos)                       { }
        public void BroadcastDestroyIronShield(PaddleSide side)                                  { }
        public void OnNetworkEvent(object ev)                                                     { }
    }
#endif
}
