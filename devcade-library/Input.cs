using Microsoft.Xna.Framework.Input;

namespace Devcade
{
  public static class Input
  {
    /// <summary>
    /// Enum of button names available on the cabinet. Maps directly to the 
    /// equivalent in the Buttons enum. Allows you to use existing controller 
    /// logic and essentially just rename the buttons but you must explicitly 
    /// cast it to a Button each time.
    /// </summary>
    public enum ArcadeButtons
    {
      A1=Buttons.X,
      A2=Buttons.Y,
      A3=Buttons.RightShoulder,
      A4=Buttons.LeftShoulder,
      B1=Buttons.A,
      B2=Buttons.B,
      B3=Buttons.RightTrigger,
      B4=Buttons.LeftTrigger,
      Menu=Buttons.Start,
      StickDown=Buttons.LeftThumbstickDown,
      StickUp=Buttons.LeftThumbstickUp,
      StickLeft=Buttons.LeftThumbstickLeft,
      StickRight=Buttons.LeftThumbstickRight
    }
  }
}