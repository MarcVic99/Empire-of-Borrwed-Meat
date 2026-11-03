using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankenstein.Presentation
{
    public static class ImperialArt
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        [Serializable] private sealed class Catalogue { public Atlas[] atlases; }
        [Serializable] private sealed class Frame { public float x, y, width, height; public Rect Rect => new Rect(x, y, width, height); }
        [Serializable] private sealed class Atlas { public string id; public int columns, rows; public Frame[] frames; }
        private static Catalogue catalogue;
        public static Texture2D Load(string key)
        {
            if (!cache.TryGetValue(key, out var texture))
            {
                texture = Resources.Load<Texture2D>("Frankenstein/Imperio/" + key + "-v2") ?? Resources.Load<Texture2D>("Frankenstein/Imperio/" + key);
                if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; cache[key] = texture; }
            }
            return texture;
        }
        public static Rect UV(string key, int column, int row, int columns, int rows)
        {
            if (catalogue == null)
            {
                var json = Resources.Load<TextAsset>("Frankenstein/Imperio/layouts");
                catalogue = json == null ? new Catalogue() : JsonUtility.FromJson<Catalogue>(json.text);
            }
            if (catalogue.atlases != null)
                foreach (var atlas in catalogue.atlases)
                    if (atlas.id == key && atlas.frames != null && row * columns + column < atlas.frames.Length)
                        return atlas.frames[row * columns + column].Rect;
            return new Rect(column / (float)columns, 1 - (row + 1) / (float)rows, 1f / columns, 1f / rows);
        }
        public static bool Draw(string key, Rect r, int column = 0, int row = 0, int columns = 1, int rows = 1)
        {
            var texture = Load(key); if (texture == null) return false;
            GUI.DrawTextureWithTexCoords(r, texture, UV(key, column, row, columns, rows), true); return true;
        }
        public static string Role(Profession role) => role == Profession.Hunter ? "hunter" : role == Profession.Dancer ? "dancer" : "grafter";
        public static string EnemyId(Creature c)
        {
            foreach (var pair in Content.Profiles) if (ReferenceEquals(c.Profile, pair.Value)) return pair.Key;
            return c.Appearance == "cabeza_lobo" ? "lobo" : c.Appearance == "cabeza_ghoul" ? "ghoul" : c.Appearance == "cabeza_arana" ? "arana" : c.Sex == Sex.Female ? "bandida" : "bandido";
        }
        public static int EnemyIndex(string id) => id == "bandida" ? 1 : id == "lobo" ? 2 : id == "ghoul" ? 3 : id == "arana" ? 4 : id == "jefe" ? 5 : 0;
        public static int IconIndex(string id)
        {
            switch (id)
            {
                case "espada": case "daga": return 0;
                case "maza": return 1; case "arco": return 2; case "baculo": return 3;
                case "cruz": case "amuleto": return 4; case "sierra": return 5; case "aguja": return 6; case "cintas": return 7;
                case "escudo": return 8; case "abrigo": case "delantal": case "cota": return 9;
                case "balm": return 10; case "thread": return 11; case "gold": return 12;
                case "pierna_humana": return 14; case "garra_lobo": return 15; default: return 13;
            }
        }
        public static bool Icon(string id, Rect r)
        {
            if ((id.StartsWith("cabeza_") || id == "brazo_orco" || id == "pata_arana") && Graft(id, r, false)) return true;
            int i = IconIndex(id); return Draw("icons", r, i % 4, i / 4, 4, 4);
        }
        public static bool Graft(string id, Rect r, bool flip)
        {
            int i = id == "cabeza_humana" ? 0 : id == "cabeza_lobo" ? 1 : id == "cabeza_ghoul" ? 2 : id == "cabeza_tentaculos" ? 3 : id == "brazo_humano" || id == "brazo_podrido" ? 4 : id == "brazo_orco" ? 5 : id == "garra_lobo" ? 6 : id == "pata_arana" ? 7 : -1;
            if (i < 0) return false;
            var texture = Load("grafts"); if (texture == null) return false;
            var uv = UV("grafts", i % 4, i / 4, 4, 2);
            if (flip) { uv.x += uv.width; uv.width = -uv.width; }
            GUI.DrawTextureWithTexCoords(r, texture, uv, true); return true;
        }
        public static bool HeldItem(string id, Rect figure, Vector2 grip, float side)
        {
            var texture = Load("icons"); if (texture == null) return false;
            int index = IconIndex(id); var uv = UV("icons", index % 4, index / 4, 4, 4);
            float height = figure.height * (id == "daga" ? .22f : id == "escudo" ? .27f : id == "cintas" ? .22f : id == "arco" ? .45f : .36f);
            float width = height * Mathf.Abs(uv.width * texture.width / (uv.height * texture.height));
            Vector2 handle = new Vector2(.78f, .20f); float sourceAngle = 135;
            if (id == "maza" || id == "baculo") { handle = new Vector2(.16f, .82f); sourceAngle = -45; }
            if (id == "cruz") { handle = new Vector2(.5f, .85f); sourceAngle = -90; }
            float angle = Mathf.Atan2(-.25f, side * .10f) * Mathf.Rad2Deg - sourceAngle;
            if (id == "arco") { handle = new Vector2(.5f, .5f); angle = -40; }
            if (id == "escudo" || id == "cintas") { handle = new Vector2(.5f, .35f); angle = 0; }
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, grip);
            GUI.DrawTextureWithTexCoords(new Rect(grip.x - width * handle.x, grip.y - height * handle.y, width, height), texture, uv, true);
            GUI.matrix = matrix; return true;
        }
    }
}
