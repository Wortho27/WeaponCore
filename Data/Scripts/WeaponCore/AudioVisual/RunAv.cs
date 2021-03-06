﻿using System.Collections.Generic;
using VRage.Collections;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using WeaponCore.Platform;
using WeaponCore.Support;

namespace WeaponCore.Support
{
    class RunAv
    {
        internal readonly MyConcurrentPool<AvShot> AvShotPool = new MyConcurrentPool<AvShot>(128, shot => shot.Close());
        internal readonly MyConcurrentPool<KeensMess> KeenMessPool = new MyConcurrentPool<KeensMess>(128, mess => mess.Clean());

        internal readonly List<AvBarrel> AvBarrels1 = new List<AvBarrel>(128);
        internal readonly List<AvBarrel> AvBarrels2 = new List<AvBarrel>(128);
        internal readonly List<KeensMess> KeensBrokenParticles = new List<KeensMess>();
        internal readonly Dictionary<MyParticleEffect, KeensMess> RipMap = new Dictionary<MyParticleEffect, KeensMess>();

        internal readonly List<AvShot> AvShots = new List<AvShot>(128);
        internal readonly List<AvShot> HitSounds = new List<AvShot>(128);
        internal readonly Stack<AfterGlow> Glows = new Stack<AfterGlow>();

        internal Session Session;

        internal int ExplosionCounter;
        internal int MaxExplosions = 20;

        internal bool ExplosionReady
        {
            get {
                if (ExplosionCounter + 1 <= MaxExplosions)
                {
                    ExplosionCounter++;
                    return true;
                }
                return false;
            }
        }

        internal RunAv(Session session)
        {
            Session = session;
        }

        internal void RipParticles()
        {
            for (int i = KeensBrokenParticles.Count - 1; i >= 0; i--)
            {
                var rip = KeensBrokenParticles[i];
                var effect = rip.Effect;
                if (effect.IsEmittingStopped || effect.IsStopped || !effect.Enabled || effect.GetElapsedTime() >= effect.DurationMax)
                {
                    KeensBrokenParticles.RemoveAtFast(i);
                    RipMap.Remove(rip.Effect);
                    KeenMessPool.Return(rip);
                }
                else if (Session.Tick != rip.LastTick)
                {
                    var velSimulation = effect.WorldMatrix.Translation + (rip.Velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS);
                    effect.SetTranslation(ref velSimulation);
                }
            }
        }

        private int _onScreens = 0;
        private int _shrinks = 0;
        private int _glows = 0;
        private int _models = 0;

        internal void End()
        {
            if (KeensBrokenParticles.Count > 0) RipParticles();
            if (AvBarrels1.Count > 0) RunAvBarrels1();
            if (AvBarrels2.Count > 0) RunAvBarrels2();
            if (HitSounds.Count > 0) RunHitSounds();

            for (int i = AvShots.Count - 1; i >= 0; i--)
            {
                var av = AvShots[i];
                var refreshed = av.LastTick == Session.Tick;
                var shrinkCnt = av.TracerShrinks.Count;
                var glowCnt = av.GlowSteps.Count;
                var noNextStep = glowCnt == 0 && shrinkCnt == 0 && av.Dirty;

                if (refreshed)
                {
                    if (av.PrimeEntity != null)
                    {
                        _models++;
                        if (av.OnScreen != AvShot.Screen.None)
                        {
                            if (!av.PrimeEntity.InScene && !av.Cloaked)
                            {
                                av.PrimeEntity.InScene = true;
                                av.PrimeEntity.Render.UpdateRenderObject(true, false);
                            }
                            av.PrimeEntity.PositionComp.SetWorldMatrix(ref av.PrimeMatrix, null, false, false, false);
                        }

                        if ((av.Cloaked || av.OnScreen == AvShot.Screen.None) && av.PrimeEntity.InScene)
                        {
                            av.PrimeEntity.InScene = false;
                            av.PrimeEntity.Render.RemoveRenderObjects();
                        }
                    }
                    if (av.Triggered && av.TriggerEntity != null)
                    {
                        if ((!av.TriggerEntity.InScene))
                        {
                            av.TriggerEntity.InScene = true;
                            av.TriggerEntity.Render.UpdateRenderObject(true, false);
                        }
                        av.TriggerEntity.PositionComp.SetWorldMatrix(ref av.TriggerMatrix, null, false, false, false);
                    }

                    if (av.HasTravelSound)
                    {

                        if (!av.AmmoSound)
                        {
                            double distSqr;
                            Vector3D.DistanceSquared(ref av.TracerFront, ref Session.CameraPos, out distSqr);
                            if (distSqr <= av.AmmoDef.Const.AmmoTravelSoundDistSqr)
                                av.AmmoSoundStart();
                        }
                        else av.TravelEmitter.SetPosition(av.TracerFront);
                    }

                    if (av.HitParticle == AvShot.ParticleState.Custom) 
                    {

                        av.HitParticle = AvShot.ParticleState.Dirty;
                        if (av.OnScreen != AvShot.Screen.None) {
                            var pos = av.Hit.HitTick == Session.Tick && !MyUtils.IsZero(av.Hit.HitPos) ? av.Hit.HitPos : av.TracerFront;
                            var matrix = MatrixD.CreateTranslation(pos);

                            MyParticleEffect hitEffect;
                            if (MyParticlesManager.TryCreateParticleEffect(av.AmmoDef.AmmoGraphics.Particles.Hit.Name, ref matrix, ref pos, uint.MaxValue, out hitEffect)) {

                                hitEffect.UserColorMultiplier = av.AmmoDef.AmmoGraphics.Particles.Hit.Color;
                                var scaler = 1;
                                hitEffect.UserRadiusMultiplier = av.AmmoDef.AmmoGraphics.Particles.Hit.Extras.Scale * scaler;
                                var scale = av.AmmoDef.Const.HitParticleShrinks ? MathHelper.Clamp(MathHelper.Lerp(1, 0, av.DistanceToLine / av.AmmoDef.AmmoGraphics.Particles.Hit.Extras.MaxDistance), 0.05f, 1) : 1;

                                hitEffect.UserScale = scale * scaler;
                                if (!MyUtils.IsZero(av.Hit.HitVelocity, 1E-01F))
                                {
                                    var hitVel = av.Hit.HitVelocity;
                                    Vector3D.ClampToSphere(ref hitVel, (float)av.MaxSpeed);
                                    KeensBrokenParticles.Add(new KeensMess { Effect = hitEffect, AmmoDef = av.AmmoDef, Velocity = hitVel, LastTick = Session.Tick });
                                }
                            }
                        }
                    }
                    else if (av.HitParticle == AvShot.ParticleState.Explosion)
                    {
                        av.HitParticle = AvShot.ParticleState.Dirty;
                        if (ExplosionReady && av.OnScreen != AvShot.Screen.None)
                        {
                            var pos = !MyUtils.IsZero(av.Hit.HitPos) ? av.Hit.HitPos : av.TracerFront;
                            if (av.DetonateFakeExp) SUtils.CreateFakeExplosion(Session, av.AmmoDef.AreaEffect.Detonation.DetonationRadius, pos, av.AmmoDef);
                            else SUtils.CreateFakeExplosion(Session, av.AmmoDef.AreaEffect.AreaEffectRadius, pos, av.AmmoDef);
                        }
                    }
                }

                if (noNextStep)
                {
                    AvShotPool.Return(av);
                    AvShots.RemoveAtFast(i);
                }
            }
        }


        internal void Run()
        {
            if (Session.Tick180) {

                Log.LineShortDate($"(DRAWS) --------------- AvShots:[{AvShots.Count}] OnScreen:[{_onScreens}] Shrinks:[{_shrinks}] Glows:[{_glows}] Models:[{_models}] P:[{Session.Projectiles.ActiveProjetiles.Count}] P-Pool:[{Session.Projectiles.ProjectilePool.Count}] AvPool:[{AvShotPool.Count}] (AvBarrels 1:[{AvBarrels1.Count}] 2:[{AvBarrels2.Count}])");
                _glows = 0;
                _shrinks = 0;
            }

            _onScreens = 0;
            _models = 0;
            for (int i = AvShots.Count - 1; i >= 0; i--)
            {
                var av = AvShots[i];
                if (av.OnScreen != AvShot.Screen.None) _onScreens++;
                var refreshed = av.LastTick == Session.Tick;

                if (refreshed && av.Tracer != AvShot.TracerState.Off && av.OnScreen != AvShot.Screen.None)
                {
                    var color = av.Color;
                    if (av.ShotFade > 0)
                        color *= MathHelper.Clamp(1f - av.ShotFade, 0.005f, 1f);

                    if (!av.AmmoDef.Const.OffsetEffect)
                    {
                        if (av.Tracer != AvShot.TracerState.Shrink)
                            MyTransparentGeometry.AddLineBillboard(av.AmmoDef.Const.TracerMaterial, color, av.TracerBack, av.PointDir, (float)av.VisualLength, (float)av.TracerWidth);
                    }
                    else
                    {
                        var list = av.Offsets;
                        for (int x = 0; x < list.Count; x++)
                        {
                            Vector3D fromBeam;
                            Vector3D toBeam;

                            if (x == 0)
                            {
                                fromBeam = av.OffsetMatrix.Translation;
                                toBeam = Vector3D.Transform(list[x], av.OffsetMatrix);
                            }
                            else
                            {
                                fromBeam = Vector3D.Transform(list[x - 1], av.OffsetMatrix);
                                toBeam = Vector3D.Transform(list[x], av.OffsetMatrix);
                            }

                            Vector3 dir = (toBeam - fromBeam);
                            var length = dir.Length();
                            var normDir = dir / length;
                            MyTransparentGeometry.AddLineBillboard(av.AmmoDef.Const.TracerMaterial, color, fromBeam, normDir, length, (float)av.TracerWidth);

                            if (Vector3D.DistanceSquared(av.OffsetMatrix.Translation, toBeam) > av.TracerLengthSqr) break;
                        }
                        list.Clear();
                    }
                }

                var shrinkCnt = av.TracerShrinks.Count;
                if (shrinkCnt > _shrinks) _shrinks = shrinkCnt;

                if (shrinkCnt > 0)
                    RunShrinks(av);

                var glowCnt = av.GlowSteps.Count;

                if (glowCnt > _glows)
                    _glows = glowCnt;

                if (av.Trail != AvShot.TrailState.Off)
                {
                    var steps = av.AmmoDef.AmmoGraphics.Lines.Trail.DecayTime;
                    var widthScaler = !av.AmmoDef.AmmoGraphics.Lines.Trail.UseColorFade;
                    var remove = false;
                    for (int j = glowCnt - 1; j >= 0; j--)
                    {
                        var glow = av.GlowSteps[j];

                        if (!refreshed)
                            glow.Line = new LineD(glow.Line.From + av.ShootVelStep, glow.Line.To + av.ShootVelStep, glow.Line.Length);

                        if (av.OnScreen != AvShot.Screen.None)
                        {
                            var reduction = (av.GlowShrinkSize * glow.Step);
                            var width = widthScaler ? (av.AmmoDef.Const.TrailWidth - reduction) * av.TrailScaler : av.AmmoDef.Const.TrailWidth * av.TrailScaler;
                            var color = av.AmmoDef.AmmoGraphics.Lines.Trail.Color;

                            if (!widthScaler)
                                color *= MathHelper.Clamp(1f - reduction, 0.01f, 1f);
                            
                            MyTransparentGeometry.AddLineBillboard(av.AmmoDef.Const.TrailMaterial, color, glow.Line.From, glow.Line.Direction, (float) glow.Line.Length, width);
                        }

                        if (++glow.Step >= steps)
                        {
                            glow.Parent = null;
                            glow.Step = 0;
                            remove = true;
                            glowCnt--;
                            Glows.Push(glow);
                        }
                    }

                    if (remove) av.GlowSteps.Dequeue();
                }
            }
        }

        private void RunShrinks(AvShot av)
        {
            var s = av.TracerShrinks.Dequeue();
            if (av.LastTick != Session.Tick) {

                if (!av.AmmoDef.Const.OffsetEffect) {

                    if (av.OnScreen != AvShot.Screen.None)
                        MyTransparentGeometry.AddLineBillboard(av.AmmoDef.Const.TracerMaterial, s.Color, s.NewFront, av.PointDir, s.Length, s.Thickness);
                }
                else if (av.OnScreen != AvShot.Screen.None)
                    av.DrawLineOffsetEffect(s.NewFront, -av.PointDir, s.Length, s.Thickness, s.Color);

                if (av.Trail != AvShot.TrailState.Off && av.Back)
                    av.RunGlow(ref s, true);
            }

            if (av.TracerShrinks.Count == 0) av.ResetHit();
        }

        internal void RunHitSounds()
        {
            for (int i = 0; i < HitSounds.Count; i++)
            {
                var av = HitSounds[i];
                av.HitEmitter.SetPosition(av.TracerFront);
                av.HitEmitter.PlaySound(av.HitSound);
            }
            HitSounds.Clear();
        }

        internal void RunAvBarrels1()
        {
            for (int i = AvBarrels1.Count - 1; i >= 0; i--) {

                var avBarrel = AvBarrels1[i];
                var weapon = avBarrel.Weapon;
                var muzzle = avBarrel.Muzzle;
                var ticksAgo = weapon.Comp.Session.Tick - avBarrel.StartTick;

                if (!muzzle.Av1Looping && ticksAgo >= weapon.System.Barrel1AvTicks || weapon.StopBarrelAv) {

                    if (weapon.BarrelEffects1[muzzle.MuzzleId] != null) {

                        weapon.StopBarrelAv = false;
                        weapon.BarrelEffects1[muzzle.MuzzleId].Stop();
                        weapon.BarrelEffects1[muzzle.MuzzleId] = null;
                    }
                    muzzle.Av1Looping = false;
                    AvBarrels1.RemoveAtFast(i);
                    continue;
                }

                if (weapon.Comp.Ai != null && weapon.Comp.Ai.VelocityUpdateTick != weapon.Comp.Session.Tick) {

                    weapon.Comp.Ai.GridVel = weapon.Comp.Ai.MyGrid.Physics?.LinearVelocity ?? Vector3D.Zero;
                    weapon.Comp.Ai.IsStatic = weapon.Comp.Ai.MyGrid.Physics?.IsStatic ?? false;
                    weapon.Comp.Ai.VelocityUpdateTick = weapon.Comp.Session.Tick;
                }

                var entityExists = weapon.MuzzlePart.Entity?.Parent != null && !weapon.MuzzlePart.Entity.MarkedForClose;
                var matrix = MatrixD.Zero;

                var pos = weapon.Dummies[muzzle.MuzzleId].Info.Position;
                if (entityExists) matrix = MatrixD.CreateWorld(pos, weapon.MyPivotDir, weapon.MyPivotUp);

                if (entityExists && !weapon.StopBarrelAv) {

                    var particles = weapon.System.Values.HardPoint.Graphics;
                    matrix.Translation +=  Vector3D.Rotate(particles.Barrel1.Offset, matrix);
                    if (weapon.BarrelEffects1[muzzle.MuzzleId] == null && ticksAgo <= 0) {
;
                        if (MyParticlesManager.TryCreateParticleEffect(particles.Barrel1.Name, ref matrix, ref pos, uint.MaxValue, out weapon.BarrelEffects1[muzzle.MuzzleId])) {

                            weapon.BarrelEffects1[muzzle.MuzzleId].UserColorMultiplier = particles.Barrel1.Color;
                            weapon.BarrelEffects1[muzzle.MuzzleId].UserRadiusMultiplier = particles.Barrel1.Extras.Scale;
                            weapon.BarrelEffects1[muzzle.MuzzleId].WorldMatrix = matrix;
                            weapon.BarrelEffects1[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                            weapon.BarrelEffects1[muzzle.MuzzleId].Play();
                        }
                    }
                    else if (particles.Barrel1.Extras.Restart && weapon.BarrelEffects1[muzzle.MuzzleId] != null && weapon.BarrelEffects1[muzzle.MuzzleId].IsEmittingStopped) {

                        weapon.BarrelEffects1[muzzle.MuzzleId].WorldMatrix = matrix;
                        weapon.BarrelEffects1[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                        weapon.BarrelEffects1[muzzle.MuzzleId].Play();
                    }
                    else if (weapon.BarrelEffects1[muzzle.MuzzleId] != null) {

                        weapon.BarrelEffects1[muzzle.MuzzleId].WorldMatrix = matrix;
                        weapon.BarrelEffects1[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                    }
                }
            }
        }

        internal void RunAvBarrels2()
        {
            for (int i = AvBarrels2.Count - 1; i >= 0; i--) {

                var avBarrel = AvBarrels2[i];
                var weapon = avBarrel.Weapon;
                var muzzle = avBarrel.Muzzle;
                var ticksAgo = weapon.Comp.Session.Tick - avBarrel.StartTick;

                if (!muzzle.Av2Looping && ticksAgo >= weapon.System.Barrel2AvTicks || weapon.StopBarrelAv || !weapon.Comp.State.Value.Online || !weapon.Comp.Set.Value.Overrides.Activate || !weapon.Set.Enable) {

                    if (weapon.BarrelEffects2[muzzle.MuzzleId] != null) {

                        weapon.StopBarrelAv = false;
                        weapon.BarrelEffects2[muzzle.MuzzleId].Stop();
                        weapon.BarrelEffects2[muzzle.MuzzleId] = null;
                    }
                    muzzle.Av2Looping = false;
                    AvBarrels2.RemoveAtFast(i);
                    continue;
                }

                if (weapon.Comp.Ai != null && weapon.Comp.Ai.VelocityUpdateTick != weapon.Comp.Session.Tick) {

                    weapon.Comp.Ai.GridVel = weapon.Comp.Ai.MyGrid.Physics?.LinearVelocity ?? Vector3D.Zero;
                    weapon.Comp.Ai.IsStatic = weapon.Comp.Ai.MyGrid.Physics?.IsStatic ?? false;
                    weapon.Comp.Ai.VelocityUpdateTick = weapon.Comp.Session.Tick;
                }

                var entityExists = weapon.MuzzlePart.Entity?.Parent != null && !weapon.MuzzlePart.Entity.MarkedForClose;
                var pos = weapon.Dummies[muzzle.MuzzleId].Info.Position;

                var matrix = MatrixD.Zero;
                if (entityExists) matrix = MatrixD.CreateWorld(pos, weapon.MyPivotDir, weapon.MyPivotUp);

                if (entityExists && !weapon.StopBarrelAv) {

                    var particles = weapon.System.Values.HardPoint.Graphics;
                    matrix.Translation += Vector3D.Rotate(particles.Barrel2.Offset, matrix);
                    if (weapon.BarrelEffects2[muzzle.MuzzleId] == null && ticksAgo <= 0) {
                        
                        if (MyParticlesManager.TryCreateParticleEffect(particles.Barrel2.Name, ref matrix, ref pos, uint.MaxValue, out weapon.BarrelEffects2[muzzle.MuzzleId])) {

                            weapon.BarrelEffects2[muzzle.MuzzleId].UserColorMultiplier = particles.Barrel2.Color;
                            weapon.BarrelEffects2[muzzle.MuzzleId].UserRadiusMultiplier = particles.Barrel2.Extras.Scale;
                            weapon.BarrelEffects2[muzzle.MuzzleId].WorldMatrix = matrix;
                            weapon.BarrelEffects2[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                            weapon.BarrelEffects2[muzzle.MuzzleId].Play();
                        }
                    }
                    else if (particles.Barrel2.Extras.Restart && weapon.BarrelEffects2[muzzle.MuzzleId] != null && weapon.BarrelEffects2[muzzle.MuzzleId].IsEmittingStopped) {

                        weapon.BarrelEffects2[muzzle.MuzzleId].WorldMatrix = matrix;
                        weapon.BarrelEffects2[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                        weapon.BarrelEffects2[muzzle.MuzzleId].Play();
                    }
                    else if (weapon.BarrelEffects2[muzzle.MuzzleId] != null) {

                        weapon.BarrelEffects2[muzzle.MuzzleId].WorldMatrix = matrix;
                        weapon.BarrelEffects2[muzzle.MuzzleId].Velocity = weapon.Comp.Ai?.GridVel ?? Vector3D.Zero;
                    }
                }
            }
        }
    }

    internal struct AvBarrel
    {
        internal Weapon Weapon;
        internal Weapon.Muzzle Muzzle;
        internal uint StartTick;
    }

    internal class KeensMess
    {
        internal MyParticleEffect Effect;
        internal WeaponDefinition.AmmoDef AmmoDef;
        internal Vector3D Velocity;
        internal uint LastTick;
        internal bool Looping;

        public void Clean()
        {
            Effect?.Stop();
            Effect = null;
            AmmoDef = null;
            Velocity = Vector3D.Zero;
            LastTick = 0;
            Looping = false;
        }
    }
}
