using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private bool DrawPartyCard(Rect available, Creature creature)
    {
        float width = Mathf.Min(available.width, 270);
        var card = new Rect(available.center.x - width / 2, available.y, width,
            Mathf.Min(available.height, width * 1.45f));
        bool clicked = GUI.Button(card, GUIContent.none, GUIStyle.none);
        ExplorationArt.Panel(card, .96f);
        DrawPartyPortrait(new Rect(card.x + 7, card.y + 7, card.width - 14, card.height - 119), creature);
        Text(new Rect(card.x + 12, card.yMax - 110, card.width - 24, 34), creature.Name, heading);
        DrawPartyVitals(new Rect(card.x + 8, card.yMax - 73, card.width - 16, 65), creature);
        var edge = card.Contains(Event.current.mousePosition) ? BattlePainter.Gold : new Color(.40f, .28f, .24f);
        BattlePainter.Border(card, edge);
        return clicked;
    }

    private void DrawPartyPortrait(Rect frame, Creature creature)
    {
        BattlePainter.Fill(frame, new Color(.14f, .085f, .10f));
        var portrait = creature.Profession == Profession.Hunter || creature.Profession == Profession.Dancer ||
                       creature.Profession == Profession.Grafter
            ? ImperialArt.Load(ImperialArt.Role(creature.Profession) + "-close") : null;
        if (portrait != null)
        {
            float aspect = frame.width * portrait.height / (frame.height * portrait.width);
            float width = Mathf.Min(1, aspect);
            float height = Mathf.Min(1, 1 / aspect);
            float scale = Mathf.Min(.82f / width, .72f / height);
            width *= scale;
            height *= scale;
            var uv = new Rect((1 - width) / 2, .98f - height, width, height);
            var tint = GUI.color;
            GUI.color = new Color(.80f, .75f, .76f, tint.a);
            GUI.DrawTextureWithTexCoords(frame, portrait, uv, false);
            GUI.color = tint;
        }
        else
        {
            GUI.BeginGroup(frame);
            float height = frame.height * 1.55f;
            var tint = GUI.color;
            GUI.color = new Color(.75f, .68f, .70f, tint.a);
            CharacterArt.Draw(creature, new Rect(0, 4, frame.width, height), 0);
            GUI.color = tint;
            GUI.EndGroup();
        }
        BattlePainter.Border(frame, new Color(.35f, .24f, .23f));
    }

    private void DrawPartyVitals(Rect area, Creature creature)
    {
        BattlePainter.Fill(area, new Color(.105f, .064f, .082f, .94f));
        BattlePainter.Border(area, new Color(.34f, .24f, .22f));
        var rows = new Rect(area.x + 5, area.y + 4, area.width - 10, area.height - 8);
        foreach (var slot in AnatomySlots)
        {
            var row = Cell(rows, (int)slot, 2, AnatomySlots.Length);
            var part = creature.GetPiece(slot);
            string key = slot switch
            {
                BodySlot.Cabeza => "CAB",
                BodySlot.Torso => "TOR",
                BodySlot.BrazoIzq => "BI",
                BodySlot.BrazoDer => "BD",
                BodySlot.PiernaIzq => "PI",
                _ => "PD"
            };
            float health = part == null ? 0 : part.Health / (float)part.MaxHealth;
            Text(new Rect(row.x, row.y - 1, 27, row.height - 2), key, small, part == null ? muted : BattlePainter.Gold);
            string value = part == null ? "—" : part.Health + "/" + part.MaxHealth;
            float valueWidth = small.CalcSize(new GUIContent(value)).x;
            Text(new Rect(row.xMax - valueWidth, row.y - 1, valueWidth, row.height - 2), value, small, part == null ? muted : BattlePainter.Paper);
            BattlePainter.Bar(new Rect(row.x, row.yMax - 2, row.width, 2), health,
                Color.Lerp(BattlePainter.Blood, BattlePainter.Gold, health));
            GUI.Label(row, new GUIContent("", SlotName(slot)));
        }
    }

    private void DrawPartyAnatomy(Rect body, Creature creature)
    {
        var portrait = new Rect(body.x, body.y, body.width * .33f, body.height - 12);
        ExplorationArt.Panel(portrait, .94f);
        DrawPartyPortrait(new Rect(portrait.x + 7, portrait.y + 7, portrait.width - 14, portrait.height - 86), creature);
        DrawPartyVitals(new Rect(portrait.x + 8, portrait.yMax - 73, portrait.width - 16, 65), creature);

        float x = body.x + body.width * .36f;
        float width = body.width * .64f;
        Text(new Rect(x, body.y, width, 42), $"DEF {creature.Defense} · Esquiva {creature.Evasion:P0} · Equilibrio {creature.BalanceAccuracy:P0}", small, BattlePainter.Gold);
        var slots = new Rect(x, body.y + 45, width, body.height - 115);
        foreach (var slot in AnatomySlots)
        {
            var tile = Cell(slots, (int)slot, 2, AnatomySlots.Length);
            var part = creature.GetPiece(slot);
            BattlePainter.Fill(tile, new Color(.16f, .09f, .12f, .86f));
            BattlePainter.Border(tile, new Color(.32f, .21f, .23f));
            DrawVitalsIcon(new Vector2(tile.x + 13, tile.y + 13), slot, part == null ? muted : BattlePainter.Gold);
            Text(new Rect(tile.x + 24, tile.y + 3, tile.width - 30, 20), SlotName(slot), small, BattlePainter.Gold);
            Text(new Rect(tile.x + 10, tile.y + 24, tile.width - 20, tile.height - 32),
                part == null ? "Ausente" : part.Definition.Name + $" · {part.Health}/{part.MaxHealth} PV", small,
                part == null ? muted : BattlePainter.Paper);
            if (part != null)
                BattlePainter.Bar(new Rect(tile.x + 10, tile.yMax - 7, tile.width - 20, 3),
                    part.Health / (float)part.MaxHealth, BattlePainter.Gold);
        }
    }
}
