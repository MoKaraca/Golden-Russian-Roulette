# Game Developer Demo

## Project Structure

The scripts are organized under `Assets/Scripts/` into four distinct modules in this structure:

```
Mini_Game_Demo/
│
├── Core/
│   ├── GameManager.cs
│   ├── PlayerProfile.cs
│   ├── SoundManager.cs
│   └── Enums.cs
│
├── Data/
│   ├── WheelConfigData.cs
│   ├── RewardItemData.cs
│   ├── RewardDatabase.cs
│   └── WheelSliceData.cs
│
├── Wheel/
│   ├── WheelController.cs
│   ├── WheelVisuals.cs
│   └── WheelSliceVisual.cs
│
└── UI/
    ├── GameUIManager.cs
    ├── MainMenuUIController.cs
    ├── GameOverPanelController.cs
    ├── InventoryUIController.cs
    ├── InventoryItemUI.cs
    ├── ZoneMapUIController.cs
    ├── ZoneItemUI.cs
    └── ResponsiveCanvas.cs
```

### 1. `Core`
The main logic for the game and player data:
- **`GameManager.cs`**: A Singleton that acts as the brain of the game. It manages the `GameState` (MainMenu, Playing, Spinning, GameOver), the current zone logic, and collected rewards. It fires events whenever the state, zone, or rewards change.
- **`PlayerProfile.cs`**: A static class handling the player's persistent currency, saving and loading from `PlayerPrefs`.
- **`SoundManager.cs`**: A Singleton that handles all audio. It dynamically manages background intro music (fading it automatically when the game starts) and provides centralized methods to play UI and animation sound effects.
- **`Enums.cs`**: Defines global enums like `RewardType`, `GameState`, and `ZoneTier`.

### 2. `Data`
Data structures and ScriptableObjects defining the game's configuration:
- **`WheelConfigData.cs`**: A ScriptableObject determining the rules for each `ZoneTier` (Standard, Safe, Super). It stores the Bomb reward asset and decides zone intervals.
- **`RewardItemData.cs`**: A ScriptableObject representing individual rewards (e.g., Gold, Weapons). It calculates scaling based on the current zone.
- **`RewardDatabase.cs`**: A centralized ScriptableObject holding a list of all valid rewards.
- **`WheelSliceData.cs`**: A plain C# class representing the generated data for a single wheel slice.

### 3. `Wheel`
Scripts that handle the logic and visuals of the spinning wheel:
- **`WheelController.cs`**: The logical controller. It uses rules from `WheelConfigData` to randomly pick distinct rewards and generate slices. It then commands the visual counterpart.
- **`WheelVisuals.cs`**: The visual animator. It dynamically instantiates slice prefabs around a circle, handles sprite swapping for different zone tiers, and plays `DOTween` animations for spinning, winning, and bombs.
- **`WheelSliceVisual.cs`**: Attached to the slice prefab, it updates the Image and Text based on `RewardItemData`.

### 4. `UI`
A purely reactive UI layer that listens to `GameManager` events:
- **`GameUIManager.cs`**: Manages the gameplay panel, spin button, and leave button. Hides/shows based on `GameState`.
- **`MainMenuUIController.cs`**: Manages the main menu panel and entry buttons.
- **`GameOverPanelController.cs`**: Manages the popup that appears when a bomb is hit, offering revive or give up options.
- **`InventoryUIController.cs`** & **`InventoryItemUI`**: Listens for collected rewards and dynamically populates a `GridLayoutGroup` with animated item badges.
- **`ZoneMapUIController.cs`** & **`ZoneItemUI`**: Visualizes the current and upcoming zones in a `HorizontalLayoutGroup`.
- **`ResponsiveCanvas.cs`**: A utility script automatically adjusting the Canvas scaler for 20:9 ultrawide aspect ratios.(was used for testing not actual implementation)

---

## Unity Editor structure and Connections 

```
SampleScene
├── Main Camera
├── Directional Light
├── Canvas [Components: MainMenuUIController, ResponsiveCanvas]
│   ├── ui_img_background
│   ├── ui_panel_menu_wheel [Components: Animation, Aniamtor]
│   │   └── ui_img_wheel
│   ├── ui_img_border
│   ├── ui_btn_play_standard
│   │   └── Text (TMP)
│   ├── ui_btn_play_super
│   │   └── Text (TMP)
│   ├── ui_img_coin_1
│   ├── ui_img_coin_2
│   ├── ui_img_skip_bg
│   ├── ui_txt_skip
│   ├── ui_txt_title
│   ├── ui_img_coin_3
│   ├── ui_img_curr_border
│   └── ui_txt_currency_value
├── EventSystem
├── GameManager [Component: GameManager, SoundManager]
└── Canvas_Gameplay [Components: GameUIManager, GameOverPanelController]
    └── ui_panel_gameplay
        ├── ui_img_background
        ├── ui_panel_wheel_root [Components: WheelController, WheelVisuals]
        │   └── ui_animator_wheel
        │       └── ui_image_wheel_base_value
        ├── ui_img_indicator
        ├── ui_btn_spin
        ├── ui_img_inventory_border
        │   └── ui_panel_inventory [Component: InventoryUIController]
        ├── ui_btn_leave
        │   └── ui_txt_leave
        ├── ui_img_zone_border
        │   └── ui_panel_zone_map [Component: ZoneMapUIController]
        └── ui_panel_gameover
            ├── ui_txt_bomb_title
            └── ui_img_bomb_border

```
### 1. Core Logic

#### GameManager GameObject & Component
Intentionally kept entirely separate from the UI canvases. It tracks the current game state (MainMenu vs Playing vs GameOver), the player's current zone progression, and the loot they have accumulated.

#### SoundManager Component
A dedicated GameObject housing the audio logic. It handles playing SFX across the entire UI and automatically manages the fading in and out of the Main Menu intro music based on `GameManager` state changes, ensuring audio never interrupts the gameplay flow.

### 2. Main Menu Layer (Canvas)

#### MainMenuUIController
Manages the entry point of the game. It links up the "Play" and "Skip 20 Zones" buttons, and displays the player's persistent currency. When the GameManager announces the state has changed to Playing, this component automatically fades out and disables the entire Main Menu Canvas.

### 3. Gameplay Layer (Canvas_Gameplay)

#### GameUIManager
Manages the general gameplay layout (`ui_panel_gameplay`), the linking of the "Spin" and "Leave" buttons. It listens to the `GameManager` to safely toggle button interactivity (e.g., disabling the spin button while the wheel is actively spinning) and checks if the player is allowed to leave with their loot.

#### GameOverPanelController
Manages the panel that appears when the player hits a Bomb. It handles the logic and UI updates for the "Revive" and "Give Up" choices.


### The Wheel (ui_panel_wheel_root)

#### WheelController
Handles the *math*. It uses the rules for the current zone to calculate item weights, pick random rewards, determine if a bomb should be added, and decide exactly what slice the wheel will land on.

#### WheelVisuals
Handles the visuals. Commanded by the `WheelController`, it swaps the background graphics (Standard/Safe/Super), dynamically spawns the slice icons in a circle, and runs the `DOTween` animations for spinning, win bounces, and bomb shakes.


### 5. Dynamic Trackers

#### InventoryUIController (on ui_panel_inventory)
Listens to the `GameManager` for newly collected rewards. The script dynamically instantiates and stacks animated reward badges as the player progresses.

#### ZoneMapUIController (on ui_panel_zone_map)
Visualizes the zone progression. Utilizing a `HorizontalLayoutGroup`, it spawns the badges representing the current zone and the upcoming 10 zones so the player knows when a Safe or Super zone is approaching.