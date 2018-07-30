﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalWindTunnel.Graphing
{
    public class GraphableCollection : DataGenerators.IGraphableProvider, IGraphable
    {
        public string Name { get; set; } = "";
        public virtual float XMin { get; protected set; } = float.NaN;
        public virtual float XMax { get; protected set; } = float.NaN;
        public virtual float YMin { get; protected set; } = float.NaN;
        public virtual float YMax { get; protected set; } = float.NaN;
        public Func<float, float> XAxisScale { get; set; } = (v) => v;
        public Func<float, float> YAxisScale { get; set; } = (v) => v;

        protected List<IGraphable> graphs = new List<IGraphable>();

        public event EventHandler ValuesChanged;

        public virtual List<IGraphable> Graphables { get { return graphs.ToList(); } }

        public GraphableCollection() { }
        public GraphableCollection(IEnumerable<IGraphable> graphs)
        {
            foreach (IGraphable g in graphs)
                AddGraph(g);
        }

        public virtual void Draw(ref UnityEngine.Texture2D texture, float xLeft, float xRight, float yBottom, float yTop)
        {
            for (int i = 0; i < graphs.Count; i++)
            {
                graphs[i].Draw(ref texture, XMin, XMax, YMin, YMax);
            }
        }
        public virtual bool RecalculateLimits()
        {
            float[] oldLimits = new float[] { XMin, XMax, YMin, YMax};
            XMin = XMax = YMin = YMax = float.NaN;

            for (int i = 0; i < graphs.Count; i++)
            {
                float xMin = graphs[i].XAxisScale(graphs[i].XMin);
                float xMax = graphs[i].XAxisScale(graphs[i].XMax);
                float yMin = graphs[i].YAxisScale(graphs[i].YMin);
                float yMax = graphs[i].YAxisScale(graphs[i].YMax);
                if (xMin < this.XMin || float.IsNaN(this.XMin)) this.XMin = xMin;
                if (xMax > this.XMax || float.IsNaN(this.XMax)) this.XMax = xMax;
                if (yMin < this.YMin || float.IsNaN(this.YMin)) this.YMin = yMin;
                if (yMax > this.YMax || float.IsNaN(this.YMax)) this.YMax = yMax;
            }
            
            if (!(oldLimits[0] == XMin && oldLimits[1] == XMax && oldLimits[2] == YMin && oldLimits[3] == YMax))
            {
                return true;
            }
            return false;
        }

        public float ValueAt(float x, float y)
        {
            return ValueAt(x, y, 0);
        }

        public float ValueAt(float x, float y, int index = 0)
        {
            if (graphs.Count - 1 < index)
                return float.NaN;
            
            return graphs[index].ValueAt(x, y);
        }

        public string GetFormattedValueAt(float x, float y)
        {
            if (graphs.Count == 0)
                return "";

            string returnValue = graphs[0].GetFormattedValueAt(x, y);
            for (int i = 1; i < graphs.Count; i++)
            {
                returnValue += String.Format("\n{0}", graphs[i].GetFormattedValueAt(x, y));
            }
            return returnValue;
        }

        protected virtual void OnValuesChanged(EventArgs eventArgs)
        {
            RecalculateLimits();
            ValuesChanged?.Invoke(this, eventArgs);
        }

        public virtual IGraphable GetGraphableByName(string name)
        {
            return graphs.Find(g => g.Name.ToLower() == name.ToLower());
        }


        public virtual IGraphable FindGraph(Predicate<IGraphable> predicate)
        {
            return graphs.Find(predicate);
        }

        public virtual void Clear()
        {
            for (int i = graphs.Count - 1; i >= 0; i--)
                RemoveGraphAt(i);
            OnValuesChanged(null);
        }

        public virtual int IndexOf(IGraphable graphable)
        {
            return graphs.IndexOf(graphable);
        }

        public virtual void AddGraph(IGraphable newGraph)
        {
            graphs.Add(newGraph);
            newGraph.ValuesChanged += ValuesChangedSubscriber;
            OnValuesChanged(null);
        }

        public virtual void InsertGraph(int index, IGraphable newGraph)
        {
            graphs.Insert(index, newGraph);
            newGraph.ValuesChanged += ValuesChangedSubscriber;
            OnValuesChanged(null);
        }

        public virtual void RemoveGraph(IGraphable graph)
        {
            graphs.Remove(graph);
            graph.ValuesChanged -= ValuesChangedSubscriber;
            OnValuesChanged(null);
        }
        public virtual void RemoveGraphAt(int index)
        {
            IGraphable graphable = graphs[index];
            graphs.RemoveAt(index);
            graphable.ValuesChanged -= ValuesChangedSubscriber;
            OnValuesChanged(null);
        }

        protected virtual void ValuesChangedSubscriber(object sender, EventArgs e) { }
    }

    public class GraphableCollection3 : GraphableCollection, IGraphable3
    {
        public virtual float ZMin { get; protected set; } = float.NaN;
        public virtual float ZMax { get; protected set; } = float.NaN;
        public float ZAxisScaler { get; set; } = 1;
        public Func<float, float> ZAxisScale { get; set; } = (v) => v;

        public ColorMap dominantColorMap = ColorMap.Jet_Dark;

        public GraphableCollection3() : base() { }
        public GraphableCollection3(IEnumerable<IGraphable> graphs) : base(graphs) { }

        public override bool RecalculateLimits()
        {
            float[] oldLimits = new float[] { XMin, XMax, YMin, YMax, ZMin, ZMax };
            XMin = XMax = YMin = YMax = ZMin = ZMax = float.NaN;
            dominantColorMap = null;

            for (int i = 0; i < graphs.Count; i++)
            {
                if (graphs[i] is Graphable3 surf)
                {
                    float zMin = surf.ZAxisScale(surf.ZMin);
                    float zMax = surf.ZAxisScale(surf.ZMax);
                    if (zMin < this.ZMin || float.IsNaN(this.ZMin)) this.ZMin = zMin;
                    if (zMax > this.ZMax || float.IsNaN(this.ZMax)) this.ZMax = zMax;
                    if (dominantColorMap == null)
                        dominantColorMap = surf.Color;
                }
                float xMin = graphs[i].XAxisScale(graphs[i].XMin);
                float xMax = graphs[i].XAxisScale(graphs[i].XMax);
                float yMin = graphs[i].YAxisScale(graphs[i].YMin);
                float yMax = graphs[i].YAxisScale(graphs[i].YMax);
                if (xMin < this.XMin || float.IsNaN(this.XMin)) this.XMin = xMin;
                if (xMax > this.XMax || float.IsNaN(this.XMax)) this.XMax = xMax;
                if (yMin < this.YMin || float.IsNaN(this.YMin)) this.YMin = yMin;
                if (yMax > this.YMax || float.IsNaN(this.YMax)) this.YMax = yMax;
            }

            if (dominantColorMap == null)
                dominantColorMap = ColorMap.Jet_Dark;
            
            if (!(oldLimits[0] == XMin && oldLimits[1] == XMax && oldLimits[2] == YMin && oldLimits[3] == YMax && oldLimits[4] == ZMin && oldLimits[5] == ZMax))
            {
                return true;
            }
            return false;
        }
    }
}