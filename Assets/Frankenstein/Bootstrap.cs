using System;
using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;
public sealed partial class Bootstrap : MonoBehaviour
{
    [SerializeField] private int seed = Rules.DefaultSeed;
    public Campaign Run { get; private set; }
    public Combat Battle => Run?.Battle;
    private enum Menu { Main, Skills, AttackTarget, Parts, TalkTarget, Talk, Offer, Aftermath, Corpse, Inventory, Log, Throw, ExecutionActor, ItemTarget, TalkActor }
    private Menu menu;
    private Combat observed;
    private CombatAbility ability;
    private Creature target, member, executionTarget;
    private BodyPiece graft;
    private GearItem selectedGear;
    private Vector2 scroll;
    private float enemyAt;
    private GUIStyle label, small, heading, button, centered;
    private int fontSize;
    private Profession selectedClass = Profession.Hunter;
    private Creature hunterPreview, dancerPreview, grafterPreview;
    private readonly Color muted = new Color(.62f, .65f, .62f);
    private bool PlayerTurn => Battle != null && !Battle.IsOver && Battle.Current != null && Battle.Current.IsPlayer;
    private Combat.SpokenLine spoken;
    private float speechStarted, speechUntil;
    private readonly GameInput controls = new GameInput();
    private bool clearGuiFocus;
    private void OnApplicationFocus(bool focused) { if (!focused) controls.Reset(); }
    private void OnApplicationPause(bool paused) { if (paused) controls.Reset(); }
    private void OnDisable() { controls.Reset(); }
    private void Awake() { EnsureDisplayCamera(); Application.runInBackground = true; Restart(); }
    private void EnsureDisplayCamera()
    {
        if (UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Any(c => c.isActiveAndEnabled && c.targetDisplay == 0 && c.targetTexture == null)) return;
        var o = new GameObject("Presentation Camera"); o.transform.SetParent(transform, false); o.transform.localPosition = new Vector3(0, 0, -10);
        var camera = o.AddComponent<Camera>(); camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BattlePainter.Ink; camera.cullingMask = 0; camera.depth = -100;
    }
    private void Restart()
    {
        controls.Reset(); clearGuiFocus = true;
        seed = Guid.NewGuid().GetHashCode() & int.MaxValue; worldMenu = puzzleMenu = false; worldTab = 0;
        Run = null; observed = null; spoken = null; scroll = Vector2.zero; member = null; selectedGear = null;
        hunterPreview = Content.Hero(Profession.Hunter); dancerPreview = Content.Hero(Profession.Dancer); grafterPreview = Content.Hero(Profession.Grafter);
    }
    private void StartRun(Profession profession)
    { controls.Reset(); clearGuiFocus = true; worldMenu = puzzleMenu = false; selectedClass = profession; Run = new Campaign(seed, profession); observed = null; worldCamera = Vector2.zero; SyncBattle(); }
    private void SyncBattle()
    {
        if (observed == Battle) return;
        observed = Battle;
        ResetBattleMotion();
        if (Battle != null)
        {
            Battle.DeferTurnsForDialogue = true;
            Battle.PresentAttack = BeginStrike;
            Battle.DamagePresented = ShowDamage;
        } ability = null; target = executionTarget = null; graft = null; spoken = null;
        menu = Menu.Main; scroll = Vector2.zero; enemyAt = Time.unscaledTime + .9f;
    }
    private bool UpdateSpeech()
    {
        bool skip = (controls.Pressed & GameButton.Continue) != 0;
        if (spoken != null && (Time.unscaledTime >= speechUntil || skip && Time.unscaledTime - speechStarted > .9f)) spoken = null;
        if (spoken == null)
        {
            Battle.ResumeAfterDialogue();
            spoken = Battle.NextDialogue();
            if (spoken != null) { speechStarted = Time.unscaledTime; speechUntil = speechStarted + Mathf.Clamp(1.5f + spoken.Text.Length * .038f, 2.4f, 5); }
        }
        return spoken != null;
    }
    private void Update()
    {
        controls.Poll();
        if (Run == null) return;
        if (Run.State == CampaignState.Exploring) { spoken = null; UpdateExploration(); return; }
        SyncBattle();
        UpdateBattleCamera();
        if (UpdateStrike()) return;
        if (Battle != null && UpdateSpeech()) return;
        if (cameraZoom > 1.004f) return;
        RefreshPresentation();
        if (Run.State == CampaignState.Exploring) { worldCamera = new Vector2((float)Run.Dungeon.X, (float)Run.Dungeon.Y); return; }
        if (Battle != null && !Battle.IsOver && !PlayerTurn && Time.unscaledTime >= enemyAt && menu != Menu.Log && menu != Menu.Inventory)
        { Battle.StepEnemy(); enemyAt = Time.unscaledTime + .9f; menu = Menu.Main; }
    }
    private void RefreshPresentation()
    {
        SyncBattle(); Run.UpdateState();
        if (Run.State == CampaignState.Exploring || Battle == null) return;
        if (Battle.IsOver && menu != Menu.Aftermath && menu != Menu.Corpse && menu != Menu.Talk && menu != Menu.Offer && menu != Menu.Log && menu != Menu.Inventory && menu != Menu.ExecutionActor && menu != Menu.TalkActor)
            menu = Menu.Aftermath;
        if (!Battle.IsOver && !PlayerTurn && menu != Menu.Log && menu != Menu.Inventory) menu = Menu.Main;
    }
    private void Styles()
    {
        int size = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .022f), 12, 19);
        if (label != null && fontSize == size) return; fontSize = size;
        label = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true, alignment = TextAnchor.MiddleLeft }; label.normal.textColor = BattlePainter.Paper;
        small = new GUIStyle(label) { fontSize = Mathf.Max(10, size - 3) };
        heading = new GUIStyle(label) { fontSize = size + 5, fontStyle = FontStyle.Bold };
        button = new GUIStyle(label) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 9, 4, 4) };
        centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
    }
    private void OnGUI()
    {
        if (clearGuiFocus) { GUI.FocusControl(null); GUIUtility.keyboardControl = 0; clearGuiFocus = false; }
        var inputEvent = Event.current;
        if (Run != null && GameInput.Map(inputEvent.keyCode) != GameButton.None && (inputEvent.type == EventType.KeyDown || inputEvent.type == EventType.KeyUp))
        { controls.Capture(inputEvent.keyCode, inputEvent.type == EventType.KeyDown); inputEvent.Use(); }
        Styles(); BattlePainter.Fill(new Rect(0, 0, Screen.width, Screen.height), BattlePainter.Ink);
        if (Run == null) { DrawSelection(new Rect(16, 12, Screen.width - 32, Screen.height - 24)); return; }
        if (Run.State == CampaignState.Exploring) { DrawDungeon(); return; }

        if (Run.IsFinished) { DrawFinal(new Rect(20, 20, Screen.width - 40, Screen.height - 40)); return; }
        if (Run.State == CampaignState.Exploring) { DrawDungeon(); return; }
        DrawArena(new Rect(0, 0, Screen.width, Screen.height));
        DrawAllyVitals();
        if (spoken != null) { DrawFocusedSpeech(); return; }
        if (BattleInputLocked) return;
        Text(new Rect(20, 8, Screen.width * .65f, 30), $"Piso {Run.Dungeon.Floor} · Ronda {Battle.Round}", small, muted);
        if (Btn(new Rect(Screen.width - 122, 10, 102, 30), "Registro")) Go(Menu.Log);
        if (menu == Menu.Inventory || menu == Menu.Log || menu == Menu.Offer) DrawBattleOverlay();
        else DrawBattleMenus();
    }
    private void Text(Rect r, string value, GUIStyle style = null, Color? color = null)
    {
        style = style ?? label; var old = style.normal.textColor; var oldContent = GUI.contentColor;
        style.normal.textColor = color ?? BattlePainter.Paper; GUI.contentColor = Color.white; GUI.Label(r, value, style); style.normal.textColor = old; GUI.contentColor = oldContent;
    }
    private bool Btn(Rect r, string value, bool enabled = true, bool emphasis = false)
    { button.fontSize = r.width < 150 ? small.fontSize : fontSize; return ButtonSkin.Draw(r, value, enabled, emphasis, button); }
    private bool ItemButton(Rect r, string id, string value, bool enabled = true, bool selected = false)
    {
        bool clicked = Btn(r, "", enabled, selected);
        float size = Mathf.Min(46, r.height - 8);
        ImperialArt.Icon(id, new Rect(r.x + 7, r.center.y - size / 2, size, size));
        Text(new Rect(r.x + size + 15, r.y + 3, r.width - size - 22, r.height - 6), value, small, enabled ? BattlePainter.Paper : muted);
        return clicked;
    }
    private Rect Cell(Rect r, int index, int columns, int count)
    { columns = Mathf.Max(1, columns); int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns)); float w = (r.width - (columns - 1) * 7) / columns, h = (r.height - (rows - 1) * 6) / rows; return new Rect(r.x + index % columns * (w + 7), r.y + index / columns * (h + 6), w, h); }
    private void Go(Menu next) { menu = next; scroll = Vector2.zero; GUIUtility.ExitGUI(); }
    private void Acted() { ability = null; target = executionTarget = null; menu = Battle.IsOver ? Menu.Aftermath : Menu.Main; enemyAt = Time.unscaledTime + .9f; GUIUtility.ExitGUI(); }
    private string SlotName(BodySlot slot) => slot == BodySlot.BrazoIzq ? "Brazo izquierdo" : slot == BodySlot.BrazoDer ? "Brazo derecho" : slot == BodySlot.PiernaIzq ? "Pierna izquierda" : slot == BodySlot.PiernaDer ? "Pierna derecha" : slot.ToString();
    private string ClassName(Profession p) => p == Profession.Hunter ? "Cazadora" : p == Profession.Dancer ? "Bailarina" : "Injertador";
    private string PieceText(BodyPiece piece) => piece.Definition.Name + $" · {piece.Health}/{piece.MaxHealth} PV\n" + (piece.Definition.Action == null ? "Sin acción natural" : piece.Definition.Action.Name) + $" · DEF {piece.Definition.Defense} · VEL {piece.Definition.Speed}";
    private string Describe(ActionDefinition a) => a.Kind == ActionKind.Block ? "Reduce el daño recibido un 50 %.\nEquipo: " + a.AcquiredWith : $"Daño base {a.Damage} · Precisión base {a.Accuracy:P0}\nOrigen: {a.AcquiredWith}" + (a.Charm > 0 ? "\nAplica seducción según el objetivo." : "");
}
