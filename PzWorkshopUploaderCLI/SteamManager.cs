using Steamworks;
using System;

namespace PzWorkshopUploaderCLI
{
    public class SteamManager
    {
        public static uint m_steamAppId = 108600;

        public static bool IsSteamManagerInitialized = false;

        public static void Start()
        {
            PreInitialise();
            Initialise();
        }

        public static void Update()
        {
            SteamAPI.RunCallbacks();
        }

        public static void Stop()
        {
            Shutdown();
        }

        private static SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
        private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
        {
            Console.WriteLine(pchDebugText);
        }

        private static void PreInitialise()
        {
            if (m_SteamAPIWarningMessageHook == null)
            {
                // Set up our callback to receive warning messages from Steam.
                // You must launch with "-debug_steamapi" in the launch args to receive warnings.
                m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
                SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
            }
        }


        private static bool Initialise()
        {
            if (!Packsize.Test())
            {
                Console.WriteLine("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
            }

            if (!DllCheck.Test())
            {
                Console.WriteLine("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
            }

            // load app id from .txt
            // don't put this in config.json because Steam expects steam_appid.txt to exist (see the comment below about API init)

            Console.WriteLine("[Steamworks.NET] App ID is: " + m_steamAppId.ToString());

            try
            {
                // Initializes the Steamworks API.
                // If this returns false then this indicates one of the following conditions:
                // [*] The Steam client isn't running. A running Steam client is required to provide implementations of the various Steamworks interfaces.
                // [*] The Steam client couldn't determine the App ID of game. If you're running your application from the executable or debugger directly then you must have a [code-inline]steam_appid.txt[/code-inline] in your game directory next to the executable, with your app ID in it and nothing else. Steam will look for this file in the current working directory. If you are running your executable from a different directory you may need to relocate the [code-inline]steam_appid.txt[/code-inline] file.
                // [*] Your application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
                // [*] Ensure that you own a license for the App ID on the currently active Steam account. Your game must show up in your Steam library.
                // [*] Your App ID is not completely set up, i.e. in Release State: Unavailable, or it's missing default packages.
                // Valve's documentation for this is located here:
                // https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
                IsSteamManagerInitialized = SteamAPI.Init();
                if (!IsSteamManagerInitialized)
                {
                    Console.WriteLine("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.");
                }
            }
            catch (System.DllNotFoundException e)
            { // We catch this exception here, as it will be the first occurrence of it.
                Console.WriteLine("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e);
            }

            return IsSteamManagerInitialized;
        }

        // OnApplicationQuit gets called too early to shutdown the SteamAPI.
        // Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
        // Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().
        private static void Shutdown()
        {
            SteamAPI.Shutdown();
        }
    }
}
