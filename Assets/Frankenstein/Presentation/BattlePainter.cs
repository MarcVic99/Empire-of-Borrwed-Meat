using Frankenstein;
using UnityEngine;

namespace Frankenstein.Presentation
{
    // 2D sprite presentation with geometric fallbacks for missing assets.
    public static class BattlePainter
    {
        public static readonly Color Ink = new Color(27 / 255f, 20 / 255f, 26 / 255f);
        public static readonly Color Panel = new Color(50 / 255f, 28 / 255f, 39 / 255f);
        public static readonly Color Paper = new Color(226 / 255f, 204 / 255f, 183 / 255f);
        public static readonly Color Gold = new Color(193 / 255f, 147 / 255f, 83 / 255f);
        public static readonly Color Green = new Color(.46f, .63f, .40f);
        public static readonly Color Blood = new Color(135 / 255f, 61 / 255f, 77 / 255f);
        private static Texture2D disc;
        public static void Fill(Rect rect, Color color)
        { var previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
        public static void Border(Rect r, Color color, float width = 1)
        { Fill(new Rect(r.x, r.y, r.width, width), color); Fill(new Rect(r.x, r.yMax - width, r.width, width), color);
            Fill(new Rect(r.x, r.y, width, r.height), color); Fill(new Rect(r.xMax - width, r.y, width, r.height), color); }
        public static void Oval(Rect rect, Color color)
        {
            if (disc == null)
            {
                disc = new Texture2D(48, 48, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                { float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(24, 24));
                    disc.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(24 - distance))); }
                disc.Apply();
            }
            var previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, disc); GUI.color = previous;
        }
        public static void Line(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
            Fill(new Rect(a.x, a.y - width / 2, Vector2.Distance(a, b), width), color);
            GUI.matrix = matrix;
        }
        public static void Bar(Rect r, float fraction, Color color)
        { Fill(r, new Color(.035f, .04f, .035f)); Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), color); }

        public static void Arena(Rect r)
        {
            if (ImperialArt.Draw("arena", r)) { Fill(r, new Color(.04f, .01f, .03f, .18f)); return; }
            Fill(r, new Color(.105f, .115f, .105f));
            Fill(new Rect(r.x, r.y + r.height * .53f, r.width, r.height * .47f), new Color(.135f, .125f, .105f));
            var stone = new Color(.17f, .18f, .16f);
            for (int i = 0; i < 7; i++)
            {
                float x = r.x + i * r.width / 6;
                Fill(new Rect(x - 12, r.y, 24, r.height * .56f), stone);
                Fill(new Rect(x - 18, r.y + r.height * .48f, 36, 8), stone);
            }
            for (int i = 1; i < 4; i++)
                Line(new Vector2(r.x, r.y + r.height * (.53f + .12f * i)), new Vector2(r.xMax, r.y + r.height * (.53f + .12f * i)), new Color(.18f, .17f, .14f));
        }

        public static void Creature(Creature creature, Rect r, bool rear, bool active, BodySlot? selected = null)
        {
            if (CharacterArt.Draw(creature, r, rear ? 1 : 0)) return;
            float size = Mathf.Min(r.width / 120, r.height / 170);
            var origin = new Vector2(r.center.x, r.yMax - 7 * size);
            if (active) Oval(new Rect(origin.x - 48 * size, origin.y - 8 * size, 96 * size, 16 * size), Gold);
            Oval(new Rect(origin.x - 42 * size, origin.y - 5 * size, 84 * size, 12 * size), new Color(0, 0, 0, .5f));
            bool spider = creature.GetPiece(BodySlot.PiernaIzq)?.Definition.Id == "pata_arana";
            if (spider)
            {
                for (int i = 0; i < 4; i++) foreach (int side in new[] { -1, 1 })
                {
                    var a = origin + new Vector2(side * 12, -62 + i * 9) * size;
                    var b = origin + new Vector2(side * (43 + i * 4), -90 + i * 17) * size;
                    var c = origin + new Vector2(side * (55 - i * 5), -10 + i * 2) * size;
                    Line(a, b, new Color(.32f, .34f, .27f), 5 * size); Line(b, c, new Color(.27f, .29f, .22f), 4 * size);
                }
            }
            DrawPart(creature, BodySlot.PiernaIzq, CharacterArt.Part(r, BodySlot.PiernaIzq, rear), rear, selected);
            DrawPart(creature, BodySlot.PiernaDer, CharacterArt.Part(r, BodySlot.PiernaDer, rear), rear, selected);
            DrawPart(creature, BodySlot.Torso, CharacterArt.Part(r, BodySlot.Torso, rear), rear, selected);
            // Anatomical left appears on the right when the enemy faces the camera.
            DrawPart(creature, BodySlot.BrazoIzq, CharacterArt.Part(r, BodySlot.BrazoIzq, rear), rear, selected);
            DrawPart(creature, BodySlot.BrazoDer, CharacterArt.Part(r, BodySlot.BrazoDer, rear), rear, selected);
            var head = CharacterArt.Part(r, BodySlot.Cabeza, rear);
            head = new Rect(head.center.x - head.width * .22f, head.y, head.width * .44f, head.height);
            DrawPart(creature, BodySlot.Cabeza, head, rear, selected);
            if (!rear && creature.GetPiece(BodySlot.Cabeza) != null)
            {
                Fill(new Rect(head.x + 8 * size, head.y + 14 * size, 5 * size, 3 * size), Paper);
                Fill(new Rect(head.xMax - 13 * size, head.y + 14 * size, 5 * size, 3 * size), Paper);
                Line(new Vector2(head.center.x - 7 * size, head.y + 29 * size), new Vector2(head.center.x + 7 * size, head.y + 29 * size), Ink, 2 * size);
            }
            if (creature.GetPiece(BodySlot.Cabeza)?.Definition.Id == "cabeza_lobo")
            {
                Line(new Vector2(head.x + 4 * size, head.y + 5 * size), new Vector2(head.x + 1 * size, head.y - 10 * size), Gold, 7 * size);
                Line(new Vector2(head.xMax - 4 * size, head.y + 5 * size), new Vector2(head.xMax - 1 * size, head.y - 10 * size), Gold, 7 * size);
            }
            if (creature.GetPiece(BodySlot.Cabeza)?.Definition.Id == "cabeza_tentaculos")
            {
                for (int i = 0; i < 6; i++)
                {
                    float side = i < 3 ? -1 : 1;
                    var a = head.center;
                    var b = a + new Vector2(side * (22 + i % 3 * 9), -18 - i % 3 * 12) * size;
                    var c = b + new Vector2(side * 12, 20 + i % 3 * 9) * size;
                    Line(a, b, new Color(.57f, .24f, .30f), 6 * size);
                    Line(b, c, new Color(.67f, .33f, .38f), 4 * size);
                }
            }
        }

        public static void Graft(Creature creature, BodySlot slot, Rect r, bool rear)
        {
            bool left = (slot == BodySlot.BrazoIzq || slot == BodySlot.PiernaIzq) == rear;
            var spriteRect = r;
            if (slot == BodySlot.Cabeza) spriteRect = new Rect(r.center.x - r.height * .45f, r.y, r.height * .9f, r.height);
            else if (slot == BodySlot.BrazoIzq || slot == BodySlot.BrazoDer) spriteRect.height *= .64f;
            if (ImperialArt.Graft(creature.GetPiece(slot)?.Definition.Id, spriteRect, left)) return;
            DrawPart(creature, slot, r, rear, null);
            if (creature.GetPiece(slot)?.Definition.Id != "cabeza_tentaculos") return;
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1 : 1;
                var bend = r.center + new Vector2(side * r.width * (.25f + i % 3 * .09f), -r.height * (.2f + i % 3 * .18f));
                Line(r.center, bend, new Color(.41f, .44f, .30f), 6);
                Line(bend, bend + new Vector2(side * r.width * .10f, r.height * .45f), new Color(.55f, .53f, .36f), 4);
            }
        }
        public static void EquipmentOverlay(Creature c, Rect r, bool rear)
        {
            var drawn = new System.Collections.Generic.HashSet<GearItem>();
            foreach (var hand in new[] { GearSlot.ManoIzq, GearSlot.ManoDer })
            {
                var item = c.GetGear(hand); if (item == null || !drawn.Add(item) || !c.CanEquipGear(hand, item)) continue;
                float side = (hand == GearSlot.ManoIzq) == rear ? -1 : 1;
                float handX = c.Profession == Profession.Grafter ? side < 0 ? (rear ? .12f : .18f) : .90f : side < 0 ? .18f : .87f;
                var grip = new Vector2(r.x + r.width * handX, r.y + r.height * .52f);
                var tip = grip + new Vector2(side * r.width * .10f, -r.height * .25f);
                if (ImperialArt.HeldItem(item.Definition.Id, r, grip, side)) continue;
                string id = item.Definition.Id;
                if (id == "daga" || id == "aguja") tip = grip + new Vector2(side * r.width * .08f, -r.height * .13f);
                if (id == "baculo") tip = grip + new Vector2(0, -r.height * .44f);
                if (id == "cintas")
                {
                    var end = grip + new Vector2(side * r.width * .18f, r.height * .19f);
                    var bend = (grip + end) / 2 + new Vector2(side * 12, -8);
                    Line(grip, bend, new Color(.60f, .30f, .32f), 5); Line(bend, end, new Color(.65f, .40f, .31f), 4); continue;
                }
                if (id == "escudo") { Oval(new Rect(grip.x - r.width * .10f, grip.y - r.height * .12f, r.width * .20f, r.height * .24f), new Color(.26f, .30f, .29f)); continue; }
                if (id == "arco")
                {
                    var top = grip + new Vector2(0, -r.height * .22f); var bottom = grip + new Vector2(0, r.height * .16f);
                    var bow = grip + new Vector2(side * r.width * .14f, 0);
                    Line(top, bow, Gold, 4); Line(bow, bottom, Gold, 4); Line(top, bottom, Paper, 1); continue;
                }
                Line(grip, tip, Ink, id == "aguja" ? 3 : id == "sierra" ? 9 : 6);
                Line(grip, tip, id == "baculo" || id == "maza" ? Gold : new Color(.61f, .65f, .64f), id == "aguja" ? 1 : id == "sierra" ? 6 : 3);
                if (id == "maza") Oval(new Rect(tip.x - 10, tip.y - 12, 20, 24), new Color(.45f, .47f, .43f));
                if (id == "baculo") Oval(new Rect(tip.x - 6, tip.y - 8, 12, 15), Gold);
                if (id == "cruz") Line(tip + new Vector2(-12, r.height * .055f), tip + new Vector2(12, r.height * .055f), Paper, 4);
                if (id == "espada" || id == "daga") Line(grip + new Vector2(-8, -6), grip + new Vector2(8, -6), Gold, 3);
                if (id == "sierra") for (int i = 1; i < 7; i++) { var tooth = Vector2.Lerp(grip, tip, i / 7f); Line(tooth, tooth + new Vector2(side * 7, -3), Paper, 2); }
            }
        }

        private static void DrawPart(Creature creature, BodySlot slot, Rect r, bool rear, BodySlot? selected)
        {
            var piece = creature.GetPiece(slot);
            if (piece == null)
            { return; }
            string id = piece.Definition.Id;
            Color color = id.Contains("lobo") ? new Color(.46f, .39f, .27f) :
                id.Contains("ghoul") || id.Contains("podrido") ? new Color(.36f, .46f, .32f) :
                id.Contains("arana") ? new Color(.40f, .35f, .32f) :
                id.Contains("seda") || id.Contains("bailarina") || id.Contains("abanico") ? new Color(.49f, .30f, .47f) : new Color(.37f, .43f, .45f);
            if (rear) color *= .83f;
            if (creature.IsDead) color = new Color(.22f, .22f, .20f);
            Fill(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(0, 0, 0, .35f));
            Fill(r, color); Border(r, new Color(.07f, .08f, .07f), 2);
            if (slot == BodySlot.PiernaIzq)
                Fill(new Rect(r.center.x - 2, r.y + r.height * .25f, 4, r.height * .75f), Ink);
            if (slot == BodySlot.Torso)
            {
                Line(new Vector2(r.x + 5, r.y + 9), new Vector2(r.xMax - 5, r.yMax - 10), new Color(.23f, .23f, .19f), 3);
                Fill(new Rect(r.x, r.yMax - 10, r.width, 5), Gold * .6f);
            }
            if (rear && slot == BodySlot.Cabeza) Fill(new Rect(r.x + 3, r.y + 2, r.width - 6, r.height * .45f), color * .6f);
            if (selected == slot) Border(r, Gold, 3);
            float fraction = (float)piece.Health / piece.MaxHealth;
            if (creature.IsPlayer && fraction < 1) Bar(new Rect(r.x, r.yMax - 3, r.width, 3), fraction, Blood);
        }
    }
}
