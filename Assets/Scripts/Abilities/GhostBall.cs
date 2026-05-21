using System;
using UnityEngine;

namespace PongLegends
{
    public class GhostBall : MonoBehaviour
    {
        public Vector2 velocity;
        public Action  onDestroyed;

        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);

            float y = transform.position.y;
            if (y + Ball.Radius >= Ball.WorldHalfHeight)
            {
                velocity.y = -Mathf.Abs(velocity.y);
                transform.position = new Vector3(transform.position.x, Ball.WorldHalfHeight - Ball.Radius, 0f);
            }
            else if (y - Ball.Radius <= -Ball.WorldHalfHeight)
            {
                velocity.y = Mathf.Abs(velocity.y);
                transform.position = new Vector3(transform.position.x, -Ball.WorldHalfHeight + Ball.Radius, 0f);
            }
        }

        private void OnDestroy() => onDestroyed?.Invoke();
    }
}
