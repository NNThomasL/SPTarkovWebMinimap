namespace TechHappy.MapLocation.Services
{
    public interface ILocalizationHelper
    {
        string Localized(string id, string prefix = null);
    }
}