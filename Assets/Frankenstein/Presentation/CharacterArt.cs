using System;
using System.Collections.Generic;
using UnityEngine;
namespace Frankenstein.Presentation
{
    public static class CharacterArt
    {
        private static readonly Dictionary<string, Texture2D> sheets = new Dictionary<string, Texture2D>();
        public static Rect Figure(Rect r, Creature creature = null)
        {
            float aspect = creature?.Appearance == "cabeza_arana" ? 1.25f : creature != null && creature.Profile.Power >= 3 ? .72f : .60f;
            float height = Mathf.Min(r.height, r.width / aspect);
            return new Rect(r.center.x - height * aspect / 2, r.yMax - height, height * aspect, height);
        }
        public static Rect Part(Rect r, BodySlot slot, bool rear, Creature creature = null)
        {
            r = Figure(r, creature);
            if (creature?.Appearance == "cabeza_arana")
            {
                if (slot == BodySlot.Cabeza) return new Rect(r.x + r.width * .26f, r.y + r.height * .35f, r.width * .48f, r.height * .30f);
                if (slot == BodySlot.Torso) return new Rect(r.x + r.width * .26f, r.y, r.width * .48f, r.height * .35f);
                if (slot == BodySlot.PiernaIzq || slot == BodySlot.PiernaDer)
                { bool left = (slot == BodySlot.PiernaIzq) == rear; return new Rect(left ? r.x : r.x + r.width * .74f, r.y, r.width * .26f, r.height); }
                return new Rect(r.x, r.y, 0, 0);
            }
            float x = 0, y = 0, w = 1, h = .20f;
            if (slot == BodySlot.Torso) { x = .26f; y = .20f; w = .48f; h = .42f; }
            else if (slot == BodySlot.BrazoIzq || slot == BodySlot.BrazoDer)
            { bool left = (slot == BodySlot.BrazoIzq) == rear; x = left ? 0 : .74f; y = .20f; w = .26f; h = .80f; }
            else if (slot == BodySlot.PiernaIzq || slot == BodySlot.PiernaDer)
            { bool left = (slot == BodySlot.PiernaIzq) == rear; x = left ? .26f : .5f; y = .62f; w = .24f; h = .38f; }
            return new Rect(r.x + r.width * x, r.y + r.height * y, r.width * w, r.height * h);
        }
        public static bool Draw(Creature creature, Rect r, int view)
        {
            if (view != 2 && HunterCombatArt.Draw(creature, r, view == 1)) return true;
            if (DrawImperial(creature, r, view)) return true;
            string name = creature.Profession == Profession.Hunter ? "hunter" : creature.Profession == Profession.Dancer ? "dancer-v2" : creature.Profession == Profession.Grafter ? "grafter-unarmed" :
                creature.Appearance == "cabeza_lobo" ? "wolf" : creature.Appearance == "cabeza_ghoul" ? "ghoul" : null;
            if (name == null) { if (creature.Appearance == "cabeza_humana") { ExplorationArt.Enemy("bandido", Figure(r)); return true; } return false; }
            if (!sheets.TryGetValue(name, out var sheet))
            { sheet = Resources.Load<Texture2D>("Frankenstein/Characters/" + name); sheets[name] = sheet; }
            if (sheet == null) return false;
            var figure = Figure(r);
            var uv = new Rect(view / 3f, 0, 1 / 3f, 1);
            if (view == 2)
            {
                var clip = figure;
                GUI.BeginGroup(clip);
                GUI.DrawTextureWithTexCoords(new Rect(figure.x - clip.x, 0, figure.width, figure.height), sheet, uv, true);
                GUI.EndGroup(); return true;
            }
            var original = name == "wolf" ? Content.Enemy("lobo") : name == "ghoul" ? Content.Enemy("ghoul") : Content.Hero(name == "hunter" ? Profession.Hunter : creature.Profession);
            foreach (BodySlot slot in Enum.GetValues(typeof(BodySlot)))
            {
                var piece = creature.GetPiece(slot); if (piece == null) continue;
                var part = Part(r, slot, view == 1);
                if (piece.Definition.Id != original.GetPiece(slot)?.Definition.Id)
                { BattlePainter.Graft(creature, slot, part, view == 1); continue; }
                GUI.BeginGroup(part);
                GUI.DrawTextureWithTexCoords(new Rect(figure.x - part.x, figure.y - part.y, figure.width, figure.height), sheet, uv, true);
                GUI.EndGroup();
            }
            BattlePainter.EquipmentOverlay(creature, figure, view == 1);
            return true;
        }
        private static bool DrawImperial(Creature creature, Rect r, int view)
        {
            bool hero = creature.Profession != Profession.None;
            string id = hero ? ImperialArt.Role(creature.Profession) : ImperialArt.EnemyId(creature);
            string key = hero ? id : view == 1 && ImperialArt.Load("enemies-back") != null ? "enemies-back" : "enemies";
            string medium = key + "-medium";
            if (ImperialArt.Load(medium) != null) key = medium;
            var sheet = ImperialArt.Load(key); if (sheet == null) return false;
            int index = hero ? view : ImperialArt.EnemyIndex(id);
            var uv = ImperialArt.UV(key, index % 3, hero ? 0 : index / 3, 3, hero ? 1 : 2);
            var figure = Figure(r, creature);
            if (view == 2) { GUI.DrawTextureWithTexCoords(figure, sheet, uv, true); return true; }
            var original = hero ? Content.Hero(creature.Profession) : Content.Enemy(id);
            foreach (BodySlot slot in Enum.GetValues(typeof(BodySlot)))
            {
                var piece = creature.GetPiece(slot); if (piece == null) continue;
                var part = Part(r, slot, view == 1, creature);
                if (piece.Definition.Id != original.GetPiece(slot)?.Definition.Id)
                { BattlePainter.Graft(creature, slot, part, view == 1); continue; }
                GUI.BeginGroup(part);
                GUI.DrawTextureWithTexCoords(new Rect(figure.x - part.x, figure.y - part.y, figure.width, figure.height), sheet, uv, true);
                GUI.EndGroup();
                if (creature.Appearance == "cabeza_arana" && slot == BodySlot.Torso)
                {
                    var lower = new Rect(part.x, figure.y + figure.height * .65f, part.width, figure.height * .35f);
                    GUI.BeginGroup(lower); GUI.DrawTextureWithTexCoords(new Rect(figure.x - lower.x, figure.y - lower.y, figure.width, figure.height), sheet, uv, true); GUI.EndGroup();
                }
            }
            BattlePainter.EquipmentOverlay(creature, figure, view == 1); return true;
        }
    }
}
