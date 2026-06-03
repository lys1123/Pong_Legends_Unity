namespace PongLegends
{
    // Photon RaiseEvent byte codes. Keep in sync across all machines.
    public static class NetEvent
    {
        // Game-state stream (unreliable, high frequency)
        public const byte BallState   = 1;  // host → others: Vector2 pos, Vector2 vel, bool inPlay
        public const byte P1PaddleY   = 2;  // host → others: float y
        public const byte P2PaddleY   = 3;  // client → others: float y

        // Game flow (reliable)
        public const byte SyncScore        = 10; // host → others: int p1Score, int p2Score
        public const byte GameOver         = 11; // host → others: bool p1Won, string winnerName
        public const byte ServeSignal      = 12; // host → others: int direction
        public const byte ReturnToLobby    = 13; // host → all

        // Ability events (reliable, host → others unless noted)
        public const byte RequestAbility   = 20; // client → host: int side
        public const byte SpawnProjectile  = 21; // host → others: int abilityType, float x, float y, float xDir
        public const byte SpawnGhostBalls  = 22; // host → others: float[] posX, posY, velX, velY
        public const byte PaparazziFlash   = 23; // host → others
        public const byte PaddleEffect     = 24; // host → others: int side, int effectType, float duration
        public const byte SpawnIronShield  = 25; // host → others: int side, float x, float y
        public const byte DestroyIronShield= 26; // host → others: int side

        // Lobby events (reliable)
        public const byte StartGame        = 30; // host → all: int p1CharIdx, int p2CharIdx
    }
}
