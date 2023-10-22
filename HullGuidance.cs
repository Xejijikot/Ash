using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.EntityComponents.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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
	public class HullGuidance
	{
		bool _firstrun = true;
		const double TURNGYROCONST = 0.0035;	//При увеличении скорости на это значение в тик, достигается максимальная мощность гироскопа
        const float SOFTNAVCONST = 0.8f;
		Vector3D lastTargetDir = Vector3D.Zero;
		MatrixD lastShipMatrix = MatrixD.Identity;
		MatrixD shipMatrix;
		double lastPitch = 0;
		double lastYaw = 0;
		double maneuvrabilityYaw = 0;
		double maneuvrabilityPitch = 0;
        bool fullDriveYaw = false;				//Флаги для замера маневрненности, замеряем маневрненность только при максимальной мощности гироскопа
        bool fullDrivePitch = false;
        bool b;
		double yawDeltaInput = 0;
		double yawInputSpeed = 0;
        double pitchDeltaInput = 0;
		double pitchInputSpeed = 0;
        public void Control(ref string debuginfo, IMyShipController shipController, Vector3D targetDir, float rollIndicator, List<IMyGyro> gyros, bool dir = true, bool autoAim = true, float yawMult = 1, float pitchMult = 1)
		{
            if (!dir)
                targetDir -= shipController.WorldMatrix.Translation;
            shipMatrix = shipController.WorldMatrix;
			Vector3D WorldAngularVelocity = shipController.GetShipVelocities().AngularVelocity;
			Vector3D LocalAngularVelocity = Vector3D.TransformNormal(WorldAngularVelocity, MatrixD.Transpose(shipMatrix));

			double ownYaw = -LocalAngularVelocity.Y / 60;				//speed from rad/s to rad/tick
			double ownPitch = -LocalAngularVelocity.X / 60;

			double wantedYaw, wantedPitch;
			double yawSpeed, pitchSpeed;
			double targetAngularVelYaw, targetAngularVelPitch;

			//Угол на который надо повернуться
			Vector3D targetVecLoc = Vector3D.TransformNormal(targetDir, MatrixD.Transpose(shipMatrix));
			wantedYaw = Math.Atan2(targetVecLoc.X, -targetVecLoc.Z);
			double xyLenght = new Vector2D(targetVecLoc.X, targetVecLoc.Z).Length();
            if (targetVecLoc.Z > 0)
            {
                xyLenght *= -1;
            }
            wantedPitch = Math.Atan2(-targetVecLoc.Y, xyLenght);
            if (_firstrun)
			{
				lastYaw = 0;
				lastPitch = 0;
				lastShipMatrix = shipMatrix;
				_firstrun = false;
				lastTargetDir = targetDir;

                yawDeltaInput = 0;
                pitchDeltaInput = 0;
                yawInputSpeed = 0; pitchInputSpeed = 0;
                if (wantedYaw > 0)
				{
					fullDriveYaw = true;
                    yawSpeed = ownYaw + TURNGYROCONST;
				}
				else yawSpeed = ownYaw - TURNGYROCONST;
				if (wantedPitch > 0)
				{
                    fullDrivePitch = true;
                    pitchSpeed = ownPitch + TURNGYROCONST;
				}
				else pitchSpeed = ownPitch - TURNGYROCONST;
            }
			else
			{
                //Движения цели
                Vector3D lastTargetVecLoc = Vector3D.TransformNormal(lastTargetDir, MatrixD.Transpose(shipMatrix));

				double lastTargetDirYaw = Math.Atan2(lastTargetVecLoc.X, -lastTargetVecLoc.Z);

                xyLenght = new Vector2D(lastTargetVecLoc.X, lastTargetVecLoc.Z).Length();
                if (lastTargetVecLoc.Z > 0)
                {
                    xyLenght *= -1;
                }
                double lastTargetDirPitch = Math.Atan2(-lastTargetVecLoc.Y, xyLenght);

				targetAngularVelYaw = wantedYaw - lastTargetDirYaw;
                targetAngularVelPitch = wantedPitch - lastTargetDirPitch;

				//wantedYaw += yawError;
				//wantedPitch += pitchError;

				//Собственное движение и маневрненность
				Vector3D myLastDir = lastShipMatrix.Forward;
				Vector3D myLastDirLoc = Vector3D.TransformNormal(myLastDir, MatrixD.Transpose(shipMatrix));
				if(fullDriveYaw)
					maneuvrabilityYaw = Math.Abs(lastYaw - ownYaw);
				if(fullDrivePitch)
                    maneuvrabilityPitch = Math.Abs(lastPitch - ownPitch);
                fullDriveYaw = false;
				fullDrivePitch = false;
				//Управление пользователем
                double yawRotationInput = yawMult * shipController.RotationIndicator.Y;
                double pitchRotationInput = pitchMult * shipController.RotationIndicator.X;
                if (!autoAim)
                {
					if (Math.Abs(yawInputSpeed - yawRotationInput) > maneuvrabilityYaw)
					{
						if (yawInputSpeed - yawRotationInput > 0)
                            yawInputSpeed -= maneuvrabilityYaw;
						else
                            yawInputSpeed += maneuvrabilityYaw;
					}
					else
					{
						yawInputSpeed = yawRotationInput;
                    }
					if (Math.Abs(pitchInputSpeed - pitchRotationInput) > maneuvrabilityPitch)
					{
						if (pitchInputSpeed - pitchRotationInput > 0)
                            pitchInputSpeed -= maneuvrabilityPitch;
						else
                            pitchInputSpeed += maneuvrabilityPitch;
					}
					else { pitchInputSpeed = pitchRotationInput; }
                    yawDeltaInput += yawInputSpeed;
                    pitchDeltaInput += pitchInputSpeed;
                }
                else
                {
                    yawRotationInput = 0; pitchRotationInput = 0;
                    yawDeltaInput = 0; pitchDeltaInput = 0;
                }
                //YAW 
                double timeToStopYaw = Math.Abs((ownYaw - targetAngularVelYaw - yawRotationInput) / maneuvrabilityYaw);		//вычисляем реальную скорость относительно точки прицеливания
                double avaibleDistanceYaw = wantedYaw + yawDeltaInput - ownYaw + (targetAngularVelYaw + yawRotationInput) * timeToStopYaw;	//
                

				//Оптимальная скорость - максимальная скорость, при которой успеваем затормозить
                //За пространство для доп маневров отвечает SOFTNAVCONST
                double optimalAngularVelYaw = SOFTNAVCONST * Math.Sqrt(Math.Abs(2 * maneuvrabilityYaw * avaibleDistanceYaw));

                if (wantedYaw + yawDeltaInput < 0)
                    optimalAngularVelYaw *= -1;
                optimalAngularVelYaw += targetAngularVelYaw; //надо не прийти в точку, а двигаться в этой точке со скоростью цели

                b = Math.Abs(wantedYaw + yawDeltaInput) > maneuvrabilityYaw;
                if (b)//rough
                {
                    if (ownYaw < optimalAngularVelYaw)
                    {
                        yawSpeed = ownYaw + TURNGYROCONST;
                        fullDriveYaw = true;
                    }
                    else
                    {
                        yawSpeed = ownYaw - TURNGYROCONST;
                        fullDriveYaw = true;
                    }
                }
                else//soft
                {
					yawSpeed = wantedYaw + yawDeltaInput + targetAngularVelYaw;//в скобках растояние которое необходимо преодолеть за тик, остальное - за этот тик расстояние изменится
                    fullDriveYaw = false;
                }
                //PITCH
                double timeToStopPitch = Math.Abs((ownPitch - targetAngularVelPitch - pitchRotationInput) / maneuvrabilityPitch);
				double avaibleDistancePitch = wantedPitch + pitchDeltaInput - ownPitch + (targetAngularVelPitch + pitchRotationInput)* timeToStopPitch;

                double optimalAngularVelPitch = SOFTNAVCONST * Math.Sqrt(Math.Abs(2 * maneuvrabilityPitch * avaibleDistancePitch));

				if (wantedPitch + pitchDeltaInput < 0)
					optimalAngularVelPitch *= -1;
				optimalAngularVelPitch += targetAngularVelPitch;

				b = Math.Abs(wantedPitch + pitchDeltaInput) > maneuvrabilityPitch;
                if (b)//rough
                {
					if (ownPitch < optimalAngularVelPitch)
					{
						pitchSpeed = ownPitch + TURNGYROCONST;
						fullDrivePitch = true;
					}
					else
					{
						pitchSpeed = ownPitch - TURNGYROCONST;
						fullDrivePitch = true;
					}
				}
				else//soft
				{
					pitchSpeed = wantedPitch + pitchDeltaInput + targetAngularVelPitch;
                    fullDrivePitch = false;
                }
            }


            double rollSpeed = rollIndicator;
            lastShipMatrix = shipMatrix;
			lastTargetDir = targetDir;
			lastYaw = ownYaw;
			lastPitch = ownPitch;
			pitchSpeed *= 60;
			yawSpeed *= 60;
            ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, gyros, shipMatrix);
        }
		public void Drop(List<IMyGyro> gyroList)
		{
			_firstrun = true;

			DropGyro(gyroList);
            yawDeltaInput = 0;
			pitchDeltaInput = 0;
		}
        public static void DropGyro(List<IMyGyro> gyroList)
        {
            foreach (var thisGyro in gyroList)
            {
                thisGyro.GyroOverride = false;
            }
        }
        public static void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
		{
			var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
			var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
			foreach (var thisGyro in gyroList)
			{
				var transformedRotationVec = Vector3D.TransformNormal(
					relativeRotationVec,
					Matrix.Transpose(thisGyro.WorldMatrix)
				);
				thisGyro.Pitch = (float)transformedRotationVec.X;
				thisGyro.Yaw = (float)transformedRotationVec.Y;
				thisGyro.Roll = (float)transformedRotationVec.Z;
				thisGyro.GyroOverride = true;
			}
		}
	}
}
