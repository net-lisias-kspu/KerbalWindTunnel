﻿using System.Collections.Generic;
using KerbalWindTunnel.Extensions;
using Smooth.Pools;
using UnityEngine;

namespace KerbalWindTunnel.VesselCache
{
    public class SimulatedLiftingSurface
    {
        private static readonly Pool<SimulatedLiftingSurface> pool = new Pool<SimulatedLiftingSurface>(Create, Reset);

        public Vector3 liftVector;
        public bool omnidirectional;
        public bool perpendicularOnly;
        public FloatCurve liftCurve;
        public FloatCurve liftMachCurve;
        public FloatCurve dragCurve;
        public FloatCurve dragMachCurve;
        public float deflectionLiftCoeff;
        public bool useInternalDragModel;
        public SimulatedPart part;

        private static SimulatedLiftingSurface Create()
        {
            SimulatedLiftingSurface surface = new SimulatedLiftingSurface();
            return surface;
        }

        private static void Reset(SimulatedLiftingSurface surface) { }

        public void Release()
        {
            pool.Release(this);
        }

        public static void Release(List<SimulatedLiftingSurface> objList)
        {
            for (int i = 0; i < objList.Count; ++i)
            {
                objList[i].Release();
            }
        }

        public static SimulatedLiftingSurface Borrow(ModuleLiftingSurface module, SimulatedPart part)
        {
            SimulatedLiftingSurface surface = pool.Borrow();
            surface.Init(module, part);
            return surface;
        }

        protected void Init(ModuleLiftingSurface surface, SimulatedPart part)
        {
            surface.SetupCoefficients(Vector3.forward, out Vector3 nVel, out this.liftVector, out float liftDot, out float absDot);
            this.omnidirectional = surface.omnidirectional;
            this.perpendicularOnly = surface.perpendicularOnly;
            this.liftCurve = surface.liftCurve.Clone();
            this.liftMachCurve = surface.liftMachCurve.Clone();
            this.dragCurve = surface.dragCurve.Clone();
            this.dragMachCurve = surface.dragMachCurve.Clone();
            this.deflectionLiftCoeff = surface.deflectionLiftCoeff;
            this.useInternalDragModel = surface.useInternalDragModel;
            this.part = part;
        }

        public Vector3 GetLift(Vector3 velocityVect, float mach)
        {
            float dot = Vector3.Dot(velocityVect, liftVector);
            float absdot = omnidirectional ? Mathf.Abs(dot) : Mathf.Clamp01(dot);
            Vector3 lift = Vector3.zero;
            lock (this.liftCurve)
                lift = -liftVector * Mathf.Sign(dot) * liftCurve.Evaluate(absdot) * liftMachCurve.Evaluate(mach) * deflectionLiftCoeff * PhysicsGlobals.LiftMultiplier;
            if (perpendicularOnly)
                lift = Vector3.ProjectOnPlane(lift, -velocityVect);
            return lift * 1000;
        }

        public Vector3 GetForce(Vector3 velocityVect, float mach)
        {
            float dot = Vector3.Dot(velocityVect, liftVector);
            float absdot = omnidirectional ? Mathf.Abs(dot) : Mathf.Clamp01(dot);
            Vector3 lift = Vector3.zero;
            lock (this.liftCurve)
                lift = -liftVector * Mathf.Sign(dot) * liftCurve.Evaluate(absdot) * liftMachCurve.Evaluate(mach) * deflectionLiftCoeff * PhysicsGlobals.LiftMultiplier;
            if (perpendicularOnly)
                lift = Vector3.ProjectOnPlane(lift, -velocityVect);
            if (!useInternalDragModel)
                return lift * 1000;
            Vector3 drag = Vector3.zero;
            lock (this.dragCurve)
                drag = -velocityVect * dragCurve.Evaluate(absdot) * dragMachCurve.Evaluate(mach) * deflectionLiftCoeff * PhysicsGlobals.LiftDragMultiplier;

            return (lift + drag) * 1000;
        }
    }
}
