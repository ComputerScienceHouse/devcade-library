using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Devcade.events {
    public class CButton {

        public static readonly CButton exit = or(
            Keys.Escape,
            and(
                from(Buttons.Start, PlayerIndex.One), 
                from(Buttons.Start, PlayerIndex.Two)
            )
        );
        
        private enum Operator {
            And,
            Or,
            Xor,
            Not,
            None
        }
        
        private readonly Operator op;
        private readonly PlayerIndex playerIndex = PlayerIndex.One;
        private readonly CButton left;
        private readonly CButton right;
        private readonly GButton button;
        
        private CButton(GButton button, PlayerIndex playerIndex) {
            this.button = button;
            this.op = Operator.None;
            this.left = null;
            this.right = null;
            this.playerIndex = playerIndex;
        }

        private CButton(CButton left, CButton right, Operator op) {
            this.op = op;
            this.left = left;
            this.right = right;
        }

        /// <summary>
        /// Creates a new Compound Button from the given Key
        /// </summary>
        /// <param name="key">A Keyboard Key</param>
        /// <returns></returns>
        public static CButton from(Keys key) {
            return new CButton((GButton)key, PlayerIndex.Four); // PlayerIndex.Four is a dummy value that will always refer to the keyboard
        }
        
        /// <summary>
        /// Creates a new Compound Button from the given Button with a default PlayerIndex of One
        /// </summary>
        /// <param name="btn">A Gamepad Button or Devcade Button</param>
        /// <returns></returns>
        public static CButton from(Buttons btn) {
            return new CButton((GButton)btn, PlayerIndex.One);
        }
        
        /// <summary>
        /// Creates a new Compound Button from the given Button with the given PlayerIndex
        /// </summary>
        /// <param name="btn">A Gamepad Button or Devcade Button</param>
        /// <param name="playerIndex">PlayerIndex of the button</param>
        /// <returns></returns>
        public static CButton from(Buttons btn, PlayerIndex playerIndex) {
            return new CButton((GButton)btn, playerIndex);
        }
        
        /// <summary>
        /// Creates a new Compound Button that is active when both of the buttons are active
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static CButton operator &(CButton left, CButton right) {
            return new CButton(left, right, Operator.And);
        }
        
        /// <summary>
        /// Creates a new Compound Button that is active when either of the buttons are active
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static CButton operator |(CButton left, CButton right) {
            return new CButton(left, right, Operator.Or);
        }
        
        /// <summary>
        /// Creates a new Compound Button that is active when exactly one of the buttons are active
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static CButton operator ^(CButton left, CButton right) {
            return new CButton(left, right, Operator.Xor);
        }
        
        /// <summary>
        /// Creates a new Compound Button that is active when the given button is not active
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static CButton operator !(CButton button) {
            return new CButton(button, null, Operator.Not);
        }

        /// <summary>
        /// Creates a Compound Button which is active when any of the buttons in the array are active
        /// </summary>
        /// <param name="btns"></param>
        /// <returns></returns>
        public static CButton or(params CButton[] btns) {
            switch (btns.Length) {
                case 2:
                    return btns[0] | btns[1];
                case 1:
                    return btns[0];
                default: {
                    int mid = btns.Length / 2;
                    return or(btns.Take(mid).ToArray()) | or(btns.Skip(mid).ToArray());
                }
            }
        }

        /// <summary>
        /// Creates a Compound Button which is active when any of the buttons in the array are active.
        /// </summary>
        /// <param name="btns">An array of Compound Buttons, Buttons, or Keys</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if one of the input objects is not a valid button type</exception>
        public static CButton or(params object[] btns) {
            var cbtns = new CButton[btns.Length];
            for (int i = 0; i < btns.Length; i++) {
                switch (btns[i]) {
                    case CButton _:
                        cbtns[i] = (CButton)btns[i];
                        break;
                    case Buttons _:
                        cbtns[i] = from((Buttons)btns[i]);
                        break;
                    case Keys _:
                        cbtns[i] = from((Keys)btns[i]);
                        break;
                    default:
                        throw new ArgumentException("buttons must only contain CompoundButtons, Buttons, or Keys");
                }
            }
            return or(cbtns);
        }
        
        /// <summary>
        /// Creates a Compound Button which is active when all of the buttons in the array are active.
        /// </summary>
        /// <param name="btns">An array of Compound Buttons</param>
        /// <returns></returns>
        public static CButton and(params CButton[] btns) {
            switch (btns.Length) {
                case 2:
                    return btns[0] & btns[1];
                case 1:
                    return btns[0];
                default: {
                    int mid = btns.Length / 2;
                    return and(btns.Take(mid).ToArray()) & and(btns.Skip(mid).ToArray());
                }
            }
        }
        
        /// <summary>
        /// Creates a Compound Button which is active when all of the buttons in the array are active.
        /// </summary>
        /// <param name="btns">An array of Compound Buttons, Buttons, or Keys</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if one of the input objects is not a valid button type</exception>
        public static CButton and(params object[] btns) {
            var cbtns = new CButton[btns.Length];
            for (int i = 0; i < btns.Length; i++) {
                cbtns[i] = btns[i] switch {
                    CButton c => c,
                    Buttons b => from(b),
                    Keys k => from(k),
                    _ => throw new ArgumentException("buttons must only contain CompoundButtons, Buttons, or Keys")
                };
            }
            return and(cbtns);
        }

        internal bool isDown(DevState state) {
            return op switch {
                Operator.And => left.isDown(state) && right.isDown(state),
                Operator.Or => left.isDown(state) || right.isDown(state),
                Operator.Xor => left.isDown(state) ^ right.isDown(state),
                Operator.Not => !left.isDown(state),
                Operator.None => state.isDown(button, playerIndex),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}