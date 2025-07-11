using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Threading;
using UniqueSmith.Config;
using UniqueSmith.Patch;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace UniqueSmith
{
    public class UniqueSmithModSystem : ModSystem
    {
        public Harmony harmony;
        public static ModConfig config;
        FileSystemWatcher watcher;

        static public ICoreServerAPI sapi;
        static public ICoreClientAPI capi;

        INetworkChannel channel;
        private void LoadConfig()
        {
            if (!File.Exists(Path.Combine(GamePaths.ModConfig, "UniqueSmith.json")))
            {
                UniqueSmithModSystem.config = new ModConfig
                {
                   Blacklist = new System.Collections.Generic.List<SmithClassItemsObject>
                   {
                       new SmithClassItemsObject
                       {
                           ClassName = "commoner",
                           Itemlist = new string[] { }
                       },
                       new SmithClassItemsObject
                       {
                           ClassName = "hunter",
                           Itemlist = new string[] { "steel", "iron", "bronze", "cokeovendoor" }
                       }
                   },
                    Allowlist = new System.Collections.Generic.List<SmithClassItemsObject>
                    {
                       new SmithClassItemsObject
                       {
                           ClassName = "commoner",
                           Itemlist = new string[] { "copper" }
                       },
                    },
                };


                UniqueSmithModSystem.sapi.StoreModConfig<ModConfig>(UniqueSmithModSystem.config, "UniqueSmith.json");
                return;
            }
            UniqueSmithModSystem.config = UniqueSmithModSystem.sapi.LoadModConfig<ModConfig>("UniqueSmith.json");
        }

        public override void Start(ICoreAPI api)
        {
            channel = api.Network.RegisterChannel(Mod.Info.ModID).RegisterMessageType<ModConfig>();

            harmony = new Harmony(base.Mod.Info.ModID);
            MethodInfo methodInfo = AccessTools.Method(typeof(BlockEntityAnvil), "OpenDialog", null, null);
            MethodInfo method = typeof(BlockEntityAnvilPatch).GetMethod("OpenDialogPrefix");
            harmony.Patch(methodInfo, new HarmonyMethod(method));

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            UniqueSmithModSystem.sapi = api;
            this.LoadConfig();

            var configPath = Path.Combine(GamePaths.ModConfig, "UniqueSmith.json");

            watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath), Path.GetFileName(configPath));
            watcher.Changed += OnConfigFileChanged;
            watcher.EnableRaisingEvents = true;

            sapi.Event.PlayerJoin += (IServerPlayer player) =>
            {
                sapi.Network.GetChannel(Mod.Info.ModID).SendPacket(config, player);
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            UniqueSmithModSystem.capi = api;

            capi.Network.RegisterChannel(Mod.Info.ModID).RegisterMessageType<ModConfig>().SetMessageHandler<ModConfig>(OnReceiveConfig);
        }

        private void OnReceiveConfig(ModConfig config)
        {
            UniqueSmithModSystem.config = config;
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {

            Thread.Sleep(100); // Let the write finish
            config = sapi.LoadModConfig<ModConfig>("UniqueSmith.json");

            foreach (var player in sapi.World.AllOnlinePlayers)
            {
                UniqueSmithModSystem.sapi.Network.GetChannel(Mod.Info.ModID).SendPacket(config, (IServerPlayer)player);
            }
        }

    }
}
