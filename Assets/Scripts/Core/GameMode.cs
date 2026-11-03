// Which mode the main menu started. Survives the scene load because it is static.
// Opening Main Game directly in the editor runs the Demo.
public static class GameMode
{
    public enum Mode { Demo, Challenge }
    public static Mode Current = Mode.Demo;

    public static bool IsChallenge { get { return Current == Mode.Challenge; } }

    public const int MenuScene = 0;
    public const int GameScene = 1;
}
