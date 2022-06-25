﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[HarmonyPatch(typeof(CostMultiple), nameof(CostMultiple.HasEnoughInInventory))]
class HasEnoughInInventoryPatch
{
    static bool Postfix(bool __result, CostMultiple __instance, Inventory inventory)
    {
        // player inventory should already have been checked
        var enoughInPlayerInventory = __result;

        if (!CraftFromStorageManager.HasUnlimitedResources() && !enoughInPlayerInventory)
        {
            // verify if current open storage has enough items andd return early
            var currentStorageInventory = InventoryManager.GetCurrentStorageInventory();

            if (currentStorageInventory != null && currentStorageInventory == inventory)
            {
                // We potentially call this patch recursively with the above check, thus we bail out.
                //Debug.Log("current storage and inventory we are checking are the same, bail out.");
                return __result;
            }

            if (currentStorageInventory != null && __instance.HasEnoughInInventory(currentStorageInventory))
            {
                //Debug.Log($"current storage has enough: {inventory.GetInstanceID()} {currentStorageInventory?.GetInstanceID()}");
                return true;
            }

            // currentstorage does not have enough items, or none
            int num = 0;
            foreach (var costMultipleItems in __instance.items)
            {
                foreach (Storage_Small storage in StorageManager.allStorages)
                {
                    Inventory container = storage.GetInventoryReference();
                    if (storage.IsOpen || container == null /*|| !Helper.LocalPlayerIsWithinDistance(storage.transform.position, player.StorageManager.maxDistanceToStorage)*/)
                        continue;

                    num += container.GetItemCountWithoutDuplicates(costMultipleItems.UniqueName);

                    if (num >= __instance.amount)
                    {
                        // bail out early so we don't check ALL storage
                        return true;
                    }
                }
            }

            return num >= __instance.amount;
        }
        return __result;
    }
}

[HarmonyPatch(typeof(BuildingUI_CostBox), nameof(BuildingUI_CostBox.SetAmountInInventory), typeof(PlayerInventory), typeof(bool))]
class SetAmountInInventoryPatch
{
    static void Postfix(BuildingUI_CostBox __instance, PlayerInventory inventory, bool includeSecondaryInventory)
    {
        var isPlayerInventory = inventory is PlayerInventory;

        if (!inventory || !isPlayerInventory)
        {
            return;
        }

        if (!CraftFromStorageManager.HasUnlimitedResources())
        {
            //Debug.Log($"BuildingUI_CostBox.SetAmountInInventory includeSecondaryInventory {includeSecondaryInventory}");
            var playerInventoryAmount = 0;

            List<Item_Base> items = CraftFromStorageManager.getItemsFromCostBox(__instance);

            //var currentStorageInventory = InventoryManager.GetCurrentStorageInventory();

            int storageInventoryAmount = 0;
            foreach (var costBoxItem in items)
            {
                playerInventoryAmount += inventory.GetItemCount(costBoxItem);
                //Debug.Log($"player {inventory.name} {playerInventoryAmount}");

                if (inventory.secondInventory != null)
                {
                    storageInventoryAmount += inventory.secondInventory.GetItemCountWithoutDuplicates(costBoxItem.UniqueName);
                    //Debug.Log($"open storage {inventory.secondInventory.name} {storageInventoryAmount}");
                }

                foreach (Storage_Small storage in StorageManager.allStorages)
                {
                    Inventory container = storage.GetInventoryReference();

                    if (storage.IsOpen || container == null /*|| !Helper.LocalPlayerIsWithinDistance(storage.transform.position, player.StorageManager.maxDistanceToStorage)*/)
                    {
                        continue;
                    }

                    if (inventory == container || inventory.secondInventory == container)
                    {
                        Debug.Log($"{container.name} being skipped, it is player inventory or secondary inventory.");
                    }

                    //var amount = container.GetItemCount(costBoxItem);
                    var amount = container.GetItemCountWithoutDuplicates(costBoxItem.UniqueName);
                    //Debug.Log($"chest storage {container.name} {amount}");
                    storageInventoryAmount += amount;
                }
            }

            __instance.SetAmount(playerInventoryAmount + storageInventoryAmount);
        }
    }
}

//// TODO: there seems to be some wrong crafting with quick crafting of bolts... needs to investigate.
///*
// * Attempting to craft 2/6 nails crashes the client to desktop, possibly because I only have 3/2 scrap?
// */

[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveCostMultiple), typeof(CostMultiple[]), typeof(bool))]
class RemoveCostMultiple
{
    static bool Prefix(CostMultiple[] costMultiple, bool manipulateCostAmount = false)
    {
        // RemoveCostMultipleIncludeSecondaryInventories calls this twice if a secondary inventory (chest) is opened.
        // So it is important to manipulate the cost amount to prevent resources getting removed multiple times
        CraftFromStorageManager.RemoveCostMultiple(costMultiple, manipulateCostAmount);
        return false; // Prevent the original RemoveCostMultiple from running.
    }
}

