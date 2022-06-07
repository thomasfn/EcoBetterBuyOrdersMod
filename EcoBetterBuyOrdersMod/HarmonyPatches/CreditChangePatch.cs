using System;
using System.Reflection;

using HarmonyLib;

namespace Eco.Mods.BetterBuyOrders.HarmonyPatches
{
    using Gameplay.Components;

    [HarmonyPatch(typeof(StoreComponent))]
    [HarmonyPatch("OnCurrencyChange"), HarmonyPatch("OnBankAccountChange"), HarmonyPatch("OnBalanceChanged")]
    internal class CreditChangePatch
    {
        private static MethodInfo markStockDirtyMethod
            = typeof(StoreComponent).GetMethod("MarkStockDirty", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Postfix(StoreComponent __instance)
        {
            markStockDirtyMethod.Invoke(__instance, null);
        }
    }
}