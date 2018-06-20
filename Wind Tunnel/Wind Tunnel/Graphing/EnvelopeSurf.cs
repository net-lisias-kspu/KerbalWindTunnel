﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace KerbalWindTunnel.Graphing
{
    public static class EnvelopeSurf
    {
        public static EnvelopePoint[,] envelopePoints = new EnvelopePoint[0, 0];
        private static CalculationManager calculationManager = new CalculationManager();
        public static CalculationManager.RunStatus Status
        {
            get
            {
                CalculationManager.RunStatus status = calculationManager.Status;
                if (status == CalculationManager.RunStatus.Completed && !valuesSet)
                    return CalculationManager.RunStatus.Running;
                if (status == CalculationManager.RunStatus.PreStart && valuesSet)
                    return CalculationManager.RunStatus.Completed;
                return status;
            }
        }
        public static float PercentComplete
        {
            get { return calculationManager.PercentComplete; }
        }
        private static bool valuesSet = false;
        public static Conditions currentConditions = Conditions.Blank;
        private static Dictionary<Conditions, EnvelopePoint[,]> cache = new Dictionary<Conditions, EnvelopePoint[,]>();

        public static void Cancel()
        {
            calculationManager.Cancel();
            calculationManager = new CalculationManager();
            valuesSet = false;
        }
        public static void Clear()
        {
            calculationManager.Cancel();
            calculationManager = new CalculationManager();
            currentConditions = Conditions.Blank;
            cache.Clear();
            envelopePoints = new EnvelopePoint[0,0];
        }

        public static void Calculate(AeroPredictor vessel, CelestialBody body, float lowerBoundSpeed = 0, float upperBoundSpeed = 2000, float stepSpeed = 50f, float lowerBoundAltitude = 0, float upperBoundAltitude = 60000, float stepAltitude = 500)
        {
            Conditions newConditions = new Conditions(body, lowerBoundSpeed, upperBoundSpeed, stepSpeed, lowerBoundAltitude, upperBoundAltitude, stepAltitude);
            if (newConditions.Equals(currentConditions))
            {
                valuesSet = true;
                return;
            }

            Cancel();

            if (!cache.TryGetValue(newConditions, out envelopePoints))
            {
                WindTunnel.Instance.StartCoroutine(Processing(calculationManager, newConditions, vessel, WindTunnelWindow.Instance.rootSolver));
            }
            else
            {
                currentConditions = newConditions;
                calculationManager.Status = CalculationManager.RunStatus.Completed;
                valuesSet = true;
            }
        }

        private static IEnumerator Processing(CalculationManager manager, Conditions conditions, AeroPredictor vessel, RootSolvers.RootSolver solver)
        {
            int numPtsX = (int)Math.Ceiling((conditions.upperBoundSpeed - conditions.lowerBoundSpeed) / conditions.stepSpeed);
            int numPtsY = (int)Math.Ceiling((conditions.upperBoundAltitude - conditions.lowerBoundAltitude) / conditions.stepAltitude);
            EnvelopePoint[,] newEnvelopePoints = new EnvelopePoint[numPtsX + 1, numPtsY + 1];
            float trueStepX = (conditions.upperBoundSpeed - conditions.lowerBoundSpeed) / numPtsX;
            float trueStepY = (conditions.upperBoundAltitude - conditions.lowerBoundAltitude) / numPtsY;
            CalculationManager.State[,] results = new CalculationManager.State[numPtsX + 1, numPtsY + 1];

            for (int j = 0; j <= numPtsY; j++)
            {
                for (int i = 0; i <= numPtsX; i++)
                {
                    //newAoAPoints[i] = new AoAPoint(vessel, conditions.body, conditions.altitude, conditions.speed, conditions.lowerBound + trueStep * i);
                    GenData genData = new GenData(vessel, conditions, conditions.lowerBoundSpeed + trueStepX * i, conditions.lowerBoundAltitude + trueStepY * j, solver, manager);
                    results[i, j] = genData.storeState;
                    ThreadPool.QueueUserWorkItem(GenerateAoAPoint, genData);
                }
            }

            while (!manager.Completed)
            {
                //Debug.Log(manager.PercentComplete + "% done calculating...");
                if (manager.Status == CalculationManager.RunStatus.Cancelled)
                    yield break;
                yield return 0;
            }

            for (int j = 0; j <= numPtsY; j++)
            {
                for (int i = 0; i <= numPtsX; i++)
                {
                    newEnvelopePoints[i, j] = (EnvelopePoint)results[i, j].Result;
                }
            }
            if (!manager.Cancelled)
            {
                cache.Add(conditions, newEnvelopePoints);
                envelopePoints = newEnvelopePoints;
                currentConditions = conditions;
                valuesSet = true;
            }
        }
        private static void GenerateAoAPoint(object obj)
        {
            GenData data = (GenData)obj;
            if (data.storeState.manager.Cancelled)
                return;
            //Debug.Log("Starting point: " + data.altitude + "/" + data.speed);
            EnvelopePoint result = new EnvelopePoint(data.vessel, data.conditions.body, data.altitude, data.speed, data.solver);
            //Debug.Log("Point solved: " + data.altitude + "/" + data.speed);

            data.storeState.StoreResult(result);
        }

        private struct GenData
        {
            public readonly Conditions conditions;
            public readonly AeroPredictor vessel;
            public readonly CalculationManager.State storeState;
            public readonly RootSolvers.RootSolver solver;
            public readonly float speed;
            public readonly float altitude;

            public GenData(AeroPredictor vessel, Conditions conditions, float speed, float altitude, RootSolvers.RootSolver solver, CalculationManager manager)
            {
                this.vessel = vessel;
                this.conditions = conditions;
                this.speed = speed;
                this.altitude = altitude;
                this.solver = solver;
                this.storeState = manager.GetStateToken();
            }
        }
        public struct EnvelopePoint
        {
            public readonly float AoA_level;
            public readonly float Thrust_excess;
            public readonly float Accel_excess;
            public readonly float Lift_max;
            public readonly float AoA_max;
            public readonly float Thrust_available;
            public readonly float altitude;
            public readonly float speed;
            public readonly float LDRatio;
            public readonly Vector3 force;
            public readonly Vector3 liftforce;

            public EnvelopePoint(AeroPredictor vessel, CelestialBody body, float altitude, float speed, RootSolvers.RootSolver solver, float AoA_guess = 0)
            {
                this.altitude = altitude;
                this.speed = speed;
                float atmPressure, atmDensity, mach, gravParameter, radius;
                bool oxygenAvailable;
                lock (body)
                {
                    atmPressure = (float)body.GetPressure(altitude);
                    atmDensity = (float)Extensions.KSPClassExtensions.GetDensity(body, altitude);
                    mach = (float)(speed / body.GetSpeedOfSound(atmPressure, atmDensity));
                    oxygenAvailable = body.atmosphereContainsOxygen;
                    gravParameter = (float)body.gravParameter;
                    radius = (float)body.Radius;
                }
                float weight = (vessel.Mass * gravParameter / ((radius + altitude) * (radius + altitude))); // TODO: Minus centrifugal force...
                Vector3 thrustForce = vessel.GetThrustForce(mach, atmDensity, atmPressure, oxygenAvailable);
                AoA_level = solver.Solve(
                    (aoa) =>
                        AeroPredictor.GetLiftForceMagnitude(
                            vessel.GetLiftForce(body, speed, altitude, aoa, mach, atmDensity) + thrustForce, aoa)
                        - weight,
                    0, WindTunnelWindow.Instance.solverSettings);
                AoA_max = float.NaN;
                Lift_max = float.NaN;
                Thrust_available = thrustForce.magnitude;
                if (!float.IsNaN(AoA_level))
                {
                    force = vessel.GetAeroForce(body, speed, altitude, AoA_level, mach, atmDensity);
                    liftforce = AeroPredictor.ToFlightFrame(force, AoA_level); //vessel.GetLiftForce(body, speed, altitude, AoA_level, mach, atmDensity);
                    float drag = AeroPredictor.GetDragForceMagnitude(force, AoA_level);
                    Thrust_excess = -drag - AeroPredictor.GetDragForceMagnitude(thrustForce, AoA_level);
                    Accel_excess = (Thrust_excess / weight);
                    LDRatio = Mathf.Abs(weight / drag);
                }
                else
                {
                    force = vessel.GetAeroForce(body, speed, altitude, 30, mach, atmDensity);
                    liftforce = force; // vessel.GetLiftForce(body, speed, altitude, 30, mach, atmDensity);
                    Thrust_excess = float.NegativeInfinity;
                    Accel_excess = float.NegativeInfinity;
                    LDRatio = 0;// weight / AeroPredictor.GetDragForceMagnitude(force, 30);
                }
            }
        }

        public struct Conditions : IEquatable<Conditions>
        {
            public readonly CelestialBody body;
            public readonly float lowerBoundSpeed;
            public readonly float upperBoundSpeed;
            public readonly float stepSpeed;
            public readonly float lowerBoundAltitude;
            public readonly float upperBoundAltitude;
            public readonly float stepAltitude;

            public static readonly Conditions Blank = new Conditions(null, 0, 0, 0, 0, 0, 0);

            public Conditions(CelestialBody body, float lowerBoundSpeed, float upperBoundSpeed, float stepSpeed, float lowerBoundAltitude, float upperBoundAltitude, float stepAltitude)
            {
                this.body = body;
                if (body != null && lowerBoundAltitude > body.atmosphereDepth)
                    lowerBoundAltitude = upperBoundAltitude = (float)body.atmosphereDepth;
                if (body != null && upperBoundAltitude > body.atmosphereDepth)
                    upperBoundAltitude = (float)body.atmosphereDepth;
                this.lowerBoundSpeed = lowerBoundSpeed;
                this.upperBoundSpeed = upperBoundSpeed;
                this.stepSpeed = stepSpeed;
                this.lowerBoundAltitude = lowerBoundAltitude;
                this.upperBoundAltitude = upperBoundAltitude;
                this.stepAltitude = stepAltitude;
            }

            public bool Equals(Conditions conditions)
            {
                return this.body == conditions.body &&
                    this.lowerBoundSpeed == conditions.lowerBoundSpeed &&
                    this.upperBoundSpeed == conditions.upperBoundSpeed &&
                    this.stepSpeed == conditions.stepSpeed &&
                    this.lowerBoundAltitude == conditions.lowerBoundAltitude &&
                    this.upperBoundAltitude == conditions.upperBoundAltitude &&
                    this.stepAltitude == conditions.stepAltitude;
            }

            public override int GetHashCode()
            {
                return Extensions.HashCode.Of(this.body).And(this.lowerBoundSpeed).And(this.upperBoundSpeed).And(this.stepSpeed).And(this.lowerBoundAltitude).And(this.upperBoundAltitude).And(this.stepAltitude);
            }
        }
    }
}
