using System;
using System.Collections.Generic;
using System.Linq;

namespace Frankenstein
{
    public sealed class Combat
    {
        private readonly Random random;
        private readonly Dictionary<string, int> resistedHits = new Dictionary<string, int>();
        public sealed class SpokenLine
        { public readonly Creature Speaker; public readonly string Text; public SpokenLine(Creature speaker, string text) { Speaker = speaker; Text = text; } }
        private readonly Queue<SpokenLine> dialogue = new Queue<SpokenLine>();
        private string lastSpeech = "";
        private Creature lastSpeaker;
        public SpokenLine NextDialogue() => dialogue.Count == 0 ? null : dialogue.Dequeue();
        public void Say(Creature speaker, string text)
        { if (speaker == lastSpeaker && text == lastSpeech) return; lastSpeaker = speaker; lastSpeech = text; dialogue.Enqueue(new SpokenLine(speaker, text)); }
        public void RecruitmentHint(Creature c)
        {
            Say(c, c.Enraged ? "¡Basta de palabras! No me uniré a vosotros." : c.Profile.BribeAffinity >= 1.3 ? "El oro pesa más que mis juramentos. Haz una oferta digna." :
                c.Profile.Power < 1 ? "No quiero acabar aquí. Quizá podamos llegar a un acuerdo." :
                c.Profile.CharmAffinity >= 1 ? "Esa danza... me cuesta mantener la guardia." : "No cederé tan fácilmente.");
        }
        private readonly Dictionary<Creature, List<BodyPiece>> cutPieces = new Dictionary<Creature, List<BodyPiece>>();
        private readonly Dictionary<Creature, List<GearItem>> droppedGear = new Dictionary<Creature, List<GearItem>>();
        private readonly HashSet<Creature> looted = new HashSet<Creature>();
        public bool PartyEscaped { get; private set; }
        public double SurrenderChance(Creature enemy, Creature attacker = null)
        {
            int cuts = enemy.MutilationsThisCombat; if (cuts == 0) return 0;
            double power = enemy.Profile.Power;
            double chance = power <= 1 ? .05 + .02 * (cuts - 1) : power < 2 ? .03 + .01 * (cuts - 1) : .01 + .005 * (cuts - 1);
            if (attacker != null && attacker.Profession == Profession.Hunter) chance *= 1.5;
            return Math.Min(.30, chance);
        }
        public double EscapeChance
        {
            get
            {
                var allies = creatures.Where(c => c.IsPlayer && !c.IsDead && !c.HasFled).ToArray();
                var foes = creatures.Where(c => !c.IsPlayer && !c.IsDead && !c.HasFled && !c.HasSurrendered).ToArray();
                if (allies.Sum(c => c.LegCount) == 0) return 0;
                Func<Creature, double> mobility = c => (.15 + c.LegCount) * Math.Max(.25, c.Profile.Power) * Math.Max(.2, 1 + c.Speed / 10.0);
                double ratio = allies.Sum(mobility) / Math.Max(.1, foes.Sum(mobility));
                return Math.Max(.02, Math.Min(.95, .45 * ratio * (allies.Any(c => c.LegCount == 0) ? .65 : 1)));
            }
        }
        public bool TryEscape()
        {
            if (IsOver || IsClosed || Current == null || !Current.IsPlayer || Current.IsThrown) return false;
            if (random.NextDouble() < EscapeChance)
            { Say(Current, "¡Retirada! Volvamos al vestíbulo."); PartyEscaped = IsOver = true; Current = null; log.Add("El grupo escapa sin botín ni recompensa."); return true; }
            Say(Current, "¡Nos cortan el paso!"); log.Add("La huida falla y consume el turno."); FinishAction(); return true;
        }
        public bool PrepareExecution(Creature actor, Creature victim)
        {
            if (!IsOver || IsClosed || Hero.IsDead || actor == null || actor.IsDead || !actor.IsPlayer || actor.IsLimbless || actor.ThrownAttackSpent ||
                !creatures.Contains(actor) || victim == null || !Aftermath.Enemies.Contains(victim) || victim.IsDead || !victim.HasSurrendered || Aftermath.Resolution(victim) != EnemyResolution.Pending) return false;
            Current = actor; IsOver = false; TurnCount++; return true;
        }
        internal void CollectRemains(Creature target)
        {
            if (!looted.Add(target)) return;
            var candidates = target.OccupiedSlots.Where(s => s != BodySlot.Torso).Select(target.GetPiece).ToList();
            if (cutPieces.TryGetValue(target, out var cuts)) candidates.AddRange(cuts);
            if (candidates.Count > 0 && random.NextDouble() < .35)
            { var part = candidates[random.Next(candidates.Count)]; Inventory.Harvest(part); log.Add("Hallazgo recuperable: " + part.Definition.Name + "."); }
            var equipment = target.ReleaseAllGear().ToList();
            if (droppedGear.TryGetValue(target, out var weapons)) equipment.AddRange(weapons);
            if (target.IsPlayer) foreach (var item in equipment.Distinct()) Inventory.AddEquipment(item);
            else if (equipment.Count > 0 && random.NextDouble() < .25) Inventory.AddEquipment(equipment[random.Next(equipment.Count)]);
        }
        public bool CanThrow(Creature ally) => !IsOver && !IsClosed && Current != null && Current.IsPlayer && !Current.IsThrown &&
            (Current.GetPiece(BodySlot.BrazoIzq) != null || Current.GetPiece(BodySlot.BrazoDer) != null) &&
            ally != null && ally != Current && creatures.Contains(ally) && ally.IsPlayer && !ally.IsDead && ally.IsLimbless && !ally.IsThrown;
        public bool ThrowAlly(Creature ally)
        {
            if (!CanThrow(ally)) return false;
            log.Add($"{Current.Name} emplea su turno en lanzar a {ally.Name}. Tiene un único ataque este combate.");
            ally.IsThrown = true; Current = ally; TurnCount++; return true;
        }
        public bool DeferTurnsForDialogue { get; set; }
        private bool advancePending;
        private void AdvanceOrWait() { if (DeferTurnsForDialogue && dialogue.Count > 0) advancePending = true; else Advance(); }
        public void ResumeAfterDialogue() { if (advancePending && dialogue.Count == 0) { advancePending = false; Advance(); } }
        private void FinishAction() { if (Current != null && Current.IsThrown) Current.ThrownAttackSpent = true; AdvanceOrWait(); }
        private readonly List<Creature> creatures;
        private readonly Queue<Creature> turns = new Queue<Creature>();
        private readonly List<string> log = new List<string>();
        private sealed class OngoingDamage
        {
            public Creature Target;
            public BodySlot Slot;
            public BodyPiece Piece;
            public int Damage, Remaining;
            public string Name;
        }
        private readonly List<OngoingDamage> effects = new List<OngoingDamage>();
        private readonly HashSet<Creature> joiningNextRound = new HashSet<Creature>();
        public IReadOnlyList<Creature> Creatures => creatures.AsReadOnly();
        public IReadOnlyList<string> Log => log.AsReadOnly();
        public PieceInventory Inventory { get; }
        public Creature Hero { get; }
        public Aftermath Aftermath { get; }
        public SewingStation Sewing { get; }
        public Creature Current { get; private set; }
        public int Round { get; private set; }
        public int TurnCount { get; private set; }
        public bool IsOver { get; private set; }
        public bool IsClosed { get; private set; }
        internal void Close() { IsClosed = true; }
        public string StatusFor(Creature target) => IsOver ? "" :
            (target.Seduction > 0 ? $"Seducción {target.Seduction:P0} · guardia baja. " : "") +
            string.Join(", ", effects.Where(e => e.Target == target && ReferenceEquals(target.GetPiece(e.Slot), e.Piece))
                .Select(e => $"{e.Name}: {e.Slot}, {e.Damage} × {e.Remaining} turnos"));
        public Combat(int seed) : this(seed, new[] { Content.Hero(), Content.Werewolf() }) { }
        public Combat(int seed, IEnumerable<Creature> combatants, PieceInventory inventory = null)
        {
            Inventory = inventory ?? new PieceInventory();
            random = new Random(seed); creatures = combatants.ToList();
            Hero = creatures.First(c => c.IsPlayer);
            foreach (var c in creatures) { c.ResetCombatState(); c.AngerThreshold = random.Next(1, 4); }
            Aftermath = new Aftermath(this);
            Sewing = new SewingStation(this);
            log.Add($"Duelo iniciado. Semilla: {seed}."); Advance();
        }
        private bool UpdateOutcome()
        {
            IsOver = PartyEscaped || Hero.IsDead || creatures.All(c => c.IsPlayer || c.IsDead || c.HasSurrendered || c.HasFled);
            if (IsOver)
            {
                foreach (var ally in creatures.Where(c => c.IsPlayer && c.IsDead && c != Hero)) CollectRemains(ally);
                Current = null;
                log.Add(!Hero.IsDead ? "Victoria. Decide el destino de cada enemigo." : "El héroe ha muerto. Derrota.");
                return true;
            }
            return false;
        }
        private void Advance()
        {
            while (!UpdateOutcome())
            {
                if (turns.Count == 0)
                {
                    if (!creatures.Any(c => !c.IsDead && !c.HasSurrendered && !c.HasFled && !c.IsLimbless && !c.ThrownAttackSpent))
                    { Hero.Kill(); UpdateOutcome(); return; }
                    Round++;
                    joiningNextRound.Clear();
                    foreach (var c in creatures.Where(c => !c.IsDead && !c.HasSurrendered && !c.HasFled).OrderByDescending(c => c.Speed)) turns.Enqueue(c);
                    log.Add($"Ronda {Round}: {string.Join(" → ", turns.Select(c => c.Name))}.");
                }
                Current = turns.Dequeue();
                if (Current.IsDead || Current.HasSurrendered || Current.HasFled || Current.IsLimbless || Current.ThrownAttackSpent || joiningNextRound.Contains(Current)) continue;
                if (Current.LegCount == 0 && Round % 2 == 0)
                { log.Add(Current.Name + " se arrastra y tarda en recuperar la posición."); continue; }
                Current.Blocking = false;
                TurnCount++;
                foreach (var effect in effects.Where(e => e.Target == Current).ToArray())
                {
                    if (Current.IsDead) break;
                    if (ReferenceEquals(Current.GetPiece(effect.Slot), effect.Piece))
                        DealDamage(Current, effect.Slot, effect.Damage, $"{effect.Name} persiste");
                    effect.Remaining--;
                }
                effects.RemoveAll(e => e.Remaining <= 0 || e.Target.IsDead || !ReferenceEquals(e.Target.GetPiece(e.Slot), e.Piece));
                if (!Current.IsDead)
                {
                    int regeneration = Current.Regeneration;
                    var wounded = Current.OccupiedSlots.Select(Current.GetPiece).Where(p => p.Health < p.MaxHealth)
                        .OrderByDescending(p => p.MaxHealth - p.Health).FirstOrDefault();
                    if (regeneration > 0 && wounded != null)
                    {
                        int restored = Math.Min(regeneration, wounded.MaxHealth - wounded.Health);
                        wounded.Heal(restored);
                        log.Add($"{Current.Name} regenera {restored} de vida en {wounded.Definition.Name}.");
                    }
                }
                if (UpdateOutcome()) return;
                if (Current.IsDead || Current.HasSurrendered) continue;
                return;
            }
        }
        public bool Act(BodySlot source, Creature target, BodySlot targetSlot)
        {
            if (IsOver || IsClosed || Current == null) return false;
            var piece = Current.GetPiece(source);
            if (piece == null || Current.ActionFor(source) == null) return false;
            var action = Current.ActionFor(source);
            return ExecuteAction(action, source, target, targetSlot);
        }
        private bool ExecuteAction(ActionDefinition action, BodySlot source, Creature target, BodySlot targetSlot)
        {
            if (Current.IsThrown && (Current.ThrownAttackSpent || action.Kind != ActionKind.Attack)) return false;
            if (action.Kind == ActionKind.Block)
            {
                Current.Blocking = true;
                log.Add($"{Current.Name} usa {action.Name}: daño recibido reducido al 50 % hasta su próximo turno.");
            }
            else if (action.Kind == ActionKind.Charm)
            {
                if (Current.Sex != Sex.Female || !ValidEnemy(target)) return false;
                double chance = HitChance(action, target, BodySlot.Torso);
                if (random.NextDouble() < chance)
                {
                    target.ApplySeduction(action.Charm * Negotiation.SeductionAffinity(target, Current));
                    log.Add($"{Current.Name}: {action.Name} → {target.Name}. Seducción {target.Seduction:P0}; baja su guardia.");
                }
                else log.Add($"{target.Name} resiste {action.Name} ({chance:P0} de precisión).");
            }
            else
            {
                if (!ValidAttack(target) || target.GetPiece(targetSlot) == null) return false;
                double chance = HitChance(action, target, targetSlot);
                if (random.NextDouble() >= chance)
                {
                    log.Add($"{Current.Name}: {action.Name} falla contra {target.Name}, {targetSlot} ({chance:P0} de precisión).");
                }
                else
                {
                    var struckPiece = target.GetPiece(targetSlot);
                    bool wasMutated = target.HasMutated;
                    bool critical = action.CriticalChance > 0 && random.NextDouble() < action.CriticalChance;
                    int raw = (int)Math.Round((action.Damage + (action.Damage > 0 ? Current.DamageBonus : 0)) * Current.AttackMultiplier);
                    if (critical) raw *= action.CriticalMultiplier;
                    int damage = Math.Max(0, raw - target.Defense);
                    if (target.Blocking) damage = (int)Math.Ceiling(damage * Rules.BlockDamageMultiplier);
                    DealDamage(target, targetSlot, damage, $"{Current.Name}: {action.Name} ({source})" + (critical ? " ¡CRÍTICO!" : ""));
                    if (!target.IsDead && !target.IsPlayer && target.HasMutated == wasMutated && Current.Sex == Sex.Female && action.Charm > 0)
                    {
                        target.ApplySeduction(action.Charm * Negotiation.SeductionAffinity(target, Current));
                        log.Add($"{target.Name}: seducción {target.Seduction:P0}; su guardia disminuye.");
                    }
                    if (!target.IsDead && ReferenceEquals(target.GetPiece(targetSlot), struckPiece) && action.DotTurns > 0)
                    {
                        effects.Add(new OngoingDamage { Target = target, Slot = targetSlot, Piece = target.GetPiece(targetSlot),
                            Damage = action.DotDamage, Remaining = action.DotTurns, Name = action.Name });
                        log.Add($"{target.Name}, {targetSlot}: {action.DotDamage} daño por turno durante {action.DotTurns} turnos.");
                    }
                }
            }
            FinishAction(); return true;
        }
        private bool ValidEnemy(Creature target) => target != null && creatures.Contains(target) && !target.IsDead &&
            !target.HasFled && target.IsPlayer != Current.IsPlayer;
        private bool ValidAttack(Creature target) => target != null && target != Current && creatures.Contains(target) && !target.IsDead && !target.HasFled;
        public double HitChance(ActionDefinition action, Creature target, BodySlot slot)
        {
            if (action == null || target == null) return 0;
            if (action.Kind == ActionKind.Block) return 1;
            double access = slot == BodySlot.Torso ? 1 : slot == BodySlot.Cabeza ? 1 : .85;
            if (action.Kind != ActionKind.Charm && target.IsWeakPoint(slot))
            {
                access = slot == BodySlot.Cabeza ? Rules.WeakHeadAccess : slot == BodySlot.Torso ? Rules.WeakTorsoAccess : Rules.WeakOtherAccess;
                access = Math.Min(1, access + target.MissingGuardParts * Rules.AccessPerMissingPart);
            }
            double chance = action.Accuracy * access * (1 - target.Evasion);
            if (action.Kind != ActionKind.Charm) chance += target.Seduction * Rules.SeductionAccuracyBonus;
            if (action.Kind == ActionKind.Attack && Current != null) chance *= Current.BalanceAccuracy;
            return Math.Max(Rules.MinimumAccuracy, Math.Min(Rules.MaximumAccuracy, chance));
        }
        public bool UseAbility(CombatAbility ability, Creature target, BodySlot targetSlot)
        {
            if (ability == null || ability.Owner != Current || IsOver || IsClosed) return false;
            if (ability.Definition == Content.Dance && Current.Profession == Profession.Dancer && Current.Sex == Sex.Female && Current.GetPiece(BodySlot.Cabeza) != null && !Current.IsThrown)
                return ExecuteAction(Content.Dance, BodySlot.Cabeza, target, targetSlot);
            if (Current.IsThrown && !Current.ThrownAttackSpent && ReferenceEquals(ability.Definition, Creature.ThrownImpact))
                return ExecuteAction(ability.Definition, BodySlot.Torso, target, targetSlot);
            foreach (var source in Current.ActionSlots)
                if (ReferenceEquals(Current.ActionFor(source), ability.Definition))
                    return Act(source, target, targetSlot);
            return false;
        }

        public double Willingness(Creature enemy)
        {
            int losses = Aftermath.Enemies.Count(e => e != enemy && (e.IsDead || e.HasFled || e.IsPlayer));
            int foes = Aftermath.Enemies.Count(e => !e.IsDead && !e.HasFled && !e.IsPlayer);
            int allies = creatures.Count(c => c.IsPlayer && !c.IsDead);
            return Negotiation.Willingness(enemy, losses, allies > foes);
        }
        internal void NegotiationFinished(Creature enemy, bool recruited)
        {
            if (IsOver && !enemy.Enraged) return;
            if (recruited) joiningNextRound.Add(enemy);
            AdvanceOrWait();
        }
        public bool Pass()
        {
            if (IsOver || IsClosed) return false;
            log.Add($"{Current.Name} pasa el turno."); FinishAction(); return true;
        }
        public bool StepEnemy()
        {
            if (IsOver || IsClosed || Current.IsPlayer) return false;
            var actions = Current.ActionSlots.ToArray();
            if (actions.Length == 0) return Pass();
            var source = Current.Behavior == Behavior.TentacleFrenzy && actions.Contains(BodySlot.Cabeza) && random.NextDouble() < Rules.MutantHeadActionChance ?
                BodySlot.Cabeza : actions[random.Next(actions.Length)];
            if (Current.ActionFor(source).Kind == ActionKind.Block) return Act(source, null, BodySlot.Cabeza);
            var targets = creatures.Where(c => c.IsPlayer && !c.IsDead).ToArray();
            var target = targets[random.Next(targets.Length)];
            var vital = target.OccupiedSlots.Where(target.IsWeakPoint).ToArray();
            var ordinary = target.OccupiedSlots.Where(s => !target.IsWeakPoint(s)).ToArray();
            var slots = random.NextDouble() < (Current.Behavior == Behavior.TentacleFrenzy ? Rules.MutantWeakTargetChance : Rules.AiVitalTargetChance) ? vital : ordinary;
            if (slots.Length == 0) slots = target.OccupiedSlots.ToArray();
            return Act(source, target, slots[random.Next(slots.Length)]);
        }
        internal void Record(string message) { log.Add(message); }
        internal double RollNegotiation() => random.NextDouble();
        private void DealDamage(Creature target, BodySlot slot, int damage, string source)
        {
            bool mutated = target.HasMutated;
            if (!target.IsPlayer)
            {
                string key = creatures.IndexOf(target) + ":" + slot;
                resistedHits.TryGetValue(key, out int count); resistedHits[key] = count + 1;
                if (target.IsWeakPoint(slot) && damage > 0 && count % 3 == 0)
                    Say(target, "¡Aparta eso de ahí! ¡No vuelvas a tocarme ahí!");
                else if (damage <= 3 && count >= 1)
                    Say(target, "Puedes seguir golpeando esa coraza. Apenas lo noto.");
                else if (count == 2) RecruitmentHint(target);
            }
            var cut = target.TakeDamage(slot, damage);
            log.Add($"{source} → {target.Name}, {slot}: {damage} daño.");
            if (cut != null)
            {
                if (slot != BodySlot.Torso)
                { if (!cutPieces.TryGetValue(target, out var list)) cutPieces[target] = list = new List<BodyPiece>(); list.Add(cut); }
                foreach (var item in target.ReleaseInvalidGear())
                {
                    if (target.IsPlayer) Inventory.AddEquipment(item);
                    else { if (!droppedGear.TryGetValue(target, out var list)) droppedGear[target] = list = new List<GearItem>(); list.Add(item); }
                }
                log.Add($"¡Corte! {target.Name} pierde {slot} ({cut.Definition.Name}).");
                if (!target.IsDead && !target.IsPlayer && !target.HasSurrendered && random.NextDouble() < SurrenderChance(target, Current))
                { target.Surrender(); Say(target, "¡Me rindo! ¡No sigas!"); log.Add(target.Name + " se rinde tras la mutilación."); }
            }
            if (!mutated && target.HasMutated)
                log.Add($"¡MUTACIÓN! De {target.Name} brota una corona de tentáculos. La criatura se retuerce y cambia su forma de atacar.");
            if (target.IsDead) { Say(target, "La voz se apaga y el cuerpo cae."); }
            if (target.IsDead) log.Add($"{target.Name} muere tras el corte.");
        }
    }
}
