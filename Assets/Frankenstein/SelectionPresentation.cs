using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private readonly float[] selectionHover = new float[3];
    private float selectionLastPaint;

    private void DrawSelection(Rect r)
    {
        var roles = new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter };
        var previews = new[] { hunterPreview, dancerPreview, grafterPreview };
        string[] specialties = { "Presión de caza", "Danza cautivadora", "Injertos de campo" };
        string[] descriptions = {
            "Más daño y rendiciones. No recluta a rendidos.",
            "Gran evasión y seducción. Menor defensa y daño.",
            "Injerta sin mesa con un hilo. Le cuesta convencer."
        };
        Text(new Rect(r.x, r.y, r.width, 36), "ELIGE QUIÉN DESCIENDE", heading, BattlePainter.Gold);
        var cards = new Rect(r.x, r.y + 43, r.width, r.height - 102);
        float now = Time.realtimeSinceStartup;
        float dt = Mathf.Clamp(now - selectionLastPaint, 0, .05f);
        if (Event.current.type == EventType.Repaint) selectionLastPaint = now;

        for (int i = 0; i < roles.Length; i++)
        {
            Rect card = Cell(cards, i, 3, 3);
            bool hover = card.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                selectionHover[i] = Mathf.MoveTowards(selectionHover[i], hover ? 1 : 0, dt / .24f);
            if (Btn(card, "", true, selectedClass == roles[i])) selectedClass = roles[i];
            bool selected = selectedClass == roles[i];
            Color edge = Color.Lerp(BattlePainter.Gold * .55f, BattlePainter.Gold, selected ? 1 : selectionHover[i]);
            float captionHeight = Mathf.Clamp(card.height * .24f, 87, 130);
            var image = new Rect(card.x + 6, card.y + 6, card.width - 12, card.height - captionHeight - 8);
            DrawSelectionArtwork(previews[i], roles[i], image, selectionHover[i]);
            var caption = new Rect(card.x + 4, image.yMax, card.width - 8, captionHeight + 1);
            ExplorationArt.Panel(caption);
            BattlePainter.Border(card, edge, selected ? 2 : 1);
            BattlePainter.Line(new Vector2(caption.x + 8, caption.y + 1), new Vector2(caption.xMax - 8, caption.y + 1), edge);
            var crest = new Vector2(card.center.x, caption.y + 1);
            BattlePainter.Line(crest + new Vector2(-6, 0), crest + new Vector2(0, -5), edge, 2);
            BattlePainter.Line(crest + new Vector2(0, -5), crest + new Vector2(6, 0), edge, 2);
            Text(new Rect(caption.x + 12, caption.y + 6, caption.width - 24, 24), ClassName(roles[i]).ToUpperInvariant(), card.height < 360 ? label : heading, selected ? BattlePainter.Gold : BattlePainter.Paper);
            Text(new Rect(caption.x + 12, caption.y + 31, caption.width - 24, 19), specialties[i], small, BattlePainter.Gold);
            Text(new Rect(caption.x + 12, caption.y + 51, caption.width - 24, caption.height - 54), descriptions[i], small);
        }

        var descend = new Rect(r.center.x - 112, r.yMax - 48, 224, 44);
        if (Btn(descend, "", true, true))
        {
            facing = 0;
            StartRun(selectedClass);
            GUIUtility.ExitGUI();
        }
        Text(descend, "Descender", new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter }, BattlePainter.Paper);
    }

    private void DrawSelectionArtwork(Creature preview, Profession role, Rect viewport, float hover)
    {
        var art = ImperialArt.Load(ImperialArt.Role(role) + "-close");
        if (art == null) { CharacterArt.Draw(preview, viewport, 2); return; }

        GUI.BeginGroup(viewport);
        var local = new Rect(0, 0, viewport.width, viewport.height);
        BattlePainter.Fill(local, new Color(.13f, .075f, .095f));
        Rect artwork = SelectionArtworkRect(local, art.width / (float)art.height, Mathf.SmoothStep(0, 1, hover));
        GUI.DrawTexture(artwork, art, ScaleMode.StretchToFill, false);
        GUI.EndGroup();
    }

    public static Rect SelectionArtworkRect(Rect viewport, float aspect, float hover)
    {
        float height = Mathf.Min(viewport.height, viewport.width / aspect);
        float width = height * aspect;
        float zoom = 1 + .08f * Mathf.Clamp01(hover);
        float y = viewport.center.y - height / 2 - height * .12f * (zoom - 1);
        return new Rect(viewport.center.x - width * zoom / 2, y, width * zoom, height * zoom);
    }
}
