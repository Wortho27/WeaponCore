﻿using SpaceEngineers.Game.ModAPI;
using WeaponCore.Support;

namespace WeaponCore
{
    using System;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class LogicState
    {
        internal LogicStateValues Value = new LogicStateValues();
        internal readonly WeaponComponent Comp;
        internal readonly IMyLargeMissileTurret Turret;
        internal LogicState(WeaponComponent comp)
        {
            Comp = comp;
            Turret = comp.Turret;
            Value.Weapons = new WeaponStateValues[Comp.Platform.Weapons.Length];
            for (int i = 0; i < Comp.Platform.Weapons.Length; i++)
                if (Value.Weapons[i] == null) Value.Weapons[i] = new WeaponStateValues();
        }

        internal void StorageInit()
        {
            if (Turret.Storage == null)
            {
                Turret.Storage = new MyModStorageComponent {[Session.Instance.LogicSettingsGuid] = ""};
            }
        }

        internal void SaveState(bool createStorage = false)
        {
            if (Turret.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Value);
            Turret.Storage[Session.Instance.LogicStateGuid] = Convert.ToBase64String(binary);
        }

        internal bool LoadState()
        {
            if (Turret.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Turret.Storage.TryGetValue(Session.Instance.LogicStateGuid, out rawData))
            {
                LogicStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<LogicStateValues>(base64);

                if (loadedState != null)
                {
                    Value = loadedState;
                    loadedSomething = true;
                }
            }
            return loadedSomething;
        }

        #region Network
        internal void NetworkUpdate()
        {

            if (Session.Instance.IsServer)
            {
                Value.MId++;
                Session.Instance.PacketizeToClientsInRange(Turret, new DataLogicState(Turret.EntityId, Value)); // update clients with server's state
            }
        }
        #endregion
    }

    internal class LogicSettings
    {
        internal LogicSettingsValues Value = new LogicSettingsValues();
        internal readonly WeaponComponent Comp;
        internal readonly IMyLargeMissileTurret Turret;
        internal LogicSettings(WeaponComponent comp)
        {
            Comp = comp;
            Turret = comp.Turret;
            Value.Weapons = new WeaponSettingsValues[Comp.Platform.Weapons.Length];
            for (int i = 0; i < Comp.Platform.Weapons.Length; i++)
                if (Value.Weapons[i] == null) Value.Weapons[i] = new WeaponSettingsValues();
        }

        internal void SaveSettings(bool createStorage = false)
        {
            if (Turret.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Value);
            Turret.Storage[Session.Instance.LogicSettingsGuid] = Convert.ToBase64String(binary);
        }

        internal bool LoadSettings()
        {
            if (Turret.Storage == null) return false;
            string rawData;
            bool loadedSomething = false;

            if (Turret.Storage.TryGetValue(Session.Instance.LogicSettingsGuid, out rawData))
            {
                LogicSettingsValues loadedSettings = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<LogicSettingsValues>(base64);

                if (loadedSettings != null && loadedSettings.Weapons != null)
                {
                    Value = loadedSettings;
                    loadedSomething = true;
                }
                //if (Session.Enforced.Debug == 3) Log.Line($"Loaded -LogicId [{Logic.EntityId}]:\n{Value.ToString()}");
            }
            return loadedSomething;
        }

        #region Network
        internal void NetworkUpdate()
        {
            Value.MId++;
            if (Session.Instance.IsServer)
            {
                Session.Instance.PacketizeToClientsInRange(Turret, new DataLogicSettings(Turret.EntityId, Value)); // update clients with server's settings
            }
            else // client, send settings to server
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataLogicSettings(Turret.EntityId, Value));
                MyAPIGateway.Multiplayer.SendMessageToServer(Session.PACKET_ID, bytes);
            }
        }
        #endregion
    }
}