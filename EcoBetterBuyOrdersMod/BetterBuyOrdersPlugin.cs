using System;

using HarmonyLib;

namespace Eco.Mods.BetterBuyOrders
{
    using Core.Plugins.Interfaces;
    using Core.Utils;
    using Core.Plugins;

    using Shared.Localization;
    using Shared.Utils;

    using Gameplay.GameActions;

    [Localized]
    public class BetterBuyOrdersConfig
    {
        
    }

    [Localized, LocDisplayName(nameof(BetterBuyOrdersPlugin)), Priority(PriorityAttribute.High)]
    public class BetterBuyOrdersPlugin : Singleton<BetterBuyOrdersPlugin>, IModKitPlugin, IConfigurablePlugin, IInitializablePlugin
    {
        static BetterBuyOrdersPlugin()
        {
            CosturaUtility.Initialize();
        }

        public IPluginConfig PluginConfig => config;

        private PluginConfig<BetterBuyOrdersConfig> config;
        public BetterBuyOrdersConfig Config => config.Config;

        public BetterBuyOrdersPlugin()
        {
            config = new PluginConfig<BetterBuyOrdersConfig>("BetterBuyOrders");
        }

        public object GetEditObject() => Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new ThreadSafeAction<object, string>();

        public void OnEditObjectChanged(object o, string param)
        {
            this.SaveConfig();
        }

        public string GetDisplayText() => string.Empty;
        public string GetCategory() => Localizer.DoStr("Config");
        public string GetStatus() => string.Empty;
        public override string ToString() => Localizer.DoStr("BetterBuyOrders");

        public LazyResult ShouldOverrideAuth(GameAction action)
            => LazyResult.FailedNoMessage;

        public void Initialize(TimedTask timer)
        {
            var harmony = new Harmony("com.example.patch");
            harmony.PatchAll();
        }
    }
}