using UnityEngine;

namespace PongLegends
{
    [CreateAssetMenu(fileName = "SessionData", menuName = "Pong Legends/Session Data")]
    public class SessionData : ScriptableObject
    {
        public CharacterDefinition playerCharacter;
        public CharacterDefinition aiCharacter;

        // Multiplayer state — reset to Offline for local games
        public NetworkMode networkMode       = NetworkMode.Offline;
        public int         remoteCharacterIndex = -1; // set by LobbyManager before loading Game
    }
}
