using UnityEditor;

// Phase entry points now validate the current rules; the original numeric balance was superseded.
public static class PhaseOneChecks
{
    [MenuItem("Frankenstein/Comprobar fase 1")]
    public static void Run() { PrototypeChecks.AnatomyAndActions(); }
}
