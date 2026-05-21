namespace PongLegends
{
    // Ordinal values are stored in ScriptableObject assets — order must not change.
    public enum AbilityType
    {
        CoolWave      = 0,  // Johnny Pong  - slow projectile, slows target
        Uppercut      = 1,  // Ryu Pong     - launches nearby ball at sharp upward angle, 10× speed
        LightningBolt = 2,  // Electra Pong - instant projectile, teleports target
        IronShell     = 3,  // Tank Pong    - heavy projectile, deflects ball / shrinks paddle
        ShadowClone   = 4,  // Shadow Pong  - splits ball into 3 (no projectile)
        GlitchBomb    = 5,  // Pixel Pong   - glitch projectile, scrambles ball / inverts controls
        Fireball      = 6,  // Inferno Pong - fast projectile, speeds ball / shrinks paddle
        IceShot       = 7   // Frost Pong   - projectile, freezes whatever it first touches
    }
}
