using Aki.Reflection.Patching;
using System;
using System.Reflection;

namespace TechHappy.MinimapSender
{
    public class OnConditionValueChangedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            //return RefHelper.HookRef.Create(RefTool.GetEftType(x =>
            //        x.GetMethod("OnConditionValueChanged", BindingFlags.DeclaredOnly | RefTool.NonPublic) != null),
            //    "OnConditionValueChanged").TargetMethod;

            foreach (var type in typeof(EFT.AbstractGame).Assembly.GetTypes())
            {
                // type.Name.StartsWith("GClass") &&
                // find class that is hte in-game QuestControllerClass (at least I think that is what the class is for...)
                if (
                  type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic | BindingFlags.Instance) != null &&
                  type.BaseType == typeof(QuestControllerClass))
                {
                    //// set variables for later use then break from foreach after doing so
                    //screenController = type;
                    //ScreenControllerInstance = screenController.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    //ScreenSetFov = screenController.GetMethod("SetFov", BindingFlags.Public | BindingFlags.Instance);
                    //break;

                    return type.GetMethod("TryNotifyConditionChanged", BindingFlags.NonPublic| BindingFlags.Instance);
                }
            }

            MinimapSenderPlugin.MinimapSenderLogger.LogError($"Unable to find class derived from QuestControllerClass with method TryNotifyConditionChanged");

            return null;
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            try
            {
                //MinimapSenderPlugin.MinimapSenderLogger.LogError($"QuestClass -> OnConditionValueChanged was called!");

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
