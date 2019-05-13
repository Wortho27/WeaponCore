﻿using System;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using WeaponCore.Support;

namespace WeaponCore.Platform
{
    public partial class Weapon
    {
        public Weapon(IMyEntity entity, WeaponSystem weaponSystem)
        {
            EntityPart = entity;
            _localTranslation = entity.LocalMatrix.Translation;
            _pivotOffsetVec =
                (Vector3.Transform(entity.PositionComp.LocalAABB.Center, entity.PositionComp.LocalMatrix) -
                 entity.GetTopMostParent(typeof(MyCubeBlock)).PositionComp.LocalAABB.Center);
            _upPivotOffsetLen = _pivotOffsetVec.Length();

            WeaponSystem = weaponSystem;
            WeaponType = weaponSystem.WeaponType;
            TurretMode = WeaponType.TurretMode;
            TrackTarget = WeaponType.TrackTarget;
            _ticksPerShot = (uint) (3600 / WeaponType.RateOfFire);
            _timePerShot = (3600d / WeaponType.RateOfFire);
            _numOfBarrels = WeaponSystem.Barrels.Length;

            BeamSlot = new uint[_numOfBarrels];
        }

        public IMyEntity EntityPart;
        public WeaponSystem WeaponSystem;
        public WeaponDefinition WeaponType;
        public Dummy[] Dummies;
        public Muzzle[] Muzzles;
        public Logic Logic;
        public uint[] BeamSlot { get; set; }
        public MyEntity Target { get; set; }
        public Random Rnd = new Random(902138212);
        private readonly Vector3 _localTranslation;
        private readonly float _upPivotOffsetLen;

        private MatrixD _weaponMatrix;
        private MatrixD _oldWeaponMatrix;
        private Vector3D _weaponPosition;
        private Vector3D _oldWeaponPosition;
        private Vector3 _pivotOffsetVec;

        private int _rotationTime;
        private int _numOfBarrels;
        private int _shotsInCycle;
        private int _nextMuzzle;
        private uint _posUpdatedTick = uint.MinValue;
        private uint _posChangedTick = 1;
        private uint _targetTick;
        private uint _ticksPerShot;
        private uint _shotCounter;
        private double _timePerShot;
        private double _step = 0.05d;
        private double _azimuth;
        private double _elevation;
        private double _desiredAzimuth;
        private double _desiredElevation;

        private bool _newCycle = false;
        private bool _firstRun = true;
        private bool _weaponReady = true;
        private bool _azOk;
        private bool _elOk;

        internal bool TurretMode { get; set; }
        internal bool TrackTarget { get; set; }
        internal bool ReadyToTrack => Target != null && Logic.Turret.Target != Target && _azOk && _elOk;
        internal bool ReadyToShoot => _weaponReady && Target != null && Logic.Turret.Target == Target;
        internal bool TargetSwap => (Target != null || !Logic.Turret.HasTarget) && _targetTick++ > 60 || _firstRun;

        public void PositionChanged(MyPositionComponentBase pComp)
        {
            _posChangedTick = Session.Instance.Tick;
        }

        public class Muzzle
        {
            public Vector3D Position;
            public Vector3D Direction;
            public uint LastShot;
            public uint LastPosUpdate;
        }
    }

    public class MyWeaponPlatform
    {
        public readonly Weapon[] Weapons;
        public readonly RecursiveSubparts SubParts = new RecursiveSubparts();
        public readonly WeaponStructure Structure;

        public MyWeaponPlatform(MyStringHash subTypeIdHash, IMyEntity entity, Logic logic)
        {
            Structure = Session.Instance.WeaponStructure[subTypeIdHash];
            //PartNames = Structure.PartNames;
            var subPartCount = Structure.PartNames.Length;

            Weapons = new Weapon[subPartCount];

            SubParts.Entity = entity;
            SubParts.CheckSubparts();
            for (int i = 0; i < subPartCount; i++)
            {
                var barrelCount = Structure.WeaponSystems[Structure.PartNames[i]].Barrels.Length;
                IMyEntity subPartEntity;
                SubParts.NameToEntity.TryGetValue(Structure.PartNames[i].String, out subPartEntity);
                Weapons[i] = new Weapon(subPartEntity, Structure.WeaponSystems[Structure.PartNames[i]])
                {
                    Muzzles = new Weapon.Muzzle[barrelCount],
                    Dummies = new Dummy[barrelCount],
                    Logic = logic,
                };
            }

            CompileTurret();
        }

        private void CompileTurret()
        {
            var c = 0;
            foreach (var m in Structure.WeaponSystems)
            {
                var subPart = SubParts.NameToEntity[m.Key.String];
                var barrelCount = m.Value.Barrels.Length;
                Weapons[c].EntityPart.PositionComp.OnPositionChanged += Weapons[c].PositionChanged;
                for (int i = 0; i < barrelCount; i++)
                {
                    var barrel = m.Value.Barrels[i];
                    Weapons[c].Dummies[i] = new Dummy(subPart, barrel);
                    Weapons[c].Muzzles[i] = new Weapon.Muzzle();
                }
                c++;
            }
        }
    }
}