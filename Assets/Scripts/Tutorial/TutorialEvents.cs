public static class TutorialEvents
{
    public static event System.Action<string> Raised;
    public static void Raise(string id) { if (Raised != null) Raised(id); }
}