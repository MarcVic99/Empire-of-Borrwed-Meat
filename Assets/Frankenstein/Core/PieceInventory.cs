using System;
using System.Collections.Generic;

namespace Frankenstein
{
    // Stores the actual detached pieces. Harvesting does not heal or equip them.
    public sealed class PieceInventory
    {
        private readonly List<GearItem> equipment = new List<GearItem>();
        public IReadOnlyList<GearItem> Equipment => equipment.AsReadOnly();
        public int Threads { get; private set; } = 4;
        public void AddThreads(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Threads += amount; }
        internal bool SpendThreads(int amount) { if (amount < 1 || amount > Threads) return false; Threads -= amount; return true; }
        internal void AddEquipment(GearItem item) { if (item == null || equipment.Contains(item)) throw new InvalidOperationException("Equipo inválido o duplicado."); equipment.Add(item); }
        internal bool RemoveEquipment(GearItem item) => equipment.Remove(item);
        private readonly List<BodyPiece> pieces = new List<BodyPiece>();
        public IReadOnlyList<BodyPiece> Pieces { get; }
        public int Count => pieces.Count;
        public int TotalHarvested { get; private set; }
        public int Gold { get; private set; }
        public void AddGold(int amount)
        { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Gold += amount; }
        internal bool SpendGold(int amount)
        { if (amount <= 0 || amount > Gold) return false; Gold -= amount; return true; }

        public PieceInventory() { Pieces = pieces.AsReadOnly(); }

        internal void Add(BodyPiece piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            if (piece.Definition.Kind == PieceKind.Torso) throw new InvalidOperationException("Los torsos no son objetos.");
            if (pieces.Contains(piece)) throw new InvalidOperationException("Esta pieza ya está en el inventario.");
            pieces.Add(piece);
        }
        internal bool Remove(BodyPiece piece) => pieces.Remove(piece);
        internal void Harvest(BodyPiece piece) { Add(piece); TotalHarvested++; }
    }
}
