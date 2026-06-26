# Mini Game Demo

Welcome to the Mini Game Demo project! This README provides a comprehensive overview of the script architecture, project structure, and how UI components are connected to the Unity Editor.

## Project Structure

The scripts are organized under `Assets/Scripts/` into four distinct modules:

### 1. `Core`
The central hub for game logic and player data:
- **`GameManager.cs`**: A Singleton that acts as the brain of the game. It manages the `GameState` (MainMenu, Playing, Spinning, GameOver), the current zone logic, and collected rewards. It fires events whenever the state, zone, or rewards change.
- **`PlayerProfile.cs`**: A static class handling the player's persistent currency, saving and loading from `PlayerPrefs`.
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
- **`ZoneMapUIController.cs`** & **`ZoneItemUI`**: Visualizes the current and upcoming zones in a `HorizontalLayoutGroup`, color-coding standard, safe, and super zones.
- **`ResponsiveCanvas.cs`**: A utility script automatically adjusting the Canvas scaler for 20:9 ultrawide aspect ratios.

---

## Editor Connections & UI Binding

To enforce loose coupling and cleaner code architecture, **there are no `onClick` UnityEvents configured via the Inspector.** All buttons and UI elements are wired up dynamically in code via `Awake()` or `OnValidate()`.

### How Connections Work:
1. **`OnValidate` Auto-Assignment**: Most UI scripts utilize `#if UNITY_EDITOR` with an `OnValidate()` method to automatically find child objects by name (e.g., `"ui_btn_spin"`, `"ui_panel_gameplay"`) and assign them to serialized fields. This minimizes manual drag-and-drop mistakes in the Inspector.
2. **Code-based Listeners**: Buttons have their logic attached during `Awake()`. For instance, in `GameUIManager.cs`:
   ```csharp
   _btn_spin.onClick.AddListener(OnSpinClicked);
   _btn_leave.onClick.AddListener(OnLeaveClicked);
   ```
3. **Event-Driven UI**: UI managers never poll. They subscribe to events like `GameManager.Instance.OnStateChanged` and `PlayerProfile.OnCurrencyChanged` in `Start()`, and update canvas groups, text values, and block raycasts accordingly.

### Modifying the UI:
If you need to change the UI in the Unity Editor:
- **Renaming**: Do not change the names of critical GameObjects (e.g., `ui_btn_spin` or `ui_txt_zone_value`) as the `OnValidate` scripts rely on these specific names to link references.
- **Prefabs**: 
  - The Inventory uses the `InventoryItemUI` prefab.
  - The Zone Map uses the `ZoneItemUI` prefab.
  - The Wheel uses the `WheelSliceVisual` prefab.
  Ensure these prefabs retain their inner object names so scripts can hook into them properly.
