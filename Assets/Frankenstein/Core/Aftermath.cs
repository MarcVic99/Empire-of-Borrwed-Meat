using System.Collections.Generic;
using System.Linq;

namespace Frankenstein
{
    public enum EnemyResolution { Pending, Harvested, Reanimated, Recruited, Fled }

    public sealed class Aftermath
    {
        private readonly Combat battle;
        private readonly List<Creature> party;
        private readonly Dictionary<Creature, EnemyResolution> resolutions;
        private Creature chosenNegotiator;
        public IReadOnlyList<Creature> Enemies { get; }
        public IReadOnlyList<Creature> Party { get; }
        public bool Available => battle.IsOver && !battle.Hero.IsDead && !battle.IsClosed;
        public bool IsComplete => Available && resolutions.Values.All(r => r != EnemyResolution.Pending);
        public bool HasPartySpace => party.Count(c => !c.IsDead) < Rules.MaxPartySize;

        internal Aftermath(Combat battle)
        {
            this.battle = battle;
            party = battle.Creatures.Where(c => c.IsPlayer).ToList();
            Party = party.AsReadOnly();
            Enemies = battle.Creatures.Where(c => !c.IsPlayer).ToList().AsReadOnly();
            resolutions = Enemies.ToDictionary(c => c, c => EnemyResolution.Pending);
        }
        public EnemyResolution Resolution(Creature enemy) => resolutions[enemy];
        private bool CanResolve(Creature enemy) => Available && enemy != null &&
            resolutions.ContainsKey(enemy) && resolutions[enemy] == EnemyResolution.Pending;
        private bool Pending(Creature enemy) => enemy != null && resolutions.ContainsKey(enemy) && resolutions[enemy] == EnemyResolution.Pending;
        public Creature Negotiator => !battle.IsOver && battle.Current != null && battle.Current.IsPlayer ? battle.Current :
            chosenNegotiator != null && !chosenNegotiator.IsDead ? chosenNegotiator : battle.Hero;
        public bool SelectNegotiator(Creature member)
        {
            if (!battle.IsOver || member == null || member.IsDead || !party.Contains(member)) return false;
            chosenNegotiator = member; return true;
        }
        public double NegotiationChance(Creature enemy, NegotiationChoice choice, BodyPiece offered = null, int gold = 0) =>
            Negotiation.SuccessChance(enemy, choice, battle.Willingness(enemy), Negotiator,
                offered != null ? offered.Definition.TradeValue : gold, gold > 0);

        public bool FinishOff(Creature enemy)
        {
            if (!CanResolve(enemy) || enemy.IsDead || !enemy.HasSurrendered) return false;
            enemy.Kill();
            battle.Record($"Rematas a {enemy.Name}. Su cadáver conserva las piezas restantes: puedes cosecharlo o reanimarlo.");
            return true;
        }
        public bool Harvest(Creature enemy)
        {
            if (!CanResolve(enemy) || !enemy.IsDead) return false;
            battle.CollectRemains(enemy);
            resolutions[enemy] = EnemyResolution.Harvested;
            battle.Record($"{enemy.Name}: restos examinados. Solo algunas piezas son recuperables.");
            return true;
        }
        public bool Reanimate(Creature enemy)
        {
            if (!CanResolve(enemy) || !enemy.IsDead || !enemy.OccupiedSlots.Any() || !HasPartySpace) return false;
            enemy.Reanimate();
            party.Add(enemy);
            resolutions[enemy] = EnemyResolution.Reanimated;
            battle.Record($"{enemy.Name} se une como no muerto con sus piezas y vida actuales. Obedece siempre y no se cura entre combates.");
            return true;
        }
        public bool Release(Creature enemy)
        {
            if (!CanResolve(enemy) || enemy.IsDead || !enemy.HasSurrendered) return false;
            enemy.Flee(); resolutions[enemy] = EnemyResolution.Fled; return true;
        }

        public bool CanNegotiate(Creature enemy, NegotiationChoice choice, BodyPiece offered = null, int gold = 0)
        {
            return !battle.IsClosed && !battle.Hero.IsDead && (battle.IsOver || battle.Current != null && battle.Current.IsPlayer) &&
                Pending(enemy) && !enemy.Enraged && !(Negotiator.Profession == Profession.Hunter && enemy.HasSurrendered) && !enemy.IsDead && !enemy.HasFled && !enemy.IsPlayer && HasPartySpace &&
                (battle.IsOver || !Negotiator.IsThrown) && Negotiation.CanUse(Negotiator, choice) && gold >= 0 &&
                (choice != NegotiationChoice.Ofrecer ||
                    offered != null && gold == 0 && battle.Inventory.Pieces.Contains(offered) ||
                    offered == null && gold > 0 && gold <= battle.Inventory.Gold);
        }

        public NegotiationResult Negotiate(Creature enemy, NegotiationChoice choice, BodyPiece offered = null, int gold = 0)
        {
            if (!CanNegotiate(enemy, choice, offered, gold)) return NegotiationResult.Unavailable;
            battle.RecruitmentHint(enemy);
            double chance = NegotiationChance(enemy, choice, offered, gold);
            if (choice == NegotiationChoice.Ofrecer)
            {
                if (offered != null)
                {
                    battle.Inventory.Remove(offered);
                    battle.Record($"Ofreces {offered.Definition.Name} (valor {offered.Definition.TradeValue}) a {enemy.Name}.");
                }
                else
                {
                    battle.Inventory.SpendGold(gold);
                    battle.Record($"Ofreces {gold} de oro a {enemy.Name}.");
                }
            }
            battle.Record($"{Negotiator.Name} negocia con {enemy.Name}: {choice}, disposición {battle.Willingness(enemy):P0}, éxito {chance:P0}.");
            if (battle.RollNegotiation() < chance)
            {
                enemy.Recruit();
                battle.Say(enemy, "Está bien. Os acompañaré.");
                party.Add(enemy);
                resolutions[enemy] = EnemyResolution.Recruited;
                battle.Record($"{enemy.Name} acepta y se une con las piezas y heridas que conserva.");
                battle.NegotiationFinished(enemy, true);
                return NegotiationResult.Recruited;
            }
            enemy.RejectNegotiation();
            battle.Say(enemy, enemy.Enraged ? "¡Se acabó! ¡No volveré a escucharos!" : "No. Tendréis que convencerme mejor.");
            battle.Record(enemy.Name + (enemy.Enraged ? " se enfurece: no es reclutable durante este combate." : " rechaza la propuesta."));
            battle.NegotiationFinished(enemy, false);
            return NegotiationResult.Rejected;
        }
    }
}
