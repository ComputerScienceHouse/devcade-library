# Devcade-library
A monogame library for allowing games to interact with cabinet functions.

---

- [Input](#input-wrapping)
  - [ArcadeButtons enum](#arcadebuttons-enum)
  - [Get methods](#get-methods)
- [Save data](#save-data)
  
---
---

## Input wrapping
### ArcadeButtons enum
`Input.ArcadeButtons` is an enum with values 
- A1 through A4
- B1 through B4
- Menu
- StickUp, StickDown, StickRight, and StickLeft

These defifne the buttons of the devcade cabinet to be used in the get methods below.

These values are equivelant to values of the `Buttons` enum built in to xna and can be used in place of them when explicitly cast to a `Buttons`. This allows existing controller input code to be easily adapted to the Devcade control scheme.

Here is an example of using devcade buttons with generic monogame input methods:
`gamePadState.IsButtonDown((Buttons)Devcade.Input.ArcadeButtons.Menu)`

---
### Get methods

In order to use these methods `Input.Initialize()` must be called once before using them and `Input.Update()` must be called once each frame.
This sets up and updates respectively, the controller state within the library.

#### `GetButton(int playerNum, ArcadeButtons button)`

Given the player (1 or 2) and button to check, it will return true if the button is down. This will return true on the initial press and for the duration that the button is held.

Example: 
```
// Moves character down every frame that the stick is held down.
if (Devcade.Input.GetButton(1, Devcade.Input.ArcadeButtons.StickDown)){
  characterPos.y++;
}
```
#### `GetButtonDown(int playerNum, ArcadeButtons button)`

Given the player (1 or 2) and button to check, it will return true if the button is initially pressed down during the current frame. This only returns true on the initial press from up to down and will not trigger repeatedly while the button is held.

Example: 
```
// Does a thing once for every time a button is pressed down.
if (Devcade.Input.GetButtonDown(1, Devcade.Input.ArcadeButtons.A1)){
  DoThing();
}
```

#### `GetButtonUp(int playerNum, ArcadeButtons button)`

Given the player (1 or 2) and button to check, it will return true if the button is initially released during the current frame. This only returns true on the initial release from down to up and will not trigger repeatedly while the button is up.

Example: 
```
// Does a thing once for every time a button is released.
if (Devcade.Input.GetButtonUp(1, Devcade.Input.ArcadeButtons.A1)){
  DoThing();
}
```

#### `GetButtonHeld(int playerNum, ArcadeButtons button)`

Given the player (1 or 2) and button to check, it will return true if the button is being held down. This will not return true for the initial press or the release but will for every frame in between.

Example: 
```
// Does a thing every frame a button is held down but won't trigger on single frame inputs.
if (Devcade.Input.GetButtonHeld(1, Devcade.Input.ArcadeButtons.A1)){
  DoThing();
}
```

#### `GetStick(int playerNum)`

Given the player (1 or 2) it returns a `Vector2` representing the stick direction.

Example: 
```
// Moves character down every frame that the stick is held down, downleft, or downright.
if (Devcade.Input.GetStick(1).Y < 0){
  characterPos.y++;
}
```

---
---

## Save data
In the Devcade.SaveData namespace, the SaveManager singleton class has two methods used to save or load text data to or from the cloud.

**Warning: These methods will not work on windows at all. Calling them on windows _WILL_ crash. On linux it requires fifo pipes open at `Environment.GetEnvironmentVariable("DEVCADE_PATH") + "/read_game"` and `Environment.GetEnvironmentVariable("DEVCADE_PATH") + "/write_game"`**

### `SaveText(string path, string data)`
Saves the given data to the given path, returns true if it succeeds. Note: This will overwrite if the file already exists.

Example: `SaveManager.Instance.SaveText("saves/user/save1.txt", "This is save data");`

---

### `LoadText(string path)`
Loads data from a given path, returns the loaded data.

Example: `SaveManager.Instance.LoadText("highscores.txt");`
