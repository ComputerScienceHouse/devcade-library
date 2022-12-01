# Devcade-library
A monogame library for allowing games to interact with cabinet functions.

---

- [Input](#input-wrapping)
  - [ArcadeButtons enum](#arcadebuttons-enum)
  - [Get methods](#get-methods)
- [Event-Based Input](#input-events)
  - [CButton](#Combined-button)
  - [Callbacks](#Callbacks)

---
---

## Input wrapping
### ArcadeButtons enum
`Input.ArcadeButtons` is an enum with values 
- A1 through A4
- B1 through B4
- Menu
- StickUp, StickDown, StickRight, and StickLeft

These values are equivelant to values of the `Buttons` enum and can be used in place of them when explicitly cast to a `Buttons`. This allows existing controller input code to be easily adapted to the Devcade control scheme.

Example:
`gamePadState.IsButtonDown((Buttons)Devcade.Input.ArcadeButtons.Menu)`

---
### Get methods

In order to use these methods `Input.Initialize()` must be called once before using them and `Input.Update()` must be called once each frame.

#### `GetButton(int playerNum, ArcadeButtons button)`

Given the player and button to check, it will return true if the button is down. This will return true on the initial press and for the duration that the button is held.

#### `GetButtonDown(int playerNum, ArcadeButtons button)`

Given the player and button to check, it will return true if the button is pressed down during the current frame. This only returns true on the initial press from up to down and will not trigger repeatedly while the button is held.

#### `GetButtonUp(int playerNum, ArcadeButtons button)`

Given the player and button to check, it will return true if the button is released during the current frame. This only returns true on the initial release from down to up and will not trigger repeatedly while the button is up.

#### `GetButtonHeld(int playerNum, ArcadeButtons button)`

Given the player and button to check, it will return true if the button is being held down. This will not return true for the initial press or the release.

#### `GetStick(int playerNum)`

Given the player it returns a `Vector2` representing the stick direction.

---
---

## Input Events

The primary difference for event-based input is that instead of calling `Input.Update()` every frame, you should call `InputManager.Update()`

### Combined Button

Combined buttons are generally better for your health than a thousand `||`s and `&&`s between everything [Citation needed]

`CButton` is a class that represents a combined button, which will be 'pressed' when its conditions are met

A CButton can be created from a single button using `CButton.from`

```csharp
CButton keyA = CButton.from(Keys.A);
CButton gpX = CButton.from(Buttons.X);
CButton devB1 = CButton.from(Input.ArcadeButtons.B1);
```

CButtons can also be combined arbitrarily or inverted

```csharp
CButton AorX = keyA | gpX;
CButton AandB1 = keyA & devB1;
CButton notX = !gpX;
```

A CButton can be created from an arbitrary number of inputs

```csharp
CButton anyDirection = CButton.or(Keys.Up, Keys.Down, Keys.Left, Keys.Right);
CButton sprint = CButton.and(Keys.Shift, Keys.W);
```

The inputs to `CButton.or()` and `CButton.and()` can be other CButtons, Keys, or Buttons (GamePad or Devcade).

---
---

### Callbacks

The InputManager uses C#'s `Action`s, which are functions that have no inputs and return `void`

All Actions should be bound in the `Initialize()` method of your game

You have three options for binding an `Action` to a Key:

#### Global Input Manager

The Global input manager represents a group of keybinds that cannot be turned off in any circumstances. For simple games, everything can be done in the global manager, for more control over which keys are bound to what and when, use multiple instances of the Input Manager.

```csharp
// This is the 'exit' button, which is either the Esc key or both Menu buttons. 
// This is acutally defined in CButton as CButton.exit, so you won't need to
// make one yourself
CButton exit = CButton.or(
  CButton.from(Keys.Escape),
  CButton.and(
    CButton.from(Input.ArcadeButtons.Menu, PlayerIndex.One),
    CButton.from(Input.ArcadeButtons.Menu, PlayerIndex.Two),
  ),
);

InputManager.onPressedGlobal(exit, () => { Exit(); });

// You can also use a method group here, which is just a shorthand for the above
// InputManager.onPressedGlobal(exit, Exit);
```

#### Static groups

Static management interacts with the InputManager class statically to define groups of keybindings. These groups can be individually enabled and disabled to allow for finer control over which keybinds are active when. 

```csharp
// Class managing input in a submenu

CButton up = CButton.or(Keys.Up, Keys.W, Input.ArcadeButtons.StickUp);
CButton down = CButton.or(Keys.Down, Keys.S, Input.ArcadeButtons.StickUp);

// This is not necessary, as binding groups are enabled by default.
// This is only to demonstrate how to enable and disable groups
InputManager.setEnabled("submenu", true);

InputManager.onPressed(up, "submenu", () => {
  selected--;
})
InputManager.onPressed(down, "submenu", () => {
  selected++;
})
```

#### Instance groups

This uses instances of the InputManager class to define groups of keybindings. This is, under the hood, entirely identical to static management. The only difference is in code style. Both static and non-static management can be used in the same game, and can in fact both refer to the same binding group interchangeably.

```csharp
// Class managing input in a submenu

CButton up = CButton.or(Keys.Up, Keys.W, Input.ArcadeButtons.StickUp);
CButton down = CButton.or(Keys.Down, Keys.S, Input.ArcadeButtons.StickUp);

InputManager submenuManager = InputManager.getInputManager("submenu");

// This is not necessary, as binding groups are enabled by default.
// This is only to demonstrate how to enable and disable groups
submentManager.setEnabled(true);

submenuManager.onPressed(up, () => {
  selected--;
});

submenuManager.onPressed(down, () => {
  selected++;
});
```

---
