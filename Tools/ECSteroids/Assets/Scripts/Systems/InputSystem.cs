using System;
using BDG_ECS;
using UnityEngine;

namespace BDG_ECS
{
    /// <summary>
    /// maps a key to a string describing the binding
    /// </summary>
    public class KeyBinding {
        public char Key;
        public String Binding;
    }

    /// <summary>
    /// Holds dynamic state (down, just pressed, up, just released) for keys
    /// </summary>
    public class KeyState{
        public enum State {
            KeyDown,
            KeyUp,
            KeyPressed,
            KeyReleased
        };

        public char Key;
        public State CurrentState;
    }

    public class InputSystem : BDGECSBaseSystem
    {
        public InputSystem ()
        {
        }

        public override void Tick()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            }
            else {
            }
            if (Input.GetKeyDown(KeyCode.RightArrow)) {
            }
            else {
            }
        }
    }
}

