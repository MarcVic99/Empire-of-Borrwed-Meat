using UnityEditor;

// Phase entry points now validate the current rules; the original numeric balance was superseded.
public static class PhaseFourChecks
{
    [MenuItem("Frankenstein/Comprobar fase 4")]
    public static void Run() { PrototypeChecks.NegotiationAndGold(); }
}
