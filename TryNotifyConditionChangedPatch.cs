using Aki.Reflection.Patching;
using System;
using System.Reflection;

namespace TechHappy.MinimapSender
{
    /// <summary>
    /// Represents a patch that is responsible for handling the notification of quest condition changes.
    /// </summary>
    public class TryNotifyConditionChangedPatch : ModulePatch
    {
        /// <summary>
        /// Retrieves the TryNotifyConditionChanged method of the QuestControllerClass for patching.
        /// </summary>
        /// <returns>The target method to be patched.</returns>
        protected override MethodBase GetTargetMethod()
        {
            foreach (var type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                if (
                  type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic | BindingFlags.Instance) != null &&
                  type.BaseType == typeof(QuestControllerClass<>))
                {
                    return type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic| BindingFlags.Instance);
                }
            }

            MinimapSenderPlugin.MinimapSenderLogger.LogError($"Unable to find class derived from QuestControllerClass with method TryNotifyConditionChanged");

            return null;
        }

        /// <summary>
        /// Represents a method for patching the TryNotifyConditionChanged method of the QuestControllerClass.
        /// The patch calls the MinimapSenderController's UpdateQuestData method when a quest condition is changed.
        /// </summary>
        [PatchPostfix]
        public static void PatchPostfix()
        {
            try
            {
                if (MinimapSenderController.Instance != null)
                {
                    MinimapSenderController.Instance.UpdateQuestData();
                }
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}
