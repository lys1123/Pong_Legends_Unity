using UnityEngine;

namespace PongLegends
{
    // Placed on the CenterLine parent in the Game scene.
    // Assigns SpriteFactory.Square to all child SpriteRenderers at runtime,
    // since procedural sprites cannot be saved in the scene asset.
    public class CenterLine : MonoBehaviour
    {
        private void Awake()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                if (sr.sprite == null)
                    sr.sprite = SpriteFactory.Square;
        }
    }
}
