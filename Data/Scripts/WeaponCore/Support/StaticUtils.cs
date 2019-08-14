﻿using WeaponCore.Platform;
using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
namespace WeaponCore.Support
{
    internal static class UtilsStatic
    {
        public static void GetClosestHitableBlockOfType(List<MyCubeBlock> cubes, Target target, Vector3D currentPos, Weapon w = null)
        {
            var minValue = double.MaxValue;
            var minValue0 = double.MaxValue;
            var minValue1 = double.MaxValue;
            var minValue2 = double.MaxValue;
            var minValue3 = double.MaxValue;

            MyCubeBlock newEntity = null;
            MyCubeBlock newEntity0 = null;
            MyCubeBlock newEntity1 = null;
            MyCubeBlock newEntity2 = null;
            MyCubeBlock newEntity3 = null;
            var bestCubePos = Vector3D.Zero;
            var top5Count = target.Top5.Count;
            var testPos = currentPos;
            var top5 = target.Top5;
            var physics = MyAPIGateway.Physics;
            IHitInfo hitInfo = null;
            for (int i = 0; i < cubes.Count + top5Count; i++)
            {
                var index = i < top5Count ? i : i - top5Count;
                var cube = i < top5Count ? top5[index] : cubes[index];
                if (cube.MarkedForClose || cube == newEntity || cube == newEntity0 || cube == newEntity1  || cube == newEntity2 || cube == newEntity3) continue;
                var grid = cube.CubeGrid;
                var cubePos = grid.GridIntegerToWorld(cube.Position);
                var range = cubePos - testPos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue3)
                {
                    IHitInfo hit = null;
                    var best = test < minValue;
                    bool bestTest = false;
                    if (best)
                    {
                        if (w != null)
                        {
                            Vector3D targetLinVel = grid.Physics?.LinearVelocity ?? Vector3D.Zero;
                            bestTest = Weapon.CanShootTarget(w, ref cubePos, ref targetLinVel) && physics.CastRay(testPos, cubePos, out hit, 15, true) && hit?.HitEntity == cube.CubeGrid;
                        }
                        else
                            bestTest = true;
                    }
                    if (best && bestTest)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = minValue0;
                        newEntity1 = newEntity0;
                        minValue0 = minValue;
                        newEntity0 = newEntity;
                        minValue = test;

                        newEntity = cube;
                        bestCubePos = cubePos;
                        hitInfo = hit;
                    }
                    else if (test < minValue0)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = minValue0;
                        newEntity1 = newEntity0;
                        minValue0 = test;

                        newEntity0 = cube;
                    }
                    else if (test < minValue1)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = minValue1;
                        newEntity2 = newEntity1;
                        minValue1 = test;

                        newEntity1 = cube;
                    }
                    else if (test < minValue2)
                    {
                        minValue3 = minValue2;
                        newEntity3 = newEntity2;
                        minValue2 = test;

                        newEntity2 = cube;
                    }
                    else 
                    {
                        minValue3 = test;
                        newEntity3 = cube;
                    }
                }

            }
            top5.Clear();
            if (newEntity != null && hitInfo != null)
            {
                double rayDist;
                Vector3D.Distance(ref testPos, ref bestCubePos, out rayDist);
                var shortDist = rayDist * (1 - hitInfo.Fraction);
                var origDist = rayDist * hitInfo.Fraction;
                var topEntId = newEntity.GetTopMostParent().EntityId;
                target.Set(newEntity, hitInfo.Position, shortDist, origDist, topEntId);
                top5.Add(newEntity);
            }
            else if (newEntity != null)
            {
                double rayDist;
                Vector3D.Distance(ref testPos, ref bestCubePos, out rayDist);
                var shortDist = rayDist;
                var origDist = rayDist;
                var topEntId = newEntity.GetTopMostParent().EntityId;
                target.Set(newEntity, bestCubePos, shortDist, origDist, topEntId);
                top5.Add(newEntity);
            }
            else target.Reset();

            if (newEntity0 != null)
            {
                top5.Add(newEntity0);
            }

            if (newEntity1 != null)
            {
                top5.Add(newEntity1);
            }

            if (newEntity2 != null)
            {
                top5.Add(newEntity2);
            }

            if (newEntity3 != null)
            {
                top5.Add(newEntity3);
            }
        }

        public static void UpdateTerminal(this MyCubeBlock block)
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (block.IDModule != null)
            {
                ownerId = block.IDModule.Owner;
                shareMode = block.IDModule.ShareMode;
            }
            else
            {
                return;
            }
            block.ChangeOwner(ownerId, shareMode == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            block.ChangeOwner(ownerId, shareMode);
        }

        public static void FibonacciSeq(int magicNum)
        {
            var root5 = Math.Sqrt(5);
            var phi = (1 + root5) / 2;

            var n = 0;
            int Fn;
            do
            {
                Fn = (int)((Math.Pow(phi, n) - Math.Pow(-phi, -n)) / ((2 * phi) - 1));
                //Console.Write("{0} ", Fn);
                ++n;
            }
            while (Fn < magicNum);
        }

        public static void SphereCloud(int pointLimit, Vector3D[] physicsArray, MyEntity shieldEnt, bool transformAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var unscaledPosWorldMatrix = MatrixD.Rescale(MatrixD.CreateTranslation(sPosComp.WorldAABB.Center), sPosComp.WorldVolume.Radius);
            var radius = sPosComp.WorldVolume.Radius;
            for (int i = 0; i < pointLimit; i++)
            {
                var value = rnd.Next(0, physicsArray.Length - 1);
                var phi = 2 * Math.PI * i / pointLimit;
                var x = (float)(radius * Math.Sin(phi) * Math.Cos(value));
                var z = (float)(radius * Math.Sin(phi) * Math.Sin(value));
                var y = (float)(radius * Math.Cos(phi));
                var v = new Vector3D(x, y, z);

                if (transformAndScale) v = Vector3D.Transform(Vector3D.Normalize(v), unscaledPosWorldMatrix);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereCloudQuick(int pointLimit, ref Vector3D[] physicsArray, MyEntity shieldEnt, bool translateAndScale, bool debug, Random rnd = null)
        {
            if (pointLimit > 10000) pointLimit = 10000;
            if (rnd == null) rnd = new Random(0);

            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;
            var v = Vector3D.Zero;

            for (int i = 0; i < pointLimit; i++)
            {
                while (true)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                    var len2 = v.LengthSquared();
                    if (len2 < .0001) continue;
                    v *= radius / Math.Sqrt(len2);
                    break;
                }

                if (translateAndScale) physicsArray[i] = v += center;
                else physicsArray[i] = v;
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereRandomOnly(ref Vector3D[] physicsArray, Random rnd = null)
        {
            if (rnd == null) rnd = new Random(0);
            var v = Vector3D.Zero;

            for (int i = 0; i < physicsArray.Length; i++)
            {
                v.X = 0;
                v.Y = 0;
                v.Z = 0;
                while ((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z) < 0.0001)
                {
                    v.X = (rnd.NextDouble() * 2) - 1;
                    v.Y = (rnd.NextDouble() * 2) - 1;
                    v.Z = (rnd.NextDouble() * 2) - 1;
                }
                v.Normalize();
                physicsArray[i] = v;
            }
        }

        public static void UnitSphereTranslateScale(int pointLimit, ref Vector3D[] physicsArray, ref Vector3D[] scaledCloudArray, MyEntity shieldEnt, bool debug)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;

            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                scaledCloudArray[i] = v = center + (radius * v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void UnitSphereTranslateScaleList(int pointLimit, ref Vector3D[] physicsArray, ref List<Vector3D> scaledCloudList, MyEntity shieldEnt, bool debug, MyEntity grid, bool rotate = true)
        {
            var sPosComp = shieldEnt.PositionComp;
            var radius = sPosComp.WorldVolume.Radius;
            var center = sPosComp.WorldAABB.Center;
            var gMatrix = grid.WorldMatrix;
            for (int i = 0; i < pointLimit; i++)
            {
                var v = physicsArray[i];
                if (rotate) Vector3D.Rotate(ref v, ref gMatrix, out v);
                v = center + (radius * v);
                scaledCloudList.Add(v);
                if (debug) DsDebugDraw.DrawX(v, sPosComp.LocalMatrix, 0.5);
            }
        }

        public static void DetermisticSphereCloud(List<Vector3D> physicsArray, int pointsInSextant)
        {
            physicsArray.Clear();
            int stepsPerCoord = (int)Math.Sqrt(pointsInSextant);
            double radPerStep = MathHelperD.PiOver2 / stepsPerCoord;

            for (double az = -MathHelperD.PiOver4; az < MathHelperD.PiOver4; az += radPerStep)
            {
                for (double el = -MathHelperD.PiOver4; el < MathHelperD.PiOver4; el += radPerStep)
                {
                    Vector3D vec;
                    Vector3D.CreateFromAzimuthAndElevation(az, el, out vec);
                    Vector3D vec2 = new Vector3D(vec.Z, vec.X, vec.Y);
                    Vector3D vec3 = new Vector3D(vec.Y, vec.Z, vec.X);
                    physicsArray.Add(vec); //first sextant
                    physicsArray.Add(vec2); //2nd sextant
                    physicsArray.Add(vec3); //3rd sextant
                    physicsArray.Add(-vec); //4th sextant
                    physicsArray.Add(-vec2); //5th sextant
                    physicsArray.Add(-vec3); //6th sextant
                }
            }
        }

        public static Vector3D? GetLineIntersectionExactAll(MyCubeGrid grid, ref LineD line, out double distance, out IMySlimBlock intersectedBlock)
        {
            intersectedBlock = (IMySlimBlock)null;
            distance = 3.40282346638529E+38;
            Vector3I? nullable = new Vector3I?();
            Vector3I zero = Vector3I.Zero;
            double distanceSquared = double.MaxValue;
            if (grid.GetLineIntersectionExactGrid(ref line, ref zero, ref distanceSquared))
            {
                distanceSquared = Math.Sqrt(distanceSquared);
                nullable = new Vector3I?(zero);
            }
            if (!nullable.HasValue)
                return new Vector3D?();
            distance = distanceSquared;
            intersectedBlock = grid.GetCubeBlock(nullable.Value);
            if (intersectedBlock == null)
                return new Vector3D?();
            return new Vector3D?((Vector3D)zero);
        }

        public static double InverseSqrDist(Vector3D source, Vector3D target, double range)
        {
            var rangeSq = range * range;
            var distSq = (target - source).LengthSquared();
            if (distSq > rangeSq)
                return 0.0;
            return 1.0 - (distSq / rangeSq);
        }

        public static double GetIntersectingSurfaceArea(MatrixD matrix, Vector3D hitPosLocal)
        {
            var surfaceArea = -1d; 

            var boxMax = matrix.Backward + matrix.Right + matrix.Up;
            var boxMin = -boxMax;
            var box = new BoundingBoxD(boxMin, boxMax);

            var maxWidth = box.Max.LengthSquared();
            var testLine = new LineD(Vector3D.Zero, Vector3D.Normalize(hitPosLocal) * maxWidth); 
            LineD testIntersection;
            box.Intersect(ref testLine, out testIntersection);

            var intersection = testIntersection.To;

            var epsilon = 1e-6; 
            var projFront = VectorProjection(intersection, matrix.Forward);
            if (Math.Abs(projFront.LengthSquared() - matrix.Forward.LengthSquared()) < epsilon)
            {
                var a = Vector3D.Distance(matrix.Left, matrix.Right);
                var b = Vector3D.Distance(matrix.Up, matrix.Down);
                surfaceArea = a * b;
            }

            var projLeft = VectorProjection(intersection, matrix.Left);
            if (Math.Abs(projLeft.LengthSquared() - matrix.Left.LengthSquared()) < epsilon) 
            {
                var a = Vector3D.Distance(matrix.Forward, matrix.Backward);
                var b = Vector3D.Distance(matrix.Up, matrix.Down);
                surfaceArea = a * b;
            }

            var projUp = VectorProjection(intersection, matrix.Up);
            if (Math.Abs(projUp.LengthSquared() - matrix.Up.LengthSquared()) < epsilon) 
            {
                var a = Vector3D.Distance(matrix.Forward, matrix.Backward);
                var b = Vector3D.Distance(matrix.Left, matrix.Right);
                surfaceArea = a * b;
            }
            return surfaceArea;
        }

        public static void CreateMissileExplosion(float damage, double radius, Vector3D position, Vector3D direction, MyEntity owner, MyEntity hitEnt, WeaponSystem weaponSystem, bool forceNoDraw = false)
        {
            var af = weaponSystem.Values.Ammo.AreaEffect;
            var eInfo = af.Explosions;
            var sphere = new BoundingSphereD(position, radius);
            var cullSphere = sphere;
            cullSphere.Radius = radius * 5;
            MyExplosionFlags eFlags;
            var drawParticles = !forceNoDraw && !eInfo.NoVisuals && MyAPIGateway.Session.Camera.IsInFrustum(ref cullSphere);
            if (drawParticles)
                eFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION;
            else
                eFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION;

            var customParticle = eInfo.CustomParticle != string.Empty;
            var explosionType = !customParticle ? MyExplosionTypeEnum.MISSILE_EXPLOSION : MyExplosionTypeEnum.CUSTOM;
            var explosionInfo = new MyExplosionInfo
            {
                PlayerDamage = 0.0f,
                Damage = damage,
                ExplosionType = explosionType,
                ExplosionSphere = sphere,
                LifespanMiliseconds = 700,
                HitEntity = hitEnt,
                ParticleScale = eInfo.Scale,
                OwnerEntity = owner,
                Direction = direction,
                VoxelExplosionCenter = sphere.Center + radius * direction * 0.25,
                ExplosionFlags = eFlags,
                VoxelCutoutScale = 0.3f,
                PlaySound = !eInfo.NoSound,
                ApplyForceAndDamage = true,
                KeepAffectedBlocks = true,
                CustomEffect = eInfo.CustomParticle,
                CreateParticleEffect = drawParticles,
            };
            MyExplosions.AddExplosion(ref explosionInfo);
        }

        public static void CreateFakeExplosion(double radius, Vector3D position, WeaponSystem weaponSystem)
        {
            var af = weaponSystem.Values.Ammo.AreaEffect;
            var eInfo = af.Explosions;
            var sphere = new BoundingSphereD(position, radius);
            var cullSphere = sphere;
            cullSphere.Radius = af.AreaEffectRadius * 5;
            const MyExplosionFlags eFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT; ;
            var drawParticles = !eInfo.NoVisuals && MyAPIGateway.Session.Camera.IsInFrustum(ref cullSphere);

            var customParticle = eInfo.CustomParticle != string.Empty;
            var explosionType = !customParticle ? MyExplosionTypeEnum.MISSILE_EXPLOSION : MyExplosionTypeEnum.CUSTOM;
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0.0f,
                Damage = 0f,
                ExplosionType = explosionType,
                ExplosionSphere = sphere,
                LifespanMiliseconds = 0,
                ParticleScale = eInfo.Scale,
                Direction = Vector3.Down,
                VoxelExplosionCenter = position,
                ExplosionFlags = eFlags,
                VoxelCutoutScale = 0f,
                PlaySound = !eInfo.NoSound,
                ApplyForceAndDamage = false,
                ObjectsRemoveDelayInMiliseconds = 0,
                CustomEffect = eInfo.CustomParticle,
                CreateParticleEffect = drawParticles,
            };
            MyExplosions.AddExplosion(ref explosionInfo);
        }

        public static Vector3D NearestPointOnLine(Vector3D start, Vector3D end, Vector3D pnt)
        {
            var line = (end - start);
            var len = line.Length();
            line.Normalize();

            var v = pnt - start;
            var d = Vector3.Dot(v, line);
            MathHelper.Clamp(d, 0f, len);
            return start + line * d;
        }

        /*
        ** Returns the point on the line formed by (point1 + dir1 * x) that is closest to the point
        ** on the line formed by line (point2 + dir2 * t)
        */
        public static Vector3D GetClosestPointOnLine1(Vector3D point1, Vector3D dir1, Vector3D point2, Vector3D dir2)
        {
            Vector3D axis = Vector3D.Cross(dir1, dir2);
            if (Vector3D.IsZero(axis))
                return point1;
            Vector3D perpDir2 = Vector3D.Cross(dir2, axis);
            Vector3D point1To2 = point2 - point1;
            return point1 + Vector3D.Dot(point1To2, perpDir2) / Vector3D.Dot(dir1, perpDir2) * dir1;
        }

        /*
        ** Returns the point on the line1 that is closest to the point on line2
        */
        public static Vector3D GetClosestPointOnLine2(Vector3D line1Start, Vector3D line1End, Vector3D line2Start, Vector3D line2End)
        {
            Vector3D dir1 = line1End - line1Start;
            Vector3D dir2 = line2End - line2Start;
            Vector3D axis = Vector3D.Cross(dir1, dir2);
            if (Vector3D.IsZero(axis))
                return line1Start;
            Vector3D perpDir2 = Vector3D.Cross(dir2, axis);
            Vector3D point1To2 = line2Start - line1Start;
            return line1Start + Vector3D.Dot(point1To2, perpDir2) / Vector3D.Dot(dir1, perpDir2) * dir1;
        }

        private static Vector3D VectorProjection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a.Dot(b) / b.LengthSquared() * b;
        }

        public static bool sameSign(float num1, double num2)
        {
            if (num1 > 0 && num2 < 0)
                return false;
            if (num1 < 0 && num2 > 0)
                return false;
            return true;
        }

        public static bool NearlyEqual(double f1, double f2)
        {
            // Equal if they are within 0.00001 of each other
            return Math.Abs(f1 - f2) < 0.00001;
        }

    }
}
