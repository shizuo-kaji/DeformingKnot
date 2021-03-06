﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrawCurve;

// 暗黙の仮定：隣接する2点の間隔は一定
public class Moebius
{
    private List<List<Vector3>> pointsList;
    private List<List<Vector3>> momentumList;
    private List<int> countList;
    private List<float> segmentList;
    private int count;
    private float lr = 1e-04f; // longitude 64, segment 0.03f -> 1e-05f;
    private float alpha = 0.95f;
    private List<List<Vector3>> gradientList;

    public Moebius(List<Curve> curveList)
    {
        this.count = curveList.Count;
        this.pointsList = curveList.Select(curve => curve.points).ToList();
        this.momentumList = curveList.Select(curve => curve.momentum).ToList();
        this.segmentList = curveList.Select(curve => curve.segment).ToList();
        this.countList = new List<int>();
        for (int i = 0; i < this.count; i++)
        {
            this.countList.Add(this.pointsList[i].Count);
        }
        this.gradientList = Gradient();
    }

    public void Flow()
    {
        for (int i = 0; i < this.count; i++)
        {
            for (int j = 0; j < this.countList[i]; j++)
            {
                this.pointsList[i][j] -= this.gradientList[i][j];
                // if (this.gradientList[i][j].magnitude > 0.001f) Debug.Log(this.gradientList[i][j].magnitude);
            }
        }
    }

    // momentum SGD
    public void MomentumFlow()
    {
        for (int i = 0; i < this.count; i++)
        {
            for (int j = 0; j < this.countList[i]; j++)
            {
                this.momentumList[i][j] = this.alpha * this.momentumList[i][j] + this.gradientList[i][j];
                this.pointsList[i][j] -= this.momentumList[i][j];
                // if (this.momentumList[i][j].magnitude > 0.001f) Debug.Log(this.momentumList[i][j].magnitude);
            }
        }
    }

    public List<List<Vector3>> Gradient()
    {
        List<List<Vector3>> gradientList = new List<List<Vector3>>();

        for (int i1 = 0; i1 < this.count; i1++)
        {
            gradientList.Add(new List<Vector3>());
            for (int j1 = 0; j1 < this.countList[i1]; j1++)
            {
                Vector3 gradient = new Vector3();
                for (int j2 = 1; j2 < this.countList[i1]; j2++)
                {
                    int j3 = (j1 + j2) % this.countList[i1];
                    Vector3 first = this.CoulombDiff(this.pointsList[i1][j1], this.pointsList[i1][j3])
                                    * Mathf.Pow(this.segmentList[i1], 2);
                    Vector3 second = this.Coulomb(this.pointsList[i1][j1], this.pointsList[i1][j3])
                                     * 2 * this.segmentList[i1] * this.SegmentDiff(i1, j1);
                    gradient += 2 * (first + second);
                }
                for (int i2 = 1; i2 < this.count; i2++)
                {
                    int i3 = (i1 + i2) % this.count;
                    for (int j2 = 0; j2 < this.countList[i3]; j2++)
                    {
                        Vector3 first = this.CoulombDiff(this.pointsList[i1][j1], this.pointsList[i3][j2])
                                        * this.segmentList[i1] * this.segmentList[i3];
                        Vector3 second = this.Coulomb(this.pointsList[i1][j1], this.pointsList[i3][j2])
                                         * this.SegmentDiff(i1, j1) * this.segmentList[i3];
                        gradient += 2 * (first + second);
                    }
                }
                gradientList[i1].Add(this.lr * gradient);
            }
        }

        return gradientList;
    }

    private float Energy()
    {
        float energy = 0.0f;

        for (int i1 = 0; i1 < this.count; i1++)
        {
            for (int j1 = 0; j1 < this.countList[i1]; j1++)
            {
                for (int j2 = 1; j2 < this.countList[i1]; j2++)
                {
                    int j3 = (j1 + j2) % this.countList[i1];
                    energy += this.Coulomb(this.pointsList[i1][j1], this.pointsList[i1][j3])
                              * Mathf.Pow(this.segmentList[i1], 2);
                }

                for (int i2 = 1; i2 < this.count; i2++)
                {
                    int i3 = (i1 + i2) % this.count;
                    for (int j2 = 0; j2 < this.countList[i3]; j2++)
                    {
                        energy += this.Coulomb(this.pointsList[i1][j1], this.pointsList[i3][j2])
                                  * this.segmentList[i1] * this.segmentList[i3];
                    }
                }
            }
        }

        return energy;
    }

    private float Coulomb(Vector3 v, Vector3 w)
    {
        return 1 / Mathf.Pow(Vector3.Distance(v, w), 2);
    }

    private Vector3 CoulombDiff(Vector3 v, Vector3 w)
    {
        return - 2 * (v - w) / Mathf.Pow(Vector3.Distance(v, w), 4);
    }

    private float ArcLength(List<Vector3> points)
    {
        float arc = 0.0f;
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            arc += Vector3.Distance(points[i], points[(i + 1) % count]);
        }

        return arc;
    }

    private Vector3 SegmentDiff(int i, int j)
    {
        int jp = (j + 1) % this.countList[i];
        int jn = (j + this.countList[i] - 1) % this.countList[i];
        Vector3 tangentp = (this.pointsList[i][j] - this.pointsList[i][jp]).normalized;
        Vector3 tangentn = (this.pointsList[i][j] - this.pointsList[i][jn]).normalized;
        return (tangentp + tangentn) / this.countList[i];
    }
}