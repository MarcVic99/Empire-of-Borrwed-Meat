using System.Collections.Generic;
namespace Frankenstein
{
    public sealed class EncounterDefinition
    {
        public readonly string Name; public readonly string[] Enemies;
        public EncounterDefinition(string name, params string[] enemies) { Name = name; Enemies = enemies; }
    }
    public static class Content
    {
        public static readonly EncounterDefinition[] Encounters = { new EncounterDefinition("La salida", "jefe") };
        public static readonly ActionDefinition Punch = new ActionDefinition("Puñetazo", ActionKind.Attack, 3, accuracy: .88, acquiredWith: "brazo natural");
        public static readonly ActionDefinition Dance = new ActionDefinition("Danza cautivadora", ActionKind.Charm, accuracy: .95, acquiredWith: "especialidad de Bailarina", charm: .25);
        public static readonly Dictionary<Race, RacePreference> Races = new Dictionary<Race, RacePreference>
        {
            { Race.Bestia, new RacePreference(NegotiationChoice.Seducir, NegotiationChoice.Ofrecer) },
            { Race.Humano, new RacePreference(NegotiationChoice.Ofrecer, NegotiationChoice.Hablar) },
            { Race.NoMuerto, new RacePreference(NegotiationChoice.Hablar, NegotiationChoice.Seducir) }
        };
        public static readonly Dictionary<string, GearDefinition> Gear = new Dictionary<string, GearDefinition>
        {
            { "espada", new GearDefinition("espada", "Espada de caza", GearKind.Weapon, new ActionDefinition("Tajo de espada", ActionKind.Attack, 10, accuracy: .91, acquiredWith: "espada")) },
            { "daga", new GearDefinition("daga", "Daga", GearKind.Weapon, new ActionDefinition("Puñalada", ActionKind.Attack, 6, criticalChance: .25, accuracy: .95, acquiredWith: "daga")) },
            { "maza", new GearDefinition("maza", "Maza", GearKind.Weapon, new ActionDefinition("Golpe de maza", ActionKind.Attack, 13, accuracy: .78, acquiredWith: "maza"), speed: -1) },
            { "arco", new GearDefinition("arco", "Arco largo", GearKind.Weapon, new ActionDefinition("Disparo", ActionKind.Attack, 10, accuracy: .96, acquiredWith: "arco: requiere dos brazos"), hands: 2) },
            { "baculo", new GearDefinition("baculo", "Báculo", GearKind.Weapon, new ActionDefinition("Descarga de báculo", ActionKind.Attack, 8, accuracy: .94, acquiredWith: "báculo")) },
            { "cruz", new GearDefinition("cruz", "Cruz de hierro", GearKind.Weapon, new ActionDefinition("Exorcismo", ActionKind.Attack, 7, accuracy: .96, acquiredWith: "cruz")) },
            { "sierra", new GearDefinition("sierra", "Sierra", GearKind.Weapon, new ActionDefinition("Serrado", ActionKind.Attack, 8, accuracy: .90, acquiredWith: "sierra")) },
            { "aguja", new GearDefinition("aguja", "Aguja quirúrgica", GearKind.Weapon, new ActionDefinition("Punción", ActionKind.Attack, 5, accuracy: .99, acquiredWith: "aguja")) },
            { "cintas", new GearDefinition("cintas", "Cintas de danza", GearKind.Weapon, new ActionDefinition("Azote de cintas", ActionKind.Attack, 4, accuracy: .94, acquiredWith: "cintas", charm: .08)) },
            { "escudo", new GearDefinition("escudo", "Escudo", GearKind.Weapon, new ActionDefinition("Bloquear", ActionKind.Block, acquiredWith: "escudo", accuracy: 1), defense: 1) },
            { "abrigo", new GearDefinition("abrigo", "Abrigo de cazadora", GearKind.Armor, hands: 0, defense: 3) },
            { "delantal", new GearDefinition("delantal", "Delantal reforzado", GearKind.Armor, hands: 0, defense: 2) },
            { "cota", new GearDefinition("cota", "Cota de malla", GearKind.Armor, hands: 0, defense: 3, speed: -1) },
            { "amuleto", new GearDefinition("amuleto", "Amuleto de viento", GearKind.Relic, hands: 0, evasion: .06) }
        };
        public static readonly Dictionary<string, PieceDefinition> Pieces = new Dictionary<string, PieceDefinition>
        {
            { "cabeza_humana", new PieceDefinition("cabeza_humana", "Cabeza humana", PieceKind.Head, 25, vital: true, tradeValue: 20) },
            { "torso_humano", new PieceDefinition("torso_humano", "Torso humano", PieceKind.Torso, 105, vital: true, tradeValue: 0) },
            { "brazo_humano", new PieceDefinition("brazo_humano", "Brazo humano", PieceKind.Arm, 15, Punch, tradeValue: 14) },
            { "brazo_orco", new PieceDefinition("brazo_orco", "Brazo de orco", PieceKind.Arm, 24, Punch, damage: 2, tradeValue: 28) },
            { "pierna_humana", new PieceDefinition("pierna_humana", "Pierna humana", PieceKind.Legs, 16, speed: 2, tradeValue: 14) },
            { "cabeza_lobo", new PieceDefinition("cabeza_lobo", "Cabeza de lobo", PieceKind.Head, 28, new ActionDefinition("Mordisco", ActionKind.Attack, 6, accuracy: .88, acquiredWith: "cabeza de lobo"), vital: true, tradeValue: 28) },
            { "torso_lobo", new PieceDefinition("torso_lobo", "Torso de lobo", PieceKind.Torso, 125, vital: true, tradeValue: 0) },
            { "garra_lobo", new PieceDefinition("garra_lobo", "Brazo con garra de lobo", PieceKind.Arm, 15, new ActionDefinition("Zarpazo", ActionKind.Attack, 7, accuracy: .90, acquiredWith: "garra natural"), tradeValue: 22) },
            { "pata_lobo", new PieceDefinition("pata_lobo", "Pata de lobo", PieceKind.Legs, 17, speed: 3, tradeValue: 18) },
            { "cabeza_ghoul", new PieceDefinition("cabeza_ghoul", "Cabeza de ghoul", PieceKind.Head, 28, new ActionDefinition("Mordisco infeccioso", ActionKind.Attack, 5, dotDamage: 1, dotTurns: 3, accuracy: .84, acquiredWith: "cabeza de ghoul"), vital: true, tradeValue: 28) },
            { "torso_ghoul", new PieceDefinition("torso_ghoul", "Torso de ghoul", PieceKind.Torso, 135, regeneration: 1, vital: true, tradeValue: 0) },
            { "brazo_podrido", new PieceDefinition("brazo_podrido", "Brazo de ghoul", PieceKind.Arm, 14, new ActionDefinition("Golpe", ActionKind.Attack, 5, accuracy: .86, acquiredWith: "brazo de ghoul"), tradeValue: 12) },
            { "pata_arana", new PieceDefinition("pata_arana", "Pata de araña", PieceKind.Legs, 28, speed: 2, evasion: .05, tradeValue: 28) },
            { "cabeza_arana", new PieceDefinition("cabeza_arana", "Cabeza de araña", PieceKind.Head, 25, new ActionDefinition("Veneno", ActionKind.Attack, dotDamage: 2, dotTurns: 4, accuracy: .86, acquiredWith: "cabeza de araña"), tradeValue: 28) },
            { "torso_caballero", new PieceDefinition("torso_caballero", "Torso del caballero", PieceKind.Torso, 160, defense: 2, vital: true, tradeValue: 0) },
            { "cabeza_tentaculos", new PieceDefinition("cabeza_tentaculos", "Corona de tentáculos", PieceKind.Head, 30, new ActionDefinition("Latigazo tentacular", ActionKind.Attack, 9, accuracy: .78, acquiredWith: "corona de tentáculos"), vital: true, speed: 2, tradeValue: 40) }
        };
        public static readonly Dictionary<string, MonsterProfile> Profiles = new Dictionary<string, MonsterProfile>
        {
            { "lobo", new MonsterProfile(.05, .8, 1, power: 1.2, bribeAffinity: .30, charmAffinity: .85) },
            { "bandido", new MonsterProfile(.12, 1, 1, power: .8, bribeAffinity: 1.5, charmAffinity: 1) },
            { "bandida", new MonsterProfile(.12, 1, 1, power: .9, bribeAffinity: 1.5, charmAffinity: 1) },
            { "ghoul", new MonsterProfile(.01, .5, .5, power: 1.6, bribeAffinity: .15, charmAffinity: .45, headReplacement: Pieces["cabeza_tentaculos"]) },
            { "arana", new MonsterProfile(.04, 1, 1, power: 1.1, bribeAffinity: .15, charmAffinity: .6, weakPoints: new[] { BodySlot.PiernaIzq, BodySlot.PiernaDer }) },
            { "jefe", new MonsterProfile(.01, .3, .3, power: 3, bribeAffinity: 2.2, charmAffinity: .55) }
        };
        public static Creature Hero(Profession profession = Profession.Hunter)
        {
            var c = Create(profession == Profession.Hunter ? "Cazadora" : profession == Profession.Dancer ? "Bailarina" : "Injertador", true, Race.Humano, 1, profession,
                profession == Profession.Grafter ? Sex.Other : Sex.Female, new MonsterProfile(power: profession == Profession.Hunter ? 1.4 : profession == Profession.Dancer ? .85 : 1),
                "cabeza_humana", "torso_humano", "brazo_humano", "brazo_humano", "pierna_humana");
            Give(c, GearSlot.ManoIzq, profession == Profession.Hunter ? "espada" : profession == Profession.Dancer ? "cintas" : "sierra");
            Give(c, GearSlot.ManoDer, profession == Profession.Grafter ? "aguja" : "daga");
            if (profession != Profession.Dancer) Give(c, GearSlot.Armadura, profession == Profession.Hunter ? "abrigo" : "delantal");
            return c;
        }
        public static Creature Werewolf() => Enemy("lobo");
        public static Creature Enemy(string id, string suffix = "")
        {
            Creature c;
            switch (id)
            {
                case "lobo": c = Create("Licántropo" + suffix, false, Race.Bestia, 1, Profession.None, Sex.Male, Profiles[id], "cabeza_lobo", "torso_lobo", "garra_lobo", "garra_lobo", "pata_lobo"); break;
                case "bandido": case "bandida":
                    c = Create((id == "bandido" ? "Bandido" : "Bandida") + suffix, false, Race.Humano, 1, Profession.None, id == "bandido" ? Sex.Male : Sex.Female, Profiles[id], "cabeza_humana", "torso_humano", "brazo_humano", "brazo_humano", "pierna_humana");
                    Give(c, GearSlot.ManoIzq, "daga"); Give(c, GearSlot.ManoDer, "daga"); Give(c, GearSlot.Armadura, "cota"); break;
                case "ghoul": c = Create("Ghoul" + suffix, false, Race.NoMuerto, 1, Profession.None, Sex.Other, Profiles[id], "cabeza_ghoul", "torso_ghoul", "brazo_podrido", "brazo_podrido", "pierna_humana"); break;
                case "arana": c = Create("Araña gigante" + suffix, false, Race.Bestia, 1, Profession.None, Sex.Female, Profiles[id], "cabeza_arana", "torso_lobo", null, null, "pata_arana"); break;
                case "jefe": c = Create("Caballero hueco", false, Race.NoMuerto, 2, Profession.None, Sex.Male, Profiles[id], "cabeza_ghoul", "torso_caballero", "brazo_orco", "brazo_orco", "pierna_humana"); Give(c, GearSlot.ManoIzq, "maza"); Give(c, GearSlot.ManoDer, "escudo"); break;
                default: throw new System.ArgumentException("Criatura desconocida: " + id);
            }
            return c;
        }
        private static void Give(Creature c, GearSlot slot, string id) { c.Wear(slot, new GearItem(Gear[id])); }
        private static Creature Create(string name, bool player, Race race, int multiplier, Profession profession, Sex sex, MonsterProfile profile, params string[] ids)
        {
            var body = new Dictionary<BodySlot, PieceDefinition>();
            for (int i = 0; i < ids.Length; i++) if (ids[i] != null) body.Add((BodySlot)i, Pieces[ids[i]]);
            if (ids.Length == 5 && ids[4] != null) body.Add(BodySlot.PiernaDer, Pieces[ids[4]]);
            return new Creature(name, player, body, race, multiplier, profession, sex, profile);
        }
    }
}
