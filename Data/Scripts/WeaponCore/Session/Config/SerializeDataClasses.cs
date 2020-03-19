﻿using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.ComponentModel;
using VRage;
using VRageMath;
using WeaponCore.Support;
using static WeaponCore.Platform.Weapon;
using static WeaponCore.Support.Target;

namespace WeaponCore
{

    public enum PacketType
    {
        Invalid,
        GridSyncRequestUpdate,
        CompStateUpdate,
        CompSettingsUpdate,
        WeaponSyncUpdate,
        WeaponPacket,
        FakeTargetUpdate,
        ClientMouseEvent,
        ActiveControlUpdate,
        PlayerIdUpdate,
        ActiveControlFullUpdate,
        FocusUpdate,
        MagUpdate,
        ReticleUpdate,
        OverRidesUpdate,
        PlayerControlUpdate,
        TargetExpireUpdate,
        WeaponUpdateRequest,
        ClientEntityClosed,
        RequestMouseStates,
        FullMouseUpdate,
        CompToolbarShootState,
        WeaponToolbarShootState,
        RangeUpdate,
        GridAiUiMidUpdate,
        CycleAmmo,
        ReassignTargetUpdate,
        NextActiveUpdate,
        ReleaseActiveUpdate

    }

    #region packets
    [ProtoContract]
    [ProtoInclude(4, typeof(StatePacket))]
    [ProtoInclude(5, typeof(SettingPacket))]
    [ProtoInclude(6, typeof(GridWeaponPacket))]
    [ProtoInclude(7, typeof(MouseInputPacket))]
    [ProtoInclude(8, typeof(BoolUpdatePacket))]
    [ProtoInclude(9, typeof(FakeTargetPacket))]
    [ProtoInclude(10, typeof(ControllingPacket))]
    [ProtoInclude(11, typeof(FocusPacket))]
    [ProtoInclude(12, typeof(MagUpdatePacket))]
    [ProtoInclude(13, typeof(OverRidesPacket))]
    [ProtoInclude(14, typeof(ControllingPlayerPacket))]
    [ProtoInclude(15, typeof(WeaponIdPacket))]
    [ProtoInclude(16, typeof(RequestTargetsPacket))]
    [ProtoInclude(17, typeof(MouseInputSyncPacket))]
    [ProtoInclude(18, typeof(ShootStatePacket))]
    [ProtoInclude(19, typeof(RangePacket))]
    [ProtoInclude(20, typeof(CycleAmmoPacket))]
    public class Packet
    {
        [ProtoMember(1)] internal long EntityId;
        [ProtoMember(2)] internal ulong SenderId;
        [ProtoMember(3)] internal PacketType PType;

        public virtual void CleanUp()
        {
            EntityId = 0;
            SenderId = 0;
            PType = PacketType.Invalid;
        }

        //can override in other packet
        protected bool Equals(Packet other)
        {
            return (EntityId.Equals(other.EntityId) && SenderId.Equals(other.SenderId) && PType.Equals(other.PType));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Packet)obj);
        }

        public override int GetHashCode()
        {
            return (EntityId.GetHashCode() + PType.GetHashCode() + SenderId.GetHashCode());
        }
    }

    [ProtoContract]
    public class StatePacket : Packet
    {
        [ProtoMember(1)] internal CompStateValues Data;

        public StatePacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class SettingPacket : Packet
    {
        [ProtoMember(1)] internal CompSettingsValues Data;
        public SettingPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class GridWeaponPacket : Packet
    {
        [ProtoMember(1)] internal List<WeaponData> Data;
        public GridWeaponPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class FakeTargetPacket : Packet
    {
        [ProtoMember(1)] internal Vector3 Data;
        public FakeTargetPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = new Vector3();
        }
    }


    [ProtoContract]
    public class MouseInputPacket : Packet
    {
        [ProtoMember(1)] internal MouseStateData Data;
        public MouseInputPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class ControllingPacket : Packet
    {
        [ProtoMember(1)] internal ControllingPlayersSync Data;
        public ControllingPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = new ControllingPlayersSync();
        }
    }

    [ProtoContract]
    public class MagUpdatePacket : Packet
    {
        [ProtoMember(1)] internal MyFixedPoint Mags;
        [ProtoMember(2), DefaultValue(-1)] internal int WeaponId;
        public MagUpdatePacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Mags = 0;
            WeaponId = -1;
        }
    }

    [ProtoContract]
    public class BoolUpdatePacket : Packet
    {
        [ProtoMember(1)] internal bool Data;
        public BoolUpdatePacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = false;
        }
    }

    [ProtoContract]
    public class FocusPacket : Packet
    {
        [ProtoMember(1)] internal long TargetId;
        [ProtoMember(2), DefaultValue(-1)] internal int FocusId;
        [ProtoMember(3)] internal bool AddSecondary;
        public FocusPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            TargetId = 0;
            FocusId = -1;
            AddSecondary = false;
        }
    }

    [ProtoContract]
    public class OverRidesPacket : Packet
    {
        [ProtoMember(1)] internal GroupOverrides Data;
        [ProtoMember(2), DefaultValue("")] internal string GroupName = "";
        [ProtoMember(3)] internal uint MId;

        public OverRidesPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
            GroupName = "";
            MId = 0;
        }
    }

    [ProtoContract]
    public class ControllingPlayerPacket : Packet
    {
        [ProtoMember(1)] internal PlayerControl Data;
        [ProtoMember(2)] internal uint MId;

        public ControllingPlayerPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = null;
        }
    }

    [ProtoContract]
    public class WeaponIdPacket : Packet
    {
        [ProtoMember(1), DefaultValue(-1)] internal int WeaponId = -1;

        public WeaponIdPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            WeaponId = -1;
        }
    }

    [ProtoContract]
    public class MIdPacket : Packet
    {
        [ProtoMember(1)] internal uint Id;

        public MIdPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Id = 0;
        }
    }

    [ProtoContract]
    public class RequestTargetsPacket : Packet
    {
        [ProtoMember(1)] internal long[] Comps;

        public RequestTargetsPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Comps = new long[0];
        }
    }

    [ProtoContract]
    public class MouseInputSyncPacket : Packet
    {
        [ProtoMember(1)] internal PlayerMouseData[] Data = new PlayerMouseData[0];
        public MouseInputSyncPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = new PlayerMouseData[0];
        }
    }

    [ProtoContract]
    public class ShootStatePacket : Packet
    {
        [ProtoMember(1)] internal TerminalActionState Data = TerminalActionState.ShootOff;
        [ProtoMember(2)] internal uint MId;
        [ProtoMember(3), DefaultValue(-1)] internal int WeaponId = -1;
        public ShootStatePacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = TerminalActionState.ShootOff;
            MId = 0;
            WeaponId = -1;
        }
    }

    [ProtoContract]
    public class RangePacket : Packet
    {
        [ProtoMember(1)] internal float Data;
        [ProtoMember(2)] internal uint MId;
        public RangePacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            Data = 0f;
            MId = 0;
        }
    }

    [ProtoContract]
    public class CycleAmmoPacket : Packet
    {
        [ProtoMember(1)] internal uint MId;
        [ProtoMember(2), DefaultValue(-1)] internal int AmmoId = -1;
        [ProtoMember(3), DefaultValue(-1)] internal int WeaponId = -1;
        public CycleAmmoPacket() { }

        public override void CleanUp()
        {
            base.CleanUp();
            MId = 0;
            AmmoId = -1;
            WeaponId = -1;
        }
    }
    #endregion

    #region packet Data

    [ProtoContract]
    public class WeaponData
    {
        [ProtoMember(1)] internal TransferTarget TargetData;
        [ProtoMember(2)] internal long CompEntityId;
        [ProtoMember(3)] internal WeaponSyncValues SyncData;
        [ProtoMember(4)] internal WeaponTimings Timmings;

        public WeaponData() { }
    }

    [ProtoContract]
    internal class MouseStateData
    {
        [ProtoMember(1)] internal bool MouseButtonLeft;
        [ProtoMember(2)] internal bool MouseButtonMiddle;
        [ProtoMember(3)] internal bool MouseButtonRight;
    }

    [ProtoContract]
    internal class PlayerMouseData
    {
        [ProtoMember(1)] internal long PlayerId;
        [ProtoMember(2)] internal MouseStateData MouseStateData;
    }

    [ProtoContract]
    internal class GroupSettingsData
    {
        [ProtoMember(1)] internal string SettingName;
        [ProtoMember(2)] internal bool Value;
    }

    [ProtoContract]
    public class TransferTarget
    {
        [ProtoMember(1)] internal long EntityId;
        [ProtoMember(2)] internal Vector3 TargetPos;
        [ProtoMember(3)] internal float HitShortDist;
        [ProtoMember(4)] internal float OrigDistance;
        [ProtoMember(5)] internal long TopEntityId;
        [ProtoMember(6)] internal TargetInfo State = TargetInfo.Expired;
        [ProtoMember(7)] internal int WeaponId;

        public enum TargetInfo
        {
            IsEntity,
            IsProjectile,
            IsFakeTarget,
            Expired
        }

        internal void SyncTarget(Target target, bool allowChange = true)
        {
            var entity = MyEntities.GetEntityByIdOrDefault(EntityId);
            target.Entity = entity;
            target.TargetPos = TargetPos;
            target.HitShortDist = HitShortDist;
            target.OrigDistance = OrigDistance;
            target.TopEntityId = TopEntityId;

            target.IsProjectile = false;
            target.IsFakeTarget = false;

            if (State == TargetInfo.IsProjectile)
                target.IsProjectile = true;

            else if (State == TargetInfo.IsFakeTarget)
                target.IsFakeTarget = true;

            var state = State != TargetInfo.Expired ? States.Acquired : States.Expired;

            
            target.StateChange(State != TargetInfo.Expired, state);

            if (!allowChange)
                target.TargetChanged = false;
        }

        public TransferTarget()
        {
        }
    }
    #endregion
}
