using System;
using System.Collections.Generic;
using System.Linq;
namespace Frankenstein
{
    public enum GearSlot { ManoIzq, ManoDer, Armadura, Amuleto }
    public enum GearKind { Weapon, Armor, Relic }
    public sealed class GearDefinition
    {
        public readonly string Id, Name;
        public readonly GearKind Kind;
        public readonly ActionDefinition Action;
        public readonly int Hands, Defense, Speed;
        public readonly double Evasion;
        public GearDefinition(string id, string name, GearKind kind, ActionDefinition action = null, int hands = 1, int defense = 0, int speed = 0, double evasion = 0)
        { Id = id; Name = name; Kind = kind; Action = action; Hands = hands; Defense = defense; Speed = speed; Evasion = evasion; }
        public bool Fits(GearSlot slot) => Kind == GearKind.Weapon ? slot == GearSlot.ManoIzq || slot == GearSlot.ManoDer :
            Kind == GearKind.Armor ? slot == GearSlot.Armadura : slot == GearSlot.Amuleto;
    }
    public sealed class GearItem
    {
        public GearDefinition Definition { get; }
        public GearItem(GearDefinition definition) { Definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
    }
    public static class Equipment
    {
        public static bool IsHand(GearSlot s) => s == GearSlot.ManoIzq || s == GearSlot.ManoDer;
        public static BodySlot BodyFor(GearSlot s) => s == GearSlot.ManoIzq ? BodySlot.BrazoIzq : s == GearSlot.ManoDer ? BodySlot.BrazoDer : s == GearSlot.Armadura ? BodySlot.Torso : BodySlot.Cabeza;
        public static GearSlot HandFor(BodySlot s) => s == BodySlot.BrazoIzq ? GearSlot.ManoIzq : GearSlot.ManoDer;
    }
}
