using System;
using System.Collections.Generic;
using UnityEngine;
namespace Frankenstein.Presentation
{
    public static class CharacterArt
    {
        private static readonly Dictionary<string, Texture2D> sheets = new Dictionary<string, Texture2D>();
        public static Rect Figure(Rect r)
        {
            float height = Mathf.Min(r.height, r.width / .60f);
            return new Rect(r.center.x - height * .30f, r.yMax - height, height * .60f, height);
        }
        public static Rect Part(Rect r, BodySlot slot, bool rear)
        {
            r = Figure(r);
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
    }
}
