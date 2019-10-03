﻿using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using WeaponCore.Platform;

namespace WeaponCore.Support
{
    public partial class WeaponComponent
    {
        internal readonly MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;
        private int _count = -1;

        private bool _allInited;
        private bool _clientOn;
        private bool _isServer;
        private bool _isDedicated;
        private bool _mpActive;
        private bool _clientNotReady;
        private bool _firstRun = true;
        private bool _firstLoop = true;
        private bool _readyToSync;
        private bool _firstSync;

        private DSUtils Dsutil1 { get; set; } = new DSUtils();
        private HashSet<string> UIControls = new HashSet<string>(); 
        internal GridAi Ai { get; set; }
        internal MySoundPair RotationSound;
        internal MyEntity3DSoundEmitter RotationEmitter; 

        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == Turret.EntityId;

        internal MyFixedPoint MaxInventoryVolume;
        internal MyFixedPoint MaxInventoryMass;
        internal uint LastAmmoUnSuspendTick;
        internal uint LastTrackedTick;
        internal uint LastRayCastTick;
        internal uint LastUpdateTick;
        internal uint lastInventoryChangedTick;
        internal uint LastPivotUpdateTick;
        internal uint ShootTick = 0;
        internal uint DelayTicks = 0;
        internal uint IsWorkingChangedTick;
        internal uint PositionUpdateTick;
        internal int PullingAmmoCnt;
        internal float OptimalDPS;
        internal float CurrentDPS;
        internal float CurrentHeat;
        internal float MaxHeat;
        internal float HeatPerSecond;
        internal float HeatSinkRate;
        internal float MaxAmmoVolume;
        internal float MaxAmmoMass;
        internal float SinkPower;
        internal float MaxRequiredPower;
        internal float CurrentSinkPowerRequested;
        internal float CompPowerPerc;
        internal float IdlePower;
        internal bool Overheated;
        internal bool TurretTargetLock;
        internal bool Gunner;
        internal bool NotFailed;
        internal bool WarmedUp;
        internal bool Starting;
        internal bool Sync = true;
        internal int Shooting;
        internal bool Charging;
        internal bool ReturnHome;
        internal bool Debug;
        internal bool Controlling;
        internal Start Status;
        internal enum Start
        {
            Started,
            Starting,
            Stopped,
            ReInit,
            WarmingUp,
        }

        internal MyCubeBlock MyCube;
        internal MyCubeGrid MyGrid;
        internal MyPhysicsComponentBase Physics;
        internal MyWeaponPlatform Platform;
        internal MyObjectBuilder_UpgradeModule Ob;
        internal IMyUpgradeModule Turret;
        internal Weapon TrackingWeapon;
        internal MyInventory BlockInventory;
        internal Vector3D MyPivotPos;
        internal Vector3D MyPivotDir;
        internal Vector3D MyPivotUp;
        internal Vector3D AimOffset;
        internal LineD MyCenterTestLine;
        internal LineD MyBarrelTestLine;
        internal LineD MyPivotTestLine;
        internal double MyPivotOffset;
        //internal IMyGunObject<MyGunBase> Gun;
        internal bool MainInit;
        internal bool SettingsUpdated;
        internal bool ClientUiUpdate;
        internal bool IsFunctional;
        internal bool IsWorking;
        internal bool FullInventory;
        internal bool MultiInventory;
        internal bool AiMoving;
        internal bool HasEnergyWeapon;
        internal bool FixedOffset;
        internal LogicSettings Set;
        internal LogicState State;
        //internal MyResourceSinkComponent Sink => MyCube.ResourceSink;
        internal MyResourceSinkComponent Sink;
        public WeaponComponent(GridAi ai, MyCubeBlock myCube, IMyUpgradeModule turret)
        {
            if (myCube == null)
                Log.Line("Cube null");

            if (ai == null)
                Log.Line("ai null");

            Ai = ai;
            MyCube = myCube;

            MyGrid = MyCube.CubeGrid;
            Turret = turret;
            //Gun = (IMyGunObject<MyGunBase>)MyCube;          

            BlockInventory = new MyInventory(0.384f,Vector3.Zero, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);

            MyCube.Components.Add(BlockInventory);

            if (BlockInventory == null)
                Log.Line("Inventory null");

            //BlockInventory.Constraint.m_useDefaultIcon = false;
            MaxInventoryVolume = BlockInventory.MaxVolume;
            MaxInventoryMass = BlockInventory.MaxMass;
            PowerInit();

            //IdlePower = Turret.ResourceSink.RequiredInputByType(GId);
            SinkPower = IdlePower;
            
            Ob = (MyObjectBuilder_UpgradeModule)myCube.GetObjectBuilderCubeBlock();
        }        
    }
}
