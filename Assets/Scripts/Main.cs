﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DrawCurve;
using DebugUtil;

public class Main : MonoBehaviour
{
    private Controller controller;
    private State state;
    private ButtonConfig button;
    private List<Curve> curves;
    private List<Curve> preCurves;
    private Curve drawingCurve;
    private List<int> movingCurves;
    private Knot deformingCurve;
    private string text;

    // Start is called before the first frame update
    void Start()
    {
        MyController.SetUp(ref controller);
        state = State.Base;
        button = new ButtonConfig(controller);
        Player.SetUp(controller, button);
        Curve.SetUp(controller, button.draw, button.move);

        curves = new List<Curve>();
        preCurves = new List<Curve>();
        drawingCurve = new Curve(new List<Vector3>(), false);
        movingCurves = new List<int>();
    }

    // Update is called once per frame
    void Update()
    {
        MyController.Update(this.controller);
        text = state.ToString();

        if (state == State.Base && button.ValidBaseButtonInput())
        {
            Player.DeepCopy(curves, ref preCurves);
            Player.ChangeState(ref curves, ref state, ref deformingCurve);
            Player.Draw(ref drawingCurve, ref curves);
            Player.Move(curves, ref movingCurves);
            Player.Select(curves);
            Player.Cut(ref curves);
            Player.Combine(ref curves);
            Player.Remove(ref curves);
            Player.Undo(ref curves, preCurves);
        }
        else if (state == State.ContiDeform)
        {
            Player.ChangeState(ref curves, ref state, ref deformingCurve);
            deformingCurve.Update();
        }

        Player.Display(curves);
    }

    public void UpdateFixedInterface(FixedInterface.FixedInterfaceSetting setting)
    {
        setting.text = text;
        if (state == State.ContiDeform)
        {
            deformingCurve.UpdateFixedInterface(setting);
        }
    }
}