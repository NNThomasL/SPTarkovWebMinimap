using System;
using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;

namespace TechHappy.MinimapSender
{
    public class UpdateConditionsVisibilityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(QuestClass).GetMethod("UpdateConditionsVisibility", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            try
            {
                //MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"QuestClass -> UpdateConditionsVisibility was called!");

                //MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Quest: {__instance.ToString()}");

                MinimapSenderController.Instance.UpdateQuestData();
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}
