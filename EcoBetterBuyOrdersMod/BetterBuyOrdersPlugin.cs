using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HarmonyLib;

namespace Eco.Mods.BetterBuyOrders
{
    using Core.Plugins.Interfaces;
    using Core.Utils;
    using Core.Plugins;

    using Shared.Localization;
    using Shared.Utils;

    using Gameplay.GameActions;

    using Simulation.Time;
    using Core.Utils.Threading;

    internal readonly struct UpdateStockRunData
    {
        public readonly TimeSpan OriginalSpan;
        public readonly TimeSpan LimitBySpaceSpan;
        public readonly TimeSpan LimitByCurrencySpan;
        public readonly int DepositInventoryCount;

        public UpdateStockRunData(TimeSpan originalSpan, TimeSpan limitBySpaceSpan, TimeSpan limitByCurrencySpan, int depositInventoryCount)
        {
            OriginalSpan = originalSpan;
            LimitBySpaceSpan = limitBySpaceSpan;
            LimitByCurrencySpan = limitByCurrencySpan;
            DepositInventoryCount = depositInventoryCount;
        }
    }

    [Localized]
    public class BetterBuyOrdersConfig
    {
        [LocDescription("Whether to enable profiling of the buy order logic.")]
#if DEBUG
        public bool EnableProfiling { get; set; } = true;
#else
        public bool EnableProfiling { get; set; } = false;
#endif
    }

    [Localized, LocDisplayName(nameof(BetterBuyOrdersPlugin)), Priority(PriorityAttribute.High)]
    public class BetterBuyOrdersPlugin : Singleton<BetterBuyOrdersPlugin>, IModKitPlugin, IInitializablePlugin, IThreadedPlugin, IConfigurablePlugin
    {
        public const double PROFILE_TICK_TIME = 30.0;

        static BetterBuyOrdersPlugin()
        {
            CosturaUtility.Initialize();
        }

        private RepeatableActionWorker tickWorker;
        private IList<UpdateStockRunData> profilingData = new List<UpdateStockRunData>();

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

        public void Run() => this.tickWorker.Start(ThreadPriorityTaskFactory.Lowest);
        public Task ShutdownAsync() => this.tickWorker.ShutdownAsync();

        public LazyResult ShouldOverrideAuth(GameAction action)
            => LazyResult.FailedNoMessage;

        public void Initialize(TimedTask timer)
        {
            var harmony = new Harmony("Eco.Mods.BetterBuyOrders");
            harmony.PatchAll();
            tickWorker = PeriodicWorkerFactory.Create(TimeSpan.FromSeconds(PROFILE_TICK_TIME), this.ProfileTick);
        }

        public void ReportUpdateStockRun(TimeSpan originalSpan, TimeSpan limitBySpaceSpan, TimeSpan limitByCurrencySpan, TimeSpan updateStoreSpan, int depositInventoryCount)
        {
            if (!Config.EnableProfiling) { return; }
            lock (profilingData)
            {
                profilingData.Add(new UpdateStockRunData(originalSpan, limitBySpaceSpan, limitByCurrencySpan, depositInventoryCount));
            }
        }

        private void ProfileTick()
        {
            if (!Config.EnableProfiling) { return; }
            lock (profilingData)
            {
                if (profilingData.Count == 0) { return; }
                double minOriginal = double.MaxValue, sumOriginal = 0.0, maxOriginal = double.MinValue;
                double minLimitBySpace = double.MaxValue, sumLimitBySpace = 0.0, maxLimitBySpace = double.MinValue;
                double minLimitByCurrency = double.MaxValue, sumLimitByCurrency = 0.0, maxLimitByCurrency = double.MinValue;
                int totalDepositInventoryCount = 0;
                foreach (var runData in profilingData)
                {
                    minOriginal = Math.Min(minOriginal, runData.OriginalSpan.TotalMilliseconds);
                    maxOriginal = Math.Max(maxOriginal, runData.OriginalSpan.TotalMilliseconds);
                    minLimitBySpace = Math.Min(minLimitBySpace, runData.LimitBySpaceSpan.TotalMilliseconds);
                    maxLimitBySpace = Math.Max(maxLimitBySpace, runData.LimitBySpaceSpan.TotalMilliseconds);
                    minLimitByCurrency = Math.Min(minLimitByCurrency, runData.LimitByCurrencySpan.TotalMilliseconds);
                    maxLimitByCurrency = Math.Max(maxLimitByCurrency, runData.LimitByCurrencySpan.TotalMilliseconds);
                    
                    sumOriginal += runData.OriginalSpan.TotalMilliseconds;
                    sumLimitBySpace += runData.LimitBySpaceSpan.TotalMilliseconds;
                    sumLimitByCurrency += runData.LimitByCurrencySpan.TotalMilliseconds;

                    totalDepositInventoryCount += runData.DepositInventoryCount;
                }
                double meanOriginal = sumOriginal / profilingData.Count;
                double meanLimitBySpace = sumLimitBySpace / profilingData.Count;
                double meanLimitByCurrency = sumLimitByCurrency / profilingData.Count;
                double meanPerCall = meanOriginal + meanLimitBySpace + meanLimitByCurrency;

                Logger.Debug($"{profilingData.Count} calls to StoreComponent.UpdateStock in the last {PROFILE_TICK_TIME:N}s, taking {meanPerCall:N}ms each on average");
                Logger.Debug($"- Original vanilla logic:           ~{meanOriginal:N}ms ({meanOriginal / meanPerCall:0.00%}) min={minOriginal:N}ms max={maxOriginal:N}ms");
                Logger.Debug($"- Modded 'limit by space' logic:    ~{meanLimitBySpace:N}ms ({meanLimitBySpace / meanPerCall:0.00%}) min={minLimitBySpace:N}ms max={maxLimitBySpace:N}ms (~{sumLimitBySpace / totalDepositInventoryCount:N}ms per inv for a total of {totalDepositInventoryCount} invs)");
                Logger.Debug($"- Modded 'limit by currency' logic: ~{meanLimitByCurrency:N}ms ({meanLimitByCurrency / meanPerCall:0.00%}) min={minLimitByCurrency:N}ms max={maxLimitByCurrency:N}ms");
                profilingData.Clear();
            }
        }
    }
}