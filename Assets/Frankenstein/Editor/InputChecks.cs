using System;
using Frankenstein.Presentation;
using UnityEditor;
using UnityEngine;

public static class InputChecks
{
    [MenuItem("Frankenstein/Comprobar controles")]
    public static void RunAll()
    {
        var input = new GameInput();
        foreach (var key in new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow })
        {
            input.Reset(); input.Capture(key, true); input.Sample(GameButton.None);
            Check(input.Movement.sqrMagnitude > .99f, "Movimiento por eventos: " + key);
            input.Sample(GameButton.None); Check(input.Movement.sqrMagnitude > .99f, "Mantener " + key);
            input.Capture(key, false); input.Sample(GameButton.None); Check(input.Movement == Vector2.zero, "Soltar " + key);
            input.Sample(GameInput.Map(key)); Check(input.Movement.sqrMagnitude > .99f, "Movimiento nativo: " + key);
        }
        foreach (var key in new[] { KeyCode.E, KeyCode.Tab, KeyCode.Escape, KeyCode.Space })
        {
            var action = GameInput.Map(key); input.Reset(); input.Sample(action);
            Check(input.Pressed == action, "Pulsación nativa: " + key);
            input.Capture(key, true); input.Sample(action);
            Check(input.Pressed == GameButton.None, "Evento retrasado no duplica: " + key);
            input.Capture(key, true); input.Sample(action);
            Check(input.Pressed == GameButton.None, "Autorrepetición no repite: " + key);
            input.Capture(key, false); input.Sample(GameButton.None);
            input.Capture(key, true); input.Capture(key, false); input.Sample(GameButton.None);
            Check(input.Pressed == action, "Toque breve entre frames: " + key);
            input.Sample(GameButton.None); Check(input.Pressed == GameButton.None, "Un solo toque: " + key);
            input.Capture(key, true); input.Sample(action); Check(input.Pressed == action, "Fuentes simultáneas: " + key);
        }
        input.Reset(); input.Capture(KeyCode.W, true); input.Capture(KeyCode.D, true); input.Sample(GameButton.None);
        Check(Mathf.Abs(input.Movement.magnitude - 1) < .001f, "Diagonal sin velocidad extra");
        input.Capture(KeyCode.S, true); input.Capture(KeyCode.A, true); input.Sample(GameButton.None);
        Check(input.Movement == Vector2.zero, "Direcciones opuestas se cancelan");
        input.Reset(); input.Sample(GameButton.None);
        Check(input.Movement == Vector2.zero && input.Pressed == GameButton.None, "Perder foco limpia teclas y acciones");
        Debug.Log("Frankenstein: controles WASD/flechas, interacción, menú y diálogo comprobados sin duplicaciones ni teclas retenidas.");
    }
    private static void Check(bool condition, string description)
    { if (!condition) throw new Exception("Controles: " + description); }
}
