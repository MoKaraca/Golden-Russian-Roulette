namespace MiniGameDemo.Core
{
    public enum RewardType
    {
        Currency,
        Gold,
        Weapon,
        Consumable,
        Bomb
    }

    /// <summary>
    /// The state machine for the entire game session.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,   // Wheel is idle, player can act
        Spinning,  // Wheel is animating
        GameOver   // Bomb was hit
    }

    /// <summary>
    /// Describes what kind of wheel (and risk level) a zone uses.
    /// Standard = has bomb | Safe = silver, no bomb (every 5th) | Super = gold, premium (every 30th).
    /// </summary>
    public enum ZoneTier
    {
        Standard,
        Safe,
        Super
    }
}
