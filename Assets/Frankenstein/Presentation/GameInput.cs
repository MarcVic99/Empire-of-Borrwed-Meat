using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frankenstein.Presentation
{
    [Flags]
    public enum GameButton { None = 0, Left = 1, Right = 2, Up = 4, Down = 8, Interact = 16, Menu = 32, Continue = 64 }

    // Merge native polling with Game View events. One physical press must stay
    // one action even when both sources deliver it on adjacent frames.
    public sealed class GameInput
    {
        private readonly HashSet<KeyCode> guiKeys = new HashSet<KeyCode>();
        private GameButton previous, pending;
        public Vector2 Movement { get; private set; }
        public GameButton Pressed { get; private set; }

        public void Capture(KeyCode key, bool down)
        {
            var action = Map(key);
            if (action == GameButton.None) return;
            if (down)
            {
                if (guiKeys.Add(key) && (previous & action) == 0) pending |= action;
            }
            else guiKeys.Remove(key);
        }

        public void Sample(GameButton native)
        {
            var held = native;
            foreach (var key in guiKeys) held |= Map(key);
            Pressed = pending | (held & ~previous);
            previous = held; pending = GameButton.None;
            Movement = new Vector2(((held & GameButton.Right) != 0 ? 1 : 0) - ((held & GameButton.Left) != 0 ? 1 : 0),
                ((held & GameButton.Down) != 0 ? 1 : 0) - ((held & GameButton.Up) != 0 ? 1 : 0)).normalized;
        }

        public void Reset()
        { guiKeys.Clear(); previous = pending = Pressed = GameButton.None; Movement = Vector2.zero; }

        public static GameButton Map(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.A: case KeyCode.LeftArrow: return GameButton.Left;
                case KeyCode.D: case KeyCode.RightArrow: return GameButton.Right;
                case KeyCode.W: case KeyCode.UpArrow: return GameButton.Up;
                case KeyCode.S: case KeyCode.DownArrow: return GameButton.Down;
                case KeyCode.E: return GameButton.Interact;
                case KeyCode.Tab: case KeyCode.Escape: return GameButton.Menu;
                case KeyCode.Space: return GameButton.Continue;
                default: return GameButton.None;
            }
        }

        public void Poll()
        {
            var held = GameButton.None;
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) held |= GameButton.Left;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) held |= GameButton.Right;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) held |= GameButton.Up;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) held |= GameButton.Down;
                if (k.eKey.isPressed) held |= GameButton.Interact;
                if (k.tabKey.isPressed || k.escapeKey.isPressed) held |= GameButton.Menu;
                if (k.spaceKey.isPressed) held |= GameButton.Continue;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            foreach (KeyCode key in new[] { KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.E, KeyCode.Tab, KeyCode.Escape, KeyCode.Space })
                if (Input.GetKey(key)) held |= Map(key);
#endif
            Sample(held);
        }
    }
}
