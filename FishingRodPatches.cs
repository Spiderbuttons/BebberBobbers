using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using BebberBobbers.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace BebberBobbers;

[HarmonyPatch(typeof(FishingRod))]
public static class FishingRodPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FishingRod.draw))]
    public static void FishingRod_draw_Postfix(FishingRod __instance, SpriteBatch b)
    {
        var bobberIndex = __instance.getBobberStyle(__instance.getLastFarmerToUse());
        if (bobberIndex < 40) return;

        bobberIndex -= 40;
        var selectedBobber = ModEntry.Bobbers[bobberIndex];
        string textureName = PathUtilities.NormalizeAssetName(selectedBobber.Texture ?? "");

        if (string.IsNullOrEmpty(textureName))
        {
            ModEntry.ModMonitor.LogOnce(
                $"Bobber texture is null or empty for custom bobber with Id '{selectedBobber.Id}'", LogLevel.Error);
            return;
        }

        int spriteIndex = selectedBobber.SpriteIndex;

        if (!__instance.bobber.Value.Equals(Vector2.Zero) && __instance.isFishing)
        {
            if (!ModEntry.BobberTextureCache.TryGetValue(textureName, out var texture))
            {
                texture = Game1.content.Load<Texture2D>(textureName);
                ModEntry.BobberTextureCache[textureName.ToLowerInvariant()] = texture;
            }

            Vector2 bobberPos2 = __instance.bobber.Value;
            float bobberLayerDepth = bobberPos2.Y / 10000f;
            Rectangle position = Game1.getSourceRectForStandardTileSheet(texture, spriteIndex, 16, 32);
            position.Height = 16;
            position.Y += 16;
            b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, bobberPos2), position, Color.White, 0f,
                new Vector2(8f, 8f), 4f,
                __instance.getLastFarmerToUse().FacingDirection == 1
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None, bobberLayerDepth);
            position = new Rectangle(position.X, position.Y + 8, position.Width, position.Height - 8);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(FishingRod.getBobberStyle))]
    public static void FishingRod_getBobberStyle_Prefix(FishingRod __instance, Farmer? who, out bool __state)
    {
        __state = !__instance.GetTackleQualifiedItemIDs().Contains("(O)789") && who is not null &&
                   __instance.randomBobberStyle == -1 && who.usingRandomizedBobber;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(FishingRod.getBobberStyle))]
    public static void FishingRod_getBobberStyle_Postfix(FishingRod __instance, Farmer? who, ref int __result, bool __state)
    {
        if (!__state || who is null) return;

        List<int> possibleBobberStyles = new List<int>();
        for (int i = 0; i < Math.Min(39, Game1.player.fishCaught.Count() / 2); i++)
        {
            possibleBobberStyles.Add(i);
        }

        for (int i = 0; i < ModEntry.Bobbers.Count; i++)
        {
            if (ModEntry.Bobbers[i].Condition is null ||
                GameStateQuery.CheckConditions(ModEntry.Bobbers[i].Condition))
            {
                possibleBobberStyles.Add(i + 40);
            }
        }

        who.bobberStyle.Value = possibleBobberStyles[Game1.random.Next(possibleBobberStyles.Count)];
        __instance.randomBobberStyle = who.bobberStyle.Value;
        __result = who.bobberStyle.Value;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(FishingRod.getFishingLineColor))]
    public static void FishingRod_getFishingLineColor_Postfix(FishingRod __instance, ref Color __result)
    {
        var bobberIndex = __instance.getBobberStyle(__instance.getLastFarmerToUse());
        if (bobberIndex < 40) return;

        bobberIndex -= 40;
        var selectedBobber = ModEntry.Bobbers[bobberIndex];
        if (selectedBobber.FishingLineColour is null && selectedBobber.FishingLineColor is null) return;
        __result =
            Utility.StringToColor(selectedBobber.FishingLineColour ?? selectedBobber.FishingLineColor ?? "White") ??
            Color.White;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(FishingRod.flipCurrentBobberWhenFacingRight))]
    public static void FishingRod_flipCurrentBobberWhenFacingRight_Postfix(FishingRod __instance, ref bool __result)
    {
        int styleIndex = __instance.getBobberStyle(__instance.getLastFarmerToUse());
        if (styleIndex < 40) return;

        styleIndex -= 40;
        var selectedBobber = ModEntry.Bobbers[styleIndex];
        if (selectedBobber.FlipWhenFacingRight)
        {
            __result = true;
        }
        else
        {
            __result = false;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(FishingRod.tickUpdate))]
    public static IEnumerable<CodeInstruction> FishingRod_tickUpdate_Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);
    
            var jumpLabel = il.DefineLabel();

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldc_R4, 128f),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FishingRod), nameof(FishingRod.castingPower)))
            ).ThrowIfNotMatch("Could not find entry point #1 for FishingRod.tickUpdate() transpiler").Advance(1);
    
            matcher.Insert(
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(FishingRodPatches), nameof(tickUpdate_Insert))),
                new CodeInstruction(OpCodes.Br_S, jumpLabel)
            );
            
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldc_R4, 128f),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FishingRod), nameof(FishingRod.castingPower)))
            ).ThrowIfNotMatch("Could not find entry point #2 for FishingRod.tickUpdate() transpiler").Advance(1);
    
            matcher.Insert(
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(FishingRodPatches), nameof(tickUpdate_Insert))),
                new CodeInstruction(OpCodes.Br_S, jumpLabel)
            );
    
            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(TemporaryAnimatedSpriteList), nameof(TemporaryAnimatedSpriteList.Add)))
            ).Advance(1);
    
            matcher.AddLabels([jumpLabel]);
    
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error("Error in BebberBobbers.FishingRod_tickUpdate_Transpiler: \n" + ex);
            return code;
        }
    }

    public static void tickUpdate_Insert(FishingRod rod, Farmer who)
    {
        int styleIndex = rod.getBobberStyle(who);
        string textureName = "TileSheets\\bobbers";
        Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, styleIndex, 16, 32);

        if (styleIndex >= 40)
        {
            styleIndex -= 40;
            var selectedBobber = ModEntry.Bobbers[styleIndex];
            textureName = PathUtilities.NormalizeAssetName(selectedBobber.Texture ?? "");

            if (string.IsNullOrEmpty(textureName))
            {
                ModEntry.ModMonitor.LogOnce(
                    $"Bobber texture is null or empty for custom bobber with Id '{selectedBobber.Id}'", LogLevel.Error);
                return;
            }

            if (!ModEntry.BobberTextureCache.TryGetValue(textureName, out var texture))
            {
                texture = Game1.content.Load<Texture2D>(textureName);
                ModEntry.BobberTextureCache[textureName.ToLowerInvariant()] = texture;
            }

            sourceRect = Game1.getSourceRectForStandardTileSheet(texture, selectedBobber.SpriteIndex, 16, 32);
        }

        sourceRect.Height = 16;

        if (who.FacingDirection == 1 || who.FacingDirection == 3)
        {
            float distance2 = Math.Max(128f, rod.castingPower * (rod.getAddedDistance(who) + 4) * 64f);
            distance2 -= 8f;
            float gravity2 = 0.005f;
            float velocity2 = (float)(distance2 * Math.Sqrt(gravity2 / (2f * (distance2 + 96f))));
            float t2 = 2f * (velocity2 / gravity2) +
                       (float)((Math.Sqrt(velocity2 * velocity2 + 2f * gravity2 * 96f) - velocity2) /
                               gravity2);
            Point playerPixel3 = who.StandingPixel;
            if (who.IsLocalPlayer)
            {
                rod.bobber.Set(new Vector2(
                    playerPixel3.X + (who.FacingDirection != 3 ? 1 : -1) * distance2,
                    playerPixel3.Y));
            }

            rod.animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, t2, 1, 0,
                who.Position + new Vector2(0f, -96f), flicker: false, flipped: false, playerPixel3.Y / 10000f,
                0f, Color.White, 4f, 0f, 0f, Game1.random.Next(-20, 20) / 100f)
            {
                motion = new Vector2((who.FacingDirection != 3 ? 1 : -1) * velocity2, 0f - velocity2),
                acceleration = new Vector2(0f, gravity2),
                endFunction = delegate { rod.castingEndFunction(who); },
                timeBasedMotion = true,
                flipped = who.FacingDirection == 1 && rod.flipCurrentBobberWhenFacingRight()
            });
        }
        else
        {
            float distance = 0f - Math.Max(128f, rod.castingPower * (rod.getAddedDistance(who) + 3) * 64f);
            float height = Math.Abs(distance - 64f);
            if (who.FacingDirection == 0)
            {
                distance = 0f - distance;
                height += 64f;
            }

            float gravity = 0.005f;
            float velocity = (float)Math.Sqrt(2f * gravity * height);
            float t = (float)(Math.Sqrt(2f * (height - distance) / gravity) + velocity / gravity);
            t *= 1.05f;
            if (who.FacingDirection == 0)
            {
                t *= 1.05f;
            }

            if (who.IsLocalPlayer)
            {
                Point playerPixel2 = who.StandingPixel;
                rod.bobber.Set(new Vector2(playerPixel2.X, playerPixel2.Y - distance));
            }

            rod.animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, t, 1, 0,
                who.Position + new Vector2(0f, -96f), flicker: false, flipped: false, rod.bobber.Y / 10000f, 0f,
                Color.White, 4f, 0f, 0f, Game1.random.Next(-20, 20) / 100f)
            {
                alphaFade = 0.0001f,
                motion = new Vector2(0f, 0f - velocity),
                acceleration = new Vector2(0f, gravity),
                endFunction = delegate { rod.castingEndFunction(who); },
                timeBasedMotion = true
            });
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(FishingRod.DoFunction))]
    public static IEnumerable<CodeInstruction> FishingRod_DoFunction_Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            var jumpLabel = il.DefineLabel();

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_S),
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(Character), nameof(Character.StandingPixel))),
                new CodeMatch(OpCodes.Stloc_S)
            ).ThrowIfNotMatch("Could not find entry point #1 for FishingRod.DoFunction() transpiler");

            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(FishingRodPatches), nameof(DoFunction_Insert))),
                new CodeInstruction(OpCodes.Br_S, jumpLabel)
            );

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(TemporaryAnimatedSpriteList), nameof(TemporaryAnimatedSpriteList.Add)))
            ).Advance(1);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(TemporaryAnimatedSpriteList), nameof(TemporaryAnimatedSpriteList.Add)))
            ).Advance(1);

            matcher.AddLabels([jumpLabel]);

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error("Error in BebberBobbers.FishingRod_DoFunction_Transpiler: \n" + ex);
            return code;
        }
    }

    public static void DoFunction_Insert(FishingRod rod, Farmer who)
    {
        int styleIndex = rod.getBobberStyle(who);
        string textureName = "TileSheets\\bobbers";
        Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, styleIndex, 16, 32);

        if (styleIndex >= 40)
        {
            styleIndex -= 40;
            var selectedBobber = ModEntry.Bobbers[styleIndex];
            textureName = PathUtilities.NormalizeAssetName(selectedBobber.Texture ?? "");

            if (string.IsNullOrEmpty(textureName))
            {
                ModEntry.ModMonitor.LogOnce(
                    $"Bobber texture is null or empty for custom bobber with Id '{selectedBobber.Id}'", LogLevel.Error);
                return;
            }

            if (!ModEntry.BobberTextureCache.TryGetValue(textureName, out var texture))
            {
                texture = Game1.content.Load<Texture2D>(textureName);
                ModEntry.BobberTextureCache[textureName.ToLowerInvariant()] = texture;
            }

            sourceRect = Game1.getSourceRectForStandardTileSheet(texture, selectedBobber.SpriteIndex, 16, 32);
        }

        sourceRect.Height = 16;
        float gravity = 0.005f;

        if (who.FacingDirection == 1 || who.FacingDirection == 3)
        {
            float num = Math.Abs(rod.bobber.X - who.StandingPixel.X);
            float velocity = 0f - (float)Math.Sqrt(num * gravity / 2f);
            float t = 2f * (Math.Abs(velocity - 0.5f) / gravity);
            t *= 1.2f;
            sourceRect.Height = 16;
            rod.animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, t, 1, 0,
                rod.bobber.Value + new Vector2(-32f, -48f), flicker: false, flipped: false,
                who.StandingPixel.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, Game1.random.Next(-20, 20) / 100f)
            {
                motion = new Vector2((who.FacingDirection != 3 ? 1 : -1) * (velocity + 0.2f),
                    velocity - 0.8f),
                acceleration = new Vector2(0f, gravity),
                endFunction = rod.donefishingEndFunction,
                timeBasedMotion = true,
                alphaFade = 0.001f,
                flipped = who.FacingDirection == 1 && rod.flipCurrentBobberWhenFacingRight()
            });
        }
        else
        {
            float distance = rod.bobber.Y - who.StandingPixel.Y;
            float height = Math.Abs(distance + 256f);
            float velocity2 = (float)Math.Sqrt(2f * gravity * height);
            float t2 = (float)(Math.Sqrt(2f * (height - distance) / gravity) + velocity2 / gravity);
            rod.animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, t2, 1, 0,
                rod.bobber.Value + new Vector2(-32f, -48f), flicker: false, flipped: false, rod.bobber.Y / 10000f, 0f,
                Color.White, 4f, 0f, 0f, Game1.random.Next(-20, 20) / 100f)
            {
                motion = new Vector2((who.StandingPixel.X - rod.bobber.Value.X) / 800f, 0f - velocity2),
                acceleration = new Vector2(0f, gravity),
                endFunction = rod.donefishingEndFunction,
                timeBasedMotion = true,
                alphaFade = 0.001f
            });
        }
    }
}