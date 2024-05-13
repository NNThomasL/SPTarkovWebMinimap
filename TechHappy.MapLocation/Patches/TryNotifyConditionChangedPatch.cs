using System;
using System.Reflection;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;
using TechHappy.MapLocation.Services;
using TechHappy.MapLocation.Services.Quests;

namespace TechHappy.MapLocation.Patches
{
    public class TryNotifyConditionChangedPatch : ModulePatch
    {
        private static IMapDataServer _mapDataServer;

        public TryNotifyConditionChangedPatch(IMapDataServer mapDataServer)
        {
            _mapDataServer = mapDataServer;
        }

        protected override MethodBase GetTargetMethod()
        {
            foreach (Type type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                MethodInfo method = type.GetMethod("TryNotifyConditionChanged", BindingFlags.Public | BindingFlags.Instance);

                if (method != null && method.GetParameters()[0].Name == "quest")
                {
                    return method;
                }
            }

            MapLocationPlugin.MapLocationLogger.LogError("Could not find TryNotifyConditionChanged method");

            return null;
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            try
            {
                TriggerWithId[] triggers = ZoneDataHelper.GetAllTriggers();

                Task.Run(
                    () =>
                    {
                        try
                        {
                            _mapDataServer.OnQuestsChanged(triggers);
                            _mapDataServer.LastQuestChangeTime = DateTime.UtcNow;
                        }
                        catch (Exception e)
                        {
                            MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
                        }
                    });
            }
            catch (Exception e)
            {
                MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}