using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Utils;

namespace TechHappy.MapLocation.Services
{
    public sealed class LocalizationHelper : ILocalizationHelper
    {
        private delegate string LocalizedDelegate(string id, string prefix = null);

        private readonly LocalizedDelegate _localize;

        public LocalizationHelper()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

            Type type = PatchConstants.EftTypes.Single(x => x.GetMethod("ParseLocalization", flags) != null);

            MethodInfo localizedMethod = type.GetMethod("Localized", new[] { typeof(string), typeof(string) });

            _localize = (LocalizedDelegate)Delegate.CreateDelegate(typeof(LocalizedDelegate), null, localizedMethod);
        }

        public string Localized(string id, string prefix = null)
        {
            return _localize(id, prefix);
        }
    }
}