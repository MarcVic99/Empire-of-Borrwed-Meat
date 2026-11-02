using System;
using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private bool worldMenu, puzzleMenu, walking;
    private int worldTab, facing;
    private Vector2 worldCamera;
    private string puzzleFeedback = "";
    private readonly string[] worldTabs = { "Mapa", "Personajes", "Objetos", "Injertos", "Equipo", "Diario" };
    private void UpdateExploration()
    {
        float x = controls.Movement.x, y = controls.Movement.y;
        bool interact = (controls.Pressed & GameButton.Interact) != 0, toggle = (controls.Pressed & GameButton.Menu) != 0;
        if (toggle) { if (puzzleMenu) puzzleMenu = false; else worldMenu = !worldMenu; }
        walking = false;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        if (!worldMenu && !puzzleMenu)
        {
            var movement = new Vector2(x, y).normalized * dt * 3.5f;
            double oldX = Run.Dungeon.X, oldY = Run.Dungeon.Y;
            Run.Dungeon.Move(movement.x, movement.y);
            walking = Dungeon.Distance(oldX, oldY, Run.Dungeon.X, Run.Dungeon.Y) > .001;
            if (walking) facing = Mathf.Abs(x) > Mathf.Abs(y) ? (x < 0 ? 1 : 2) : y < 0 ? 3 : 0;
            if (interact) HandleInteraction();
            Run.Dungeon.Tick(dt);
            if (Run.BeginEncounter()) { SyncBattle(); return; }
        }
        worldCamera = Vector2.Lerp(worldCamera, new Vector2((float)Run.Dungeon.X, (float)Run.Dungeon.Y), 1 - Mathf.Exp(-9 * dt));
    }
    private void HandleInteraction()
    {
        int oldFloor = Run.Dungeon.Floor;
        string result = Run.Interact();
        if (oldFloor != Run.Dungeon.Floor) { worldCamera = new Vector2((float)Run.Dungeon.X, (float)Run.Dungeon.Y); facing = 0; }
        if (result == "grafts") { worldMenu = true; worldTab = 3; graft = null; member = null; }
        else if (result == "puzzle") { puzzleMenu = true; puzzleFeedback = "Recuerda el orden de la nota del vestíbulo."; }
    }
    private void DrawSelection(Rect r)
    {
        Text(new Rect(r.x + 12, r.y, r.width - 24, 39), "ELIGE QUIÉN DESCIENDE", heading, BattlePainter.Gold);
        Text(new Rect(r.x + 12, r.y + 40, r.width - 24, 28), "Cada clase tiene una ventaja y un precio.", small, muted);
        var cards = new Rect(r.x + 8, r.y + 80, r.width - 16, r.height - 155);
        var roles = new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter };
        string[] tags = { "COMBATE", "EVASIÓN", "COSTURA" };
        string[] specialties = { "Presión de caza", "Danza cautivadora", "Injertos de campo" };
        string[] details = { "Más daño y rendiciones. No puede reclutar a quien ya se ha rendido.", "Alta evasión, poca defensa y daño. Seduce mejor a hombres, especialmente humanos.", "Cose sin mesa y gasta 1 hilo; los demás, 2. Le cuesta más convencer." };
        string[] kits = { "Espada · Daga", "Cintas · Daga", "Sierra · Aguja" };
        for (int i = 0; i < 3; i++)
        {
            var c = Cell(cards, i, 3, 3); bool selected = selectedClass == roles[i];
            if (Btn(c, "", true, selected)) selectedClass = roles[i];
            Text(new Rect(c.x + 18, c.y + 13, c.width - 36, 25), tags[i] + (selected ? "  /  ELEGIDO" : ""), small, BattlePainter.Gold);
            Text(new Rect(c.x + 18, c.y + 39, c.width - 36, 35), ClassName(roles[i]).ToUpperInvariant(), heading);
            var preview = i == 0 ? hunterPreview : i == 1 ? dancerPreview : grafterPreview;
            float lift = c.Contains(Event.current.mousePosition) ? Mathf.Sin(Time.unscaledTime * 2) * 2 : 0;
            CharacterArt.Draw(preview, new Rect(c.x + 18, c.y + 75 + lift, c.width - 36, c.height * .53f), 2);
            float info = c.y + c.height * .69f;
            BattlePainter.Fill(new Rect(c.x + 19, info - 9, c.width - 38, 1), BattlePainter.Gold * .5f);
            Text(new Rect(c.x + 20, info, c.width - 40, 28), specialties[i], label, BattlePainter.Gold);
            Text(new Rect(c.x + 20, info + 30, c.width - 40, c.height * .17f), details[i], small);
            Text(new Rect(c.x + 20, c.yMax - 37, c.width - 40, 25), kits[i], small, muted);
        }
        var footer = new Rect(r.x + 8, r.yMax - 62, r.width - 16, 58); ExplorationArt.Panel(footer);
        Text(new Rect(footer.x + 16, footer.y + 6, 100, 16), "SEMILLA", small, muted);
        seedText = GUI.TextField(new Rect(footer.x + 15, footer.y + 25, 130, 26), seedText ?? seed.ToString());
        if (Btn(new Rect(footer.x + 155, footer.y + 13, 118, 34), "Otra semilla")) seedText = (Guid.NewGuid().GetHashCode() & int.MaxValue).ToString();
        int parsed; bool valid = int.TryParse(seedText, out parsed);
        if (Btn(new Rect(footer.x + 285, footer.y + 10, footer.width - 300, 40), "DESCENDER COMO " + ClassName(selectedClass).ToUpperInvariant() + "  →", valid, true))
        { seed = parsed; worldCamera = Vector2.zero; facing = 0; StartRun(selectedClass); GUIUtility.ExitGUI(); }
    }
    private void DrawDungeon()
    {
        var d = Run.Dungeon;
        float scale = Mathf.Clamp(Screen.height * .052f, 26, 46);
        var viewport = new Rect(0, 67, Screen.width, Screen.height - 155);
        GUI.BeginGroup(viewport);
        var center = new Vector2(viewport.width / 2, viewport.height / 2);
        Func<double, double, Vector2> screen = (x, y) => center + new Vector2((float)x - worldCamera.x, (float)y - worldCamera.y) * scale;
        int minX = Mathf.FloorToInt(worldCamera.x - viewport.width / scale / 2) - 1, maxX = Mathf.CeilToInt(worldCamera.x + viewport.width / scale / 2) + 1;
        int minY = Mathf.FloorToInt(worldCamera.y - viewport.height / scale / 2) - 1, maxY = Mathf.CeilToInt(worldCamera.y + viewport.height / scale / 2) + 1;
        for (int y = minY; y < maxY; y++) for (int x = minX; x < maxX; x++)
        {
            if (!d.IsWalkable(x + .5, y + .5, 0)) continue;
            var p = screen(x, y); var tile = new Rect(p.x, p.y, scale + 1, scale + 1);
            float variation = ((x * 13 + y * 7) & 3) * .009f;
            BattlePainter.Fill(tile, new Color(.18f + variation, .18f + variation, .155f + variation));
            BattlePainter.Border(tile, new Color(.105f, .11f, .095f), 1);
            if (((x * 3 + y * 7) & 15) == 0) BattlePainter.Line(new Vector2(tile.x + 8, tile.y + 5), new Vector2(tile.x + 19, tile.y + 15), new Color(.11f, .115f, .10f));
            if (!d.IsWalkable(x + .5, y - .5, 0)) { BattlePainter.Fill(new Rect(tile.x, tile.y - scale * .30f, tile.width, scale * .30f), new Color(.23f, .23f, .20f)); BattlePainter.Fill(new Rect(tile.x, tile.y, tile.width, 5), new Color(0, 0, 0, .4f)); }
            if (!d.IsWalkable(x - .5, y + .5, 0)) BattlePainter.Fill(new Rect(tile.x, tile.y, 5, tile.height), new Color(.055f, .06f, .05f));
            if (!d.IsWalkable(x + 1.5, y + .5, 0)) BattlePainter.Fill(new Rect(tile.xMax - 5, tile.y, 5, tile.height), new Color(.055f, .06f, .05f));
        }
        foreach (var room in d.Rooms)
        {
            var a = screen(room.CenterX - 3.7, room.CenterY - 3.7);
            var b = screen(room.CenterX + 3.7, room.CenterY + 3.7);
            float flicker = .92f + .08f * Mathf.Sin(Time.unscaledTime * 5 + room.X);
            BattlePainter.Oval(new Rect(a.x - scale * 2, a.y - scale * 2, scale * 4, scale * 4), new Color(.65f, .39f, .12f, .07f * flicker));
            ExplorationArt.Prop(4, new Rect(a.x - scale * .6f, a.y - scale * .8f, scale * 1.2f, scale * 1.4f));
            ExplorationArt.Prop(5, new Rect(b.x - scale * .8f, b.y - scale * .7f, scale * 1.5f, scale * 1.5f));
        }
        foreach (var l in d.Landmarks.OrderBy(l => l.Y))
        {
            var p = screen(l.X, l.Y);
            if (l.Kind == LandmarkKind.Thread)
            {
                if (!l.Used) { ExplorationArt.Thread(new Rect(p.x - scale * .30f, p.y - scale * .45f, scale * .6f, scale * .7f)); }
                continue;
            }
            int index = l.Kind == LandmarkKind.Chest ? (l.Used ? 1 : 0) : l.Kind == LandmarkKind.Table ? 2 : l.Kind == LandmarkKind.Lore ? 3 : l.Kind == LandmarkKind.Altar ? 6 : 7;
            float size = l.Kind == LandmarkKind.Table ? 2.7f : l.Kind == LandmarkKind.Stairs ? 2.4f : 1.9f;
            ExplorationArt.Prop(index, new Rect(p.x - scale * size / 2, p.y - scale * size * .65f, scale * size, scale * size));
        }
        foreach (var enemy in d.Rooms.Where(r => r.HasEncounter).OrderBy(r => r.EnemyY))
        {
            var p = screen(enemy.EnemyX, enemy.EnemyY);
            if (p.x < -100 || p.x > viewport.width + 100 || p.y < -100 || p.y > viewport.height + 100) continue;
            BattlePainter.Oval(new Rect(p.x - scale * .7f, p.y - scale * .12f, scale * 1.4f, scale * .4f), new Color(0, 0, 0, .4f));
            ExplorationArt.Enemy(enemy.Enemies[0], new Rect(p.x - scale * 1.1f, p.y - scale * 1.8f + Mathf.Sin((float)enemy.PatrolTime * 6) * 2, scale * 2.2f, scale * 2.2f));
            string state = enemy.EnemyState == RoamingState.Pursue ? "¡TE HA VISTO!" : enemy.EnemyState == RoamingState.Flee ? "HUYE DE TI" : "PATRULLA";
            Text(new Rect(p.x - 85, p.y - scale * 1.95f, 170, 24), state, centered, enemy.EnemyState == RoamingState.Pursue ? new Color(.88f, .49f, .28f) : BattlePainter.Gold);
        }
        var player = screen(d.X, d.Y);
        BattlePainter.Oval(new Rect(player.x - scale * .55f, player.y - scale * .08f, scale * 1.1f, scale * .35f), new Color(0, 0, 0, .5f));
        ExplorationArt.Player(Run.StartingClass, new Rect(player.x - scale * 1.1f, player.y - scale * 1.65f, scale * 2.2f, scale * 2.2f), facing, walking, Time.unscaledTime);
        if (!worldMenu && !puzzleMenu && d.Nearby != null)
        {
            var prompt = new Rect(player.x - 175, player.y + scale * .8f, 350, 35);
            ExplorationArt.Panel(prompt); Text(prompt, "E · " + d.Nearby.Title, centered, BattlePainter.Gold);
        }
        GUI.EndGroup();
        ExplorationArt.Panel(new Rect(0, 0, Screen.width, 67));
        Text(new Rect(22, 9, Screen.width * .50f, 26), "Piso " + d.Floor + " / " + Dungeon.LastFloor, heading, BattlePainter.Gold);
        Text(new Rect(23, 36, Screen.width * .60f, 22), $"{ClassName(Run.StartingClass)} · {Run.Party.Count} en el grupo · {Run.Inventory.Gold} oro", small, muted);
        if (Btn(new Rect(Screen.width - 185, 14, 165, 38), "Menú  [Tab]", true, worldMenu)) { worldMenu = !worldMenu; puzzleMenu = false; GUIUtility.ExitGUI(); }
        var foot = new Rect(0, Screen.height - 88, Screen.width, 88); ExplorationArt.Panel(foot);
        Text(new Rect(22, foot.y + 10, Screen.width - 44, 38), Run.Notice, small, BattlePainter.Paper);
        Text(new Rect(22, foot.y + 51, Screen.width - 44, 25), "WASD / Flechas  Moverse     E  Examinar / Usar     Tab / Esc  Menú", small, muted);
        if (worldMenu) DrawWorldMenu(new Rect(24, 82, Screen.width - 48, Screen.height - 185));
        if (puzzleMenu) DrawPuzzle(new Rect(Screen.width * .18f, Screen.height * .22f, Screen.width * .64f, Screen.height * .50f));
    }
    private Creature removalTarget;
    private bool removalHarvest;
    private void DrawWorldMenu(Rect r)
    {
        BattlePainter.Fill(new Rect(0, 67, Screen.width, Screen.height - 155), new Color(0, 0, 0, .70f));
        ExplorationArt.Panel(r); float sidebar = Mathf.Clamp(r.width * .18f, 125, 185);
        Text(new Rect(r.x + 20, r.y + 12, r.width - 120, 34), "EL GRUPO", heading, BattlePainter.Gold);
        if (Btn(new Rect(r.xMax - 100, r.y + 16, 78, 30), "Cerrar")) { worldMenu = false; GUIUtility.ExitGUI(); }
        for (int i = 0; i < worldTabs.Length; i++)
            if (Btn(new Rect(r.x + 16, r.y + 64 + i * 43, sidebar - 18, 36), worldTabs[i], true, worldTab == i))
            { worldTab = i; member = null; graft = null; selectedGear = null; removalTarget = null; scroll = Vector2.zero; }
        if (Btn(new Rect(r.x + 16, r.yMax - 55, sidebar - 18, 37), "Nueva run")) { Restart(); GUIUtility.ExitGUI(); }
        var body = new Rect(r.x + sidebar + 12, r.y + 60, r.width - sidebar - 34, r.height - 79);
        if (worldTab == 0) { DrawMapOverview(body); return; }
        if (worldTab == 5)
        {
            Text(new Rect(body.x, body.y, body.width, 32), "DIARIO", heading);
            var view = new Rect(body.x, body.y + 42, body.width, body.height - 42);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, view.width - 20, Mathf.Max(view.height, Run.Journal.Count * 100)));
            for (int i = 0; i < Run.Journal.Count; i++) Text(new Rect(10, i * 100, view.width - 35, 95), Run.Journal[i], label);
            if (Run.Journal.Count == 0) Text(new Rect(10, 0, view.width - 30, 80), "Examina los libros y notas con E para conservar sus pistas.", small);
            GUI.EndScrollView(); return;
        }
        if (member != null && (!Run.Party.Contains(member) || member.IsDead)) member = null;
        if (member == null)
        {
            Text(new Rect(body.x, body.y, body.width, 40), worldTab == 3 ? "¿Quién recibirá el injerto?" : worldTab == 4 ? "¿A quién quieres equipar?" : worldTab == 2 ? "¿A quién afectará el objeto?" : "Elige un personaje", heading);
            var options = Run.Party.Where(c => !c.IsDead).ToArray();
            var cards = new Rect(body.x, body.y + 55, body.width, body.height - 55);
            for (int i = 0; i < options.Length; i++)
            {
                var c = Cell(cards, i, Mathf.Min(4, options.Length), options.Length);
                if (Btn(c, "")) { member = options[i]; scroll = Vector2.zero; GUIUtility.ExitGUI(); }
                CharacterArt.Draw(options[i], new Rect(c.x + 12, c.y + 10, c.width - 24, c.height - 58), 0);
                Text(new Rect(c.x + 15, c.yMax - 43, c.width - 30, 32), options[i].Name, centered);
            }
            return;
        }
        Text(new Rect(body.x, body.y, body.width - 200, 32), member.Name, heading, BattlePainter.Gold);
        if (Btn(new Rect(body.xMax - 180, body.y, 180, 32), "Elegir personaje")) { member = null; removalTarget = null; GUIUtility.ExitGUI(); }
        body = new Rect(body.x, body.y + 45, body.width, body.height - 45);
        if (worldTab == 1)
        {
            CharacterArt.Draw(member, new Rect(body.x, body.y, body.width * .33f, body.height - 30), 0);
            Text(new Rect(body.x + body.width * .36f, body.y, body.width * .64f, 48), $"DEF {member.Defense} · Esquiva {member.Evasion:P0} · Equilibrio {member.BalanceAccuracy:P0}", small);
            var slots = new Rect(body.x + body.width * .36f, body.y + 54, body.width * .64f, body.height - 115);
            foreach (BodySlot slot in Enum.GetValues(typeof(BodySlot)))
            { var part = member.GetPiece(slot); Text(Cell(slots, (int)slot, 2, 6), SlotName(slot) + "\n" + (part == null ? "Ausente" : part.Definition.Name + $"\n{part.Health}/{part.MaxHealth} PV"), small); }
            if (removalTarget == member)
            {
                var confirm = new Rect(body.x + body.width * .36f, body.yMax - 62, body.width * .64f, 60);
                Text(new Rect(confirm.x, confirm.y, confirm.width * .55f, 55), removalHarvest ? "Matar a este aliado y recuperar una parte. Es irreversible en esta run." : "Abandonará el grupo con todo lo que lleva.", small);
                if (Btn(new Rect(confirm.x + confirm.width * .56f, confirm.y, confirm.width * .24f, 45), "Confirmar")) { Run.RemoveAlly(member, removalHarvest); member = removalTarget = null; GUIUtility.ExitGUI(); }
                if (Btn(new Rect(confirm.x + confirm.width * .82f, confirm.y, confirm.width * .18f, 45), "Cancelar")) removalTarget = null;
            }
            else if (member != Run.Party[0])
            {
                if (Btn(new Rect(body.x + body.width * .36f, body.yMax - 50, body.width * .30f, 40), "Abandonar aliado")) { removalTarget = member; removalHarvest = false; }
                if (Btn(new Rect(body.x + body.width * .69f, body.yMax - 50, body.width * .31f, 40), "Matar y recuperar")) { removalTarget = member; removalHarvest = true; }
            }
        }
        else if (worldTab == 2)
        {
            Text(new Rect(body.x, body.y, body.width * .60f, 50), $"{Run.Inventory.Gold} oro · {Run.Inventory.Threads} hilos · {Run.Balms} bálsamos\nUn bálsamo restaura 12 PV por pieza herida, sin reponer miembros.", small);
            bool wounded = member.OccupiedSlots.Any(s => member.GetPiece(s).Health < member.GetPiece(s).MaxHealth);
            if (Btn(new Rect(body.x + body.width * .64f, body.y, body.width * .36f, 42), wounded ? "Aplicar bálsamo" : "No necesita bálsamo", wounded && Run.Balms > 0)) Run.UseBalm(member);
            DrawWorldPieces(new Rect(body.x, body.y + 65, body.width, body.height - 65), false);
        }
        else if (worldTab == 3)
        {
            string where = Run.Surgeon.Profession == Profession.Grafter ? "Puede trabajar sin mesa" : Run.CanGraft ? "Mesa disponible" : "Necesitas una mesa cercana";
            Text(new Rect(body.x, body.y, body.width, 53), $"{where} · Operador: {Run.Surgeon.Name} · Coste: {Run.ThreadCost} hilo(s) · Tienes {Run.Inventory.Threads}\nLos torsos no son extraíbles ni admiten injertos.", small, Run.CanGraft ? BattlePainter.Gold : muted);
            var items = new Rect(body.x, body.y + 63, body.width * .49f, body.height - 63); DrawWorldPieces(items, true);
            var area = new Rect(body.x + body.width * .52f, body.y + 63, body.width * .48f, body.height - 63);
            var slots = Enum.GetValues(typeof(BodySlot)).Cast<BodySlot>().Where(s => s != BodySlot.Torso).ToArray();
            for (int i = 0; i < slots.Length; i++)
            { var part = member.GetPiece(slots[i]); if (Btn(Cell(area, i, 1, slots.Length), SlotName(slots[i]) + " · " + (part == null ? "Vacío" : part.Definition.Name), Run.CanSew(member, slots[i], graft))) { Run.Sew(member, slots[i], graft); graft = null; GUIUtility.ExitGUI(); } }
        }
        else DrawEquipmentMenu(body);
    }
    private void DrawEquipmentMenu(Rect body)
    {
        string details = selectedGear == null ? "Elige un objeto y después su hueco. El arco necesita ambos brazos." : selectedGear.Definition.Name + " · " +
            (selectedGear.Definition.Action != null ? Describe(selectedGear.Definition.Action) : $"DEF {selectedGear.Definition.Defense} · VEL {selectedGear.Definition.Speed} · Esquiva {selectedGear.Definition.Evasion:P0}");
        Text(new Rect(body.x, body.y, body.width, 76), details, small, muted);
        var left = new Rect(body.x, body.y + 85, body.width * .47f, body.height - 85);
        var items = Run.Inventory.Equipment.ToArray();
        scroll = GUI.BeginScrollView(left, scroll, new Rect(0, 0, left.width - 20, Mathf.Max(left.height, items.Length * 66)));
        for (int i = 0; i < items.Length; i++)
            if (Btn(new Rect(0, i * 66, left.width - 24, 60), items[i].Definition.Name + (items[i].Definition.Action == null ? "" : "\n" + items[i].Definition.Action.Name), true, selectedGear == items[i])) selectedGear = items[i];
        if (items.Length == 0) Text(new Rect(10, 0, left.width - 30, 100), "No hay equipo en la mochila. Puedes quitar un objeto equipado o buscar en cofres y restos.", small);
        GUI.EndScrollView();
        var right = new Rect(body.x + body.width * .51f, body.y + 85, body.width * .49f, body.height - 85);
        var slots = Enum.GetValues(typeof(GearSlot)).Cast<GearSlot>().ToArray();
        for (int i = 0; i < slots.Length; i++)
        {
            var item = member.GetGear(slots[i]); var row = Cell(right, i, 1, slots.Length); float remove = Mathf.Min(88, row.width * .24f);
            if (Btn(new Rect(row.x, row.y, row.width - remove - 6, row.height), (slots[i] == GearSlot.ManoIzq ? "Mano izquierda" : slots[i] == GearSlot.ManoDer ? "Mano derecha" : slots[i].ToString()) + "\n" + (member.GetPiece(Equipment.BodyFor(slots[i])) == null ? "Falta la parte corporal" : item == null ? "Vacío" : item.Definition.Name), selectedGear != null && member.CanEquipGear(slots[i], selectedGear)))
            { Run.EquipGear(member, slots[i], selectedGear); selectedGear = null; GUIUtility.ExitGUI(); }
            if (Btn(new Rect(row.xMax - remove, row.y, remove, row.height), "Quitar", item != null)) { Run.UnequipGear(member, slots[i]); selectedGear = null; GUIUtility.ExitGUI(); }
        }
    }
    private void DrawWorldPieces(Rect r, bool selectable)
    {
        var pieces = Run.Inventory.Pieces.ToArray();
        if (pieces.Length == 0) { Text(r, "No tienes piezas. Busca piezas recuperables en cofres y restos: son escasas.", small, muted); return; }
        scroll = GUI.BeginScrollView(r, scroll, new Rect(0, 0, r.width - 20, pieces.Length * 82));
        for (int i = 0; i < pieces.Length; i++)
        {
            var row = new Rect(0, i * 82, r.width - 24, 76);
            if (selectable) { if (Btn(row, PieceText(pieces[i]), true, graft == pieces[i])) graft = pieces[i]; }
            else { ExplorationArt.Panel(row); Text(new Rect(12, row.y + 3, row.width - 24, row.height - 6), PieceText(pieces[i]), small); }
        }
        GUI.EndScrollView();
    }
    private void DrawMapOverview(Rect r)
    {
        var d = Run.Dungeon;
        Text(new Rect(r.x, r.y, r.width, 32), "EL PISO Y SUS COSTURAS", heading);
        Text(new Rect(r.x, r.y + 35, r.width, 45), "Los pasillos conectan todas las salas. Oro: posición actual. Verde: guardia resuelta.\nEl descenso requiere derrotar al guardián y resolver el altar.", small, muted);
        var bounds = new Rect(r.x + 15, r.y + 95, r.width - 30, r.height - 110);
        int minX = d.Rooms.Min(v => v.X), maxX = d.Rooms.Max(v => v.X), minY = d.Rooms.Min(v => v.Y), maxY = d.Rooms.Max(v => v.Y);
        float step = Mathf.Min(bounds.width / (maxX - minX + 2), bounds.height / (maxY - minY + 2));
        Func<int, int, Vector2> point = (x, y) => new Vector2(bounds.center.x + (x - (minX + maxX) / 2f) * step, bounds.center.y + (y - (minY + maxY) / 2f) * step);
        foreach (var room in d.Rooms)
        {
            var p = point(room.X, room.Y);
            foreach (var other in d.Rooms.Where(o => o.X == room.X + 1 && o.Y == room.Y || o.Y == room.Y + 1 && o.X == room.X)) BattlePainter.Line(p, point(other.X, other.Y), muted, 5);
        }
        foreach (var room in d.Rooms)
        {
            var p = point(room.X, room.Y); var box = new Rect(p.x - step * .29f, p.y - step * .25f, step * .58f, step * .50f);
            BattlePainter.Fill(box, room == d.Current ? new Color(.34f, .27f, .16f) : new Color(.13f, .15f, .12f));
            BattlePainter.Border(box, room == d.Current ? BattlePainter.Gold : room.Cleared ? BattlePainter.Green : muted, 2);
            Text(box, room == d.Exit ? "Salida" : room.X == 0 && room.Y == 0 ? "Inicio" : room.Visited ? "Vista" : "?", centered);
        }
    }
    private void DrawPuzzle(Rect r)
    {
        BattlePainter.Fill(new Rect(0, 67, Screen.width, Screen.height - 155), new Color(0, 0, 0, .7f));
        ExplorationArt.Panel(r);
        Text(new Rect(r.x + 25, r.y + 16, r.width - 50, 37), "LAS TRES COSTURAS", heading, BattlePainter.Gold);
        Text(new Rect(r.x + 25, r.y + 60, r.width - 50, 65), puzzleFeedback + $"\nSellos correctos: {Run.Dungeon.PuzzleProgress}/3", label);
        var options = new Rect(r.x + 25, r.y + r.height * .48f, r.width - 50, r.height * .25f);
        for (int i = 0; i < 3; i++)
            if (Btn(Cell(options, i, 3, 3), Dungeon.SealNames[i]))
            {
                bool correct = Run.ChooseSeal(i);
                puzzleFeedback = Run.Dungeon.PuzzleSolved ? "Los tres sellos encajan. El descenso está abierto." : correct ? "La piedra responde. Elige el siguiente sello." : "El altar rechaza el orden. La secuencia vuelve a empezar.";
                if (Run.Dungeon.PuzzleSolved) { puzzleMenu = false; GUIUtility.ExitGUI(); }
            }
        if (Btn(new Rect(r.x + 25, r.yMax - 55, r.width - 50, 34), "Dejar el altar")) { puzzleMenu = false; GUIUtility.ExitGUI(); }
    }
}
