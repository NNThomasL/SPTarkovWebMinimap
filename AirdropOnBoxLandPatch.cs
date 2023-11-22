using Aki.Custom.Airdrops;
using Aki.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace TechHappy.MinimapSender
{
    public class AirdropOnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo onBoxLandMethod = typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);

            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            Vector3 airdropBoxPos = __instance.transform.position;
            MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"AirdropBox OnBoxLand() was called!");
            MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Position {airdropBoxPos.x}, {airdropBoxPos.z}");

            MinimapSenderPlugin.airdrops.Add(airdropBoxPos);
        }
    }
}