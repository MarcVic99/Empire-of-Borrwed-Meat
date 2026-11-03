using System;
using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;
public sealed partial class Bootstrap
{
    private Rect combatPanel;
    private float actionRowHeight;
    private Rect ActionPanel(int rows, float width)
    {
        var vitals = AllyVitalsRect;
        float x = menu == Menu.Parts && anatomyPanel.width > 0 && anatomyPanel.x < Screen.width / 2f
            ? anatomyPanel.xMax + 8 : 12;
        width = Mathf.Min(width, Screen.width * .38f);
        float bottom = x + width + 8 > vitals.x ? vitals.y - 8 : Screen.height - 12;
        actionRowHeight = Mathf.Clamp((bottom - 48) / Mathf.Max(1, rows), 14, 36);
        float height = 42 + Mathf.Max(1, rows) * actionRowHeight;
        return new Rect(x, Mathf.Max(5, bottom - height), width, height);
    }

    private Rect ListPanel(string title, int rows, float width = 310)
    {
        combatPanel = ActionPanel(rows, width);
        ExplorationArt.Panel(combatPanel, .96f);
        Text(new Rect(combatPanel.x + 12, combatPanel.y + 4, combatPanel.width - 24, 28), title, label, BattlePainter.Gold);
        return ActionArea(combatPanel);
    }

    private static Rect ActionArea(Rect panel) => new Rect(panel.x + 8, panel.y + 35, panel.width - 16, panel.height - 41);
    private Rect ActionRow(Rect area, int i) => new Rect(area.x, area.y + i * actionRowHeight, area.width, actionRowHeight - 2);
    private bool Row(Rect area, int i, string text, bool enabled = true, bool selected = false) => Btn(ActionRow(area, i), text, enabled, selected);

    private void DrawArena(Rect r)
    {
        var stage = CameraRect(r);
        stage.xMin = Mathf.Min(stage.xMin, 0);
        stage.yMin = Mathf.Min(stage.yMin, 0);
        stage.xMax = Mathf.Max(stage.xMax, r.width);
        stage.yMax = Mathf.Max(stage.yMax, r.height);
        BattlePainter.Arena(stage);
        var foes = Battle.Aftermath.Enemies.Where(e => !e.IsPlayer && !e.HasFled)
            .Concat(Battle.Aftermath.Party.Where(c => c.IsThrown && !c.IsDead)).ToArray();
        var allies = Battle.Aftermath.Party.Where(c => (!c.IsDead || c == strikeTarget || c == spoken?.Speaker) && !c.IsThrown).ToArray();
        float ew = r.width * .82f / Mathf.Max(1, foes.Length);
        float aw = r.width * .76f / Mathf.Max(2, allies.Length);
        fighterBounds.Clear();
        for (int i = 0; i < foes.Length; i++)
            RememberFighter(foes[i], new Rect(r.width * .09f + i * ew, 48, ew, r.height * .50f));
        for (int i = 0; i < allies.Length; i++)
            RememberFighter(allies[i], new Rect(r.width * .17f + i * aw, r.height * .42f, aw, r.height * .74f));

        PrepareTargetHighlight();
        for (int i = 0; i < foes.Length; i++)
            Fighter(foes[i], new Rect(r.width * .09f + i * ew, 48, ew, r.height * .50f), foes[i].IsPlayer);
        for (int i = 0; i < allies.Length; i++)
            Fighter(allies[i], new Rect(r.width * .17f + i * aw, r.height * .42f, aw, r.height * .74f), true);
    }

    private void RememberFighter(Creature actor, Rect bounds) => fighterBounds[actor] = CharacterArt.Figure(bounds, actor);

    private void Fighter(Creature c, Rect r, bool rear)
    {
        r = FighterMotion(c, r, rear);
        var figure = CharacterArt.Figure(r, c);
        var old = GUI.color;
        GUI.color = FighterTint(c);
        var matrix = GUI.matrix;
        if (c == striker)
        {
            float age = Time.unscaledTime - strikeStarted;
            float lean = age < strikeImpact
                ? Mathf.Lerp(-4, 8, age / strikeImpact)
                : Mathf.Lerp(8, 0, (age - strikeImpact) / .24f);
            float side = strikeTarget != null && fighterBounds.TryGetValue(strikeTarget, out var victim)
                && victim.center.x < figure.center.x ? -1 : 1;
            GUIUtility.RotateAroundPivot(lean * side, new Vector2(figure.center.x, figure.yMax - figure.height * .13f));
        }
        BattlePainter.Creature(c, r, rear, !Battle.IsOver && Battle.Current == c);
        GUI.matrix = matrix;
        GUI.color = old;
        if (c == strikeTarget) DrawImpact(CharacterArt.Part(r, strikeSlot, rear, c));
        if (!rear && spoken == null)
            Text(new Rect(r.x, Mathf.Max(19, figure.y - 40), r.width, 22), c.Name + (c.HasSurrendered ? " · Rendido" : c.Enraged ? " · Furioso" : ""), centered);
        if (BattleInputLocked || c.IsDead || c.HasFled || !PlayerTurn) return;
        if (menu == Menu.AttackTarget || menu == Menu.TalkTarget)
        {
            bool eligible = menu == Menu.TalkTarget ? !c.IsPlayer : c != Battle.Current && ability != null && (ability.Definition.Kind != ActionKind.Charm || !c.IsPlayer);
            if (!eligible) return;
            if (c == highlightedTarget || figure.Contains(Event.current.mousePosition)) TargetPointer(figure);
            if (!PointerOverBattleUi() && GUI.Button(figure, GUIContent.none, GUIStyle.none))
            {
                if (menu == Menu.TalkTarget) { target = c; Battle.RecruitmentHint(c); Go(Menu.Talk); }
                else ChooseAttackTarget(c);
            }
            return;
        }

        if (menu != Menu.Parts || c != target || ability == null) return;
        TargetPointer(figure);
        bool blocked = PointerOverBattleUi();
        foreach (var slot in c.OccupiedSlots)
        {
            var part = CharacterArt.Part(r, slot, rear, c);
            if (!blocked && part.Contains(Event.current.mousePosition)) highlightedPart = slot;
            if (highlightedPart == slot)
            {
                BattlePainter.Border(part, BattlePainter.Gold, 2);
                Text(new Rect(part.center.x - 80, part.center.y - 22, 160, 21), SlotName(slot), centered, BattlePainter.Gold);
            }
            if (!blocked && GUI.Button(part, GUIContent.none, GUIStyle.none) && Battle.UseAbility(ability, c, slot)) Acted();
        }
    }

    private Creature[] AttackTargets() => ability == null ? Array.Empty<Creature>() : Battle.Creatures.Where(c => c != Battle.Current && !c.IsDead && !c.HasFled && (ability.Definition.Kind != ActionKind.Charm || !c.IsPlayer)).ToArray();
    private void ChooseAttackTarget(Creature c)
    { target = c; if (ability.Definition.Kind == ActionKind.Charm) { if (Battle.UseAbility(ability, c, BodySlot.Torso)) Acted(); return; } Go(Menu.Parts); }
    private void DrawBattleMenus()
    {
        if (!Battle.IsOver && !PlayerTurn) { var wait = ListPanel("Turno enemigo", 1); Text(wait, Battle.Current.Name + " prepara su acción…", small); return; }
        if (menu == Menu.Main && Battle.IsOver) menu = Menu.Aftermath;
        if (menu == Menu.Main)
        {
            var a = ListPanel(Battle.Current.Name, 6);
            if (Row(a, 0, "Atacar / Habilidades", true, true)) Go(Menu.Skills);
            if (Row(a, 1, "Negociar", !Battle.Current.IsThrown)) Go(Menu.TalkTarget);
            if (Row(a, 2, "Objetos")) Go(Menu.Inventory);
            if (Row(a, 3, $"Huir · {Battle.EscapeChance:P0}", !Battle.Current.IsThrown && Battle.EscapeChance > 0)) { Battle.TryEscape(); Acted(); }
            if (Row(a, 4, "Lanzar aliado", Battle.Aftermath.Party.Any(Battle.CanThrow))) Go(Menu.Throw);
            if (Row(a, 5, "Pasar turno")) { Battle.Pass(); Acted(); }
        }
        else if (menu == Menu.Skills)
        {
            var skills = Battle.Current.Abilities.Where(s => executionTarget == null || s.Definition.Kind == ActionKind.Attack).ToArray();
            var a = ListPanel("Elige una acción", skills.Length + 1, 350);
            for (int i = 0; i < skills.Length; i++)
            {
                var skill = skills[i]; var row = ActionRow(a, i);
                if (row.Contains(Event.current.mousePosition) || ability == skill)
                { var info = new Rect(a.xMax + 15, combatPanel.y, Mathf.Min(360, Screen.width - a.xMax - 35), 115); ExplorationArt.Panel(info); Text(new Rect(info.x + 14, info.y + 10, info.width - 28, info.height - 20), Describe(skill.Definition), small); }
                if (Row(a, i, skill.Definition.Name))
                {
                    if (skill.Definition.Kind == ActionKind.Block) { Battle.UseAbility(skill, null, BodySlot.Torso); Acted(); }
                    ability = skill;
                    if (executionTarget != null) { target = executionTarget; Go(Menu.Parts); }
                    Go(Menu.AttackTarget);
                }
            }
            if (Row(a, skills.Length, "← Volver")) Go(Menu.Main);
        }
        else if (menu == Menu.AttackTarget || menu == Menu.TalkTarget)
        {
            var options = menu == Menu.TalkTarget ? Battle.Aftermath.Enemies.Where(e => !e.IsDead && !e.IsPlayer && !e.HasFled).ToArray() : AttackTargets();
            var a = ListPanel(menu == Menu.TalkTarget ? "¿Con quién hablar?" : "Elige un objetivo", options.Length + 1, 350);
            for (int i = 0; i < options.Length; i++)
                if (Row(a, i, (options[i].IsPlayer ? "Aliado · " : "") + options[i].Name + (options[i].HasSurrendered ? " · Rendido" : "")))
                { target = options[i]; if (menu == Menu.TalkTarget) { Battle.RecruitmentHint(target); Go(Menu.Talk); } else ChooseAttackTarget(target); }
            if (Row(a, options.Length, "← Volver")) Go(Menu.Main);
        }
        else if (menu == Menu.Parts)
        {
            var a = ListPanel(target.Name + (target.IsPlayer ? " · ALIADO" : ""), AnatomySlots.Length + 1);
            for (int i = 0; i < AnatomySlots.Length; i++)
            {
                var slot = AnatomySlots[i];
                bool intact = target.GetPiece(slot) != null;
                if (Row(a, i, SlotName(slot) + (intact ? "" : " · Ausente"), intact, highlightedPart == slot)
                    && Battle.UseAbility(ability, target, slot)) Acted();
            }
            if (Row(a, AnatomySlots.Length, "← Volver")) Go(executionTarget == null ? Menu.AttackTarget : Menu.Skills);
            DrawTargetAnatomy();
        }
        else if (menu == Menu.Talk) DrawTalkCompact();
        else if (menu == Menu.Aftermath) DrawAftermathCompact();
        else if (menu == Menu.Corpse)
        {
            var a = ListPanel(target.Name, 3, 340);
            if (Row(a, 0, "Examinar restos / recoger hallazgos")) { Battle.Aftermath.Harvest(target); Acted(); }
            if (Row(a, 1, "Reanimar", Battle.Aftermath.HasPartySpace)) { Battle.Aftermath.Reanimate(target); Acted(); }
            if (Row(a, 2, "← Volver")) Go(Menu.Aftermath);
        }
        else if (menu == Menu.Throw || menu == Menu.ExecutionActor || menu == Menu.ItemTarget || menu == Menu.TalkActor)
        {
            var options = Battle.Aftermath.Party.Where(c => !c.IsDead && (menu != Menu.Throw || Battle.CanThrow(c))).ToArray();
            var a = ListPanel(menu == Menu.Throw ? "¿A quién lanzar?" : menu == Menu.ItemTarget ? "¿A quién tratar?" : menu == Menu.TalkActor ? "¿Quién negociará?" : "¿Quién atacará al rendido?", options.Length + 1, 350);
            for (int i = 0; i < options.Length; i++)
                if (Row(a, i, options[i].Name, menu == Menu.ItemTarget ? options[i].OccupiedSlots.Any(s => options[i].GetPiece(s).Health < options[i].GetPiece(s).MaxHealth) : menu == Menu.TalkActor ? Negotiation.CanUse(options[i], NegotiationChoice.Hablar) : menu != Menu.ExecutionActor || !options[i].IsLimbless && !options[i].ThrownAttackSpent))
                {
                    if (menu == Menu.Throw) { Battle.ThrowAlly(options[i]); Go(Menu.Skills); }
                    if (menu == Menu.ItemTarget) { if (Run.UseBalm(options[i])) Acted(); }
                    if (menu == Menu.TalkActor) { Battle.Aftermath.SelectNegotiator(options[i]); Go(Menu.Talk); }
                    if (menu == Menu.ExecutionActor && Battle.PrepareExecution(options[i], target)) { executionTarget = target; Go(Menu.Skills); }
                }
            if (Row(a, options.Length, "← Volver")) Go(Battle.IsOver ? Menu.Aftermath : Menu.Main);
        }
    }
    private void DrawTalkCompact()
    {
        var after = Battle.Aftermath; var choices = Enum.GetValues(typeof(NegotiationChoice)).Cast<NegotiationChoice>().Where(c => c != NegotiationChoice.Seducir || Negotiation.CanUse(after.Negotiator, c)).ToArray();
        int extras = Battle.IsOver ? 5 : 1; var a = ListPanel(target.Name + (target.Enraged ? " · Furioso" : ""), choices.Length + extras, 365);
        for (int i = 0; i < choices.Length; i++)
        {
            var c = choices[i]; bool can = c == NegotiationChoice.Ofrecer ? after.CanNegotiate(target, c, null, Math.Min(25, Battle.Inventory.Gold)) || Battle.Inventory.Pieces.Any(p => after.CanNegotiate(target, c, p)) : after.CanNegotiate(target, c);
            string text = c == NegotiationChoice.Ofrecer ? "Ofrecer objeto / oro" : c + $" · {after.NegotiationChance(target, c):P0}";
            if (Row(a, i, text, can)) { if (c == NegotiationChoice.Ofrecer) Go(Menu.Offer); after.Negotiate(target, c); Acted(); }
        }
        int n = choices.Length;
        if (Battle.IsOver)
        {
            if (Row(a, n++, "Elegir interlocutor")) Go(Menu.TalkActor);
            if (Row(a, n++, "Atacar al rendido")) Go(Menu.ExecutionActor);
            if (Row(a, n++, "Rematar")) { after.FinishOff(target); Acted(); }
            if (Row(a, n++, "Dejar marchar")) { after.Release(target); Acted(); }
        }
        if (Row(a, n, "← Volver")) Go(Battle.IsOver ? Menu.Aftermath : Menu.Main);
        string reason = target.Enraged ? "No será reclutable durante este combate." : after.Negotiator.Profession == Profession.Hunter && target.HasSurrendered ? "La Cazadora no puede reclutar a un rendido. Elige otro interlocutor." : "Interlocutor: " + after.Negotiator.Name + " · Los rechazos pueden enfurecerlo.";
        var hint = new Rect(a.xMax + 15, combatPanel.y, Mathf.Min(360, Screen.width - a.xMax - 35), 100); ExplorationArt.Panel(hint); Text(new Rect(hint.x + 14, hint.y + 8, hint.width - 28, hint.height - 16), reason, small);
    }
    private void DrawAftermathCompact()
    {
        var pending = Battle.Aftermath.Enemies.Where(c => Battle.Aftermath.Resolution(c) == EnemyResolution.Pending).ToArray();
        var a = ListPanel("Después del combate", pending.Length + 1, 360);
        for (int i = 0; i < pending.Length; i++)
            if (Row(a, i, pending[i].Name + (pending[i].IsDead ? " · Restos" : " · Rendido"))) { target = pending[i]; Go(target.IsDead ? Menu.Corpse : Menu.Talk); }
        if (Row(a, pending.Length, "Volver al mapa →", pending.Length == 0 && Run.State == CampaignState.BetweenCombats)) { Run.NextBattle(); GUIUtility.ExitGUI(); }
    }
    private void DrawBattleOverlay()
    {
        var r = new Rect(Screen.width * .15f, Screen.height * .10f, Screen.width * .70f, Screen.height * .78f); ExplorationArt.Panel(r);
        Text(new Rect(r.x + 20, r.y + 12, r.width - 130, 34), menu == Menu.Log ? "REGISTRO" : menu == Menu.Offer ? "OFRECER" : "OBJETOS", heading);
        if (Btn(new Rect(r.xMax - 105, r.y + 15, 85, 30), "Volver")) Go(menu == Menu.Offer ? Menu.Talk : Battle.IsOver ? Menu.Aftermath : Menu.Main);
        if (menu == Menu.Inventory)
        {
            Text(new Rect(r.x + 20, r.y + 52, r.width - 40, 35), $"{Run.Inventory.Gold} oro · {Run.Inventory.Threads} hilos · {Run.Balms} bálsamos", label);
            if (Btn(new Rect(r.x + 20, r.y + 94, r.width - 40, 36), "Usar bálsamo: elige personaje", PlayerTurn && Run.Balms > 0 && !Battle.Current.IsThrown)) Go(Menu.ItemTarget);
        }
        float top = menu == Menu.Inventory ? 145 : 58;
        var view = new Rect(r.x + 20, r.y + top, r.width - 40, r.height - top - 20);
        int[] gold = new[] { 10, 25, 50, 100, Battle.Inventory.Gold }.Where(v => v > 0).Distinct().ToArray();
        int count = menu == Menu.Log ? Battle.Log.Count : Battle.Inventory.Count + (menu == Menu.Offer ? gold.Length : Battle.Inventory.Equipment.Count);
        scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, view.width - 20, count * 65));
        for (int i = 0; i < count; i++)
        {
            var row = new Rect(0, i * 65, view.width - 24, 59);
            if (menu == Menu.Log) Text(row, Battle.Log[count - i - 1], small);
            else if (menu == Menu.Offer && i < gold.Length)
            { int amount = gold[i]; if (ItemButton(row, "gold", $"{amount} oro · {Battle.Aftermath.NegotiationChance(target, NegotiationChoice.Ofrecer, null, amount):P0}", Battle.Aftermath.CanNegotiate(target, NegotiationChoice.Ofrecer, null, amount))) { Battle.Aftermath.Negotiate(target, NegotiationChoice.Ofrecer, null, amount); Acted(); } }
            else
            {
                int index = i - (menu == Menu.Offer ? gold.Length : 0);
                if (index >= Battle.Inventory.Count) Text(row, "Equipo: " + Battle.Inventory.Equipment[index - Battle.Inventory.Count].Definition.Name, small);
                else { var piece = Battle.Inventory.Pieces[index]; if (menu == Menu.Offer) { if (Btn(row, piece.Definition.Name + $" · {Battle.Aftermath.NegotiationChance(target, NegotiationChoice.Ofrecer, piece):P0}", Battle.Aftermath.CanNegotiate(target, NegotiationChoice.Ofrecer, piece))) { Battle.Aftermath.Negotiate(target, NegotiationChoice.Ofrecer, piece); Acted(); } } else Text(row, PieceText(piece), small); }
            }
        }
        GUI.EndScrollView();
    }
    private void DrawFinal(Rect r)
    {
        Text(new Rect(r.x, r.y, r.width, 50), Run.State == CampaignState.Victory ? "LA SALIDA ESTÁ ABIERTA" : "LA COSTURA SE HA ROTO", heading, BattlePainter.Gold);
        Text(new Rect(r.x, r.y + 58, r.width, 50), $"{Run.Inventory.TotalHarvested} partes recuperadas · {Run.TotalRecruited} reclutas");
        if (Btn(new Rect(r.x, r.yMax - 55, r.width, 45), "Nueva run")) { Restart(); GUIUtility.ExitGUI(); }
    }
}
