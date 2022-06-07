using System;

namespace Eco.Mods.BetterBuyOrders
{
    using Shared.Localization;
    using Shared.Utils;

    public static class Logger
    {
        public static void Debug(string message)
        {
            Log.Write(new LocString("[BetterBuyOrders] DEBUG: " + message + "\n"));
        }

        public static void Info(string message)
        {
            Log.Write(new LocString("[BetterBuyOrders] " + message + "\n"));
        }

        public static void Error(string message)
        {
            Log.Write(new LocString("[BetterBuyOrders] ERROR: " + message + "\n"));
        }
    }
}