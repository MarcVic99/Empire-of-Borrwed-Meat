using System.Collections.Generic;
using UnityEngine;

namespace Frankenstein.Presentation
{
    internal static class HunterCombatArt
    {
        private static readonly BodySlot[] limbs =
            { BodySlot.PiernaIzq, BodySlot.PiernaDer, BodySlot.BrazoIzq, BodySlot.BrazoDer };
        private static readonly Dictionary<string, SpriteRenderer>[] kits = new Dictionary<string, SpriteRenderer>[2];

        public static bool Draw(Creature creature, Rect bounds, bool rear)
        {
            if (creature.Profession != Profession.Hunter ||
                creature.GetPiece(BodySlot.Cabeza)?.Definition.Id != "cabeza_humana" ||
                creature.GetPiece(BodySlot.Torso)?.Definition.Id != "torso_humano") return false;
            int view = rear ? 1 : 0;
            if (kits[view] == null)
            {
                var prefab = Resources.Load<GameObject>("Frankenstein/Imperio/hunter-modular-" + (rear ? "back" : "front"));
                if (prefab == null) return false;
                kits[view] = new Dictionary<string, SpriteRenderer>();
                foreach (var part in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    kits[view].Add(part.name, part);
            }

            var kit = kits[view];
            var figure = CharacterArt.Figure(bounds, creature);
            bool spider = creature.GetPiece(BodySlot.PiernaIzq)?.Definition.Id == "pata_arana" ||
                          creature.GetPiece(BodySlot.PiernaDer)?.Definition.Id == "pata_arana";
            float scale = Mathf.Min(figure.height / 7.55f, bounds.width / (spider ? 7.4f : 4.4f));
            bool noLegs = creature.GetPiece(BodySlot.PiernaIzq) == null && creature.GetPiece(BodySlot.PiernaDer) == null;
            var origin = new Vector2(figure.center.x, figure.yMax + (noLegs ? 3.18f * scale : 0));
            foreach (var slot in limbs)
            {
                var piece = creature.GetPiece(slot);
                if (piece == null) continue;
                if (TryPiece(kit, slot, piece.Definition.Id, out var renderer, out bool flip))
                    Paint(renderer, origin, scale, flip);
                else BattlePainter.Graft(creature, slot, CharacterArt.Part(bounds, slot, rear, creature), rear);
            }
            Paint(kit["cabeza-torso"], origin, scale, false);
            BattlePainter.EquipmentOverlay(creature, figure, rear, hand =>
            {
                var slot = hand == GearSlot.ManoIzq ? BodySlot.BrazoIzq : BodySlot.BrazoDer;
                var piece = creature.GetPiece(slot);
                if (piece != null && TryPiece(kit, slot, piece.Definition.Id, out var part, out bool flip))
                {
                    var rect = Placement(part, origin, scale, flip);
                    bool left = (slot == BodySlot.BrazoIzq) == rear;
                    return new Vector2(rect.x + rect.width * (left ? .26f : .74f), rect.y + rect.height * .90f);
                }
                var fallback = CharacterArt.Part(bounds, slot, rear, creature);
                return new Vector2(fallback.center.x, figure.y + figure.height * .52f);
            });
            return true;
        }

        private static bool TryPiece(Dictionary<string, SpriteRenderer> kit, BodySlot slot, string id,
            out SpriteRenderer part, out bool flip)
        {
            bool left = slot == BodySlot.BrazoIzq || slot == BodySlot.PiernaIzq;
            flip = id == "garra_lobo" && !left;
            string name = id switch
            {
                "brazo_humano" => left ? "brazo-izquierdo" : "brazo-derecho",
                "pierna_humana" => left ? "pierna-izquierda" : "pierna-derecha",
                "garra_lobo" => "brazo-lobo-izquierdo",
                "pata_arana" => left ? "patas-arana-izquierda" : "patas-arana-derecha",
                _ => ""
            };
            return kit.TryGetValue(name, out part);
        }

        private static Rect Placement(SpriteRenderer part, Vector2 origin, float scale, bool flip)
        {
            var sprite = part.sprite;
            var local = part.transform.localPosition;
            float factor = scale * part.transform.localScale.x / sprite.pixelsPerUnit;
            float x = flip ? -local.x : local.x;
            float pivotX = flip ? sprite.rect.width - sprite.pivot.x : sprite.pivot.x;
            return new Rect(origin.x + x * scale - pivotX * factor,
                origin.y - local.y * scale - (sprite.rect.height - sprite.pivot.y) * factor,
                sprite.rect.width * factor, sprite.rect.height * factor);
        }

        private static void Paint(SpriteRenderer part, Vector2 origin, float scale, bool flip)
        {
            var sprite = part.sprite;
            var uv = sprite.rect;
            uv.x /= sprite.texture.width;
            uv.y /= sprite.texture.height;
            uv.width /= sprite.texture.width;
            uv.height /= sprite.texture.height;
            if (flip) { uv.x += uv.width; uv.width = -uv.width; }
            GUI.DrawTextureWithTexCoords(Placement(part, origin, scale, flip), sprite.texture, uv, true);
        }
    }
}
