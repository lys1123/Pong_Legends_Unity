using System;
using UnityEngine;

namespace PongLegends
{
    public class Projectile : MonoBehaviour
    {
        private Vector2        _velocity;
        private Ball           _ball;
        private Paddle         _targetPaddle;
        private Action<Ball>   _onHitBall;
        private Action<Paddle> _onHitPaddle;
        private Action         _onDestroyed;
        private const float    HitRadius = 0.08f;

        public void Initialize(Ball ball, Paddle targetPaddle, Vector2 velocity,
                               Action<Ball> onHitBall, Action<Paddle> onHitPaddle, Action onDestroyed)
        {
            _ball         = ball;
            _targetPaddle = targetPaddle;
            _velocity     = velocity;
            _onHitBall    = onHitBall;
            _onHitPaddle  = onHitPaddle;
            _onDestroyed  = onDestroyed;
        }

        private void Update()
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);

            if (Mathf.Abs(transform.position.x) > Ball.WorldHalfWidth + 1f)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 pos = transform.position;

            if (_ball != null && _ball.IsInPlay())
            {
                float dist = Vector2.Distance(pos, (Vector2)_ball.transform.position);
                if (dist < HitRadius + Ball.Radius)
                {
                    _onHitBall?.Invoke(_ball);
                    Destroy(gameObject);
                    return;
                }
            }

            if (_targetPaddle != null)
            {
                Bounds b = _targetPaddle.GetBounds();
                bool hit = pos.x + HitRadius > b.min.x && pos.x - HitRadius < b.max.x &&
                           pos.y + HitRadius > b.min.y && pos.y - HitRadius < b.max.y;
                if (hit)
                {
                    _onHitPaddle?.Invoke(_targetPaddle);
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy() => _onDestroyed?.Invoke();
    }
}
