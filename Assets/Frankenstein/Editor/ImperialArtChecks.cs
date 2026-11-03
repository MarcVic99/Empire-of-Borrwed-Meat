using System;
using System.IO;
using System.Linq;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEditor;
using UnityEngine;

public static class ImperialArtChecks
{
    [MenuItem("Frankenstein/Comprobar arte imperial")]
    public static void RunAll()
    {
        foreach (var key in new[] { "hunter", "dancer", "grafter", "walk-hunter", "walk-dancer", "walk-grafter", "enemies", "enemies-back", "roaming", "props", "icons", "grafts", "tiles", "panel", "arena", "portrait-hunter", "portrait-dancer", "portrait-grafter", "hunter-close", "dancer-close", "grafter-close", "hunter-medium", "dancer-medium", "grafter-medium", "enemies-medium", "enemies-back-medium", "props-medium" })
        {
            var texture = ImperialArt.Load(key); Check(texture != null, "Recurso disponible: " + key);
            Check(texture.filterMode == FilterMode.Point, "Píxeles nítidos: " + key);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            Check(importer != null && !importer.mipmapEnabled, "Importación sin mipmaps: " + key);
        }
        foreach (var role in new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter })
        {
            string key = "walk-" + ImperialArt.Role(role);
            var texture = new Texture2D(2, 2);
            ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(ImperialArt.Load(key))));
            for (int direction = 0; direction < 4; direction++) for (int frame = 0; frame < 4; frame++)
            {
                var uv = ExplorationArt.FrameUV(role, direction, frame);
                Check(uv.height > 0 && Mathf.Abs(uv.width) > 0, "Fotograma no vacío");
                int left = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(uv.x, uv.x + uv.width) * texture.width), 0, texture.width - 1);
                int right = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(uv.x, uv.x + uv.width) * texture.width) - 1, 0, texture.width - 1);
                int top = Mathf.Clamp(Mathf.RoundToInt(uv.yMax * texture.height), 0, texture.height - 1);
                for (int x = left; x <= right; x++) Check(texture.GetPixel(x, top).a < .2f, "Cabeza completa en " + key + " dirección " + direction + " frame " + frame);
            }
            UnityEngine.Object.DestroyImmediate(texture);
        }
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Frankenstein/Art/Imperio/Animations" });
        Check(clips.Length == 32, "32 clips reutilizables");
        foreach (var guid in clips)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            var tracks = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            Check(AnimationUtility.GetAnimationClipSettings(clip).loopTime, "Clip en bucle: " + clip.name);
            Check(tracks.Length >= 1 && tracks.All(track => AnimationUtility.GetObjectReferenceCurve(clip, track).All(k => k.value is Sprite)), "Sprites válidos: " + clip.name);
        }
        CheckHeadOnlyIdle();
        var viewport = new Rect(0, 0, 400, 360);
        var resting = Bootstrap.SelectionArtworkRect(viewport, 2 / 3f, 0); var zoomed = Bootstrap.SelectionArtworkRect(viewport, 2 / 3f, 1);
        Check(Mathf.Abs(zoomed.width / resting.width - 1.08f) < .001f, "Zoom de artwork controlado");
        Check(Mathf.Abs(resting.center.x - zoomed.center.x) < .001f, "El zoom conserva el centro horizontal");
        var spider = Content.Enemy("arana"); var r = new Rect(0, 0, 300, 400);
        var figure = CharacterArt.Figure(r, spider);
        Check(figure.width > figure.height, "Araña conserva silueta ancha");
        Check(CharacterArt.Part(r, BodySlot.PiernaIzq, false, spider).width > 0 && CharacterArt.Part(r, BodySlot.PiernaDer, false, spider).width > 0, "Patas laterales conservadas");
        Check(ImperialArt.EnemyId(Content.Enemy("jefe")) == "jefe", "El jefe no comparte el sprite del ghoul");
        Debug.Log("Imperio: 27 recursos, 48 frames sin recortes, 32 clips, 12 idles de cabeza y zoom de artwork comprobados.");
    }
    private static void CheckHeadOnlyIdle()
    {
        foreach (var actor in new[] { "hunter", "dancer", "grafter" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Frankenstein/Art/Imperio/Prefabs/" + actor + "-visual.prefab");
            Check(prefab != null, "Prefab visual disponible: " + actor);
            var probe = UnityEngine.Object.Instantiate(prefab); probe.hideFlags = HideFlags.HideAndDontSave; probe.GetComponent<Animator>().enabled = false;
            try
            {
                foreach (var direction in new[] { "south", "west", "east", "north" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Frankenstein/Art/Imperio/Animations/" + actor + "-idle-" + direction + ".anim");
                    var body = probe.GetComponent<SpriteRenderer>(); var head = probe.transform.Find("Head");
                    clip.SampleAnimation(probe, 0); var original = body.sprite; var position = probe.transform.localPosition; float bottom = body.bounds.min.y, headY = head.localPosition.y;
                    var offsets = new System.Collections.Generic.HashSet<float>();
                    for (int i = 0; i < 4; i++)
                    {
                        clip.SampleAnimation(probe, i / 1.8f + .01f);
                        Check(body.sprite == original && probe.transform.localPosition == position && Mathf.Abs(body.bounds.min.y - bottom) < .0001f, "Cuerpo y pies fijos: " + actor + " " + direction);
                        Check(Mathf.Abs(head.localPosition.x) < .0001f && Mathf.Abs(head.localPosition.y - headY) < .0001f, "Cabeza anclada al cuello sin desplazamiento lateral");
                        offsets.Add(head.localScale.y);
                    }
                    Check(offsets.Count == 3, "Tres poses discretas de cabeza: " + actor + " " + direction);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("Arte imperial: " + message); }
}
