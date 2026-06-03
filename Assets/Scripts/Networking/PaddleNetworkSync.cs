using UnityEngine;

namespace PongLegends
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    // One instance per paddle. Dispatches events via the static registry so
    // NetworkManager.OnEvent can forward without knowing which side is which.
    public class PaddleNetworkSync : MonoBehaviour
    {
        // Static registry so NetworkManager can dispatch without scene references.
        private static PaddleNetworkSync _p1Sync;
        private static PaddleNetworkSync _p2Sync;

        private Paddle      _paddle;
        private PaddleSide  _side;
        private NetworkMode _mode;
        private float        _sendTimer;
        private const float  SendInterval = 0.05f; // 20 Hz

        public void Initialize(Paddle paddle, PaddleSide side, NetworkMode mode)
        {
            _paddle = paddle;
            _side   = side;
            _mode   = mode;

            if (side == PaddleSide.Player) _p1Sync = this;
            else                           _p2Sync = this;
        }

        private void OnDestroy()
        {
            if (_p1Sync == this) _p1Sync = null;
            if (_p2Sync == this) _p2Sync = null;
        }

        private void Update()
        {
            bool shouldSend =
                (_side == PaddleSide.Player && _mode == NetworkMode.OnlineHost) ||
                (_side == PaddleSide.AI     && _mode == NetworkMode.OnlineClient);

            if (!shouldSend) return;

            _sendTimer += Time.deltaTime;
            if (_sendTimer < SendInterval) return;
            _sendTimer = 0f;

            byte code = _side == PaddleSide.Player ? NetEvent.P1PaddleY : NetEvent.P2PaddleY;
            NetworkManager.RaiseOthers(code, _paddle.transform.position.y, reliable: false);

            // Send any pending kick to the host (client only).
            if (_mode == NetworkMode.OnlineClient)
            {
                KickType kick = _paddle.ConsumeKickInput();
                if (kick != KickType.None)
                    NetworkAbilityBridge.Instance?.SendKickToHost(kick);
            }
        }

        // Called by NetworkManager.OnEvent — dispatches to the right instance.
        public static void DispatchEvent(EventData ev)
        {
            if (ev.Code == NetEvent.P1PaddleY)
            {
                float y = (float)ev.CustomData;
                _p1Sync?._paddle.SetNetworkY(y);
            }
            else if (ev.Code == NetEvent.P2PaddleY)
            {
                float y = (float)ev.CustomData;
                _p2Sync?._paddle.SetNetworkY(y);
            }
        }
    }

#else
    public class PaddleNetworkSync : UnityEngine.MonoBehaviour
    {
        public void Initialize(Paddle paddle, PaddleSide side, NetworkMode mode) { }
        public static void DispatchEvent(object ev) { }
    }
#endif
}
