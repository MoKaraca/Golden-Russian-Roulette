namespace MiniGameDemo.Data
{
    /// <summary>
    /// Runtime data describing a single slice on the generated wheel.
    /// Created each time GenerateWheelForZone is called.
    /// </summary>
    public class WheelSliceData
    {
        public RewardItemData reward;
        public int amount;
        public float weight;
    }
}
