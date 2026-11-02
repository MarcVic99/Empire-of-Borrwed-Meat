using System;
using System.Linq;

namespace Frankenstein
{
    public sealed class SewingStation
    {
        private readonly Combat battle;
        public bool AllowUndead { get; }
        public bool Available => battle.Aftermath.IsComplete && battle.Aftermath.Party.Any(c => !c.IsDead && c.Profession == Profession.Grafter);
        public SewingStation(Combat battle, bool allowUndead = Rules.AllowSewingUndead)
        { this.battle = battle ?? throw new ArgumentNullException(nameof(battle)); AllowUndead = allowUndead; }

        public static bool Fits(PieceDefinition piece, BodySlot slot)
        {
            if (piece == null) return false;
            switch (slot)
            {
                case BodySlot.Cabeza: return piece.Kind == PieceKind.Head;
                case BodySlot.Torso: return false;
                case BodySlot.BrazoIzq:
                case BodySlot.BrazoDer: return piece.Kind == PieceKind.Arm;
                case BodySlot.PiernaDer:
                case BodySlot.PiernaIzq: return piece.Kind == PieceKind.Legs;
                default: return false;
            }
        }
        public bool CanSew(Creature member, BodySlot slot, BodyPiece piece) => Available &&
            member != null && battle.Aftermath.Party.Contains(member) && !member.IsDead &&
            (AllowUndead || !member.IsUndead) && piece != null && battle.Inventory.Threads >= 1 &&
            battle.Inventory.Pieces.Contains(piece) && Fits(piece.Definition, slot);

        public bool Sew(Creature member, BodySlot slot, BodyPiece piece)
        {
            if (!CanSew(member, slot, piece)) return false;
            battle.Inventory.SpendThreads(1);
            battle.Inventory.Remove(piece);
            var previous = member.Equip(slot, piece);
            piece.HealFully();
            if (previous != null) battle.Inventory.Add(previous);
            battle.Record($"Coses {piece.Definition.Name} en {member.Name}, {slot}, a {piece.Health}/{piece.MaxHealth} de vida." +
                (previous == null ? " Hueco vacío ocupado." : $" {previous.Definition.Name} vuelve al inventario con {previous.Health}/{previous.MaxHealth} de vida."));
            return true;
        }
    }
}
