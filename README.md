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

These define the buttons of the devcade cabinet to be used in the get methods below.

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

Before any data is saved `Init()` must be called once. This sets up the thread that will handle saving and loading.

The two main methods are `SaveData.Save()` and `SaveData.Load<T>()`. These are used to save and load data respectively, and are both asynchronous. Thus, they return a `Task<Result>`. This can be awaited or ignored as needed.

Sync versions of these methods are also available. These are `SaveData.SaveSync()` and `SaveData.LoadSync<T>()`. These are not recommended for many saves/loads at once as they will block the main thread while saving and loading.

### Loading data

`SaveData.Save(string group, string key, T value, JsonSerializationSettings serializerOptions = null)`

All data in a given group are stored in the same file, so using multiple groups can prevent key collisions and speed up loading for large amounts of data.

The key is the name of the data to be saved. This can be any string and is used to identify the data when loading.

This method accepts and type of data that can be serialized by Newtonsoft.Json. This includes most built in types and any simple classes that do not contain circular references.

The serializerOptions parameter is optional and allows for customizing the serialization process. This can be used to ignore certain properties or to change the formatting of the data.

### Saving data

`SaveData.Load<T>(string group, string key, JsonSerializationSettings serializerOptions = null)`

This method returns the data saved with the given key in the given group. If no data is found it will return the default value for the type.

For reference types this will be null, for value types this will be the default value for that type.

For safety, data that is not T will be returned as null. Make sure that you load data as the same type that it was saved as.

The serializerOptions parameter is optional and allows for customizing the serialization process. This can be used to ignore certain properties or to change the formatting of the data.

### Additional Save/Load info

The data is saved by the onboard backend to the file system currently. While running locally this will save data to the current directory, although this can be changed through the `SetLocalPath()` method.
