using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private readonly float[] portraitHover = new float[3];
    private float portraitPaintTime;

    private void DrawSelection(Rect r)
    {
        var roles = new[] { Profession.Hunter, Profession.Dancer, Profession.Grafter };
        var previews = new[] { hunterPreview, dancerPreview, grafterPreview };
        string[] specialties = { "Presión de caza", "Magnetismo escénico", "Injertos de campo" };
        string[] descriptions = {
            "Más daño y rendiciones. No recluta a rendidos.",
            "Gran evasión y seducción. Menor defensa y daño.",
            "Injerta sin mesa con un hilo. Le cuesta convencer."
        };
        Text(new Rect(r.x, r.y, r.width, 36), "ELIGE QUIÉN DESCIENDE", heading, BattlePainter.Gold);
        var cards = SelectionCards(r, roles.Length);
        float captionHeight = SelectionCaptionHeight(r.height - 102);

        bool repaint = Event.current.type == EventType.Repaint;
        float now = Time.realtimeSinceStartup;
        float dt = Mathf.Clamp(now - portraitPaintTime, 0, .05f);
        if (repaint) portraitPaintTime = now;

        for (int i = 0; i < roles.Length; i++)
        {
            Rect card = Cell(cards, i, 3, 3);
            var image = new Rect(card.x, card.y, card.width, card.height - captionHeight);
            bool hover = image.Contains(Event.current.mousePosition);
            if (repaint) portraitHover[i] = Mathf.MoveTowards(portraitHover[i], hover ? 1 : 0, dt / .22f);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none)) selectedClass = roles[i];
            bool selected = selectedClass == roles[i];
            Color edge = BattlePainter.Gold * .55f;
            DrawSelectionArtwork(previews[i], roles[i], image, portraitHover[i]);
            var caption = new Rect(card.x + 4, image.yMax, card.width - 8, captionHeight);
            ExplorationArt.Panel(caption);
            bool panelHover = card.Contains(Event.current.mousePosition);
            Color frame = selected ? BattlePainter.Gold : panelHover ? Color.Lerp(edge, BattlePainter.Gold, .40f) : edge;
            BattlePainter.Border(card, frame, selected ? 2 : 1);
            BattlePainter.Line(new Vector2(caption.x + 8, caption.y + 1), new Vector2(caption.xMax - 8, caption.y + 1), edge);
            var crest = new Vector2(card.center.x, caption.y + 1);
            BattlePainter.Line(crest + new Vector2(-6, 0), crest + new Vector2(0, -5), edge, 2);
            BattlePainter.Line(crest + new Vector2(0, -5), crest + new Vector2(6, 0), edge, 2);
            var nameStyle = card.height < 360 ? label : heading;
            string name = ClassName(roles[i]).ToUpperInvariant();
            var nameRect = new Rect(caption.x + 12, caption.y + 10, caption.width - 24, 24);
            Text(nameRect, name, nameStyle, selected ? BattlePainter.Gold : BattlePainter.Paper);
            Text(new Rect(caption.x + 12, caption.y + 35, caption.width - 24, 19), specialties[i], small, BattlePainter.Gold);
            Text(new Rect(caption.x + 12, caption.y + 55, caption.width - 24, Mathf.Min(34, caption.height - 58)), descriptions[i], small);
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
        // Keep the zoom inside the portrait's background margins.
        float inset = .014f * Mathf.SmoothStep(0, 1, hover);
        GUI.DrawTextureWithTexCoords(viewport, art, new Rect(inset, inset, 1 - 2 * inset, 1 - 2 * inset), false);
    }

    public static Rect SelectionCards(Rect area, int count)
    {
        const float aspect = 2f / 3f;
        const float gap = 7;
        float availableHeight = area.height - 102;
        float widthLimit = (area.width - (count - 1) * gap) / count;
        float imageHeight = Mathf.Min(availableHeight - SelectionCaptionHeight(availableHeight), widthLimit / aspect);
        float width = imageHeight * aspect * count + (count - 1) * gap;
        return new Rect(area.center.x - width / 2, area.y + 43, width, imageHeight + SelectionCaptionHeight(availableHeight));
    }

    private static float SelectionCaptionHeight(float height) => Mathf.Clamp(height * .24f, 87, 130);
}
