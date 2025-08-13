using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace BebberBobbers
{
    internal sealed class ModEntry : Mod
    {
        private static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; private set; } = null!;
        private static Harmony Harmony { get; set; } = null!;

        private static List<BobberBata>? _bobbers;

        public static List<BobberBata> Bobbers => _bobbers ??=
            ModHelper.GameContent.Load<List<BobberBata>>("Spiderbuttons.BebberBobbers/Bobbers");
        
        public static Dictionary<string, Texture2D> BobberTextureCache { get; } = new();

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
            
            GameLocation.RegisterTileAction("Bobbers", (_, _, _, _) =>
            {
                Game1.activeClickableMenu = new BobberBenu(5);
                return true;
            });
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.F2)
            {
                //
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

        private int GetBobberCount()
        {
            return Bobbers.Count;
        }
    }
}