using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PzWorkshopUploaderCLI.Steam
{
    public class SteamManager
    {
        public const uint SteamAppId = 108600;

        public static bool IsSteamManagerInitialized = false;

        private static Thread steamThread = new Thread(RunSteamCallbacks);
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static ManualResetEventSlim threadStoppedEvent = new ManualResetEventSlim(false);

        public static async Task Start()
        {
            PreInitialise();
            await Initialise();
            RegisterCallbacks();
        }

        public static void StartListening()
        {
            steamThread.Start();
        }

        public static void StopListening()
        {
            // Signal the thread to stop
            cancellationTokenSource.Cancel();

            // Wait for the thread to stop and clean up resources
            threadStoppedEvent.Wait();
            cancellationTokenSource.Dispose();
            threadStoppedEvent.Dispose();
        }

        public static async Task Stop()
        {
            await Shutdown();
        }

        public static async Task<ulong> CreateWorkshopItem(string title, string description, string contentPath, string previewImagePath)
        {
            if (!IsSteamManagerInitialized)
            {
                Console.WriteLine("SteamManager is not initialized. Call Start() before creating a Workshop item.");
                return 0;
            }

            // Create the workshop item
            SteamAPICall_t createItemCall = SteamUGC.CreateItem(new AppId_t(SteamAppId), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            //TODO HANDLE CB
            CallResult<CreateItemResult_t> createItemResult = new CallResult<CreateItemResult_t>();
            createItemResult.Set(createItemCall, (result, failure) =>
            {
                if (result.m_eResult == EResult.k_EResultOK)
                {
                    Console.WriteLine("Workshop item created successfully. Published ID: " + result.m_nPublishedFileId);
                }
                else
                {
                    Console.WriteLine("Failed to create workshop item. Error code: " + result.m_eResult);
                }
            });

            while (!createItemResult.IsActive())
            {
                SteamAPI.RunCallbacks();
                await Task.Delay(10);
            }

            //TODO HANDLE CB
            ulong publishedItemId = createItemResult.m_nPublishedFileId;

            // Set item properties
            SteamUGC.SetItemTitle(new UGCUpdateHandle_t(publishedItemId), title);
            SteamUGC.SetItemDescription(new UGCUpdateHandle_t(publishedItemId), description);

            // Set item content
            SteamUGC.SetItemContent(new UGCUpdateHandle_t(publishedItemId), contentPath);

            // Set item preview image
            SteamUGC.SetItemPreview(new UGCUpdateHandle_t(publishedItemId), previewImagePath);

            // Submit item update
            //TODO HANDLE CB
            SteamAPICall_t submitItemUpdateCall = SteamUGC.SubmitItemUpdate(new UGCUpdateHandle_t(publishedItemId), "Item update");
            CallResult<SubmitItemUpdateResult_t> submitItemUpdateResult = new CallResult<SubmitItemUpdateResult_t>();
            submitItemUpdateResult.Set(submitItemUpdateCall, (result, failure) =>
            {
                if (result.m_eResult == EResult.k_EResultOK)
                {
                    Console.WriteLine("Item update submitted successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to submit item update. Error code: " + result.m_eResult);
                }
            });

            while (!submitItemUpdateResult.IsActive())
            {
                SteamAPI.RunCallbacks();
                await Task.Delay(10);
            }

            return publishedItemId;
        }

        public static void UpdateWorkshopItem(ulong publishedFileId, string filePath)
        {
            if (!IsSteamManagerInitialized)
            {
                Console.WriteLine("SteamManager is not initialized. Call Start() before updating a Workshop item.");
                return;
            }

            // Create the update handle
            UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(new AppId_t(SteamAppId), new PublishedFileId_t(publishedFileId));
            if (updateHandle == UGCUpdateHandle_t.Invalid)
            {
                Console.WriteLine("Failed to create update handle for Workshop item.");
                return;
            }

            // Set the properties of the update handle
            SteamUGC.SetItemTitle(updateHandle, "Updated Workshop Item");
            SteamUGC.SetItemDescription(updateHandle, "This is the updated Workshop item description.");
            SteamUGC.SetItemContent(updateHandle, filePath);

            // Start the update process
            SteamAPICall_t updateCall = SteamUGC.SubmitItemUpdate(updateHandle, "Updating Workshop item");

            // Register the callback to handle the result
            CallResult<SubmitItemUpdateResult_t> updateResult = new CallResult<SubmitItemUpdateResult_t>((result, failure) =>
            {
                if (result.m_eResult == EResult.k_EResultOK)
                {
                    Console.WriteLine("Workshop item updated successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to update Workshop item. Error code: {result.m_eResult}");
                }
            });
            updateResult.Set(updateCall);
        }

        private static void RegisterCallbacks()
        {
            //TODO
        }

        private static void RunSteamCallbacks()
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    SteamAPI.RunCallbacks();

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            finally
            {
                threadStoppedEvent.Set();
            }
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


        private static Task Initialise()
        {
            if (!Packsize.Test())
            {
                Console.WriteLine("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
                return Shutdown();
            }

            if (!DllCheck.Test())
            {
                Console.WriteLine("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");
                return Shutdown();
            }

            // load app id from .txt
            // don't put this in config.json because Steam expects steam_appid.txt to exist (see the comment below about API init)

            Console.WriteLine("[Steamworks.NET] App ID is: " + SteamAppId.ToString());

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
                    return Shutdown();
                }
            }
            catch (System.DllNotFoundException e)
            { // We catch this exception here, as it will be the first occurrence of it.
                Console.WriteLine("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e);
                return Shutdown();
            }

            return Task.CompletedTask;
        }

        // OnApplicationQuit gets called too early to shutdown the SteamAPI.
        // Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
        // Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().

        private static Task Shutdown()
        {
            SteamAPI.Shutdown();
            return Task.CompletedTask;
        }
    }
}
