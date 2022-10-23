# Devcade-library
A monogame library for allowing games to interact with cabinet functions.

## Input wrapping
### ArcadeButtons enum
`Input.ArcadeButtons` is an enum with values A1 through A4, B1 through B4, Menu, StickUp, StickDown, StickRight, and StickLeft.

These values are equivelant to values of the `Buttons` enum and can be used in place of them when explicitly cast to a `Buttons`. This allows existing controller input code to be easily adapted to the Devcade control scheme.

Example:
`gamePadState.IsButtonDown((Buttons)Devcade.Input.ArcadeButtons.Menu)`