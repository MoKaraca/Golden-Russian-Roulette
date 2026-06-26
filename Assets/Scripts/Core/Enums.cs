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

    public enum GameState
    {
        MainMenu,
        Playing,   
        Spinning,  
        GameOver   
    }

    public enum ZoneTier
    {
        Standard,
        Safe,
        Super
    }
}
