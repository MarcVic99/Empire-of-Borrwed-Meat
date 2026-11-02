using System.Collections.Generic;
using UnityEngine;
namespace Frankenstein.Presentation
{
    public static class ExplorationArt
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        public static Texture2D Load(string key)
        { if (!cache.TryGetValue(key, out var t)) { t = Resources.Load<Texture2D>("Frankenstein/Exploration/" + key); if (t != null) cache[key] = t; } return t; }
        public static void Player(Profession role, Rect r, int direction, bool moving, float clock)
        {
            string key = role == Profession.Hunter ? "topdown-hunter" : role == Profession.Dancer ? "topdown-dancer-v2" : "topdown-grafter-v2";
            var texture = Load(key); if (texture == null) return;
            int frame = moving ? (int)(clock * 9) % 4 : ((int)(clock * 1.8f) % 2) * 2;
            float idle = moving ? 0 : Mathf.Sin(clock * 2.6f) * 1.1f;
            r.y += idle;
            GUI.DrawTextureWithTexCoords(r, texture, FrameUV(role, direction, frame), true);
        }
        // Actual transparent gutters, measured from the delivered atlas rather than assuming equal rows.
        // In particular, the north-facing hat extends above the nominal 75% line.
        private static readonly float[][] rowBounds = {
            new[] { 0f, .2531898f, .4988038f, .7364434f, 1f },
            new[] { 0f, .2527911f, .4984051f, .7456141f, 1f },
            new[] { 0f, .2527911f, .4976076f, .7428229f, 1f }
        };
        public static Rect FrameUV(Profession role, int direction, int frame)
        {
            var rows = rowBounds[role == Profession.Hunter ? 0 : role == Profession.Dancer ? 1 : 2];
            return new Rect(frame * .25f, 1 - rows[direction + 1], .25f, rows[direction + 1] - rows[direction]);
        }
        public static void Prop(int index, Rect r)
        {
            var texture = Load("props");
            if (texture != null) GUI.DrawTextureWithTexCoords(r, texture, new Rect(index % 4 * .25f, (1 - index / 4) * .5f, .25f, .5f), true);
            else BattlePainter.Oval(r, BattlePainter.Gold);
        }
        public static void Thread(Rect r)
        {
            BattlePainter.Oval(new Rect(r.x, r.y, r.width, r.height * .25f), BattlePainter.Gold);
            BattlePainter.Fill(new Rect(r.x + r.width * .16f, r.y + r.height * .12f, r.width * .68f, r.height * .72f), BattlePainter.Paper);
            for (int i = 1; i < 6; i++) BattlePainter.Line(new Vector2(r.x + 3, r.y + r.height * (.15f + i * .10f)), new Vector2(r.xMax - 3, r.y + r.height * (.19f + i * .10f)), new Color(.48f, .43f, .33f));
            BattlePainter.Oval(new Rect(r.x, r.yMax - r.height * .25f, r.width, r.height * .25f), BattlePainter.Gold);
        }
        public static void Enemy(string id, Rect r)
        {
            int index = id == "lobo" ? 1 : id == "arana" ? 3 : id == "ghoul" || id == "jefe" ? 2 : 0;
            var texture = Load("roaming");
            // The generated atlas has its clear horizontal gutter at 46.6%.
            // Respect that gutter so the next row's heads cannot bleed into a sprite.
            float split = .466f;
            if (texture != null) GUI.DrawTextureWithTexCoords(r, texture,
                new Rect(index % 2 * .5f, index < 2 ? 1 - split : 0, .5f, index < 2 ? split : 1 - split), true);
        }
        public static void Panel(Rect r, float alpha = 1)
        {
            var texture = Load("panel");
            if (texture == null) { BattlePainter.Fill(r, new Color(.08f, .085f, .075f, alpha)); return; }
            var old = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
            float edge = Mathf.Clamp(Mathf.Min(r.width, r.height) * .14f, 6, 20);
            float[] xs = { r.x, r.x + edge, r.xMax - edge, r.xMax };
            float[] ys = { r.y, r.y + edge, r.yMax - edge, r.yMax };
            float[] us = { 0, .06f, .94f, 1 }, vs = { 0, .11f, .89f, 1 };
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                GUI.DrawTextureWithTexCoords(new Rect(xs[x], ys[y], xs[x + 1] - xs[x], ys[y + 1] - ys[y]), texture,
                    new Rect(us[x], 1 - vs[y + 1], us[x + 1] - us[x], vs[y + 1] - vs[y]), true);
            GUI.color = old;
        }
    }
    public static class ButtonSkin
    {
        private sealed class Motion { public float Hover, Press, Last, Click = -10; }
        private static readonly Dictionary<int, Motion> motion = new Dictionary<int, Motion>();
        public static bool Draw(Rect r, string text, bool enabled, bool selected, GUIStyle style)
        {
            int key = text.GetHashCode() ^ Mathf.RoundToInt(r.x) * 397 ^ Mathf.RoundToInt(r.y) * 31;
            if (!motion.TryGetValue(key, out var m)) { m = new Motion(); motion[key] = m; }
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            float now = Time.realtimeSinceStartup;
            if (Event.current.type == EventType.Repaint)
            {
                float dt = Mathf.Min(.05f, now - m.Last); m.Last = now;
                m.Hover = Mathf.Lerp(m.Hover, hover || selected ? 1 : 0, 1 - Mathf.Exp(-14 * dt));
                m.Press = Mathf.Lerp(m.Press, hover && InputHeld() ? 1 : 0, 1 - Mathf.Exp(-24 * dt));
            }
            var visual = new Rect(r.x, r.y + m.Press * 2, r.width, r.height - m.Press * 2);
            ExplorationArt.Panel(visual);
            BattlePainter.Fill(visual, new Color(.60f, .44f, .20f, enabled ? m.Hover * .13f : 0));
            var edge = Color.Lerp(new Color(.24f, .25f, .21f), BattlePainter.Gold, enabled ? m.Hover : .1f);
            BattlePainter.Border(visual, edge, selected ? 2 : 1);
            if (selected) BattlePainter.Fill(new Rect(visual.x + 9, visual.yMax - 4, visual.width - 18, 2), BattlePainter.Gold);
            float pulse = Mathf.Clamp01(1 - (now - m.Click) / .28f);
            if (pulse > 0) BattlePainter.Fill(visual, new Color(.9f, .75f, .40f, pulse * .16f));
            style.normal.textColor = enabled ? BattlePainter.Paper : new Color(.57f, .59f, .54f);
            bool click = false;
            if (enabled && GUI.enabled) click = GUI.Button(visual, text, style);
            else GUI.Label(visual, text, style);
            if (click) m.Click = now;
            if (motion.Count > 800) motion.Clear();
            return click;
        }
        private static bool InputHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}
