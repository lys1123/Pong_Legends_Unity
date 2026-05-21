using UnityEngine;

namespace PongLegends
{
    [CreateAssetMenu(fileName = "Character", menuName = "Pong Legends/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        public string characterName;
        public Color paddleColor = Color.white;
        public Color accentColor = Color.gray;
        public AbilityType abilityType;
        public float paddleHeightMultiplier = 1f;
        public VisualFeature visualFeature;
    }
}
