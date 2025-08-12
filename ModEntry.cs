using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using BebberBobbers.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Tools;

namespace BebberBobbers
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        private static List<BobberBata>? _bobbers;

        public static List<BobberBata> Bobbers => _bobbers ??=
            ModHelper.GameContent.Load<List<BobberBata>>("Spiderbuttons.BebberBobbers/Bobbers");
        
        public static Dictionary<string, Texture2D> BobberTextureCache { get; } = new Dictionary<string, Texture2D>();

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            Harmony = new Harmony(ModManifest.UniqueID);

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.Content.AssetReady += OnAssetReady;
            Helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            
            GameLocation.RegisterTileAction("Bobbers", (_, _, _, _) =>
            {
                Game1.activeClickableMenu = new BobberBenu();
                return true;
            });
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            //
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // if (!Context.IsWorldReady)
            //     return;

            if (e.Button is SButton.F2)
            {
                Harmony.UnpatchAll(ModManifest.UniqueID);
                Harmony.PatchAll();
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.BebberBobbers/Bobbers"))
            {
                e.LoadFrom(() => new List<BobberBata>(), AssetLoadPriority.Medium);
            }
        }

        private void OnAssetReady(object? sender, AssetReadyEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.BebberBobbers/Bobbers"))
            {
                FishingRod.NUM_BOBBER_STYLES += GetBobberCount();
            }
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var assetName in e.NamesWithoutLocale)
            {
                if (assetName.IsEquivalentTo("Spiderbuttons.BebberBobbers/Bobbers"))
                {
                    _bobbers = null;
                    FishingRod.NUM_BOBBER_STYLES = 39;
                }

                BobberTextureCache.Remove(assetName.Name.ToLowerInvariant());
            }
        }

        public int GetBobberCount()
        {
            return Bobbers.Count;
        }
    }
}