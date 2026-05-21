namespace PongLegends
{
    // Ordinal values are stored in ScriptableObject assets — order must not change.
    public enum AbilityType
    {
        Paparazzi     = 0,  // Johnny Pong  - cameras appear at corners for 1 s, then flash freezes both paddles for 1 s
        Uppercut      = 1,  // Ryu Pong     - launches nearby ball at sharp upward angle, 10× speed
        LightningBolt = 2,  // Tele-Pong    - instant projectile, teleports target
        IronShell     = 3,  // Tank Pong    - heavy projectile, deflects ball / shrinks paddle
        ShadowClone   = 4,  // Shadow Pong  - splits ball into 3 (no projectile)
        GlitchBomb    = 5,  // Pixel Pong   - glitch projectile, scrambles ball / inverts controls
        Fireball      = 6,  // Inferno Pong - fast projectile, speeds ball / shrinks paddle
        IceShot       = 7   // Frost Pong   - projectile, freezes whatever it first touches
    }

    public static class AbilityTypeExtensions
    {
        public static string DisplayName(this AbilityType type) => type switch
        {
            AbilityType.Paparazzi     => "Paparazzi",
            AbilityType.Uppercut      => "Uppercut",
            AbilityType.LightningBolt => "Lightning Bolt",
            AbilityType.IronShell     => "Iron Shell",
            AbilityType.ShadowClone   => "Shadow Clone",
            AbilityType.GlitchBomb    => "Glitch Bomb",
            AbilityType.Fireball      => "Fireball",
            AbilityType.IceShot       => "Ice Shot",
            _                         => type.ToString()
        };
    }
}
