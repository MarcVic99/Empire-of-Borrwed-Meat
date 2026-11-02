using UnityEditor;

// Phase entry points now validate the current rules; the original numeric balance was superseded.
public static class PhaseFiveChecks
{
    [MenuItem("Frankenstein/Comprobar fase 5")]
    public static void Run() { PrototypeChecks.InventoryAndSewing(); }
}
