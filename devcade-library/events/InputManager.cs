using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Devcade.events {
    public class InputManager {
        private static readonly Dictionary<string, InputManager> inputManagers = new();
        private static Dictionary<string, bool> enabled = new();
        private static readonly InputManager globalInputManager = new();

        private const int debounce = 6;
        private readonly DevState[] _states = new DevState[debounce];
        private int ptr;
        private readonly List<Event> events = new();
        public string name { get; }

        private enum State {
            Held,
            Released,
            Pressed,
        }
        private struct Event {
            public CButton button { get; set; }
            public State state { get; set; }
            public Action action { get; set; }
            public bool async { get; set; }

            public bool Invoke() {
                if (async) {
                    Task.Run(action);
                }
                else {
                    action();
                }
                return true;
            }

            public bool Matches(InputManager inputManager) {
                if (!enabled[inputManager.name]) return false;
                switch (state) {
                    case State.Held:
                        if (inputManager.IsHeld(button)) return true;
                        break;
                    case State.Pressed:
                        if (inputManager.IsPressed(button)) return true;
                        break;
                    case State.Released:
                        if (inputManager.IsReleased(button)) return true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return false;
            }
        }

        private InputManager(string name) {
            this.name = name;
        }

        private InputManager() {
            this.name = null;
        }

        /// <summary>
        /// Update Input and invoke events
        /// </summary>
        public static void Update() {
            Input.UpdateInternal();
            (GamePadState, GamePadState) gamePad = Input.GetStates();
            KeyboardState kbState = Keyboard.GetState();
            DevState state = new(kbState, gamePad.Item1, gamePad.Item2);
            globalInputManager.Update(state);
            foreach (InputManager inputManager in inputManagers.Values) {
                inputManager.Update(state);
            }
        }

        private void Update(DevState state) {
            _states[ptr++] = state;
            if (ptr > debounce) {
                ptr = 0;
            }
            events.ForEach(e => {
                bool _ = e.Matches(this) && e.Invoke();
            });
        }

        private DevState GetState(int offset) {
            int index = ptr - offset;
            if (index < 0) {
                index += debounce;
            }

            return _states[index];
        }

        private bool IsPressed(CButton btn) {
            return btn.isDown(GetState(0)) && !btn.isDown(GetState(1));
        }

        private bool IsReleased(CButton btn) {
            return !btn.isDown(GetState(0)) && btn.isDown(GetState(1));
        }

        private bool IsHeld(CButton btn) {
            int acc = 0;
            for (int i = 0; i < debounce; i++) {
                if (btn.isDown(GetState(i))) {
                    acc++;
                }
                else {
                    return false; // if any of the previous frames were not held, then it is not held
                }

                if (acc > 1) return true;
            }

            return false;
        }
        
        public void OnPress(CButton btn, Action action, bool async = false) {
            events.Add(new Event {
                button = btn,
                state = State.Pressed,
                action = action,
                async = async
            });
        }
        
        public void OnRelease(CButton btn, Action action, bool async = false) {
            events.Add(new Event {
                button = btn,
                state = State.Released,
                action = action,
                async = async
            });
        }
        
        public void OnHeld(CButton btn, Action action, bool async = false) {
            events.Add(new Event {
                button = btn,
                state = State.Held,
                action = action,
                async = async
            });
        }

        public static void OnPressGlobal(CButton btn, Action action, bool async = false) {
            globalInputManager.OnPress(btn, action, async);
        }
        
        public static void OnReleaseGlobal(CButton btn, Action action, bool async = false) {
            globalInputManager.OnRelease(btn, action, async);
        }
        
        public static void OnHeldGlobal(CButton btn, Action action, bool async = false) {
            globalInputManager.OnHeld(btn, action, async);
        }
        
        public static void OnHeld(CButton btn, Action action, string name, bool async = false) {
            if (!inputManagers.ContainsKey(name)) {
                inputManagers[name] = new InputManager();
            }
            inputManagers[name].OnHeld(btn, action, async);
        }
        
        public static void OnPress(CButton btn, Action action, string name, bool async = false) {
            if (!inputManagers.ContainsKey(name)) {
                inputManagers[name] = new InputManager();
            }
            inputManagers[name].OnPress(btn, action, async);
        }
        
        public static void OnRelease(CButton btn, Action action, string name, bool async = false) {
            if (!inputManagers.ContainsKey(name)) {
                inputManagers[name] = new InputManager();
            }
            inputManagers[name].OnRelease(btn, action, async);
        }

        public static InputManager getInputManager(string name) {
            if (inputManagers.ContainsKey(name)) {
                return inputManagers[name];
            }
            InputManager inputManager = new(name);
            inputManagers[name] = inputManager;
            enabled[name] = true;
            return inputManager;
        }

        public void setEnabled(bool enabled) {
            InputManager.enabled[name] = enabled;
        }

        public static void setEnabled(string name, bool enabled) {
            if (!inputManagers.ContainsKey(name)) return;
            InputManager.enabled[name] = enabled;
        }

        public static void setSoleEnabled(string name) {
            foreach (string key in inputManagers.Keys) {
                enabled[key] = key == name;
            }
        }
        
        public static void setAllEnabled(bool enabled) {
            foreach (string key in inputManagers.Keys) {
                InputManager.enabled[key] = enabled;
            }
        }

        public static List<string> getEnabled() {
            return inputManagers.Keys.Where(key => enabled[key]).ToList();
        }
        
        public static List<string> getDisabled() {
            return inputManagers.Keys.Where(key => !enabled[key]).ToList();
        }
        
        public static void setEnabled(IEnumerable<string> names) {
            // IEnumerable can be lazy so we need to force it to evaluate
            // otherwise every time we check name.Contains(key) it will evaluate the whole thing again
            names = names.ToList();
            foreach (string key in inputManagers.Keys) {
                enabled[key] = names.Contains(key);
            }
        }
        
        public static void setDisabled(IEnumerable<string> names) {
            // IEnumerable can be lazy so we need to force it to evaluate
            // otherwise every time we check name.Contains(key) it will evaluate the whole thing again
            names = names.ToList();
            foreach (string key in inputManagers.Keys) {
                enabled[key] = !names.Contains(key);
            }
        }
    }
}