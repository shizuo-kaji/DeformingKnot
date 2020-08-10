﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrawCurve;

public class Optimizer
{
    private Curve curve;
    private float length;
    private Loss loss;
    private Electricity electricity;
    private DiscreteMoebius discreteMoebius;
    private Elasticity elasticity;
    private float alpha = 0.95f;

    public Optimizer(Curve curve)
    {
        this.curve = curve;
        this.length = curve.positions.Count;
        this.loss = new Loss(curve.positions, 1e-08f);
        this.electricity = new Electricity(curve.positions, 1e-03f);
        this.discreteMoebius = new DiscreteMoebius(curve.positions, 1e-06f); // longitude 64, segment 0.03f -> 1e-05f
        this.elasticity = new Elasticity(curve.positions, 1.0f);
    }

    public void Flow()
    {
        List<Vector3> gradient = this.discreteMoebius.ModifiedGradient2();
        Debug.Log(this.curve.ArcLength());
        Debug.Log(this.curve.positions.Count);
        Debug.Log(this.discreteMoebius.ModifiedEnergy2());

        for (int i = 0; i < this.length; i++)
        {
            this.curve.positions[i] -= gradient[i];
        }
    }

    // momentum SGD
    public void MomentumFlow()
    {
        List<Vector3> gradient = this.discreteMoebius.ModifiedGradient();
        Debug.Log(this.curve.ArcLength());
        Debug.Log(this.curve.positions.Count);
        Debug.Log(this.discreteMoebius.ModifiedEnergy());

        for (int i = 0; i < this.length; i++)
        {
            this.curve.momentum[i] = this.alpha * this.curve.momentum[i] + gradient[i];
            this.curve.positions[i] -= this.curve.momentum[i];
        }
    }
}