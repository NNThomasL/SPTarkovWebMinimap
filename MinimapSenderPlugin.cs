using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using JetBrains.Annotations;
using NetCoreServer;
using System.Net;
using System;

namespace TechHappy.MinimapSender
{
    [BepInPlugin("com.techhappy.minimapsender", "TechHappy.MinimapSender", "1.0.0")]
    public class MinimapSenderPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource MinimapSenderLogger { get; private set; }
        internal static ConfigEntry<int> RefreshIntervalMillieconds { get; private set; }
        internal static ConfigEntry<int> DestinationPort { get; private set; }
        internal static MinimapServer _server;

        [UsedImplicitly]
        private void Awake()
        {
            MinimapSenderLogger = Logger;
            MinimapSenderLogger.LogInfo("MinimapSender loaded");

            const string configSection = "Map settings";

            RefreshIntervalMillieconds = Config.Bind
            (
                configSection,
                nameof(RefreshIntervalMillieconds),
                250,
                new ConfigDescription
                (
                    "Map position refresh interval in milliseconds (1 second = 1000 milliseconds)",
                    new AcceptableValueRange<int>(50, 5000)
                )
            );

            DestinationPort = Config.Bind
            (
                configSection,
                nameof(DestinationPort),
                8080,
                new ConfigDescription
                (
                    "Destination Port",
                    new AcceptableValueRange<int>(1024, 65535)
                )
            );

            new MinimapSenderPatch().Enable();

            try
            {
                // WebSocket server port
                int port = DestinationPort.Value;
                // WebSocket server content path
                string www = "BepInEx/plugins/TechHappy-MinimapSender/www";

                MinimapSenderLogger.LogInfo($"WebSocket server port: {port}");
                MinimapSenderLogger.LogInfo($"WebSocket server static content path: {www}");
                MinimapSenderLogger.LogInfo($"WebSocket server website: http://localhost:{port}/index.html");

                // Create a new WebSocket server
                _server = new MinimapServer(IPAddress.Any, port);
                _server.AddStaticContent(www, "/");

                // Start the server
                MinimapSenderLogger.LogInfo("Server starting...");
                _server.Start();

                MinimapSenderLogger.LogInfo("Done!");
            }
            catch (Exception e)
            {
                MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

    }
}