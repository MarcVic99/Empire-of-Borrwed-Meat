using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Frankenstein;
public static class PrototypeChecks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void RunAll()
    {
        InputChecks.RunAll();
        AnatomyAndActions(); InventoryAndSewing(); AccuracyAndSeduction(); NegotiationAndGold();
        AftermathAndTurns(); LegsThrowAndDungeon(); ContinuousExploration(); CampaignAndEffects(); NorthSpriteFrames();
        UnityEngine.Debug.Log("Frankenstein: equipo separado, anatomía, hilos, clases, rendición, furia, huida, botín y campaña comprobados.");
    }
    public static void AnatomyAndActions()
    {
        Check(typeof(Creature).GetProperty("Health") == null, "No hay vida global");
        Check(!Enum.GetNames(typeof(Profession)).Any(n => n.Contains("Inquis")) && !Enum.GetNames(typeof(NegotiationChoice)).Contains("Amenazar"), "Solo las clases y opciones actuales");
        Check(Content.Pieces.Values.All(p => !p.Name.Contains("con daga") && !p.Name.Contains("con espada")), "Anatomía sin armas incorporadas");
        foreach (var id in new[] { "espada", "maza", "arco", "baculo", "cruz", "sierra", "aguja" }) Check(Content.Gear.ContainsKey(id), "Tipos de equipo disponibles");
        var victim = Content.Hero(Profession.Grafter); var leader = Probe(); var enemy = Content.Werewolf();
        var battle = new Combat(21, new[] { leader, victim, enemy });
        var saw = victim.Abilities.Single(a => a.Definition.Name == "Serrado");
        Cut(battle, leader, victim, BodySlot.BrazoIzq);
        Check(!victim.Abilities.Any(a => a.Definition.Name == "Serrado") && victim.Abilities.Any(a => a.Definition.Name == "Punción"), "Perder un brazo retira solo el equipo que dependía de él");
        Check(battle.Inventory.Equipment.Any(i => i.Definition.Id == "sierra") && battle.Inventory.Count == 0, "Arma soltada es equipo y no injerto automático");
        Turn(battle, victim); Check(!battle.UseAbility(saw, enemy, BodySlot.Torso), "Habilidad de arma perdida no puede reutilizarse");
        var bandit = Content.Enemy("bandido"); Damage(bandit, BodySlot.Cabeza, 9999);
        Check(bandit.IsDead && bandit.GetPiece(BodySlot.Torso).Health > 0, "Destruir cabeza mata aunque quede torso");
        var ghoul = Content.Enemy("ghoul"); Damage(ghoul, BodySlot.Cabeza, 9999);
        Check(ghoul.HasMutated && !ghoul.IsDead && ghoul.Abilities.Any(a => a.Definition.Name == "Latigazo tentacular"), "Primera decapitación cambia conducta y acciones");
        Damage(ghoul, BodySlot.Cabeza, 9999); Check(ghoul.IsDead, "Segunda decapitación mata");
        Check(Content.Pieces.Values.Where(p => p.Kind == PieceKind.Torso).All(p => p.Health >= 100), "Torsos generalmente muy resistentes");
    }
    public static void InventoryAndSewing()
    {
        var run = new Campaign(321, Profession.Hunter); var hero = run.Party[0];
        var part = new BodyPiece(Content.Pieces["brazo_orco"]); Add(run.Inventory, part);
        Check(!run.CanGraft && !run.Sew(hero, BodySlot.BrazoIzq, part), "Cazadora necesita mesa");
        run.Dungeon.Move(-3, 2.8); int threads = run.Inventory.Threads;
        Check(run.CanGraft && run.Sew(hero, BodySlot.BrazoIzq, part) && run.Inventory.Threads == threads - 2, "Costura normal consume dos hilos");
        Check(hero.GetGear(GearSlot.ManoIzq).Definition.Id == "espada" && hero.GetPiece(BodySlot.BrazoIzq) == part, "Injertar no convierte el arma en anatomía");
        Check(!run.Sew(hero, BodySlot.Torso, part), "Torso no es hueco de injertos");
        bool rejected = false; try { Add(run.Inventory, new BodyPiece(Content.Pieces["torso_humano"])); } catch (TargetInvocationException) { rejected = true; }
        Check(rejected && run.Inventory.Pieces.All(p => p.Definition.Kind != PieceKind.Torso), "Inventario impide torsos");
        var grafter = new Campaign(321, Profession.Grafter); var arm = new BodyPiece(Content.Pieces["brazo_orco"]); Add(grafter.Inventory, arm);
        Check(grafter.CanGraft && grafter.Sew(grafter.Party[0], BodySlot.BrazoDer, arm) && grafter.Inventory.Threads == 3, "Injertador cose sin mesa por un hilo");
        var reserve = new BodyPiece(Content.Pieces["pierna_humana"]); Add(grafter.Inventory, reserve); Call(grafter.Inventory, "SpendThreads", 3);
        Check(!grafter.Sew(grafter.Party[0], BodySlot.PiernaIzq, reserve) && grafter.Inventory.Pieces.Contains(reserve), "Sin hilos no se consume la pieza");
        var bow = new GearItem(Content.Gear["arco"]); Gear(run.Inventory, bow);
        Check(run.EquipGear(hero, GearSlot.ManoIzq, bow) && hero.GetGear(GearSlot.ManoIzq) == hero.GetGear(GearSlot.ManoDer), "Arco ocupa dos manos");
        Check(run.Inventory.Equipment.Count == 2, "Equipo anterior vuelve a la mochila");
        Check(run.UnequipGear(hero, GearSlot.ManoDer) && hero.GetGear(GearSlot.ManoIzq) == null && run.Inventory.Equipment.Count(i => i == bow) == 1, "Quitar arco no duplica objetos");
        Call(hero, "Detach", BodySlot.BrazoDer); Check(!run.EquipGear(hero, GearSlot.ManoIzq, bow), "No usar arco con un brazo");
    }
    public static void AccuracyAndSeduction()
    {
        var hunter = Content.Hero(); var dancer = Content.Hero(Profession.Dancer); var grafter = Content.Hero(Profession.Grafter);
        Check(dancer.Defense < hunter.Defense && dancer.Evasion > hunter.Evasion && dancer.AttackMultiplier < grafter.AttackMultiplier, "Debilidad y evasión de bailarina");
        var target = Content.Werewolf(); var battle = new Combat(4, new[] { hunter, target }); Turn(battle, hunter);
        var sword = Content.Gear["espada"].Action;
        Check(battle.HitChance(sword, target, BodySlot.Torso) > battle.HitChance(sword, target, BodySlot.BrazoIzq) && battle.HitChance(sword, target, BodySlot.Cabeza) < .3, "Torso más accesible y cabeza protegida");
        double protectedHead = battle.HitChance(sword, target, BodySlot.Cabeza); Damage(target, BodySlot.BrazoIzq, 9999);
        Check(battle.HitChance(sword, target, BodySlot.Cabeza) > protectedHead, "Mutilar abre la guardia");
        double whole = battle.HitChance(sword, target, BodySlot.Torso); Call(hunter, "Detach", BodySlot.PiernaIzq); double one = battle.HitChance(sword, target, BodySlot.Torso);
        Call(hunter, "Detach", BodySlot.PiernaDer); Check(battle.HitChance(sword, target, BodySlot.Torso) < one && one < whole && hunter.Speed < 0, "Piernas afectan precisión y velocidad");
        var male = Content.Enemy("bandido"); var female = Content.Enemy("bandida");
        Check(Negotiation.SeductionAffinity(male, dancer) > 1 && Negotiation.SeductionAffinity(female, dancer) < .2, "Afinidad sexual humana diferenciada");
        double maleBar = DanceBar(11, male), femaleBar = DanceBar(11, female);
        Check(maleBar > femaleBar * 5 && femaleBar > 0, "Ataque de danza respeta sexo y raza");
        Check(Negotiation.SuccessChance(male, NegotiationChoice.Seducir, actor: dancer) > Negotiation.SuccessChance(female, NegotiationChoice.Seducir, actor: dancer), "Reclutamiento por seducción respeta afinidad");
        Check(Negotiation.SuccessChance(Content.Enemy("bandido"), NegotiationChoice.Hablar, actor: grafter) < Negotiation.SuccessChance(Content.Enemy("bandido"), NegotiationChoice.Hablar, actor: hunter), "Injertador convence peor");
        Call(dancer, "Equip", BodySlot.Cabeza, new BodyPiece(Content.Pieces["cabeza_lobo"]));
        Check(dancer.Abilities.Any(a => a.Definition.Name == "Mordisco") && dancer.Abilities.Any(a => a.Definition == Content.Dance), "Un injerto conserva su acción natural junto a la especialidad de clase");
    }
    public static void NegotiationAndGold()
    {
        foreach (var entry in new[] { new { Id = "bandido", First = .05, More = .02 }, new { Id = "lobo", First = .03, More = .01 }, new { Id = "jefe", First = .01, More = .005 } })
        {
            var target = Content.Enemy(entry.Id); var battle = new Combat(5, new[] { Content.Hero(), target });
            Check(battle.SurrenderChance(target) == 0 && !target.HasSurrendered, "Rendición inicial cero");
            Damage(target, BodySlot.BrazoIzq, 9999); Check(Math.Abs(battle.SurrenderChance(target) - entry.First) < .0001, "Primer corte usa porcentaje por poder");
            Damage(target, BodySlot.BrazoDer, 9999); Check(Math.Abs(battle.SurrenderChance(target) - entry.First - entry.More) < .0001, "Incremento exacto por mutilación");
            Check(Math.Abs(battle.SurrenderChance(target, Content.Hero()) / battle.SurrenderChance(target) - 1.5) < .001, "Cazadora mejora rendición sin amenaza");
        }
        var body = Content.Enemy("bandido"); Damage(body, BodySlot.Torso, 15); var wait = new Combat(7, new[] { Content.Hero(), body });
        for (int i = 0; i < 100; i++) wait.Pass(); Check(!body.HasSurrendered, "Esperar o herir sin mutilar no provoca rendición");
        var surrendered = Content.Enemy("bandido"); Call(surrendered, "Surrender");
        var b = new Combat(2, new[] { Content.Hero(), surrendered });
        Check(b.IsOver && !b.Aftermath.CanNegotiate(surrendered, NegotiationChoice.Hablar), "Cazadora no recluta rendidos");
        var dancer = Content.Hero(Profession.Dancer); surrendered = Content.Enemy("bandido"); Call(surrendered, "Surrender");
        b = new Combat(2, new[] { dancer, surrendered }); Check(b.Aftermath.CanNegotiate(surrendered, NegotiationChoice.Seducir), "Bailarina puede negociar tras rendición");
        bool rageChecked = false;
        for (int seed = 0; seed < 50 && !rageChecked; seed++)
        {
            var enemy = Content.Enemy("jefe"); var actor = Content.Hero(Profession.Grafter); b = new Combat(seed, new[] { actor, enemy }); Turn(b, actor);
            typeof(Creature).GetProperty("AngerThreshold").SetValue(enemy, 1);
            if (b.Aftermath.Negotiate(enemy, NegotiationChoice.Hablar) != NegotiationResult.Rejected) continue;
            Check(enemy.Enraged && !b.Aftermath.CanNegotiate(enemy, NegotiationChoice.Hablar) && b.Aftermath.NegotiationChance(enemy, NegotiationChoice.Ofrecer, null, 100) == 0, "Rechazo puede bloquear todo reclutamiento este combate");
            new Combat(seed, new[] { actor, enemy }); Check(!enemy.Enraged && enemy.NegotiationRejections == 0, "Enfado se limpia en siguiente combate"); rageChecked = true;
        }
        Check(rageChecked, "Caso de furia ejercitado");
        var boss = Content.Enemy("jefe"); b = new Combat(8, new[] { Content.Hero(), boss }); Turn(b, b.Hero); b.Inventory.AddGold(120);
        Check(b.Aftermath.NegotiationChance(boss, NegotiationChoice.Ofrecer, null, 100) > b.Aftermath.NegotiationChance(boss, NegotiationChoice.Ofrecer, null, 10), "Valor del oro sigue importando");
        Check(b.Aftermath.Negotiate(boss, NegotiationChoice.Ofrecer, null, 25) != NegotiationResult.Unavailable && b.Inventory.Gold == 95, "Oferta consume oro una vez");
    }
    public static void AftermathAndTurns()
    {
        var ally = Content.Hero(Profession.Dancer); var leader = Probe(); var enemy = Content.Werewolf(); var b = new Combat(23, new[] { leader, ally, enemy });
        Cut(b, leader, ally, BodySlot.BrazoIzq); Check(ally.GetPiece(BodySlot.BrazoIzq) == null && !ally.IsDead, "Fuego amigo mutila aliados");
        var surrendered = Content.Enemy("bandido"); Call(surrendered, "Surrender"); b = new Combat(5, new[] { Probe(), surrendered });
        Check(b.IsOver && b.PrepareExecution(b.Hero, surrendered), "Se puede elegir actor para atacar al rendido");
        Cut(b, b.Hero, surrendered, BodySlot.Torso); Check(surrendered.IsDead && b.IsOver, "Ataque tras rendición puede matar");
        int drops = 0;
        for (int seed = 0; seed < 80; seed++)
        {
            enemy = Content.Enemy("bandido"); b = new Combat(seed, new[] { Probe(), enemy }); Cut(b, b.Hero, enemy, BodySlot.Torso);
            Check(b.Inventory.Count == 0, "No se obtienen miembros automáticamente por cortar");
            Check(b.Aftermath.Harvest(enemy) && b.Inventory.Count <= 1 && b.Inventory.Pieces.All(p => p.Definition.Kind != PieceKind.Torso), "Botín escaso de una parte como máximo y sin torsos");
            drops += b.Inventory.Count; int count = b.Inventory.Count;
            Check(!b.Aftermath.Harvest(enemy) && b.Inventory.Count == count, "No duplicar hallazgos");
        }
        Check(drops > 5 && drops < 50, "Piezas no garantizadas y frecuencia de botín baja");
        var run = new Campaign(12); ally = Content.Hero(Profession.Dancer); AddAlly(run, ally);
        Check(!run.RemoveAlly(run.Party[0], true) && run.RemoveAlly(ally, true) && ally.IsDead && run.Party.Count == 1 && run.Inventory.Count == 1, "Recuperar un aliado requiere matarlo y no permite matar líder desde menú");
        ally = Content.Hero(Profession.Grafter); AddAlly(run, ally); int gear = run.Inventory.Equipment.Count;
        Check(run.RemoveAlly(ally, false) && !ally.IsDead && !run.Party.Contains(ally) && run.Inventory.Equipment.Count == gear, "Abandono desaparece con su equipo");
    }
    public static void LegsThrowAndDungeon()
    {
        var hero = Content.Hero(); var enemy = Content.Werewolf(); var b = new Combat(3, new[] { hero, enemy });
        double healthy = b.EscapeChance; Call(hero, "Detach", BodySlot.PiernaIzq); double one = b.EscapeChance;
        Check(one < healthy, "Menos piernas aliadas dificultan huir");
        Call(enemy, "Detach", BodySlot.PiernaIzq); Call(enemy, "Detach", BodySlot.PiernaDer); Check(b.EscapeChance > one, "Enemigo lento y sin piernas facilita huida");
        Call(hero, "Detach", BodySlot.PiernaDer); Check(b.EscapeChance == 0, "Sin ninguna pierna aliada no se puede huir");
        var fast = new Combat(1, new[] { Content.Hero(), Content.Enemy("bandido") }); var strong = new Combat(1, new[] { Content.Hero(), Content.Enemy("jefe") });
        Check(fast.EscapeChance > strong.EscapeChance, "Poder y velocidad enemigos afectan huida");
        bool escaped = false, failed = false;
        for (int seed = 0; seed < 60 && (!escaped || !failed); seed++)
        {
            b = new Combat(seed, new[] { Content.Hero(), Content.Werewolf() }); Turn(b, b.Hero); int before = b.TurnCount;
            Check(b.TryEscape(), "Acción huir disponible");
            if (b.PartyEscaped) { Check(b.IsOver && b.Inventory.Count == 0, "Éxito de huida termina sin botín"); escaped = true; }
            else { Check(b.TurnCount > before, "Fallar huida consume turno"); failed = true; }
        }
        Check(escaped && failed, "Ambas ramas de huida ejercitadas");
        bool retreatChecked = false;
        for (int seed = 0; seed < 30 && !retreatChecked; seed++)
        {
            var run = new Campaign(seed); run.Dungeon.Move(7, 0); Check(run.BeginEncounter(), "Encuentro inicial para probar retirada");
            var fight = run.Battle; Turn(fight, fight.Hero); Damage(fight.Hero, BodySlot.Torso, 5);
            int hp = fight.Hero.GetPiece(BodySlot.Torso).Health, gold = run.Inventory.Gold;
            fight.TryEscape(); if (!fight.PartyEscaped) continue;
            run.UpdateState();
            Check(run.State == CampaignState.Exploring && !run.Dungeon.Rooms[1].Cleared && run.Dungeon.X == 0 && run.Dungeon.Y == 0, "Retirada vuelve al vestíbulo y conserva enemigo");
            Check(run.Inventory.Gold == gold && run.Inventory.Count == 0 && run.Party[0].GetPiece(BodySlot.Torso).Health == hp, "Huir no da recompensa ni cura heridas");
            retreatChecked = true;
        }
        Check(retreatChecked, "Retirada de campaña ejercitada");
        var passenger = Content.Hero(Profession.Grafter); foreach (var slot in new[] { BodySlot.BrazoIzq, BodySlot.BrazoDer, BodySlot.PiernaIzq, BodySlot.PiernaDer }) Call(passenger, "Detach", slot);
        b = new Combat(3, new[] { Content.Hero(), passenger, Content.Werewolf() }); Turn(b, b.Hero);
        Check(b.ThrowAlly(passenger) && passenger.IsPlayer && b.Current == passenger, "Lanzar conserva bando y da acción inmediata");
        Check(b.UseAbility(passenger.Abilities.Single(a => a.Definition == Creature.ThrownImpact), b.Aftermath.Enemies[0], BodySlot.Torso) && passenger.ThrownAttackSpent, "Un único ataque lanzado");
        for (int i = 0; i < 20; i++) { Check(b.Current != passenger, "No vuelve al orden del combate"); b.Pass(); }
        new Combat(4, new[] { b.Hero, passenger, Content.Werewolf() }); Check(!passenger.IsThrown, "Vuelve al bando al cambiar de combate");
    }
    public static void ContinuousExploration()
    {
        var d = new Dungeon(321); d.Move(5.8, 0); Check(Math.Abs(d.X - 5.8) < .001, "Pasillos continuos sin teletransporte");
        d.Move(0, 3); Check(d.Y < Dungeon.CorridorHalfWidth, "Colisión con pared de pasillo");
        Check(!d.HasSight(0, -4, 14, -4) && d.HasSight(0, 0, 14, 0), "Visión bloqueada por paredes");
        d = new Dungeon(321); d.Move(2, 0); var npc = d.Rooms[1]; double old = npc.EnemyX; d.Tick(.1); Check(npc.EnemyState == RoamingState.Pursue && npc.EnemyX < old, "IA persigue");
        npc.Enemies[0] = "bandida"; old = npc.EnemyX; d.Tick(.1); Check(npc.EnemyState == RoamingState.Flee && npc.EnemyX > old, "IA huye");
        var run = new Campaign(321); var thread = run.Dungeon.Landmarks.First(l => l.Kind == LandmarkKind.Thread);
        run.Dungeon.Move(thread.X, thread.Y); int before = run.Inventory.Threads;
        Check(run.Interact() == "thread" && run.Inventory.Threads == before + 2, "Hilos recogibles en el mapa"); run.Interact(); Check(run.Inventory.Threads == before + 2, "No duplicar hilos");
        run = new Campaign(321); run.Dungeon.Move(2.5, -2.6); Check(run.Interact() == "lore" && run.Journal.Any(n => n.Contains(run.Dungeon.PuzzleClue)), "Lore guarda secuencia");
        int parts = 0;
        for (int seed = 0; seed < 30; seed++)
        { var r = new Campaign(seed); r.Dungeon.Move(-2.7, -2.6); int gold = r.Inventory.Gold; r.Interact(); Check(r.Inventory.Gold > gold && r.Inventory.Threads >= 5, "Cofres contienen recursos"); parts += r.Inventory.Count; int c = r.Inventory.Threads; r.Interact(); Check(r.Inventory.Threads == c, "Cofre no duplica recursos"); }
        Check(parts < 20, "Los cofres no garantizan injertos");
    }
    public static void CampaignAndEffects()
    {
        foreach (var role in new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter })
        {
            var run = new Campaign(12345, role);
            for (int floor = 1; floor <= Dungeon.LastFloor; floor++)
            {
                var d = run.Dungeon; Travel(d, d.Exit); d.Move(d.Exit.EnemyX - d.X, d.Exit.EnemyY - d.Y); Check(run.BeginEncounter(), "Encuentro por contacto");
                var b = run.Battle;
                for (int turn = 0; turn < 8000 && !b.IsOver; turn++)
                {
                    if (!b.Current.IsPlayer) { b.Pass(); continue; }
                    var victim = b.Aftermath.Enemies.First(e => !e.IsDead && !e.HasSurrendered && !e.IsPlayer);
                    var skill = b.Current.Abilities.Where(a => a.Definition.Kind == ActionKind.Attack).OrderByDescending(a => a.Definition.Damage).First();
                    var slot = victim.IsWeakPoint(BodySlot.Torso) ? BodySlot.Torso : victim.OccupiedSlots.First(victim.IsWeakPoint);
                    Check(b.UseAbility(skill, victim, slot), "Acción válida en campaña");
                }
                Check(b.IsOver && !b.Hero.IsDead, "Combate de prueba termina");
                foreach (var enemy in b.Aftermath.Enemies) { if (enemy.HasSurrendered) b.Aftermath.FinishOff(enemy); b.Aftermath.Harvest(enemy); }
                run.UpdateState(); run.UpdateState(); Check(run.Summaries.Count == floor && run.Inventory.Gold == Rules.StartingGold + floor * Rules.GoldRewardPerEncounter, "Recompensa y resumen sin duplicar");
                Check(run.NextBattle() && d.Exit.Cleared, "Regreso al mapa conserva victoria");
                d.Move(d.Exit.CenterX - d.X, d.Exit.CenterY - d.Y); d.Move(d.Exit.CenterX + 2.8 - d.X, d.Exit.CenterY - 2.6 - d.Y);
                foreach (var seal in d.PuzzleClue.Split(new[] { " → " }, StringSplitOptions.None)) Check(run.ChooseSeal(Array.IndexOf(Dungeon.SealNames, seal)), "Puzle del piso resoluble");
                d.Move(d.Exit.CenterX - d.X, d.Exit.CenterY - 3 - d.Y); Check(run.UseStairs(), "Avanza por escalera");
            }
            Check(run.State == CampaignState.Victory, "Tres clases pueden completar progresión");
        }
        var talker = Content.Enemy("jefe"); var combat = new Combat(3, new[] { Content.Hero(), talker }); combat.RecruitmentHint(talker);
        var line = combat.NextDialogue(); Check(line != null && line.Speaker == talker && line.Text.Contains("oro") && combat.NextDialogue() == null, "Diálogos conservan hablante y se consumen una sola vez");
        var deferredVictim = Content.Enemy("bandido"); combat = new Combat(4, new[] { Probe(), deferredVictim }); Turn(combat, combat.Hero);
        combat.DeferTurnsForDialogue = true; int before = combat.TurnCount;
        for (int i = 0; i < 20 && !deferredVictim.IsDead; i++) combat.Act(BodySlot.BrazoIzq, deferredVictim, BodySlot.Torso);
        Check(deferredVictim.IsDead && !combat.IsOver && combat.TurnCount == before, "Con presentación activa no avanza turno ni desenlace antes del diálogo");
        while (combat.NextDialogue() != null) { }
        combat.ResumeAfterDialogue(); Check(combat.IsOver, "Desenlace continúa después de consumir el diálogo");
        Check(Trace(91) == Trace(91), "IA de combate reproducible");
    }
    public static void NorthSpriteFrames()
    {
        foreach (var role in new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter })
        {
            string name = role == Profession.Hunter ? "topdown-hunter" : role == Profession.Dancer ? "topdown-dancer-v2" : "topdown-grafter-v2";
            string imperial = "Assets/Frankenstein/Resources/Frankenstein/Imperio/walk-" + Frankenstein.Presentation.ImperialArt.Role(role) + ".png";
            string path = System.IO.File.Exists(imperial) ? imperial : "Assets/Frankenstein/Resources/Frankenstein/Exploration/" + name + ".png";
            var texture = new UnityEngine.Texture2D(2, 2);
            UnityEngine.ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(path));
            for (int frame = 0; frame < 4; frame++)
            {
                var uv = Frankenstein.Presentation.ExplorationArt.FrameUV(role, 3, frame);
                int y = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(uv.yMax * texture.height), 0, texture.height - 1);
                int occupied = 0;
                for (int x = (int)(uv.x * texture.width); x < (int)(uv.xMax * texture.width); x++) if (texture.GetPixel(x, y).a > .2) occupied++;
                Check(occupied == 0, "El borde superior no corta cabeza, pelo o sombrero en ningún frame norte de " + name);
            }
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    private static string Trace(int seed)
    { var b = new Combat(seed, new[] { Content.Hero(), Content.Enemy("ghoul") }); for (int i = 0; i < 3000 && !b.IsOver; i++) { if (b.Current.IsPlayer) b.Pass(); else b.StepEnemy(); } Check(b.IsOver, "IA termina sin bloqueos"); return string.Join("|", b.Log); }
    private static double DanceBar(int seed, Creature target)
    { var dancer = Content.Hero(Profession.Dancer); var b = new Combat(seed, new[] { dancer, target }); for (int i = 0; i < 30 && target.Seduction == 0; i++) { Turn(b, dancer); b.Act(BodySlot.Cabeza, target, BodySlot.Torso); } return target.Seduction; }
    private static void Travel(Dungeon d, DungeonRoom to)
    {
        var prev = new Dictionary<DungeonRoom, DungeonRoom>(); var q = new Queue<DungeonRoom>(); q.Enqueue(d.Current); prev[d.Current] = null;
        while (q.Count > 0) { var r = q.Dequeue(); foreach (var n in d.Rooms.Where(n => Math.Abs(n.X - r.X) + Math.Abs(n.Y - r.Y) == 1)) if (!prev.ContainsKey(n)) { prev[n] = r; q.Enqueue(n); } }
        Check(prev.Count == d.Rooms.Count, "Mapa conectado"); var path = new List<DungeonRoom>(); for (var r = to; r != d.Current; r = prev[r]) path.Add(r); path.Reverse();
        foreach (var r in path) { d.Move(d.Current.CenterX - d.X, d.Current.CenterY - d.Y); d.Move(r.CenterX - d.X, r.CenterY - d.Y); Check(d.Current == r, "Pasillo alcanza sala"); }
    }
    private static Creature Probe()
    {
        var normal = Content.Hero(); var body = normal.OccupiedSlots.ToDictionary(s => s, s => normal.GetPiece(s).Definition);
        body[BodySlot.BrazoIzq] = new PieceDefinition("prueba", "Brazo de prueba", PieceKind.Arm, 999, new ActionDefinition("Impacto de prueba", ActionKind.Attack, 9999, accuracy: 1));
        return new Creature("Testigo", true, body);
    }
    private static void Turn(Combat b, Creature actor) { for (int i = 0; i < 80 && !b.IsOver && b.Current != actor; i++) b.Pass(); Check(!b.IsOver && b.Current == actor, "Actor de prueba disponible"); }
    private static void Cut(Combat b, Creature actor, Creature target, BodySlot slot)
    { var old = target.GetPiece(slot); for (int i = 0; i < 100 && !b.IsOver && ReferenceEquals(old, target.GetPiece(slot)); i++) { Turn(b, actor); b.Act(BodySlot.BrazoIzq, target, slot); } Check(!ReferenceEquals(old, target.GetPiece(slot)), "Corte esperado"); }
    private static void Damage(Creature c, BodySlot slot, int amount) => Call(c, "TakeDamage", slot, amount);
    private static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Hidden).Invoke(o, args);
    private static void Add(PieceInventory inventory, BodyPiece p) => Call(inventory, "Add", p);
    private static void Gear(PieceInventory inventory, GearItem g) => Call(inventory, "AddEquipment", g);
    private static void AddAlly(Campaign run, Creature ally) => ((List<Creature>)typeof(Campaign).GetField("party", Hidden).GetValue(run)).Add(ally);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
