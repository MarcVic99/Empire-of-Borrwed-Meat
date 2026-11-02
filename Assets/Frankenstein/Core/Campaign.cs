using System.Collections.Generic;
using System.Linq;
namespace Frankenstein
{
    public enum CampaignState { Exploring, Fighting, BetweenCombats, Victory, Defeat }
    public sealed class CombatSummary
    {
        public readonly string Name;
        public readonly int Turns, Harvested, Recruited;
        public readonly bool Won;
        public CombatSummary(string name, int turns, int harvested, int recruited, bool won)
        { Name = name; Turns = turns; Harvested = harvested; Recruited = recruited; Won = won; }
    }
    public sealed class Campaign
    {
        private readonly List<CombatSummary> summaries = new List<CombatSummary>();
        private readonly List<Creature> party = new List<Creature>();
        private readonly System.Random worldRandom;
        private int harvestedAtStart;
        private DungeonRoom encounterRoom;
        private readonly List<string> journal = new List<string>();
        public IReadOnlyList<string> Journal => journal.AsReadOnly();
        public string Notice { get; private set; } = "Busca la nota del vestíbulo y explora el corredor del este.";
        public int Balms { get; private set; } = 2;
        public Creature Surgeon => party.FirstOrDefault(c => !c.IsDead && c.Profession == Profession.Grafter) ?? party.First(c => !c.IsDead);
        public int ThreadCost => Surgeon.Profession == Profession.Grafter ? 1 : 2;
        public bool CanGraft => State == CampaignState.Exploring && (Dungeon.NearTable || Surgeon.Profession == Profession.Grafter);
        public int Seed { get; }
        public int EncounterIndex { get; private set; }
        public Combat Battle { get; private set; }
        public Dungeon Dungeon { get; }
        public IReadOnlyList<Creature> Party => party.AsReadOnly();
        public string EncounterName { get; private set; } = "";
        public PieceInventory Inventory { get; } = new PieceInventory();
        public CampaignState State { get; private set; } = CampaignState.Exploring;
        public IReadOnlyList<CombatSummary> Summaries => summaries.AsReadOnly();
        public bool IsFinished => State == CampaignState.Victory || State == CampaignState.Defeat;
        public int TotalRecruited => summaries.Sum(s => s.Recruited);
        public Profession StartingClass { get; }
        public Campaign(int seed, Profession startingClass = Profession.Hunter)
        {
            worldRandom = new System.Random(unchecked(seed ^ 513713));
            Seed = seed; StartingClass = startingClass; Dungeon = new Dungeon(seed);
            Inventory.AddGold(Rules.StartingGold); party.Add(Content.Hero(startingClass));
        }
        private int RecruitsInBattle() => Battle.Aftermath.Enemies.Count(e =>
            Battle.Aftermath.Resolution(e) == EnemyResolution.Recruited || Battle.Aftermath.Resolution(e) == EnemyResolution.Reanimated);
        public bool BeginEncounter()
        {
            if (State != CampaignState.Exploring || !Dungeon.TouchingEnemy) return false;
            foreach (var c in party) c.ResetCombatState();
            encounterRoom = Dungeon.ContactEnemy;
            var ids = encounterRoom.Enemies;
            EncounterName = $"Piso {Dungeon.Floor} · Sala {encounterRoom.X}, {encounterRoom.Y}";
            harvestedAtStart = Inventory.TotalHarvested;
            Battle = new Combat(unchecked(Seed + EncounterIndex * Rules.EncounterSeedStep), party.Concat(ids.Select((id, i) => Content.Enemy(id, ids.Length > 1 ? " " + (i + 1) : ""))), Inventory);
            State = CampaignState.Fighting; return true;
        }
        public void UpdateState()
        {
            if (State != CampaignState.Fighting || !Battle.IsOver) return;
            if (Battle.PartyEscaped)
            {
                party.Clear(); party.AddRange(Battle.Aftermath.Party.Where(c => !c.IsDead));
                Dungeon.Retreat(encounterRoom); Battle.Close(); EncounterIndex++;
                Notice = "El grupo ha escapado al vestíbulo. El enemigo sigue en el piso; no hay botín ni recompensa.";
                State = CampaignState.Exploring; return;
            }
            if (!Battle.Hero.IsDead && !Battle.Aftermath.IsComplete) return;
            summaries.Add(new CombatSummary(EncounterName, Battle.TurnCount, Inventory.TotalHarvested - harvestedAtStart, RecruitsInBattle(), !Battle.Hero.IsDead));
            if (Battle.Hero.IsDead) { State = CampaignState.Defeat; Battle.Close(); return; }
            encounterRoom.Cleared = true; Inventory.AddGold(Rules.GoldRewardPerEncounter);
            party.Clear(); party.AddRange(Battle.Aftermath.Party.Where(c => !c.IsDead));
            foreach (var member in party) { member.Blocking = false; member.RecoverBetweenCombats(); }
            State = CampaignState.BetweenCombats;
        }
        public bool NextBattle()
        {
            UpdateState(); if (State != CampaignState.BetweenCombats) return false;
            Battle.Close(); EncounterIndex++; State = CampaignState.Exploring; return true;
        }
        public string Interact()
        {
            if (State != CampaignState.Exploring) return "";
            var point = Dungeon.Nearby;
            if (point == null) { Notice = "Acércate a un objeto para examinarlo."; return ""; }
            if (point.Kind == LandmarkKind.Chest && !point.Used)
            {
                point.Used = true; Inventory.AddGold(18 + Dungeon.Floor * 7); Balms++;
                Inventory.AddThreads(1 + worldRandom.Next(2));
                string found = "Cofre abierto: oro, bálsamo e hilos.";
                if (worldRandom.NextDouble() < .20)
                { var ids = new[] { "brazo_humano", "pierna_humana", "brazo_orco", "pata_lobo" }; var part = Content.Pieces[ids[worldRandom.Next(ids.Length)]]; Inventory.Add(new BodyPiece(part)); found += " También hallas " + part.Name + "."; }
                if (worldRandom.NextDouble() < .45)
                { var gear = Content.Gear.Values.ToArray(); var item = gear[worldRandom.Next(gear.Length)]; Inventory.AddEquipment(new GearItem(item)); found += " Equipo: " + item.Name + "."; }
                Notice = found; journal.Add(found); return "chest";
            }
            if (point.Kind == LandmarkKind.Thread)
            { point.Used = true; Inventory.AddThreads(2); Notice = "Recoges dos hilos de sutura."; return "thread"; }

            if (point.Kind == LandmarkKind.Lore)
            {
                Notice = point.Text; if (!journal.Contains(point.Text)) journal.Add(point.Text); return "lore";
            }
            if (point.Kind == LandmarkKind.Table) { Notice = point.Text; return "grafts"; }
            if (point.Kind == LandmarkKind.Altar)
            {
                Notice = Dungeon.Exit.HasEncounter ? "La presencia de su guardián impide tocar los sellos." : Dungeon.PuzzleSolved ? "Los sellos ya han abierto el descenso." : point.Text;
                return Dungeon.Exit.HasEncounter || Dungeon.PuzzleSolved ? "" : "puzzle";
            }
            if (point.Kind == LandmarkKind.Stairs)
            {
                if (!UseStairs()) Notice = Dungeon.Exit.HasEncounter ? "El guardián bloquea el descenso." : "El descenso está sellado. Examina el altar y la nota del vestíbulo.";
                return "stairs";
            }
            return "";
        }
        public bool UseBalm(Creature member)
        {
            bool battleUse = State == CampaignState.Fighting && !Battle.IsOver && Battle.Current != null && Battle.Current.IsPlayer && !Battle.Current.IsThrown;
            if ((State != CampaignState.Exploring && !battleUse) || Balms <= 0 || member == null || member.IsDead ||
                !(battleUse ? Battle.Aftermath.Party.Contains(member) : party.Contains(member))) return false;
            var wounds = member.OccupiedSlots.Select(member.GetPiece).Where(p => p.Health < p.MaxHealth).ToArray();
            if (wounds.Length == 0) { Notice = "Ese personaje no tiene heridas que tratar."; return false; }
            foreach (var piece in wounds) piece.Heal(12);
            Balms--; Notice = "Aplicaste bálsamo a " + member.Name + ".";
            if (battleUse) Battle.Pass();
            return true;
        }
        public bool ChooseSeal(int seal)
        {
            if (State != CampaignState.Exploring) return false;
            bool correct = Dungeon.ChooseSeal(seal);
            Notice = Dungeon.PuzzleSolved ? "Los sellos encajan. El descenso está abierto." :
                correct ? "La piedra responde al sello." : "El altar rechaza el orden. Vuelve a empezar.";
            if (correct && Dungeon.PuzzleSolved) journal.Add("Piso " + Dungeon.Floor + ": altar resuelto, descenso abierto.");
            return correct;
        }
        public bool CanSew(Creature member, BodySlot slot, BodyPiece piece) => CanGraft && member != null &&
            party.Contains(member) && !member.IsDead && (!member.IsUndead || Rules.AllowSewingUndead) &&
            Inventory.Threads >= ThreadCost && piece != null && Inventory.Pieces.Contains(piece) && SewingStation.Fits(piece.Definition, slot);
        public bool Sew(Creature member, BodySlot slot, BodyPiece piece)
        {
            if (!CanSew(member, slot, piece)) return false;
            Inventory.SpendThreads(ThreadCost); Inventory.Remove(piece); var old = member.Equip(slot, piece); piece.HealFully();
            if (old != null) Inventory.Add(old);
            Notice = "Injerto instalado en " + member.Name + "."; return true;
        }
        public bool EquipGear(Creature target, GearSlot slot, GearItem item)
        {
            if (State != CampaignState.Exploring || target == null || !party.Contains(target) || target.IsDead ||
                item == null || !Inventory.Equipment.Contains(item) || !target.CanEquipGear(slot, item)) return false;
            Inventory.RemoveEquipment(item);
            foreach (var old in target.Wear(slot, item)) Inventory.AddEquipment(old);
            Notice = target.Name + " equipa " + item.Definition.Name + "."; return true;
        }
        public bool UnequipGear(Creature target, GearSlot slot)
        {
            if (State != CampaignState.Exploring || target == null || !party.Contains(target) || target.IsDead || target.GetGear(slot) == null) return false;
            Inventory.AddEquipment(target.RemoveGear(slot)); Notice = "Objeto devuelto al inventario."; return true;
        }
        public bool RemoveAlly(Creature target, bool harvest)
        {
            if (State != CampaignState.Exploring || target == null || target == party[0] || !party.Contains(target) || target.IsDead) return false;
            if (harvest)
            {
                target.Kill(); var parts = target.OccupiedSlots.Where(s => s != BodySlot.Torso).ToArray();
                if (parts.Length > 0) Inventory.Harvest(target.Detach(parts[worldRandom.Next(parts.Length)]));
                foreach (var item in target.ReleaseAllGear()) Inventory.AddEquipment(item);
                Notice = "Matas a " + target.Name + (parts.Length > 0 ? ". Recuperas una pieza aprovechable y su equipo." : ". No quedan partes aprovechables; recuperas su equipo.");
            }
            else Notice = "Abandonas a " + target.Name + ". Desaparece con lo que llevaba.";
            party.Remove(target); return true;
        }
        public bool UseStairs()
        {
            if (State != CampaignState.Exploring || !Dungeon.AtStairs) return false;
            if (Dungeon.Floor == Dungeon.LastFloor) State = CampaignState.Victory;
            else { Dungeon.Descend(); Notice = "Piso " + Dungeon.Floor + ": una nueva costura, un nuevo orden de sellos."; }
            return true;
        }
    }
}
