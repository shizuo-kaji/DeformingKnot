﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InputManager;
using DrawCurve;

interface IKnotState
{
    IKnotState Update();
    List<Vector3> GetPoints();
}

class KnotData
{
    public List<Vector3> points;
    public (int first, int second) chosenPoints;
    public readonly OculusTouch oculusTouch;
    public readonly int meridian;
    public readonly float radius;
    public readonly float distanceThreshold;
    public readonly List<Curve> collisionCurves;
    public readonly LogicalButton selectButton;
    public readonly LogicalButton cancelButton;
    public readonly LogicalButton optimizeButton;

    public KnotData(
        List<Vector3> points,
        (int first, int second) chosenPoints,
        OculusTouch oculusTouch,
        float radius,
        int meridian,
        float distanceThreshold,
        List<Curve> collisionCurves,
        LogicalButton selectButton,
        LogicalButton cancelButton,
        LogicalButton optimizeButton
        )
    {
        this.points = points;
        this.chosenPoints = chosenPoints;
        this.oculusTouch = oculusTouch;
        this.meridian = meridian;
        this.radius = radius;
        this.distanceThreshold = distanceThreshold;
        this.collisionCurves = collisionCurves;
        this.selectButton = selectButton;
        this.cancelButton = cancelButton;
        this.optimizeButton = optimizeButton;
    }
}



class KnotStateBase : IKnotState
{
    private KnotData data;
    private Mesh knotMesh;

    public KnotStateBase(KnotData data)
    {
        this.data = data;
        this.knotMesh = MakeMesh.GetMesh(this.data.points, this.data.meridian, this.data.radius, true);
    }

    public IKnotState Update()
    {
        Graphics.DrawMesh(this.knotMesh, Vector3.zero, Quaternion.identity, MakeMesh.SelectedCurveMaterial, 0);

        if (this.data.oculusTouch.GetButtonDown(this.data.selectButton))
        {
            return new KnotStatePull(this.data);
        }
        else if (this.data.oculusTouch.GetButtonDown(this.data.cancelButton))
        {
            return new KnotStateChoose1(this.data);
        }
        else if (this.data.oculusTouch.GetButtonDown(this.data.optimizeButton)
                || this.data.oculusTouch.GetButtonDown(LogicalOVRInput.RawButton.RHandTrigger))
        {
            return new KnotStateOptimize(this.data);
        }

        return null;
    }

    public List<Vector3> GetPoints()
    {
        return this.data.points;
    }
}

class KnotStatePull : IKnotState
{
    private KnotData data;
    private List<Curve> collisionCurves;
    private PullableCurve pullableCurve;

    public KnotStatePull(KnotData data)
    {
        this.data = data;
        this.collisionCurves = this.data.collisionCurves;
        this.pullableCurve = new PullableCurve(this.data.points,
                                                this.data.chosenPoints,
                                                this.data.oculusTouch,
                                                meridian: this.data.meridian,
                                                radius: this.data.radius,
                                                distanceThreshold: this.data.distanceThreshold);
    }

    public IKnotState Update()
    {
        // List<Vector3> collisionPoints = this.collisionPoints;
        // List<Vector3> collisionPoints = this.GetCompliment(this.chosenPoints.first, this.chosenPoints.second);
        // collisionPoints = collisionPoints.Concat(this.collisionPoints).ToList();
        this.pullableCurve.Update(this.collisionCurves);
        Mesh knotMesh = this.pullableCurve.GetMesh();
        // Mesh pointsMesh = MakeMesh.GetMeshAtPoints(this.pullableCurve.GetPoints(), this.data.radius * 3); 
        Graphics.DrawMesh(knotMesh, Vector3.zero, Quaternion.identity, MakeMesh.SelectedCurveMaterial, 0);
        // Graphics.DrawMesh(pointsMesh, Vector3.zero, Quaternion.identity, MakeMesh.PointMaterial, 0);
        // this.pointMesh = MakeMesh.GetMeshAtPoints(collisionPoints, this.radius * 2);

        if (this.data.oculusTouch.GetButtonDown(this.data.selectButton))
        {
            this.data.points = this.pullableCurve.GetPoints();
            this.data.chosenPoints = (0, this.pullableCurve.GetCount() - 1);
            return new KnotStateBase(this.data);
        }
        else if (this.data.oculusTouch.GetButton(this.data.cancelButton))
        {
            return new KnotStateBase(this.data);
        }

        return null;
    }

    public List<Vector3> GetPoints()
    {
        return this.pullableCurve.GetPoints();
    }

    private List<Vector3> GetCompliment(int start, int end)
    {
        int numPoints = this.data.points.Count;
        int margin = 2;
        if (start <= end)
        {
            List<Vector3> range1 = this.data.points.GetRange(end + margin, numPoints - end - margin);
            List<Vector3> range2 = this.data.points.GetRange(0, start - margin);
            return range1.Concat(range2).ToList();
        }
        else
        {
            return this.data.points.GetRange(end + margin, start - end - margin);
        }
    }
}

class KnotStateChoose1 : IKnotState
{
    private KnotData data;
    private Mesh knotMesh;

    public KnotStateChoose1(KnotData data)
    {
        this.data = data;
        this.knotMesh = MakeMesh.GetMesh(this.data.points, this.data.meridian, this.data.radius, true);
    }

    public IKnotState Update()
    {
        int ind1 = KnotStateChoose1.FindClosestPoint(this.data.oculusTouch, this.data.points);
        var chosenPoints = new List<Vector3>() { this.data.points[ind1] };
        Mesh pointMesh = MakeMesh.GetMeshAtPoints(chosenPoints, this.data.radius * 3);

        Graphics.DrawMesh(this.knotMesh, Vector3.zero, Quaternion.identity, MakeMesh.SelectedCurveMaterial, 0);
        Graphics.DrawMesh(pointMesh, Vector3.zero, Quaternion.identity, MakeMesh.PointMaterial, 0);

        if (this.data.oculusTouch.GetButtonDown(this.data.selectButton))
        {
            return new KnotStateChoose2(this.data, ind1);
        }
        else if (this.data.oculusTouch.GetButtonDown(this.data.cancelButton))
        {
            return new KnotStateBase(this.data);
        }

        return null;
    }

    public List<Vector3> GetPoints()
    {
        return this.data.points;
    }

    public static int FindClosestPoint(OculusTouch oculusTouch, List<Vector3> points)
    {
        // KnotStateChoose2 からも呼び出せるように static メソッドにした
        Vector3 oculusTouchPosition = oculusTouch.GetPositionR();
        int closestIndex = 0;
        float closestDistance = Vector3.Distance(points[closestIndex], oculusTouchPosition);
        for (int i = 1; i < points.Count; i++)
        {
            float distance = Vector3.Distance(points[i], oculusTouchPosition);
            if (distance < closestDistance)
            {
                closestIndex = i;
                closestDistance = distance;
            }
        }
        return closestIndex;
    }
}

class KnotStateChoose2 : IKnotState
{
    private KnotData data;
    private Mesh knotMesh;
    private int ind1;

    public KnotStateChoose2(KnotData data, int ind1)
    {
        this.data = data;
        this.knotMesh = MakeMesh.GetMesh(this.data.points, this.data.meridian, this.data.radius, true);
        this.ind1 = ind1;
    }

    public IKnotState Update()
    {
        int ind2 = KnotStateChoose1.FindClosestPoint(this.data.oculusTouch, this.data.points);
        var chosenPoints = new List<Vector3>() {
                    this.data.points[this.ind1],
                    this.data.points[ind2]
                };
        Mesh pointMesh = MakeMesh.GetMeshAtPoints(chosenPoints, this.data.radius * 3);

        Graphics.DrawMesh(this.knotMesh, Vector3.zero, Quaternion.identity, MakeMesh.SelectedCurveMaterial, 0);
        Graphics.DrawMesh(pointMesh, Vector3.zero, Quaternion.identity, MakeMesh.PointMaterial, 0);

        if (this.data.oculusTouch.GetButtonDown(this.data.selectButton))
        {
            this.data.chosenPoints = KnotStateChoose2.ChooseShorterPath(
                (this.ind1, ind2), this.data.points.Count);
            // this.chosenPoints = this.currentPoints;  // ←そのままの順序で選ぶ場合
            return new KnotStateBase(this.data);
        }
        else if (this.data.oculusTouch.GetButtonDown(this.data.cancelButton))
        {
            return new KnotStateBase(this.data);
        }

        return null;
    }

    public List<Vector3> GetPoints()
    {
        return this.data.points;
    }

    private static (int first, int second) ChooseShorterPath((int first, int second) points, int numPoints)
    {
        int smaller = Mathf.Min(points.first, points.second);
        int larger = Mathf.Max(points.first, points.second);
        if (2 * (larger - smaller) <= numPoints)
        {
            return (smaller, larger);
        }
        else
        {
            return (larger, smaller);
        }
    }
}

class KnotStateOptimize : IKnotState
{
    private KnotData data;
    private List<Vector3> newPoints;
    private List<Vector3> momentum;

    public KnotStateOptimize(KnotData data)
    {
        this.data = data;
        this.newPoints = data.points;
        this.newPoints = AdjustParameter.Equalize(this.newPoints, this.data.distanceThreshold, true);
        this.momentum = new List<Vector3>();

        for (int i = 0; i < this.newPoints.Count; i++)
        {
            this.momentum.Add(Vector3.zero);
        }
    }

    public IKnotState Update()
    {
        bool selfIntersection = false;
        if (this.newPoints.Count >= 4
            && PullableCurve.MinSegmentDist(this.newPoints, true) <= this.data.distanceThreshold * 0.2f)
        {
            selfIntersection = true;
        }
        foreach (Curve curve in this.data.collisionCurves)
        {
            if (PullableCurve.CurveDistance(this.newPoints, true, curve) <= this.data.distanceThreshold * 0.2f)
            selfIntersection = true;
        }

        if (this.data.oculusTouch.GetButton(this.data.optimizeButton) && !selfIntersection)
        {
            this.Optimize();
        }
        else if (this.data.oculusTouch.GetButtonUp(this.data.optimizeButton))
        {
            this.momentum = new List<Vector3>();
            for (int i = 0; i < this.newPoints.Count; i++)
            {
                this.momentum.Add(Vector3.zero);
            }
        }

        if (this.data.oculusTouch.GetButton(LogicalOVRInput.RawButton.RHandTrigger) && !selfIntersection)
        {
            DiscreteMoebius optimizer1 = new DiscreteMoebius(this.newPoints, this.momentum);
            optimizer1.Flow();

            while (true)
            {
                Elasticity optimizer2 = new Elasticity(this.newPoints, this.momentum, this.data.distanceThreshold);
                if (optimizer2.MaxError() < this.data.distanceThreshold * 0.1f) break;
                optimizer2.Flow();
            }
        }

        Mesh knotMesh = MakeMesh.GetMesh(this.newPoints, this.data.meridian, this.data.radius, true);
        Graphics.DrawMesh(knotMesh, Vector3.zero, Quaternion.identity, MakeMesh.SelectedCurveMaterial, 0);

        if (this.data.oculusTouch.GetButtonDown(this.data.selectButton))
        {
            this.data.points = this.newPoints;
            return new KnotStateBase(this.data);
        }
        else if (this.data.oculusTouch.GetButtonDown(this.data.cancelButton))
        {
            return new KnotStateBase(this.data);
        }

        return null;
    }

    public List<Vector3> GetPoints()
    {
        return this.newPoints;
    }

    private void Optimize()
    {
        /*DiscreteMoebius optimizer1 = new DiscreteMoebius(this.newPoints, this.momentum);

        for (int i = 0; i < this.newPoints.Count; i++)
        {
            this.newPoints[i] -= this.momentum[i] + optimizer1.gradient[i];
        }

        List<Vector3> tempPoints = new List<Vector3>();

        for (int i = 0; i < this.newPoints.Count; i++)
        {
            tempPoints.Add(this.newPoints[i]);
        }

        while (true)
        {
            Elasticity optimizer2 = new Elasticity(this.newPoints, this.momentum, this.data.distanceThreshold);
            if (optimizer2.MaxError() < this.data.distanceThreshold * 0.1f) break;
            optimizer2.Flow();
        }

        for (int i = 0; i < this.newPoints.Count; i++)
        {
            this.momentum[i] = (this.momentum[i] + optimizer1.gradient[i]) * 0.95f
                                + (tempPoints[i] - this.newPoints[i]) * 0.3f;
        }*/

        DiscreteMoebius optimizer1 = new DiscreteMoebius(this.newPoints, this.momentum);
        optimizer1.MomentumFlow();

        while (true)
        {
            Elasticity optimizer2 = new Elasticity(this.newPoints, this.momentum, this.data.distanceThreshold);
            if (optimizer2.MaxError() < this.data.distanceThreshold * 0.1f) break;
            optimizer2.Flow();
        }
    }
}