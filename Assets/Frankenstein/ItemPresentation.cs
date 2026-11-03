using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private void DrawItemInventory(Rect body)
    {
        Text(new Rect(body.x, body.y, body.width, 32), "OBJETOS", heading, BattlePainter.Gold);
        var parts = Run.Inventory.Pieces.GroupBy(p => p.Definition.Id).ToArray();
        var gear = Run.Inventory.Equipment.GroupBy(g => g.Definition.Id).ToArray();
        var list = new Rect(body.x, body.y + 40, body.width, body.height - 40);
        const float rowHeight = 45;
        float width = list.width - 22;
        bool wasChoosing = choosingBalmTarget;
        scroll = GUI.BeginScrollView(list, scroll,
            new Rect(0, 0, width, Mathf.Max(list.height, (3 + parts.Length + gear.Length) * rowHeight)));
        if (ItemButton(new Rect(0, 0, width, 41), "balm",
            $"Bálsamo ×{Run.Balms} · Restaura 12 PV por parte herida", Run.Balms > 0 && !wasChoosing))
            choosingBalmTarget = true;
        int row = 1;
        DrawInventoryEntry(new Rect(0, row++ * rowHeight, width, 41), "gold", $"Oro ×{Run.Inventory.Gold}");
        DrawInventoryEntry(new Rect(0, row++ * rowHeight, width, 41), "thread", $"Hilos ×{Run.Inventory.Threads}");
        foreach (var group in parts)
            DrawInventoryEntry(new Rect(0, row++ * rowHeight, width, 41), group.Key,
                $"{group.First().Definition.Name} ×{group.Count()} · Injertos");
        foreach (var group in gear)
            DrawInventoryEntry(new Rect(0, row++ * rowHeight, width, 41), group.Key,
                $"{group.First().Definition.Name} ×{group.Count()} · Equipo");
        GUI.EndScrollView();

        if (!choosingBalmTarget) return;
        var party = Run.Party.Where(c => !c.IsDead).ToArray();
        BattlePainter.Fill(body, new Color(0, 0, 0, .58f));
        float popupWidth = Mathf.Min(330, body.width - 16);
        float popupHeight = 76 + party.Length * 33;
        var popup = new Rect(body.center.x - popupWidth / 2, body.center.y - popupHeight / 2, popupWidth, popupHeight);
        ExplorationArt.Panel(popup);
        Text(new Rect(popup.x + 12, popup.y + 5, popup.width - 24, 31), "¿A quién das el bálsamo?", label, BattlePainter.Gold);
        for (int i = 0; i < party.Length; i++)
        {
            var ally = party[i];
            bool wounded = ally.OccupiedSlots.Any(slot =>
            {
                var part = ally.GetPiece(slot);
                return part.Health < part.MaxHealth;
            });
            if (Btn(new Rect(popup.x + 10, popup.y + 38 + i * 33, popup.width - 20, 29),
                ally.Name + (wounded ? "" : " · Sin heridas"), wounded && Run.Balms > 0) && Run.UseBalm(ally))
            {
                choosingBalmTarget = false;
                GUIUtility.ExitGUI();
            }
        }
        if (Btn(new Rect(popup.x + 10, popup.yMax - 33, popup.width - 20, 26), "Cancelar"))
            choosingBalmTarget = false;
    }

    private void DrawInventoryEntry(Rect row, string icon, string value)
    {
        ExplorationArt.Panel(row, .87f);
        ImperialArt.Icon(icon, new Rect(row.x + 8, row.y + 5, 30, 30));
        Text(new Rect(row.x + 46, row.y, row.width - 54, row.height), value, small);
    }
}
