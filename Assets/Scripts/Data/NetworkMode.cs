namespace PongLegends
{
    public enum NetworkMode
    {
        Offline,      // single-player vs AI (default)
        OnlineHost,   // Player 1 — runs ball physics, sends state
        OnlineClient, // Player 2 — sends paddle input only
        Spectator     // view-only
    }
}
