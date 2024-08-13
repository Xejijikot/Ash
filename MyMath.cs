using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
	public static class MyMath
	{
		public static Vector3D FindInterceptGVector(Vector3D myPos, Vector3D MySpeed, EnemyTargetedInfo Target, Vector3D gravity, double projectileSpeed, int targetingPoint = 0, bool dir = true)
		{
			//часть стартовой скорости будет потрачено на гравитацию, часть на на сближение
			//найдем длину вектора скорости сближения
			//при гравитации отличной от 0 будет два решения, для direct fire и для артиллерии.

			//при взятии поправки на гравитацию время полета снаряда будет увеличиваться, что потребует новой поправки
			//из за высокой скорости полета снаряда время в пути не будет превышать 2х секунд
			//так же из за всратого распределения гравитации, кривизны планеты, неправильного обсчета ускорения и неточности орудия
			//брать поправку слишком много раз не вижу смысла
			Vector3D target = Target.Position;
			if (targetingPoint == 0)
				if(Target.TargetedPoint != null)
					target = Target.TargetedPoint.GetValueOrDefault();
			if (targetingPoint == 2)
				if(Target.HitPosition != null)
					target = Target.HitPosition.GetValueOrDefault();
			Vector3D targetDirection = target - myPos;
			double speed = projectileSpeed;
			double correctedSpeed = speed;
			Vector3D sumspeed = Target.Velocity - MySpeed;
			Vector3D InterceptVector = FindInterceptVector(myPos, correctedSpeed, target, sumspeed);
			for (int i = 0; i < 10; i++)
			{
				FindGravityCorrection_DirectFire(speed, ref correctedSpeed, InterceptVector, gravity);
				InterceptVector = FindInterceptVector(myPos, correctedSpeed, target, sumspeed);
			}
			double timeToHit = InterceptVector.Length()/correctedSpeed;
			Vector3D yVector = -gravity * timeToHit * timeToHit / 2;
			InterceptVector = InterceptVector + yVector;
			if (dir)
				return InterceptVector;
			else return InterceptVector + myPos;
		}
		public static Vector3D FindBallisticPoint(Vector3D myPos, Vector3D mySpeed, EnemyTargetedInfo Target, Vector3D grav, Vector3D projectileSpeed, int targetingPoint = 0)
		{
			Vector3D target = Target.Position;
			if (targetingPoint == 0)
				if (Target.TargetedPoint != null)
					target = Target.TargetedPoint.GetValueOrDefault();
			if (targetingPoint == 2)
				if (Target.HitPosition != null)
					target = Target.HitPosition.GetValueOrDefault();
			Vector3D sumSpeed = mySpeed + projectileSpeed - Target.Velocity;
			Vector3D dirToTarget = target - myPos;
			double distanceToTarget = dirToTarget.Length();
			double projectileSumSpeed = sumSpeed.Length();
			double timeToImpact = distanceToTarget / projectileSumSpeed;
			Vector3D BallisticPoint = myPos + sumSpeed * timeToImpact + grav * timeToImpact * timeToImpact / 2;
			return BallisticPoint;
		}
		public static Vector3D FindBallisticPoint(Vector3D myPos, Vector3D mySpeed, Vector3D Target, Vector3D grav, Vector3D projectileSpeed)
		{

			Vector3D sumSpeed = mySpeed + projectileSpeed;
			Vector3D dirToTarget = Target - myPos;
			double distanceToTarget = dirToTarget.Length();
			double projectileSumSpeed = sumSpeed.Length();
			double timeToImpact = distanceToTarget / projectileSumSpeed;
			Vector3D BallisticPoint = myPos + sumSpeed * timeToImpact + grav * timeToImpact * timeToImpact / 2;
			return BallisticPoint;
		}
		static void FindGravityCorrection_DirectFire(double speed, ref double x, Vector3D targetDir, Vector3D grav)
		{
			double distanceToTarget = targetDir.Length();
			double angleCos = grav.Dot(targetDir) / (grav.Length() * targetDir.Length());
			double l = targetDir.Length();
			double g = grav.Length();
			double a = 1;
			//angleCos = 0;
			double b = (2 * g * l * angleCos) - (speed * speed);
			double c = Math.Pow((g * l / 2), 2);
			double? resoult1;
			double? resoult2;
			double? x1 = null;
			double? x2 = null;
			QuadraticEquation(a, b, c, out resoult1, out resoult2);
			if (resoult1 != null)
				if (resoult1 > 0)
				{
					x1 = Math.Sqrt(resoult1.GetValueOrDefault());
					x = x1.GetValueOrDefault();
				}
			if (resoult2 != null)
				if (resoult2 > 0)
				{
					x2 = Math.Sqrt(resoult2.GetValueOrDefault());
					if (x1 != null)
						if (x2 > x1)
							x = x2.GetValueOrDefault();
				}
		}
		public static MatrixD CreateLookAtForwardDir(Vector3D cameraPosition, Vector3D cameraForwardVector, Vector3D suggestedUp)
		{
			Vector3D up = Vector3D.Cross(Vector3D.Cross(cameraForwardVector, suggestedUp), cameraForwardVector);
			Vector3D vector3D = Vector3D.Normalize(- cameraForwardVector);
			Vector3D vector3D2 = Vector3D.Normalize(Vector3D.Cross(up, vector3D));
			Vector3D vector = Vector3D.Cross(vector3D, vector3D2);
			MatrixD result = default(MatrixD);
			result.M11 = vector3D2.X;
			result.M12 = vector3D2.Y;
			result.M13 = vector3D2.Z;
			result.M14 = 0.0;
			result.M21 = vector.X;
			result.M22 = vector.Y;
			result.M23 = vector.Z;
			result.M24 = 0.0;
			result.M31 = vector3D.X;
			result.M32 = vector3D.Y;
			result.M33 = vector3D.Z;
			result.M34 = 0.0;
			result.M41 = cameraPosition.X;
			result.M42 = cameraPosition.Y;
			result.M43 = cameraPosition.Z;
			result.M44 = 1.0;
			return result;
		}
		public static MatrixD CreateLookAtUpDir(Vector3D cameraPosition, Vector3D suggestedForward, Vector3D cameraUpVector)
		{
			Vector3D cameraForwardVector = Vector3D.Cross(Vector3D.Cross(cameraUpVector, suggestedForward), cameraUpVector);
			Vector3D vector3D = Vector3D.Normalize(-cameraForwardVector);
			Vector3D vector3D2 = Vector3D.Normalize(Vector3D.Cross(cameraUpVector, vector3D));
			Vector3D vector = Vector3D.Cross(vector3D, vector3D2);
			MatrixD result = default(MatrixD);
			result.M11 = vector3D2.X;
			result.M12 = vector3D2.Y;
			result.M13 = vector3D2.Z;
			result.M14 = 0.0;
			result.M21 = vector.X;
			result.M22 = vector.Y;
			result.M23 = vector.Z;
			result.M24 = 0.0;
			result.M31 = vector3D.X;
			result.M32 = vector3D.Y;
			result.M33 = vector3D.Z;
			result.M34 = 0.0;
			result.M41 = cameraPosition.X;
			result.M42 = cameraPosition.Y;
			result.M43 = cameraPosition.Z;
			result.M44 = 1.0;
			return result;
		}

		public static Vector3D FindInterceptVector(Vector3D shotOrigin, double shotVel, Vector3D targetOrigin, Vector3D targetVel)
		{
			Vector3D toTarget = targetOrigin - shotOrigin;
			Vector3D dirToTarget = Vector3D.Normalize(toTarget);
			Vector3D targetVelOrth = Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;
			Vector3D targetVelTang = targetVel - targetVelOrth;
			Vector3D shotVelTang = targetVelTang;
			double shotVelSpeed = shotVelTang.Length();

			if (shotVelSpeed > shotVel)
			{
				return Vector3D.Normalize(targetVel) * shotVel;
			}
			else
			{
				double shotSpeedOrth = Math.Sqrt(shotVel * shotVel - shotVelSpeed * shotVelSpeed);
				Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;
				double timeToHit = toTarget.Length() / (targetVelOrth - shotVelOrth).Length();
				return (shotVelOrth + shotVelTang).Normalized() * (timeToHit * shotVel);
			}
		}
		public static void QuadraticEquation(double a, double b, double c, out double? x1, out double? x2)
		{
			var discriminant = Math.Pow(b, 2) - 4 * a * c;
			if (discriminant < 0)
			{
				x1 = null;
				x2 = null;
			}
			else
			{
				if (discriminant == 0) //квадратное уравнение имеет два одинаковых корня
				{
					x1 = -b / (2 * a);
					x2 = x1;
				}
				else //уравнение имеет два разных корня
				{
					x1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
					x2 = (-b - Math.Sqrt(discriminant)) / (2 * a);
				}
			}
		}
		public static Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
		{
			return new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Backward));
		}
		public static double CosBetween(Vector3D a, Vector3D b) //returns radians 
		{
			if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
				return 0;
			else
				return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
		}
		public static double CalculateRotorDeviationAngle(Vector3D forwardVector, MatrixD lastOrientation)
		{
			var flattenedForwardVector = VectorRejection(forwardVector, lastOrientation.Up);
			return VectorAngleBetween(flattenedForwardVector, lastOrientation.Forward) * Math.Sign(flattenedForwardVector.Dot(lastOrientation.Left));
		}
		public static void CalculateYawVelocity(MatrixD turretMatrix, MatrixD turretLastMatrix, out double speed)
		{
			Vector3D now = turretMatrix.Forward;
			var flattenedForwardVector = VectorRejection(now, turretLastMatrix.Up);
			speed = - VectorAngleBetween(flattenedForwardVector, turretLastMatrix.Forward) * Math.Sign(flattenedForwardVector.Dot(turretLastMatrix.Left));
		}
		public static void CalculatePitchVelocity(MatrixD weaponMatrix, MatrixD weaponLastMatrix, out double speed)
		{
			Vector3D now = weaponMatrix.Forward;
			var flattenedForwardVector = VectorRejection(now, weaponLastMatrix.Right);
			speed = - VectorAngleBetween(flattenedForwardVector, weaponLastMatrix.Forward) * Math.Sign(flattenedForwardVector.Dot(weaponLastMatrix.Down));
		}
		public static Vector3D VectorProjection(Vector3D a, Vector3D b)
		{
			return a.Dot(b) / b.LengthSquared() * b;
		}
		public static Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
		{
			if (Vector3D.IsZero(b))
				return Vector3D.Zero;

			return a - a.Dot(b) / b.LengthSquared() * b;
		}

		public static double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
		{
			if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
				return 0;
			else
				return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
		}
		public static double Vector2AngleBetween(Vector2D a, Vector2D b) //returns radians 
		{
			if (a.Length() == 0 || b.Length() == 0)
				return 0;
			else
				return Math.Acos(MathHelper.Clamp(Vector2D.Dot(a, b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
		}
		public static double Vector2DeviationFromZero(Vector2D a) //returns radians 
		{
			if (a.Length() == 0)
				return 0;
			Vector2D b = new Vector2D(1, 0);
			return Math.Acos(MathHelper.Clamp(Vector2D.Dot(a, b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
		}
	}
	

}
