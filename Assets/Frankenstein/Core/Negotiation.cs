using System;
namespace Frankenstein
{
    public enum NegotiationChoice { Hablar, Ofrecer, Seducir }
    public enum NegotiationResult { Unavailable, Recruited, Fled, Rejected }
    public sealed class RacePreference
    {
        public readonly NegotiationChoice Favorite, Hated;
        public RacePreference(NegotiationChoice favorite, NegotiationChoice hated) { Favorite = favorite; Hated = hated; }
    }
    public static class Negotiation
    {
        public static bool CanUse(Creature actor, NegotiationChoice choice)
        {
            if (actor == null || actor.IsDead || actor.GetPiece(BodySlot.Cabeza) == null || !Enum.IsDefined(typeof(NegotiationChoice), choice)) return false;
            if (choice == NegotiationChoice.Seducir) return actor.Profession == Profession.Dancer && actor.Sex == Sex.Female;
            return choice != NegotiationChoice.Ofrecer || actor.GetPiece(BodySlot.BrazoIzq) != null || actor.GetPiece(BodySlot.BrazoDer) != null;
        }
        public static double SeductionAffinity(Creature target, Creature actor)
        {
            if (actor == null || actor.Profession != Profession.Dancer) return 1;
            double factor = target.Sex == Sex.Male ? 1.6 : target.Sex == Sex.Female ? .35 : .65;
            if (target.Race == Race.Humano) factor *= target.Sex == Sex.Male ? 1.25 : target.Sex == Sex.Female ? .5 : 1;
            return factor;
        }
        public static double Willingness(Creature enemy, int lostAllies = 0, bool outnumbered = false)
        {
            var p = enemy.Profile;
            return Math.Max(0, Math.Min(1, p.InitialDisposition + enemy.WoundSeverity * .35 * p.InjurySensitivity + enemy.CombatPressure +
                enemy.MutilationsThisCombat * .035 * p.MutilationSensitivity + lostAllies * .05 + (outnumbered ? .06 : 0) +
                (enemy.HasSurrendered ? .12 : 0) - enemy.NegotiationRejections * .08));
        }
        public static double SuccessChance(Creature enemy, NegotiationChoice choice, double? willingness = null, Creature actor = null, int offerValue = 0, bool gold = false)
        {
            if (enemy == null || enemy.Enraged || actor != null && (!CanUse(actor, choice) || actor.Profession == Profession.Hunter && enemy.HasSurrendered)) return 0;
            var preference = Content.Races[enemy.Race];
            double chance = .10 + (willingness ?? Willingness(enemy)) * .55;
            if (choice == preference.Favorite) chance += .15;
            if (choice == preference.Hated) chance -= .05;
            if (choice == NegotiationChoice.Seducir) chance = (chance + enemy.Seduction * .55) * SeductionAffinity(enemy, actor);
            if (choice == NegotiationChoice.Ofrecer && offerValue > 0)
            { double value = offerValue * (gold ? 2 : 1); chance += .65 * (1 - Math.Exp(-value / (30 * Math.Max(1, enemy.Profile.Power)))) * enemy.Profile.BribeAffinity; }
            if (actor != null && actor.Profession == Profession.Grafter) chance *= .6;
            return Math.Max(.01, Math.Min(.95, chance));
        }
    }
}
