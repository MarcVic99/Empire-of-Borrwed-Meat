using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private static readonly BodySlot[] GraftSlots =
        { BodySlot.BrazoIzq, BodySlot.BrazoDer, BodySlot.PiernaIzq, BodySlot.PiernaDer };
    private BodySlot? graftSlot;
    private string graftMessage;
    private float graftMessageUntil;

    private void DrawGraftMenu(Rect body)
    {
        if (member == null)
        {
            Text(new Rect(body.x, body.y, body.width - 128, 34), "¿Quién recibirá el injerto?", heading, BattlePainter.Gold);
            DrawThreadCounter(new Rect(body.xMax - 118, body.y, 118, 32));
            var party = Run.Party.Where(c => !c.IsDead).ToArray();
            var cards = new Rect(body.x, body.y + 42, body.width, body.height - 42);
            for (int i = 0; i < party.Length; i++)
            {
                var creature = party[i];
                var card = Cell(cards, i, Rules.MaxPartySize, Rules.MaxPartySize);
                card.height = Mathf.Min(card.height, 365);
                bool chosen = GUI.Button(card, GUIContent.none, GUIStyle.none);
                ExplorationArt.Panel(card, .96f);
                float portraitHeight = Mathf.Min(card.height * .56f, card.width * 1.1f);
                DrawPartyPortrait(new Rect(card.x + 7, card.y + 7, card.width - 14, portraitHeight), creature);
                float y = card.y + portraitHeight + 10;
                Text(new Rect(card.x + 10, y, card.width - 20, 25), creature.Name, label, BattlePainter.Gold);
                for (int j = 0; j < GraftSlots.Length; j++)
                {
                    var slot = GraftSlots[j];
                    var piece = creature.GetPiece(slot);
                    var row = new Rect(card.x + 10, y + 28 + j * 23, card.width - 20, 22);
                    DrawVitalsIcon(new Vector2(row.x + 5, row.center.y), slot, piece == null ? muted : BattlePainter.Gold);
                    string side = j % 2 == 0 ? "I" : "D";
                    Text(new Rect(row.x + 14, row.y, row.width - 14, row.height),
                        side + " · " + (piece?.Definition.Name ?? "Ausente"), small, piece == null ? muted : BattlePainter.Paper);
                }
                if (card.Contains(Event.current.mousePosition)) BattlePainter.Border(card, BattlePainter.Gold);
                if (chosen)
                {
                    member = creature;
                    graftSlot = null;
                    scroll = Vector2.zero;
                    GUIUtility.ExitGUI();
                }
            }
            return;
        }

        Text(new Rect(body.x, body.y, body.width - 165, 34), member.Name, heading, BattlePainter.Gold);
        if (Btn(new Rect(body.xMax - 155, body.y, 155, 32), "Volver al grupo"))
        {
            member = null;
            graftSlot = null;
            graftMessageUntil = 0;
            GUIUtility.ExitGUI();
        }

        string operatorStatus = Run.Surgeon.Profession == Profession.Grafter ? "Injertos de campo" :
            Run.CanGraft ? "Mesa disponible" : "Consulta de extremidades";
        string status = $"{operatorStatus} · {Run.Surgeon.Name} · Coste {Run.ThreadCost}";
        bool showMessage = Time.unscaledTime < graftMessageUntil;
        Text(new Rect(body.x, body.y + 39, body.width - 128, 33), showMessage ? graftMessage : status, small,
            showMessage ? BattlePainter.Gold : muted);

        DrawThreadCounter(new Rect(body.xMax - 118, body.y + 39, 118, 32));
        var slots = new Rect(body.x, body.y + 82, body.width * .44f, Mathf.Min(232, body.height - 82));
        for (int i = 0; i < GraftSlots.Length; i++)
        {
            var slot = GraftSlots[i];
            var piece = member.GetPiece(slot);
            if (!RowForGraft(Cell(slots, i, 1, GraftSlots.Length), slot, piece)) continue;
            if (!Run.CanGraft) GraftNotice("Solo disponible en una mesa de injertos");
            else
            {
                graftSlot = slot;
                scroll = Vector2.zero;
                graftMessageUntil = 0;
            }
        }

        var area = new Rect(body.x + body.width * .47f, body.y + 82, body.width * .53f, body.height - 82);
        ExplorationArt.Panel(area, .90f);
        if (!Run.CanGraft || !graftSlot.HasValue)
        {
            Text(new Rect(area.x + 14, area.y + 10, area.width - 28, 54),
                Run.CanGraft ? "Elige una extremidad para ver las piezas compatibles." : "Puedes consultar los injertos del grupo.", small, muted);
            return;
        }

        var selectedSlot = graftSlot.Value;
        Text(new Rect(area.x + 14, area.y + 5, area.width - 28, 30), SlotName(selectedSlot), label, BattlePainter.Gold);
        var options = Run.Inventory.Pieces.Where(piece => SewingStation.Fits(piece.Definition, selectedSlot)).ToArray();
        var view = new Rect(area.x + 8, area.y + 40, area.width - 16, area.height - 48);
        if (options.Length == 0)
        {
            Text(new Rect(view.x + 6, view.y, view.width - 12, 42), "No tienes piezas compatibles.", small, muted);
            return;
        }

        scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, view.width - 18, Mathf.Max(view.height, options.Length * 54)));
        BodyPiece replacement = null;
        for (int i = 0; i < options.Length; i++)
        {
            var piece = options[i];
            bool allowed = Run.CanSew(member, selectedSlot, piece);
            if (ItemButton(new Rect(0, i * 54, view.width - 20, 49), piece.Definition.Id,
                piece.Definition.Name + (allowed ? " · Injertar" : " · Faltan hilos"), allowed))
                replacement = piece;
        }
        GUI.EndScrollView();
        if (replacement != null && Run.Sew(member, selectedSlot, replacement))
        {
            GraftNotice("Injerto realizado");
            scroll = Vector2.zero;
            GUIUtility.ExitGUI();
        }
    }

    private void DrawThreadCounter(Rect bounds)
    {
        ExplorationArt.Panel(bounds, .94f);
        ImperialArt.Icon("thread", new Rect(bounds.x + 7, bounds.y + 4, 24, 24));
        Text(new Rect(bounds.x + 37, bounds.y, bounds.width - 43, bounds.height),
            Run.Inventory.Threads + " hilos", small, BattlePainter.Gold);
    }

    private bool RowForGraft(Rect row, BodySlot slot, BodyPiece piece)
    {
        bool clicked = Btn(row, "", true, graftSlot == slot);
        DrawVitalsIcon(new Vector2(row.x + 12, row.center.y), slot, Run.CanGraft ? BattlePainter.Gold : muted);
        Text(new Rect(row.x + 24, row.y + 2, row.width - 32, row.height - 4),
            SlotName(slot) + "\n" + (piece?.Definition.Name ?? "Ausente"), small);
        return clicked;
    }

    private void GraftNotice(string text)
    {
        graftMessage = text;
        graftMessageUntil = Time.unscaledTime + 3;
    }
}
