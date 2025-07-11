using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace UniqueSmith.Patch
{
    [HarmonyPatch(typeof(BlockEntityAnvil))]
    [HarmonyPatch("OpenDialog")]
    public static class BlockEntityAnvilPatch
    {
        private static GuiDialog dlg;

        [HarmonyPrefix]
        public static bool OpenDialogPrefix(BlockEntityAnvil __instance, ItemStack ingredient)
        {
            ICoreAPI api = __instance.Api;
            BlockPos Pos = __instance.Pos;
            List<SmithingRecipe> recipes = (ingredient.Collectible as IAnvilWorkable).GetMatchingRecipes(ingredient);
            List<ItemStack> list = recipes.Select((SmithingRecipe r) => r.Output.ResolvedItemstack).ToList<ItemStack>();
            ICoreClientAPI capi = api as ICoreClientAPI;
            string charClass = capi.World.Player.Entity.WatchedAttributes.GetAsString("characterClass", null);
            

            if (charClass != null) {

                if (UniqueSmithModSystem.config.Mode == "Block")
                {
                    foreach (var blacklist in UniqueSmithModSystem.config.Blacklist)
                    {
                        if (charClass == blacklist.ClassName)
                        {
                            foreach (var item in blacklist.Itemlist)
                            {
                                list.RemoveAll(items => items?.Item?.Code?.Path != null && items.Item.Code.Path.Contains(item));
                                list.RemoveAll(items => items?.Block?.Code?.Path != null && items.Block.Code.Path.Contains(item));
                            }
                        }
                    }
                } else
                {
                    var listCopy = list.ToList();
                    foreach (var allowList in UniqueSmithModSystem.config.Allowlist)
                    {
                        if (charClass == allowList.ClassName)
                        {
                            list.Clear();
                            foreach (var item in allowList.Itemlist)
                            {
                                var matchingItems = listCopy.Where(i =>
                                    (i?.Item?.Code?.Path != null && i.Item.Code.Path.Contains(item)) ||
                                    (i?.Block?.Code?.Path != null && i.Block.Code.Path.Contains(item))
                                ).ToList();
                                list.AddRange(matchingItems);
                            }
                        }
                    }
                }
            }

            GuiDialog guiDialog = BlockEntityAnvilPatch.dlg;
            if (guiDialog != null)
            {
                guiDialog.Dispose();
            }
            BlockEntityAnvilPatch.dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select smithing recipe", Array.Empty<object>()), list.ToArray(), delegate (int selectedIndex)
            {
                SmithingRecipe smithingRecipe = recipes[selectedIndex];
                AccessTools.Field(typeof(BlockEntityAnvil), "SelectedRecipeId").SetValue(__instance, smithingRecipe.RecipeId);
                capi.Network.SendBlockEntityPacket(Pos, 1001, SerializerUtil.Serialize<int>(smithingRecipe.RecipeId));
            }, delegate
            {
                capi.Network.SendBlockEntityPacket(Pos, 1003, null);
            }, Pos, capi);
            BlockEntityAnvilPatch.dlg.TryOpen();
            return false;
        }

    }
}
