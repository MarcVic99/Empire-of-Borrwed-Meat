using UnityEditor;
public static class FeedbackChecks
{
    [MenuItem("Frankenstein/Comprobar prototipo completo")]
    public static void RunAll() { PrototypeChecks.RunAll(); }
    [MenuItem("Frankenstein/Comprobar habilidades y negociación en combate")]
    public static void Run() { PrototypeChecks.AnatomyAndActions(); PrototypeChecks.AccuracyAndSeduction(); PrototypeChecks.NegotiationAndGold(); }
}
