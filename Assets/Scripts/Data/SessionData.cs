using UnityEngine;

namespace PongLegends
{
    [CreateAssetMenu(fileName = "SessionData", menuName = "Pong Legends/Session Data")]
    public class SessionData : ScriptableObject
    {
        public CharacterDefinition playerCharacter;
        public CharacterDefinition aiCharacter;
    }
}
