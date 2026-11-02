using UnityEditor;

// Phase entry points now validate the current rules; the original numeric balance was superseded.
public static class PhaseThreeChecks
{
    [MenuItem("Frankenstein/Comprobar fase 3")]
    public static void Run() { PrototypeChecks.AftermathAndTurns(); }
}
