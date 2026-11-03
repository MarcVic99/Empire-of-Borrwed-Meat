using System;
using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private static readonly BodySlot[] AnatomySlots = (BodySlot[])Enum.GetValues(typeof(BodySlot));
    private Creature highlightedTarget;
    private BodySlot? highlightedPart;
    private Rect anatomyPanel;
    private const float VitalsHeight = 104;
    private const float VitalsCardWidth = 124;
    private const float VitalsGap = 6;
    private Creature[] VitalsAllies() => Battle.Aftermath.Party.Where(c => c.IsPlayer && !c.IsDead).ToArray();

    private Rect AllyVitalsRect
    {
        get
        {
            int count = VitalsAllies().Length;
            if (count == 0) return new Rect(Screen.width - 12, Screen.height - 8, 0, 0);
            float width = Mathf.Min(Screen.width - 24, count * (VitalsCardWidth + VitalsGap) - VitalsGap);
            return new Rect(Screen.width - width - 12, Screen.height - VitalsHeight - 8, width, VitalsHeight);
        }
    }

    private void PrepareTargetHighlight()
    {
        highlightedTarget = null;
        highlightedPart = null;
        anatomyPanel = default;
        combatPanel = default;
        if (BattleInputLocked || !PlayerTurn) return;
        var mouse = Event.current.mousePosition;
        if (menu == Menu.AttackTarget || menu == Menu.TalkTarget)
        {
            var options = menu == Menu.TalkTarget
                ? Battle.Aftermath.Enemies.Where(e => !e.IsDead && !e.IsPlayer && !e.HasFled).ToArray()
                : AttackTargets();
            combatPanel = ActionPanel(options.Length + 1, 350);
            var area = ActionArea(combatPanel);
            for (int i = 0; i < options.Length; i++)
                if (ActionRow(area, i).Contains(mouse)) highlightedTarget = options[i];
            return;
        }

        if (menu != Menu.Parts || target == null) return;
        float width = Mathf.Clamp(Screen.width * .20f, 134, 220);
        float height = Mathf.Clamp(AllyVitalsRect.y - 55, 100, 264);
        var right = new Rect(Screen.width - width - 12, 46, width, height);
        var left = new Rect(12, 46, width, height);
        anatomyPanel = fighterBounds.TryGetValue(target, out var figure) && right.Overlaps(figure) ? left : right;
        combatPanel = ActionPanel(AnatomySlots.Length + 1, 310);
        var rows = ActionArea(combatPanel);
        for (int i = 0; i < AnatomySlots.Length; i++)
            if (target.GetPiece(AnatomySlots[i]) != null && ActionRow(rows, i).Contains(mouse))
                highlightedPart = AnatomySlots[i];
    }

    private bool PointerOverBattleUi()
    {
        Vector2 pointer = Event.current.mousePosition;
        return AllyVitalsRect.Contains(pointer) || combatPanel.Contains(pointer) || anatomyPanel.Contains(pointer)
            || new Rect(Screen.width - 122, 10, 102, 30).Contains(pointer);
    }

    private void TargetPointer(Rect figure)
    {
        var point = new Vector2(figure.center.x, Mathf.Max(41, figure.y - 2));
        BattlePainter.Line(point + new Vector2(-7, -7), point, BattlePainter.Gold, 2);
        BattlePainter.Line(point, point + new Vector2(7, -7), BattlePainter.Gold, 2);
        BattlePainter.Oval(new Rect(point.x - 2, point.y - 15, 4, 4), BattlePainter.Gold);
    }

    private void DrawTargetAnatomy()
    {
        if (anatomyPanel.width <= 0) return;
        ExplorationArt.Panel(anatomyPanel, .95f);
        Text(new Rect(anatomyPanel.x + 8, anatomyPanel.y + 6, anatomyPanel.width - 16, 22), "ANATOMÍA", centered, BattlePainter.Gold);
        var diagram = new Rect(anatomyPanel.x + 16, anatomyPanel.y + 31, anatomyPanel.width - 32, anatomyPanel.height - 62);
        bool rear = target.IsPlayer && !target.IsThrown;
        foreach (var slot in AnatomySlots)
        {
            var part = CharacterArt.Part(diagram, slot, rear, target);
            if (part.width < 5 || part.height < 5) continue;
            bool present = target.GetPiece(slot) != null;
            bool active = present && highlightedPart == slot;
            var inset = new Rect(part.x + 2, part.y + 2, part.width - 4, part.height - 4);
            if (target.Appearance != "cabeza_arana" && slot == BodySlot.Cabeza)
            {
                inset.x += inset.width * .30f;
                inset.width *= .40f;
            }
            if (target.Appearance != "cabeza_arana" && (slot == BodySlot.BrazoIzq || slot == BodySlot.BrazoDer))
                inset.height *= .60f;
            Color fill = active ? new Color(.72f, .36f, .29f) : present ? new Color(.40f, .27f, .27f) : new Color(.12f, .10f, .13f);
            BattlePainter.Fill(inset, fill);
            BattlePainter.Border(inset, active ? BattlePainter.Gold : present ? BattlePainter.Paper * .5f : muted * .35f);
            if (present)
            {
                var tint = GUI.color;
                GUI.BeginGroup(inset);
                GUI.color = active ? new Color(1, .92f, .67f) : new Color(.74f, .62f, .59f, .75f);
                CharacterArt.Draw(target, new Rect(diagram.position - inset.position, diagram.size), rear ? 1 : 0);
                GUI.color = tint;
                GUI.EndGroup();
            }
            else Text(inset, "×", centered, muted);
        }
        string caption = highlightedPart.HasValue ? SlotName(highlightedPart.Value) : "Elige una parte";
        Text(new Rect(anatomyPanel.x + 6, anatomyPanel.yMax - 23, anatomyPanel.width - 12, 20), caption, centered, BattlePainter.Gold);
    }

    private void DrawAllyVitals()
    {
        var allies = VitalsAllies();
        if (allies.Length == 0) return;
        var area = AllyVitalsRect;
        float width = (area.width - (allies.Length - 1) * VitalsGap) / allies.Length;
        for (int i = 0; i < allies.Length; i++)
        {
            var ally = allies[i];
            var card = new Rect(area.x + i * (width + VitalsGap), area.y, width, area.height);
            ExplorationArt.Panel(card, .86f);
            var accent = ally == Battle.Current && !Battle.IsOver ? BattlePainter.Gold : BattlePainter.Paper;
            GUI.Label(new Rect(card.x + 9, card.y + 3, card.width - 18, 18), ally.Name,
                new GUIStyle(small) { wordWrap = false, clipping = TextClipping.Clip, normal = { textColor = accent } });
            BattlePainter.Fill(new Rect(card.x + 9, card.y + 23, card.width - 18, 1), new Color(.40f, .28f, .24f, .6f));

            for (int j = 0; j < AnatomySlots.Length; j++)
            {
                var slot = AnatomySlots[j];
                var piece = ally.GetPiece(slot);
                float health = piece == null ? 0 : piece.Health / (float)piece.MaxHealth;
                var row = new Rect(card.x + 8, card.y + 26 + j * 12, card.width - 16, 12);
                var color = piece == null ? new Color(.38f, .33f, .34f) : BattlePainter.Paper;
                DrawVitalsIcon(new Vector2(row.x + 5, row.center.y), slot, color);
                string side = slot == BodySlot.BrazoIzq || slot == BodySlot.PiernaIzq ? "I"
                    : slot == BodySlot.BrazoDer || slot == BodySlot.PiernaDer ? "D" : "";
                if (side.Length != 0)
                    Text(new Rect(row.x + 10, row.y, 10, row.height), side, centered, color);
                var track = new Rect(row.x + 24, row.center.y - 2, row.width - 25, 4);
                BattlePainter.Bar(track, health, Color.Lerp(BattlePainter.Blood, BattlePainter.Gold, health));
                if (piece == null) BattlePainter.Fill(track, new Color(.20f, .16f, .18f));
                GUI.Label(row, new GUIContent("", SlotName(slot) + " · " +
                    (piece == null ? "Ausente" : piece.Health + "/" + piece.MaxHealth)));
            }
        }
    }

    private static void DrawVitalsIcon(Vector2 center, BodySlot slot, Color color)
    {
        if (slot == BodySlot.Cabeza)
        {
            BattlePainter.Oval(new Rect(center.x - 3, center.y - 5, 6, 7), color);
            BattlePainter.Fill(new Rect(center.x - 2, center.y + 1, 4, 3), color);
            return;
        }

        if (slot == BodySlot.Torso)
        {
            BattlePainter.Fill(new Rect(center.x - 4, center.y - 4, 8, 3), color);
            BattlePainter.Fill(new Rect(center.x - 3, center.y - 1, 6, 5), color);
            return;
        }

        bool arm = slot == BodySlot.BrazoIzq || slot == BodySlot.BrazoDer;
        float side = slot == BodySlot.BrazoIzq || slot == BodySlot.PiernaIzq ? -1 : 1;
        var joint = center + new Vector2(side * 2, 0);
        BattlePainter.Line(center + new Vector2(-side * 2, -4), joint, color, 2);
        BattlePainter.Line(joint, center + new Vector2(side * (arm ? 4 : 0), 4), color, 2);
        if (!arm) BattlePainter.Fill(new Rect(center.x - 2, center.y + 4, 5, 1), color);
    }
}
