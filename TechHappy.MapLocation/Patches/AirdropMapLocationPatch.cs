using System;
using System.Reflection;
using Aki.Custom.Airdrops;
using Aki.Reflection.Patching;
using TechHappy.MapLocation.Common.Requests.Data;
using TechHappy.MapLocation.Services.Airdrop;
using UnityEngine;

namespace TechHappy.MapLocation.Patches
{
    public class AirdropMapLocationPatch : ModulePatch
    {
        private static IAirdropService _airdropService;

        public AirdropMapLocationPatch(IAirdropService airdropService)
        {
            _airdropService = airdropService;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            try
            {
                Vector3 airdropPosition = __instance.transform.position;
                _airdropService.AirdropData = new AirdropData
                {
                    XPosition = airdropPosition.x,
                    ZPosition = airdropPosition.z,
                    YPosition = airdropPosition.y,
                };
            }
            catch (Exception e)
            {
                MapLocationPlugin.MapLocationLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }
    }
}