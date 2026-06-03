using UnityEngine;

namespace PongLegends
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    // Attached to the same scene as the Ball. Registered with NetworkManager on init.
    // Host streams ball state at ~20 Hz; clients lerp toward received position.
    public class BallNetworkSync : MonoBehaviour
    {
        public static BallNetworkSync Instance { get; private set; }

        private Ball       _ball;
        private NetworkMode _mode;
        private float       _sendTimer;
        private const float SendInterval = 0.05f; // 20 Hz

        public void Initialize(Ball ball, NetworkMode mode)
        {
            Instance = this;
            _ball = ball;
            _mode = mode;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_mode != NetworkMode.OnlineHost) return;
            _sendTimer += Time.deltaTime;
            if (_sendTimer < SendInterval) return;
            _sendTimer = 0f;

            var data = new object[]
            {
                (Vector2)_ball.transform.position,
                _ball.GetVelocity(),
                _ball.IsInPlay()
            };
            NetworkManager.RaiseOthers(NetEvent.BallState, data, reliable: false);
        }

        // Called by NetworkManager.OnEvent for all incoming events.
        public void OnNetworkEvent(EventData ev)
        {
            if (ev.Code != NetEvent.BallState) return;
            if (_mode == NetworkMode.OnlineHost) return; // host ignores its own echoes

            var data   = (object[])ev.CustomData;
            var pos    = (Vector2)data[0];
            var vel    = (Vector2)data[1];
            var inPlay = (bool)data[2];
            _ball.SetNetworkState(pos, vel, inPlay);
        }
    }

#else
    public class BallNetworkSync : UnityEngine.MonoBehaviour
    {
        public static BallNetworkSync Instance { get; private set; }
        public void Initialize(Ball ball, NetworkMode mode) { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
#if !PHOTON_UNITY_NETWORKING
        public void OnNetworkEvent(object ev) { }
#endif
    }
#endif
}
