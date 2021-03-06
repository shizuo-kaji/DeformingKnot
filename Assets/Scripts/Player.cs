﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrawCurve;
using InputManager;
using ContextMenu;
using FileManager;
using PullCurve;

public abstract class State
{
    protected OculusTouch oculusTouch;
    protected ContextMenu.ContextMenu contextMenu;
    protected DataHandler dataHandler;
    protected int NumberOfDefaultItems = 4;
    protected int NumberOfUnselectableItems;
    protected List<Curve> curves;
    protected float segment = 0.03f;
    protected float epsilon;
    public State newState;

    public State(OculusTouch oculusTouch, ContextMenu.ContextMenu contextMenu, DataHandler dataHandler, List<Curve> curves)
    {
        this.oculusTouch = oculusTouch;
        this.contextMenu = contextMenu;
        this.dataHandler = dataHandler;
        this.SetupMenu();
        this.curves = curves;
        this.epsilon = this.segment * 0.2f;
        this.newState = null;
    }

    protected abstract void SetupMenu();
    protected void ResetMenu()
    {
        List<MenuItem> removedItems = this.contextMenu.FindAllItems((item) => this.contextMenu.items.IndexOf(item) >= this.NumberOfDefaultItems);
        foreach (MenuItem item in removedItems)
        {
            this.contextMenu.RemoveItem(item);
        }
        this.contextMenu.cursorIndex = 0;
    }

    public void RestrictCursorPosition()
    {
        if (this.contextMenu.cursorIndex == 0)
        {
            this.contextMenu.cursorIndex = this.NumberOfUnselectableItems;
        }
        else if (this.contextMenu.cursorIndex < this.NumberOfUnselectableItems)
        {
            this.contextMenu.cursorIndex = - 1;
        }
    }

    public abstract void Update();

    public void Display()
    {
        foreach (Curve curve in this.curves)
        {
            Material material = curve.selected ? MakeMesh.SelectedCurveMaterial : MakeMesh.CurveMaterial;
            Graphics.DrawMesh(curve.mesh, curve.position, curve.rotation, material, 0);
        }
    }
}

public class BasicDeformation : State
{
    private List<Curve> preCurves;
    private Curve drawingCurve;
    private List<int> movingCurves;

    private LogicalButton draw;
    private LogicalButton move;
    private LogicalButton select;
    private LogicalButton cut;
    private LogicalButton comfirm;

    public BasicDeformation(OculusTouch oculusTouch,
                            ContextMenu.ContextMenu contextMenu,
                            DataHandler dataHandler,
                            List<Curve> curves,
                            LogicalButton changeState = null,
                            LogicalButton draw = null,
                            LogicalButton move = null,
                            LogicalButton select = null,
                            LogicalButton cut = null,
                            LogicalButton comfirm = null)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 9;
        this.preCurves = base.curves;
        this.drawingCurve = new Curve(new List<Vector3>(), false);
        this.movingCurves = new List<int>();

        if (draw != null) this.draw = draw;
        else this.draw = LogicalOVRInput.RawButton.RIndexTrigger;

        if (move != null) this.move = move;
        else this.move = LogicalOVRInput.RawButton.RHandTrigger;

        if (select != null) this.select = select;
        else this.select = LogicalOVRInput.RawButton.A;

        if (cut != null) this.cut = cut;
        else this.cut = LogicalOVRInput.RawButton.B;

        if (comfirm != null) this.comfirm = comfirm;
        else this.comfirm = LogicalOVRInput.RawButton.X;

        Curve.SetUp(base.oculusTouch, this.draw, this.move);
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("右人差し指 : 描画", () => {}));
        this.contextMenu.AddItem(new MenuItem("右中指 : 平行移動, 回転", () => {}));
        this.contextMenu.AddItem(new MenuItem("Aボタン : 選択", () => {}));
        this.contextMenu.AddItem(new MenuItem("Bボタン : 選択中の曲線を切断 (曲線は1つ選択)", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("選択中の曲線を結合 (曲線は1つか2つ選択)", () => {
            this.Combine();
        }));
        this.contextMenu.AddItem(new MenuItem("選択中の曲線を削除", () => {
            this.Remove();
        }));
        this.contextMenu.AddItem(new MenuItem("連続変形 (全て閉曲線にしておく)", () => {
            this.ChangeState();
        }));
        this.contextMenu.AddItem(new MenuItem("元に戻す", () => {
            this.Undo();
        }));
        this.contextMenu.AddItem(new MenuItem("保存", () => {
            this.Save();
        }));
        this.contextMenu.AddItem(new MenuItem("開く", () => {
            this.Open();
        }));
    }

    public override void Update()
    {
        if (this.ValidButtonInput())
        {
            this.DeepCopy();
            this.Draw();
            this.Move();
            this.Select();
            this.Cut();
        }
    }

    private bool ValidButtonInput()
    {
        int valid = 0;

        if (base.oculusTouch.GetButton(this.draw) || this.oculusTouch.GetButtonUp(this.draw)) valid++;
        if (base.oculusTouch.GetButton(this.move) || this.oculusTouch.GetButtonUp(this.move)) valid++;
        if (base.oculusTouch.GetButtonDown(this.select)) valid++;
        if (base.oculusTouch.GetButtonDown(this.cut)) valid++;
        if (base.oculusTouch.GetButtonDown(this.comfirm)) valid++; 

        return (valid == 1) ? true : false;
    }

    private void DeepCopy()
    {
        if (base.oculusTouch.GetButtonDown(this.draw)
            || base.oculusTouch.GetButtonDown(this.move)
            || base.oculusTouch.GetButtonDown(this.select)
            || base.oculusTouch.GetButtonDown(this.cut)
            || (base.oculusTouch.GetButtonDown(this.comfirm) && (this.contextMenu.cursorIndex != 12)))
        {
            this.preCurves = new List<Curve>();

            foreach (Curve curve in this.curves)
            {
                this.preCurves.Add(curve.DeepCopy());
            }
        }
    }

    private void Draw()
    {
        this.drawingCurve.Draw();

        if (base.oculusTouch.GetButtonUp(this.draw))
        {
            if (this.drawingCurve.points.Count >= 2)
            {
                base.curves.Add(this.drawingCurve);
            }

            this.drawingCurve = new Curve(new List<Vector3>(), false, segment: base.segment);
        }
        
        if (this.drawingCurve.points.Count >= 2)
        {
            Graphics.DrawMesh(this.drawingCurve.mesh, this.drawingCurve.position, this.drawingCurve.rotation, MakeMesh.CurveMaterial, 0);
        }
    }

    private void Move()
    {
        Vector3 nowPosition = base.oculusTouch.GetPositionR();

        if (base.oculusTouch.GetButtonDown(this.move))
        {
            for (int i = 0; i < base.curves.Count; i++)
            {
                if (Curve.Distance(base.curves[i].points, nowPosition).Item2 < Curve.collision)
                {
                    this.movingCurves.Add(i);
                }
            }
        }

        foreach (int i in this.movingCurves)
        {
            base.curves[i].Move();
        }
        
        if (base.oculusTouch.GetButtonUp(this.move))
        {
            this.movingCurves = new List<int>();
        }
    }

    private void Select()
    {
        if (base.oculusTouch.GetButtonDown(this.select))
        {
            for (int i = 0; i < base.curves.Count; i++)
            {
                base.curves[i].Select();
            }
        }
    }

    private void Cut()
    {
        if (base.oculusTouch.GetButtonDown(this.cut))
        {
            List<Curve> selection = base.curves.Where(curve => curve.selected).ToList();

            if (selection.Count == 1)
            {
                List<Curve> newCurves = selection[0].Cut();

                if (newCurves.Count != 0)
                {
                    base.curves.Remove(selection[0]);
                    
                    foreach (Curve curve in newCurves)
                    {
                        base.curves.Add(curve);
                    }
                }
            }
        }
    }

    public void Combine()
    {
        List<Curve> selection = base.curves.Where(curve => curve.selected).ToList();

        if (selection.Count == 1)
        {
            selection[0].Close();
        }
        else if (selection.Count == 2 && !selection[0].closed && !selection[1].closed)
        {
            List<Curve> newCurves = Curve.Combine(selection[0], selection[1]);

            if (newCurves.Count != 0)
            {
                base.curves.Remove(selection[0]);
                base.curves.Remove(selection[1]);
                base.curves.Add(newCurves[0]);
            }
        }
    }

    private void Remove()
    {
        base.curves = base.curves.Where(curve => !curve.selected).ToList();
    }

    private void Undo()
    {
        base.curves = this.preCurves;
    }

    private void ChangeState()
    {
        bool closed = true;
        foreach (Curve curve in base.curves)
        {
            if (!curve.closed) closed = false;
        }

        if (closed && !this.HaveInterSections())
        {
            base.ResetMenu();
            this.newState = new SelectAutoOrManual(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }
    }

    private bool HaveInterSections()
    {
        for (int i = 0; i < base.curves.Count; i++)
        {
            for (int j = i + 1; j < base.curves.Count; j++)
            {
                if (base.curves[i].CurveDistance(base.curves[j]) < base.epsilon) return true;
            }
        }

        return false;
    }

    private void Save()
    {
        string timeString = this.GetCurrentTimeString();
        string filename = $"curve_{timeString}.json";
        List<(List<Vector3>, bool)> curvesCore = base.curves.Select(curve => (curve.points, curve.closed)).ToList();
        base.dataHandler.SaveCurves(filename, curvesCore);
        base.ResetMenu();
        this.newState = new OpenFile(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
    }

    private void Open()
    {
        base.ResetMenu();
        this.newState = new OpenFile(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
    }

    string GetCurrentTimeString()
    {
        DateTime currentDateTime = DateTime.Now;
        return currentDateTime.ToString("yyyy-MM-dd-HH-mm-ss");
    }
}

public class OpenFile : State
{
    public OpenFile(OculusTouch oculusTouch,
                    ContextMenu.ContextMenu contextMenu,
                    DataHandler dataHandler,
                    List<Curve> curves)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 4;
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("Gallery", () => {
            base.ResetMenu();
            this.newState = new Gallery(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));

        List<string> files = base.dataHandler.GetFiles();
        foreach (string file in files)
        {
            this.contextMenu.AddItem(new MenuItem(file, () => {
                List<(List<Vector3> points, bool closed)> curvesCore = base.dataHandler.LoadCurves(file);
                List<Curve> loadedCurves = curvesCore.Select(core => new Curve(core.points, core.closed, selected: true, segment: base.segment)).ToList();
                base.ResetMenu();
                this.newState = new SelectResetOrAdd(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves, loadedCurves);
            }));
        }

        this.contextMenu.AddItem(new MenuItem("戻る", () => {
            base.ResetMenu();
            this.newState = new BasicDeformation(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));
    }
    
    public override void Update() {}
}

public class Gallery : State
{
    public Gallery(OculusTouch oculusTouch,
                   ContextMenu.ContextMenu contextMenu,
                   DataHandler dataHandler,
                   List<Curve> curves)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 4;
    }

    protected override void SetupMenu()
    {
        foreach (string file in this.KnotFiles())
        {
            List<Vector3> points = this.dataHandler.LoadCurveFromGitHub("Gallery/" + file, maxLength: 0.2f, barycenter: new Vector3(0, 0, 0.5f));
            this.contextMenu.AddItem(new MenuItem(file, () => {
                Curve curve = new Curve(points, true, selected: true, segment: base.segment);
                base.ResetMenu();
                this.newState = new SelectResetOrAdd(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves, new List<Curve>{ curve });
            }));
        }

        this.contextMenu.AddItem(new MenuItem("戻る", () => {
            base.ResetMenu();
            this.newState = new OpenFile(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));
    }
    
    public override void Update() {}

    private List<string> KnotFiles()
    {
        return new List<string>{
            "knot_0_1.json",
            "knot_3_1.json",
            "knot_4_1.json",
            "knot_5_1.json",
            "knot_5_2.json",
            "knot_6_1.json",
            "knot_6_2.json",
            "knot_6_3.json",
            "ochiai_unknot.json"
        };
    }
}

public class SelectResetOrAdd : State
{
    private List<Curve> newCurves;
    public SelectResetOrAdd(OculusTouch oculusTouch,
                            ContextMenu.ContextMenu contextMenu,
                            DataHandler dataHandler,
                            List<Curve> curves,
                            List<Curve> newCurves)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 4;
        this.newCurves = newCurves;
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("曲線をリセットしてファイルを開く", () => {
            base.ResetMenu();
            this.newState = new BasicDeformation(base.oculusTouch, base.contextMenu, base.dataHandler, this.newCurves);
        }));

        this.contextMenu.AddItem(new MenuItem("ファイルを開いて曲線を追加", () => {
            base.ResetMenu();
            this.newState = new BasicDeformation(base.oculusTouch, base.contextMenu, base.dataHandler,
                base.curves.Concat(this.newCurves).ToList());
        }));
    }
    
    public override void Update() {}
}

public class SelectAutoOrManual : State
{
    private LogicalButton select;

    public SelectAutoOrManual(OculusTouch oculusTouch,
                              ContextMenu.ContextMenu contextMenu,
                              DataHandler dataHandler,
                              List<Curve> curves,
                              LogicalButton select = null)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 6;
        if (select != null) this.select = select;
        else this.select = LogicalOVRInput.RawButton.A;
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("Aボタン : 選択", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));

        this.contextMenu.AddItem(new MenuItem("自動変形", () => {
            base.ResetMenu();
            this.newState = new AutomaticDeformation(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));

        this.contextMenu.AddItem(new MenuItem("手動変形 (曲線は1つ選択)", () => {
            List<Curve> selection = base.curves.Where(curve => curve.selected).ToList();
            if (selection.Count == 1)
            {
                base.ResetMenu();
                base.curves.Remove(selection[0]);
                this.newState = new ManualDeformation(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves, selection[0]);
            }
        }));

        this.contextMenu.AddItem(new MenuItem("平滑化", () => {
            if (!this.HaveInterSections())
            {
                List<Curve> selection = base.curves.Where(curve => curve.selected).ToList();
                foreach (Curve curve in selection)
                {
                    curve.points = this.Smoothing(curve.points);
                    curve.MeshUpdate();
                }
            }
        }));

        this.contextMenu.AddItem(new MenuItem("戻る", () => {
            base.ResetMenu();
            this.newState = new BasicDeformation(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));
    }

    public override void Update()
    {
        this.Select();
    }

    private void Select()
    {
        if (base.oculusTouch.GetButtonDown(this.select))
        {
            for (int i = 0; i < base.curves.Count; i++)
            {
                base.curves[i].Select();
            }
        }
    }

    private List<Vector3> Smoothing(List<Vector3> points)
    {
        int count = points.Count;
        List<Vector3> newPoints = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            Vector3 nowPoint = points[i];
            Vector3 postPoint = points[(i + 1) % count];
            Vector3 prePoint = points[(i - 1 + count) % count];
            float cos = Vector3.Dot((postPoint - nowPoint).normalized, (prePoint - nowPoint).normalized);
            if (cos > -0.8f)
            {
                newPoints.Add((nowPoint + postPoint + prePoint) / 3);
            }
            else
            {
                newPoints.Add(nowPoint);
            }
        }

        return newPoints;
    }

    private bool HaveInterSections()
    {
        List<Curve> selectedCurves = base.curves.Where(curve => curve.selected).ToList();
        List<Curve> unselectedCurves = base.curves.Where(curve => !curve.selected).ToList();
        int selectedCurvesCount = selectedCurves.Count;
        int unselectedCurvesCount = unselectedCurves.Count;

        for (int i = 0; i < selectedCurvesCount; i++)
        {
            if (selectedCurves[i].MinSegmentDist() < base.epsilon) return true;
            for (int j = i + 1; j < selectedCurvesCount; j++)
            {
                if (selectedCurves[i].CurveDistance(selectedCurves[j]) < base.epsilon) return true;
            }
            for (int k = 0; k < unselectedCurvesCount; k++)
            {
                if (selectedCurves[i].CurveDistance(unselectedCurves[k]) < base.epsilon) return true;
            }
        }

        return false;
    }
}

public class AutomaticDeformation : State
{
    private Optimize optimizer;
    private LogicalButton button1;
    private LogicalButton button2;
    
    public AutomaticDeformation(OculusTouch oculusTouch,
                                ContextMenu.ContextMenu contextMenu,
                                DataHandler dataHandler,
                                List<Curve> curves,
                                LogicalButton button1 = null,
                                LogicalButton button2 = null)
        : base(oculusTouch, contextMenu, dataHandler, curves.Where(curve => !curve.selected).ToList())
    {
        base.NumberOfUnselectableItems = 7;

        if (button1 != null) this.button1 = button1;
        else this.button1 = LogicalOVRInput.RawButton.A;

        if (button2 != null) this.button2 = button2;
        else this.button2 = LogicalOVRInput.RawButton.B;

        List<Curve> newCurves = curves.Where(curve => curve.selected).ToList();
        this.optimizer = new Optimize(oculusTouch, newCurves, base.curves, base.epsilon, this.button1, this.button2);
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("Aボタン : 自動変形 (遅い)", () => {}));
        this.contextMenu.AddItem(new MenuItem("Bボタン : 自動変形 (速い)", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("戻る", () => {
            base.curves = base.curves.Concat(this.optimizer.GetCurves()).ToList();
            base.ResetMenu();
            this.newState = new SelectAutoOrManual(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));
    }

    public override void Update()
    {
        this.optimizer.Update();
    }
}

public class ManualDeformation : State
{
    private Knot deformingCurve;
    
    public ManualDeformation(OculusTouch oculusTouch,
                             ContextMenu.ContextMenu contextMenu,
                             DataHandler dataHandler,
                             List<Curve> curves,
                             Curve curve)
        : base(oculusTouch, contextMenu, dataHandler, curves)
    {
        base.NumberOfUnselectableItems = 8;
        this.deformingCurve = new Knot(curve.points,
                                       oculusTouch,
                                       meridian: curve.meridian,
                                       radius: curve.radius,
                                       distanceThreshold: curve.segment,
                                       collisionCurves: curves,
                                       buttonC: LogicalOVRInput.RawButton.DisabledButton,
                                       buttonD: LogicalOVRInput.RawButton.DisabledButton,
                                       curveMaterial: MakeMesh.SelectedCurveMaterial);
    }

    protected override void SetupMenu()
    {
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("", () => {}));
        this.contextMenu.AddItem(new MenuItem("戻る", () => {
            base.curves.Add(new Curve(this.deformingCurve.GetPoints(), true, selected: true, segment: base.segment));
            base.ResetMenu();
            this.newState = new SelectAutoOrManual(base.oculusTouch, base.contextMenu, base.dataHandler, base.curves);
        }));
    }

    public override void Update()
    {
        this.deformingCurve.Update();
        MenuItem renamedItem = this.contextMenu.FindItem((item) => this.contextMenu.items.IndexOf(item) == base.NumberOfDefaultItems);
        this.contextMenu.ChangeItemMessage(renamedItem, this.Message(this.deformingCurve.GetKnotState()).Item1);
        renamedItem = this.contextMenu.FindItem((item) => this.contextMenu.items.IndexOf(item) == base.NumberOfDefaultItems + 1);
        this.contextMenu.ChangeItemMessage(renamedItem, this.Message(this.deformingCurve.GetKnotState()).Item2);
        renamedItem = this.contextMenu.FindItem((item) => this.contextMenu.items.IndexOf(item) == base.NumberOfDefaultItems + 2);
        this.contextMenu.ChangeItemMessage(renamedItem, this.Message(this.deformingCurve.GetKnotState()).Item3);
    }

    private (string, string, string) Message(IKnotState knotState)
    {
        if (knotState is KnotStateBase)
        {
            return ("可動域を確定しますか？", "Aボタン : 確定する", "Bボタン : 選択し直す");
        }
        else if (knotState is KnotStatePull)
        {
            return ("右手の動きに合わせて変形します", "Aボタン : 確定", "Bボタン : キャンセル");
        }
        else if (knotState is KnotStateChoose1)
        {
            return ("可動域の始点を選択して下さい", "Aボタン : 決定", "Bボタン : キャンセル");
        }
        else
        {
            return ("可動域の終点を選択して下さい", "Aボタン : 決定", "Bボタン : キャンセル");
        }
    }
}