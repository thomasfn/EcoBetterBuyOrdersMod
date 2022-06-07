using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

using HarmonyLib;

namespace Eco.Mods.BetterBuyOrders.HarmonyPatches
{
    using Gameplay.Items;
    using Gameplay.Components;
    using Gameplay.Economy;
    using Gameplay.Players;

    using Shared.Utils;

    [HarmonyPatch(typeof(StoreComponent))]
    [HarmonyPatch("UpdateStock")] // if possible use nameof() here
    internal class UpdateStockPatch
    {
        private static PropertyInfo stockInventoriesProperty
            = typeof(StoreComponent).GetProperty("StockInventories", BindingFlags.Instance | BindingFlags.NonPublic);

        private static PropertyInfo depositInventoriesProperty
            = typeof(StoreComponent).GetProperty("DepositInventories", BindingFlags.Instance | BindingFlags.NonPublic);

        [ThreadStatic]
        private static Stopwatch profilingTimer;

        internal static bool Prefix(StoreComponent __instance, bool initializing)
        {
            if (profilingTimer == null) { profilingTimer = new Stopwatch(); }
            var StockInventories = stockInventoriesProperty.GetValue(__instance) as IEnumerable<Inventory>;
            var DepositInventories = depositInventoriesProperty.GetValue(__instance) as IEnumerable<Inventory>;
            var StoreData = __instance.StoreData;

            profilingTimer.Reset();
            profilingTimer.Start();

            // Original impl of UpdateStock (more or less)
            {
                var stockStacks = StockInventories.AllStacks().AsList();
                var stockStock = CalculateStock(stockStacks);
                var depositStacks = DepositInventories.AllStacks().AsList();
                var depositStock = CalculateStock(depositStacks);
                foreach (var offer in StoreData.SellOffers)
                {
                    // If it's a durability item, only count items that meet the req
                    var contained = offer.Stack.Item is DurabilityItem
                         ? stockStacks.Count(stack => stack.Item?.Type == offer.Stack.Item.Type && (stack.Item as DurabilityItem)?.Durability >= offer.MinDurability)
                         : stockStock.GetOrDefault(offer.Stack.Item?.Type);
                    offer.Stack.Modify(offer.ShouldLimit ? Math.Max(0, contained - offer.Limit) : contained);
                }

                foreach (var offer in StoreData.BuyOffers)
                {
                    offer.Stack.Modify(offer.ShouldLimit ? Math.Max(0, offer.Limit - depositStock.GetOrDefault(offer.Stack.Item?.Type)) : 999);
                }
            }
            TimeSpan afterOriginalCheckpoint = profilingTimer.Elapsed;

            // Modded logic to limit buy orders
            LimitBuyOrdersBySpaceAvailable(StoreData, DepositInventories);
            TimeSpan afterLimitBySpace = profilingTimer.Elapsed;
            LimitBuyOrdersByCurrencyAvailable(StoreData, __instance.Balance);
            TimeSpan afterLimitByCurrency = profilingTimer.Elapsed;

            // Final original call to EconomyTracker
            EconomyTracker.UpdateStore(__instance, !initializing);
            TimeSpan afterUpdateStore = profilingTimer.Elapsed;
            profilingTimer.Stop();

            // Log profiling data
            if (BetterBuyOrdersPlugin.Obj.Config.EnableProfiling)
            {
                BetterBuyOrdersPlugin.Obj.ReportUpdateStockRun(afterOriginalCheckpoint, afterLimitBySpace - afterOriginalCheckpoint, afterLimitByCurrency - afterLimitBySpace, afterUpdateStore - afterLimitByCurrency, DepositInventories.Count());
            }

            // Skip original
            return false;
        }

        private static void LimitBuyOrdersBySpaceAvailable(StoreItemData storeData, IEnumerable<Inventory> depositInventories)
        {
            foreach (var offer in storeData.BuyOffers)
            {
                if (offer.Stack.Item == null) { continue; }
                // TODO: Pass the store owner user in instead of null, see what happens?
                int space = GetSpace(depositInventories, offer.Stack.Item, null);
                if (space < offer.Stack.Quantity)
                {
                    offer.Stack.Modify(space);
                }
            }
        }

        private static void LimitBuyOrdersByCurrencyAvailable(StoreItemData storeData, float balance)
        {
            foreach (var offer in storeData.BuyOffers)
            {
                if (offer.Stack.Item == null) { continue; }
                var totalCost = offer.Stack.Quantity * offer.Price;
                if (totalCost <= balance) { continue; }
                int amountCanAfford = (int)(balance / offer.Price);
                offer.Stack.Modify(amountCanAfford);
            }
        }

        private static int GetSpace(IEnumerable<Inventory> inventories, Item item, User depositor = null)
        {
            int result = 0;
            foreach (var inv in inventories)
            {
                int maxAccepted = inv.GetMaxAcceptedVal(item, item.MaxStackSize, depositor);
                if (maxAccepted == 0) { continue; }
                foreach (var stack in inv.Stacks)
                {
                    if ((stack.Quantity > 0 && stack.Item != item) || stack.Quantity >= maxAccepted) { continue; }
                    result += maxAccepted - stack.Quantity;
                }
            }
            return result;
        }

        private static Dictionary<Type, int> CalculateStock(IEnumerable<ItemStack> stacks)
        {
            var groups = stacks.GroupBy(x => x.Item?.GetType());
            var dict = groups.Where(g => g.Key != null).ToDictionary(group => group.Key, group => group.Sum(stack => stack.Quantity));
            return dict;
        }
    
        
    }
}
