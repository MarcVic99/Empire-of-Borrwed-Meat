using System;
using System.Collections.Generic;
using Frankenstein;
using Frankenstein.Presentation;
using UnityEngine;

public sealed partial class Bootstrap
{
    private float strikeImpact, strikeDuration;
    private readonly Dictionary<Creature, Rect> fighterBounds = new Dictionary<Creature, Rect>();
    private readonly Dictionary<Creature, float> damageTimes = new Dictionary<Creature, float>();
    private Creature striker, strikeTarget;
    private BodySlot strikeSlot;
    private Action pendingImpact;
    private float strikeStarted;
    private float cameraZoom = 1;
    private Vector2 cameraPan;
    private bool BattleInputLocked => striker != null || spoken != null || cameraZoom > 1.004f;

    private void ResetBattleMotion()
    {
        striker = strikeTarget = null;
        pendingImpact = null;
        fighterBounds.Clear();
        damageTimes.Clear();
        cameraZoom = 1;
        cameraPan = Vector2.zero;
    }

    private void BeginStrike(Creature actor, Creature victim, BodySlot slot, ActionDefinition action, Action resolve)
    {
        striker = actor;
        strikeTarget = victim;
        strikeSlot = slot;
        strikeImpact = action.Damage >= 10 ? .32f : .24f;
        strikeDuration = strikeImpact + .40f;
        strikeStarted = Time.unscaledTime;
        pendingImpact = resolve;
    }

    private void ShowDamage(Creature victim, int amount)
    {
        damageTimes[victim] = Time.unscaledTime;
    }

    private bool UpdateStrike()
    {
        if (striker == null) return false;
        float elapsed = Time.unscaledTime - strikeStarted;
        if (pendingImpact != null && elapsed >= strikeImpact)
        {
            var impact = pendingImpact;
            pendingImpact = null;
            impact();
        }

        if (elapsed < strikeDuration) return true;
        striker = strikeTarget = null;
        Battle.HoldingImpact = false;
        enemyAt = Time.unscaledTime + .35f;
        return false;
    }

    private void UpdateBattleCamera()
    {
        bool focused = spoken != null && fighterBounds.TryGetValue(spoken.Speaker, out _);
        float zoom = focused ? 1.16f : striker != null ? 1.025f : 1f;
        Vector2 pan = Vector2.zero;
        if (focused)
        {
            var frame = fighterBounds[spoken.Speaker];
            Vector2 point = new Vector2(frame.center.x, frame.y + frame.height * .27f);
            var center = new Vector2(Screen.width * .5f, Screen.height * .5f);
            pan = (center - point) * zoom;
            pan.x = Mathf.Clamp(pan.x, -Screen.width * .31f, Screen.width * .31f);
            pan.y = Mathf.Clamp(pan.y, -Screen.height * .25f, Screen.height * .25f);
        }

        float ease = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 10);
        cameraZoom = Mathf.Lerp(cameraZoom, zoom, ease);
        cameraPan = Vector2.Lerp(cameraPan, pan, ease);
        if (!focused && striker == null && cameraZoom < 1.004f)
        {
            cameraZoom = 1;
            cameraPan = Vector2.zero;
        }
    }

    private Rect CameraRect(Rect r)
    {
        Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        r.position = center + (r.position - center) * cameraZoom + cameraPan;
        r.size *= cameraZoom;
        if (strikeTarget != null && damageTimes.TryGetValue(strikeTarget, out float hit))
        {
            float age = Time.unscaledTime - hit;
            if (age < .16f)
                r.x += Mathf.Sin(age * 100) * 4 * (1 - age / .16f);
        }
        return r;
    }

    private Rect FighterMotion(Creature actor, Rect r, bool rear)
    {
        if (actor == striker)
        {
            float age = Time.unscaledTime - strikeStarted;
            float step = age < strikeImpact
                ? Mathf.SmoothStep(0, 1, age / strikeImpact)
                : 1 - Mathf.SmoothStep(0, 1, (age - strikeImpact) / .35f);
            Vector2 direction = new Vector2(0, rear ? -1 : 1);
            if (fighterBounds.TryGetValue(strikeTarget, out var victim))
                direction = (victim.center - CharacterArt.Figure(r, actor).center).normalized;
            r.position += direction * Mathf.Clamp(r.height * .14f, 20, 52) * step;
        }

        if (damageTimes.TryGetValue(actor, out float hit))
        {
            float age = Time.unscaledTime - hit;
            if (age < .23f)
                r.position += new Vector2(6 * Mathf.Sin(age * 72), rear ? 13 : -13) * (1 - age / .23f);
        }
        return CameraRect(r);
    }

    private Color FighterTint(Creature actor)
    {
        Color baseTint = actor.IsDead ? new Color(.53f, .49f, .53f) : Color.white;
        if (damageTimes.TryGetValue(actor, out float hit))
        {
            float age = Time.unscaledTime - hit;
            if (age < .22f)
                return Color.Lerp(baseTint, new Color(1, .24f, .30f), 1 - age / .22f);
        }
        return baseTint;
    }

    private void DrawImpact(Rect bodyPart)
    {
        float age = Time.unscaledTime - strikeStarted - strikeImpact;
        if (age < 0 || age > .18f || !damageTimes.TryGetValue(strikeTarget, out float hit) || hit < strikeStarted) return;
        float fade = 1 - age / .18f;
        var point = bodyPart.center;
        var color = new Color(1, .71f, .54f, fade);
        BattlePainter.Line(point + new Vector2(-22, 15), point + new Vector2(21, -16), color, 3);
        BattlePainter.Line(point + new Vector2(-13, -10), point + new Vector2(15, 10), color, 1);
    }

    private void DrawFocusedSpeech()
    {
        if (!fighterBounds.TryGetValue(spoken.Speaker, out var figure))
            figure = new Rect(Screen.width / 2f, Screen.height * .4f, 0, 0);
        figure = CameraRect(figure);
        float width = Mathf.Min(380, Screen.width * .46f);
        float textHeight = label.CalcHeight(new GUIContent(spoken.Text), width - 30);
        float height = textHeight + 72;
        var point = new Vector2(figure.center.x, figure.y + figure.height * .22f);
        float x = figure.xMax + 12;
        if (x + width > Screen.width - 12) x = figure.x - width - 12;
        var bubble = new Rect(Mathf.Clamp(x, 12, Screen.width - width - 12),
            Mathf.Clamp(point.y - height * .35f, 12, Mathf.Max(12, AllyVitalsRect.y - height - 10)), width, height);
        var tail = new Vector2(Mathf.Clamp(point.x, bubble.x, bubble.xMax), Mathf.Clamp(point.y, bubble.y, bubble.yMax));
        BattlePainter.Line(tail, point, BattlePainter.Gold, 1);
        ExplorationArt.Panel(bubble, .98f);
        Text(new Rect(bubble.x + 15, bubble.y + 7, width - 30, 24), spoken.Speaker.Name, small, BattlePainter.Gold);
        Text(new Rect(bubble.x + 15, bubble.y + 31, width - 30, textHeight), spoken.Text);
        if (Btn(new Rect(bubble.x + 15, bubble.yMax - 31, width - 30, 25), "Continuar · Espacio", Time.unscaledTime - speechStarted > .9f))
            spoken = null;
    }
}
