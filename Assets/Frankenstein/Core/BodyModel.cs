using System;
using System.Collections.Generic;
using System.Linq;

namespace Frankenstein
{
    public enum BodySlot { Cabeza, Torso, BrazoIzq, BrazoDer, PiernaIzq, PiernaDer }
    public enum PieceKind { Head, Torso, Arm, Legs }
    public enum ActionKind { Attack, Block, Charm }
    public enum Race { Bestia, Humano, NoMuerto }
    public enum Profession { None, Hunter, Dancer, Grafter }
    public enum Sex { Male, Female, Other }
    public enum Behavior { Normal, TentacleFrenzy }

    public sealed class MonsterProfile
    {
        public readonly double InitialDisposition, InjurySensitivity, MutilationSensitivity, Power, BribeAffinity, CharmAffinity, SurrenderAt;
        public readonly BodySlot[] WeakPoints;
        public readonly PieceDefinition HeadReplacement;
        public MonsterProfile(double initialDisposition = .08, double injurySensitivity = 1, double mutilationSensitivity = 1,
            double power = 1, double bribeAffinity = 1, double charmAffinity = 1, double surrenderAt = .94,
            BodySlot[] weakPoints = null, PieceDefinition headReplacement = null)
        {
            InitialDisposition = initialDisposition; InjurySensitivity = injurySensitivity; MutilationSensitivity = mutilationSensitivity;
            Power = power; BribeAffinity = bribeAffinity; CharmAffinity = charmAffinity; SurrenderAt = surrenderAt;
            WeakPoints = weakPoints; HeadReplacement = headReplacement;
        }
    }

    public sealed class CombatAbility
    {
        public Creature Owner { get; }
        public ActionDefinition Definition { get; }
        public IReadOnlyList<BodySlot> Sources { get; }
        internal CombatAbility(Creature owner, ActionDefinition definition, IEnumerable<BodySlot> sources)
        { Owner = owner; Definition = definition; Sources = sources.ToList().AsReadOnly(); }
    }

    public sealed class ActionDefinition
    {
        public readonly string Name;
        public readonly ActionKind Kind;
        public readonly int Damage;
        public readonly double CriticalChance;
        public readonly int CriticalMultiplier, DotDamage, DotTurns;
        public readonly double Accuracy, Charm;
        public readonly string AcquiredWith;
        public ActionDefinition(string name, ActionKind kind, int damage = 0, double criticalChance = 0,
            int criticalMultiplier = 2, int dotDamage = 0, int dotTurns = 0, double accuracy = .90,
            string acquiredWith = "injerto", double charm = 0)
        { Name = name; Kind = kind; Damage = damage; CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier; DotDamage = dotDamage; DotTurns = dotTurns;
            Accuracy = accuracy; AcquiredWith = acquiredWith; Charm = charm; }
    }

    public sealed class PieceDefinition
    {
        public readonly string Id, Name;
        public readonly PieceKind Kind;
        public readonly int Health, BonusHealth, Defense, Speed, Damage;
        public readonly bool Vital;
        public readonly ActionDefinition Action;
        public readonly int Regeneration;
        public readonly double Evasion;
        public readonly int TradeValue;
        public PieceDefinition(string id, string name, PieceKind kind, int health,
            ActionDefinition action = null, int bonusHealth = 0, int defense = 0,
            int speed = 0, int damage = 0, bool vital = false, int regeneration = 0, double evasion = 0, int tradeValue = 12)
        {
            Id = id; Name = name; Kind = kind; Health = health; Action = action;
            BonusHealth = bonusHealth; Defense = defense; Speed = speed; Damage = damage; Vital = vital;
            Regeneration = regeneration; Evasion = evasion;
            TradeValue = tradeValue;
        }
    }

    public sealed class BodyPiece
    {
        public PieceDefinition Definition { get; }
        // There is no separate global HP pool. A health modifier belongs to its source piece.
        public int HealthMultiplier { get; }
        public int MaxHealth => (Definition.Health + Definition.BonusHealth) * HealthMultiplier;
        public int Health { get; private set; }
        public BodyPiece(PieceDefinition definition, int healthMultiplier = 1)
        { Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (healthMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(healthMultiplier));
            HealthMultiplier = healthMultiplier; Health = MaxHealth; }
        internal void Hurt(int damage) { Health = Math.Max(0, Health - damage); }
        internal void HealFully() { Health = MaxHealth; }
        internal void Heal(int amount) { Health = Math.Min(MaxHealth, Health + amount); }
    }

    public sealed class Creature
    {
        private readonly Dictionary<BodySlot, BodyPiece> pieces = new Dictionary<BodySlot, BodyPiece>();
        private readonly HashSet<BodySlot> startingSlots = new HashSet<BodySlot>();
        public string Name { get; }
        public string Appearance { get; }
        public Profession Profession { get; }
        public Sex Sex { get; }
        public MonsterProfile Profile { get; }
        public bool HasMutated { get; private set; }
        public Behavior Behavior => HasMutated ? Behavior.TentacleFrenzy : Behavior.Normal;
        public double Seduction { get; private set; }
        public double CombatPressure { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasSurrendered { get; private set; }
        public bool IsUndead { get; private set; }
        public bool HasFled { get; private set; }
        public Race Race { get; private set; }
        public int PiecesLost { get; private set; }
        public int NegotiationRejections { get; private set; }
        public int AngerThreshold { get; internal set; } = 2;
        public bool Enraged { get; private set; }
        public int MutilationsThisCombat { get; private set; }
        public double AttackMultiplier => (Profession == Profession.Hunter ? 1.15 : Profession == Profession.Dancer ? .70 : 1) * (Enraged ? 1.15 : 1);
        private readonly Dictionary<GearSlot, GearItem> gear = new Dictionary<GearSlot, GearItem>();
        public GearItem GetGear(GearSlot slot) => gear.TryGetValue(slot, out var item) ? item : null;
        public IEnumerable<GearItem> WornGear => gear.Values.Distinct();
        public bool CanEquipGear(GearSlot slot, GearItem item) => item != null && item.Definition.Fits(slot) && GetPiece(Equipment.BodyFor(slot)) != null &&
            (item.Definition.Hands < 2 || GetPiece(BodySlot.BrazoIzq) != null && GetPiece(BodySlot.BrazoDer) != null);
        internal GearItem RemoveGear(GearSlot slot)
        { var item = GetGear(slot); if (item != null) foreach (var key in gear.Where(p => p.Value == item).Select(p => p.Key).ToArray()) gear.Remove(key); return item; }
        internal IEnumerable<GearItem> Wear(GearSlot slot, GearItem item)
        {
            var removed = new List<GearItem>();
            if (!CanEquipGear(slot, item)) return removed;
            var old = RemoveGear(slot); if (old != null) removed.Add(old);
            if (item.Definition.Hands == 2)
            { old = RemoveGear(slot == GearSlot.ManoIzq ? GearSlot.ManoDer : GearSlot.ManoIzq); if (old != null) removed.Add(old); gear[GearSlot.ManoIzq] = gear[GearSlot.ManoDer] = item; }
            else gear[slot] = item;
            return removed;
        }
        internal IEnumerable<GearItem> ReleaseInvalidGear()
        {
            var removed = new List<GearItem>();
            foreach (var key in gear.Keys.ToArray())
            { var item = GetGear(key); if (item != null && !CanEquipGear(key, item)) { RemoveGear(key); removed.Add(item); } }
            return removed;
        }
        internal IEnumerable<GearItem> ReleaseAllGear() { var items = WornGear.ToArray(); gear.Clear(); return items; }
        public ActionDefinition ActionFor(BodySlot slot)
        {
            var piece = GetPiece(slot); if (piece == null) return null;
            if (slot == BodySlot.Cabeza && Profession == Profession.Dancer && Sex == Sex.Female && piece.Definition.Action == null) return Content.Dance;
            if (slot == BodySlot.BrazoIzq || slot == BodySlot.BrazoDer)
            { var item = GetGear(Equipment.HandFor(slot)); if (item != null && CanEquipGear(Equipment.HandFor(slot), item)) return item.Definition.Action; }
            return piece.Definition.Action;
        }
        public bool Blocking { get; internal set; }
        public bool IsThrown { get; internal set; }
        public bool ThrownAttackSpent { get; internal set; }
        public int LegCount => (GetPiece(BodySlot.PiernaIzq) != null ? 1 : 0) + (GetPiece(BodySlot.PiernaDer) != null ? 1 : 0);
        public bool IsLimbless => LegCount == 0 && GetPiece(BodySlot.BrazoIzq) == null && GetPiece(BodySlot.BrazoDer) == null;
        public double BalanceAccuracy => LegCount == 2 ? 1 : LegCount == 1 ? .72 : .45;
        public Creature(string name, bool isPlayer, IDictionary<BodySlot, PieceDefinition> body, Race race = Race.Humano,
            int healthMultiplier = 1, Profession profession = Profession.None, Sex sex = Sex.Other, MonsterProfile profile = null)
        {
            Appearance = body.TryGetValue(BodySlot.Cabeza, out var appearanceHead) ? appearanceHead.Id : "";
            Name = name; IsPlayer = isPlayer; Race = race; IsUndead = race == Race.NoMuerto;
            Profession = profession; Sex = sex; Profile = profile ?? new MonsterProfile();
            foreach (BodySlot slot in Enum.GetValues(typeof(BodySlot)))
            {
                PieceDefinition definition;
                pieces[slot] = body.TryGetValue(slot, out definition) ? new BodyPiece(definition, healthMultiplier) : null;
                if (pieces[slot] != null) startingSlots.Add(slot);
            }
        }
        public BodyPiece GetPiece(BodySlot slot) => pieces[slot];
        public IEnumerable<BodySlot> OccupiedSlots => pieces.Where(p => p.Value != null).Select(p => p.Key);
        public IEnumerable<BodySlot> ActionSlots => OccupiedSlots.Where(s => ActionFor(s) != null && (ActionFor(s).Kind != ActionKind.Charm || Sex == Sex.Female));
        public static readonly ActionDefinition ThrownImpact = new ActionDefinition("Embestida lanzada", ActionKind.Attack, 8, accuracy: .95, acquiredWith: "ser lanzado por un aliado");
        public IEnumerable<CombatAbility> Abilities => IsThrown && !ThrownAttackSpent ?
            ActionSlots.Where(s => ActionFor(s).Kind == ActionKind.Attack).GroupBy(ActionFor)
                .Select(g => new CombatAbility(this, g.Key, g)).Concat(new[] { new CombatAbility(this, ThrownImpact, new[] { BodySlot.Torso }) }) :
            ActionSlots.GroupBy(ActionFor).Select(g => new CombatAbility(this, g.Key, g)).Concat(
                Profession == Profession.Dancer && Sex == Sex.Female && GetPiece(BodySlot.Cabeza) != null && ActionFor(BodySlot.Cabeza) != Content.Dance ?
                    new[] { new CombatAbility(this, Content.Dance, new[] { BodySlot.Cabeza }) } : Enumerable.Empty<CombatAbility>());
        public int Defense => (int)Math.Floor((OccupiedSlots.Sum(s => pieces[s].Definition.Defense) + WornGear.Sum(g => g.Definition.Defense)) * (Profession == Profession.Dancer ? .35 : 1));
        public int Speed => OccupiedSlots.Sum(s => pieces[s].Definition.Speed) + WornGear.Sum(g => g.Definition.Speed) + (Profession == Profession.Hunter ? 2 : 0) - (LegCount == 0 ? 12 : LegCount == 1 ? 3 : 0);
        public int DamageBonus => OccupiedSlots.Sum(s => pieces[s].Definition.Damage);
        public int Regeneration => OccupiedSlots.Sum(s => pieces[s].Definition.Regeneration);
        public double Evasion => Math.Min(.75, OccupiedSlots.Sum(s => pieces[s].Definition.Evasion) + WornGear.Sum(g => g.Definition.Evasion) + (Profession == Profession.Dancer ? .25 : 0));
        // A diagnostic average of wounds, never a health pool and never used to kill a creature.
        public double WoundSeverity => startingSlots.Count == 0 ? 0 : startingSlots.Average(s =>
            pieces[s] == null ? 1 : 1 - (double)pieces[s].Health / pieces[s].MaxHealth);
        public int MissingGuardParts => startingSlots.Count(s => pieces[s] == null && !IsWeakPoint(s));
        public bool IsWeakPoint(BodySlot slot)
        {
            if (HasMutated && (slot == BodySlot.Cabeza || slot == BodySlot.Torso)) return true;
            return Profile.WeakPoints != null ? Profile.WeakPoints.Contains(slot) :
                slot == BodySlot.Cabeza || slot == BodySlot.Torso || pieces[slot] != null && pieces[slot].Definition.Vital;
        }
        internal BodyPiece TakeDamage(BodySlot slot, int damage)
        {
            BodyPiece piece = pieces[slot];
            bool weakPoint = IsWeakPoint(slot);
            int actual = Math.Min(damage, piece.Health);
            if (actual > 0) CombatPressure = Math.Min(1, CombatPressure +
                (Rules.DispositionPerHit + (double)actual / piece.MaxHealth * Rules.DispositionDamagePressure) *
                Profile.InjurySensitivity / Math.Max(.25, Profile.Power));
            piece.Hurt(damage);
            if (piece.Health > 0) return null;
            pieces[slot] = null;
            PiecesLost++; MutilationsThisCombat++;
            if (slot == BodySlot.Cabeza && !HasMutated && Profile.HeadReplacement != null)
            {
                HasMutated = true;
                pieces[slot] = new BodyPiece(Profile.HeadReplacement, piece.HealthMultiplier);
                Seduction = 0; Blocking = false;
            }
            else if (weakPoint || !OccupiedSlots.Any()) Kill();
            return piece;
        }
        internal void Surrender() { HasSurrendered = true; Blocking = false; }
        internal void Kill() { IsDead = true; HasSurrendered = false; Blocking = false; }
        internal BodyPiece Detach(BodySlot slot)
        {
            if (slot == BodySlot.Torso) return null;
            var piece = pieces[slot]; pieces[slot] = null; return piece;
        }
        internal BodyPiece Equip(BodySlot slot, BodyPiece piece)
        {
            if (slot == BodySlot.Torso) throw new InvalidOperationException("El torso no admite injertos.");
            var previous = pieces[slot]; pieces[slot] = piece; return previous;
        }
        internal void Reanimate()
        {
            IsDead = false; HasSurrendered = false; IsUndead = true; Race = Race.NoMuerto; IsPlayer = true; Blocking = false;
        }
        internal void Recruit() { HasSurrendered = false; IsPlayer = true; Blocking = false; }
        internal void Flee() { HasFled = true; HasSurrendered = false; Blocking = false; }
        internal void RejectNegotiation()
        { NegotiationRejections++; if (NegotiationRejections >= AngerThreshold) { Enraged = true; HasSurrendered = false; } }
        internal void ApplySeduction(double amount) { Seduction = Math.Min(1, Seduction + amount * Profile.CharmAffinity); }
        internal void ResetCombatState() { MutilationsThisCombat = 0; NegotiationRejections = 0; Enraged = false; Seduction = 0; CombatPressure = 0; Blocking = false; IsThrown = false; ThrownAttackSpent = false; }
        public void RecoverBetweenCombats()
        {
            if (IsDead || IsUndead) return;
            foreach (var slot in OccupiedSlots) pieces[slot].HealFully();
        }
    }
}
