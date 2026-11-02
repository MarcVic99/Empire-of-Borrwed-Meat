using UnityEditor;

// Phase entry points now validate the current rules; the original numeric balance was superseded.
public static class PhaseSixChecks
{
    [MenuItem("Frankenstein/Comprobar fase 6")]
    public static void Run() { PrototypeChecks.CampaignAndEffects(); }
}
