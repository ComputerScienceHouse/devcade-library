using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Devcade.events {
    internal class DevState {
        private KeyboardState kbState;
        private GamePadState gp1;
        private GamePadState gp2;

        public DevState() {
            this.kbState = Keyboard.GetState();
            this.gp1 = GamePad.GetState(PlayerIndex.One);
            this.gp2 = GamePad.GetState(PlayerIndex.Two);
        }
        
        public DevState(KeyboardState kbState, GamePadState gp1, GamePadState gp2) {
            this.kbState = kbState;
            this.gp1 = gp1;
            this.gp2 = gp2;
        }

        private bool isKeyDown(Keys key) {
            return kbState.IsKeyDown(key);
        }
        
        private bool isButtonDown(Buttons button, PlayerIndex player) {
            switch (player) {
                case PlayerIndex.One:
                    return gp1.IsButtonDown(button);
                case PlayerIndex.Two:
                    return gp2.IsButtonDown(button);
                case PlayerIndex.Three:
                case PlayerIndex.Four:
                default:
                    return false;
            }
        }

        public bool isDown(GButton button, PlayerIndex playerIndex) {
            switch (playerIndex) {
                case PlayerIndex.One:
                case PlayerIndex.Two:
                    return isButtonDown((Buttons)button, playerIndex);
                case PlayerIndex.Four:
                    return isKeyDown((Keys)button);
                case PlayerIndex.Three:
                default:
                    throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, null);
            }
        }
    }
}