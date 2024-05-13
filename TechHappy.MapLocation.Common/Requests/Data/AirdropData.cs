namespace TechHappy.MapLocation.Common.Requests.Data
{
    public sealed class AirdropData
    {
        public float XPosition { get; set; }

        public float YPosition { get; set; }

        public float ZPosition { get; set; }

        public bool Equals(AirdropData other)
        {
            return XPosition.Equals(other.XPosition) && YPosition.Equals(other.YPosition) && ZPosition.Equals(other.ZPosition);
        }
    }
}