﻿using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using WeaponCore.Platform;

namespace WeaponCore.Support
{
    public partial class WeaponComponent
    {
        internal volatile bool InventoryInited;
        internal volatile BlockType BaseType;

        internal readonly MyCubeBlock MyCube;
        internal readonly IMySlimBlock Slim;
        internal readonly MyStringHash SubtypeHash;
        internal readonly List<PartAnimation> AllAnimations = new List<PartAnimation>();

        internal readonly Session Session;
        internal readonly MyInventory BlockInventory;
        internal readonly IMyTerminalBlock TerminalBlock;
        internal readonly IMyFunctionalBlock FunctionalBlock;
        internal readonly IMyLargeTurretBase TurretBase;
        internal readonly CompSettings Set;
        internal readonly CompState State;

        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;

        internal MatrixD CubeMatrix;
        internal GridAi Ai;
        internal Weapon TrackingWeapon;
        internal MyWeaponPlatform Platform;
        internal WeaponValues WeaponValues = new WeaponValues();
        internal CompMids SyncIds = new CompMids();

        internal uint LastRayCastTick;
        internal uint LastInventoryChangedTick;
        internal uint IsWorkingChangedTick;
        //internal float MaxInventoryVolume;
        internal float EffectiveDps;
        internal float PeakDps;
        internal float ShotsPerSec;
        internal float BaseDps;
        internal float AreaDps;
        internal float DetDps;
        internal float CurrentDps;
        internal float CurrentHeat;
        internal float MaxHeat;
        internal float HeatPerSecond;
        internal float HeatSinkRate;
        internal float SinkPower;
        internal float MaxRequiredPower;
        internal float IdlePower = 0.001f;
        internal float MaxIntegrity;
        internal bool SettingsUpdated;
        internal bool ClientUiUpdate;
        internal bool IsFunctional;
        internal bool IsWorking;
        internal bool HasEnergyWeapon;
        internal bool IgnoreInvChange;
        internal bool HasGuidanceToggle;
        internal bool HasDamageSlider;
        internal bool HasRofSlider;
        internal bool CanOverload;
        internal bool HasTurret;
        internal bool HasChargeWeapon;
        internal bool WasControlled;
        internal bool TrackReticle;
        internal bool WasTrackReticle;
        //internal bool OtherPlayerTrackingReticle;
        internal bool UserControlled;
        internal bool Debug;
        internal bool UnlimitedPower;
        internal bool Registered;
        internal bool ResettingSubparts;

        internal MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;

        internal Start Status;

        internal enum Start
        {
            Started,
            Starting,
            Stopped,
            ReInit,
        }

        internal enum BlockType
        {
            Turret,
            Fixed,
            Sorter
        }

        public WeaponComponent(Session session, GridAi ai, MyCubeBlock myCube, MyStringHash subtype)
        {
            Ai = ai;
            Session = session;
            MyCube = myCube;
            Slim = myCube.SlimBlock;
            SubtypeHash = subtype;

            MaxIntegrity = Slim.MaxIntegrity;

            var turret = MyCube as IMyLargeTurretBase;
            if (turret != null)
            {
                TurretBase = turret;
                TurretBase.EnableIdleRotation = false;
                BaseType = BlockType.Turret;
            }
            else if (MyCube is IMyConveyorSorter)
                BaseType = BlockType.Sorter;
            else
                BaseType = BlockType.Fixed;

            TerminalBlock = myCube as IMyTerminalBlock;
            FunctionalBlock = myCube as IMyFunctionalBlock;
            
            BlockInventory = (MyInventory)MyCube.GetInventoryBase();
            SinkPower = IdlePower;
            Platform = session.PlatFormPool.Get();
            Platform.Setup(this);

            State = new CompState(this);
            Set = new CompSettings(this);

            MyCube.OnClose += Session.CloseComps;
        }        
    }
}
