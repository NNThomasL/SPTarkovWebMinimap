using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using JetBrains.Annotations;
using TechHappy.MapLocation.DI;
using TechHappy.MapLocation.Patches;
using TechHappy.MapLocation.Services;
using TechHappy.MapLocation.Services.Airdrop;

namespace TechHappy.MapLocation
{
    [BepInPlugin("com.techhappy.webminimap", "TechHappy.WebMinimap", "2.0.0")]
    public sealed class MapLocationPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource MapLocationLogger { get; private set; }

        internal static ConfigEntry<bool> OpenMapToggle { get ; private set; }
        
        internal static ConfigEntry<string> ListenIpAddress { get; private set; }

        internal static ConfigEntry<int> ListenPort { get; private set; }

        // We use LightInject here for easy and lightweight dependency injection
        internal static ServiceContainer ServiceContainer { get; private set; }

        [UsedImplicitly]
        internal void Start()
        {
            MapLocationLogger = Logger;
            MapLocationLogger.LogInfo("MapLocation loaded");
            
            string ipAddressToShow = "Failed to find IPv4 address";
            
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddressToShow = ip.ToString();
                    break;
                }
            }

            string configSection = "Local IPv4 Address: " + ipAddressToShow;
            
            OpenMapToggle = Config.Bind
            (
                configSection,
                "Open Map",
                false,
                new ConfigDescription
                (
                    "Opens the map when toggled on"
                )
            );
            
            OpenMapToggle.SettingChanged += OpenMapSettingChanged;

            ListenIpAddress = Config.Bind(
                configSection,
                nameof(ListenIpAddress),
                "0.0.0.0",
                new ConfigDescription("Listen IP Address (requires restarting the game)")
            );

            ListenPort = Config.Bind(
                configSection,
                nameof(ListenPort),
                45366,
                new ConfigDescription(
                    "Destination Port (requires restarting the game)",
                    new AcceptableValueRange<int>(1024, 65535)
                )
            );

            ServiceContainer = ContainerBuilder.BuildContainer();
            var server = ServiceContainer.GetInstance<IMapDataServer>();
            server.StartServer(ListenIpAddress.Value, ListenPort.Value);

            new MapLocationPatch().Enable();
            new AirdropMapLocationPatch(ServiceContainer.GetInstance<IAirdropService>()).Enable();
            new TryNotifyConditionChangedPatch(ServiceContainer.GetInstance<IMapDataServer>()).Enable();
        }
        
        static void OpenMapSettingChanged(object sender, EventArgs e)
        {
            MapLocationLogger.LogInfo($"OpenMap setting changed");

            if (OpenMapToggle.Value)
            {
                OpenMapToggle.Value = false;

                Process.Start($"http://localhost:{ListenPort.Value}/");
            }
        }
    }
}