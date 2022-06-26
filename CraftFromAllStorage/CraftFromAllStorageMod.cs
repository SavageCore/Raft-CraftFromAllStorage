﻿using HarmonyLib;
using System.Reflection;
using UnityEngine;

/*
 * Thanks
 *  Azzmurr - Creating the original Craft From Storage mod that this mod is derived from.
 *  Aidanamite - for all their efforts and help on the mod discord, making this mod a reality.
 *  Fynikoto - For debugging MoreStorages duplicate issues, feature suggestions and guidance.
 *  DeadByte42 - For the inital Raft v1 PR.
 */
public class CraftFromAllStorageMod : Mod
{
    public static string ModNamePrefix = "<color=#d16e17>[Craft From All Storage]</color>";
    private const string harmonyId = "com.thmsn.craft-from-all-storage";
    Harmony harmony;

    public void Start()
    {
        harmony = new Harmony(harmonyId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Debug.Log(ModNamePrefix + " has been loaded!");

    }

    public void OnModUnload()
    {
        Debug.Log(ModNamePrefix + " has been unloaded!");
        harmony.UnpatchAll(harmonyId);
    }

    public void FixedUpdate()
    {
        NetworkMessage netMessage = RAPI.ListenForNetworkMessagesOnChannel(Storage_SmallPatchOnIsRayed.CHANNEL_ID);
        if (netMessage != null)
        {
            Message message = netMessage.message;
            if (message.Type == Storage_SmallPatchOnIsRayed.MESSAGE_TYPE)
            {
                var msg = message as Message_Storage_Small_AdditionalData;
                var storage = NetworkIDManager.GetNetworkIDFromObjectIndex<Storage_Small>(msg.NetworkID_ObjectIndex);
                var data = storage.GetAdditionalData();
                // TODO: notification of toggled state
                data.SetData(msg.data);
                if (data.excludeFromCraftFromAllStorage)
                {
                    Debug.Log("A storage is now excluded from Craft From All Storage");
                }
                else
                {
                    Debug.Log("A storage is now included in Craft From All Storage");
                }
            }
        }
    }
}
