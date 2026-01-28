using ScottPlot;
using ScottPlot.Plottables.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{

    public class BaseHorizontalLine : InteractiveHorizontalLine, IInteractiveLine
    {

        private SimulatorViewModel parent;
        private Plot plot;
        private double hitArea = 0.003;
        private Coordinates lastMouseCoords;

        public BaseHorizontalLine(double y) : base()
        {
            parent = SimulatorViewModel.Instance;
            plot = parent.Plot.Plot;
            Y = y;
            plot.PlottableList.Add(this);
        }

        public bool SnappedMode { get; set; }

        public bool IsSelected { get; private set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Remove()
        {
            plot.Remove(this);
        }

        #region on mouse actions
        public bool IsMouseOver(Coordinates mouseCoords)
        {
            //TODO
            return true;
        }

        public void MouseLeftButtonDown(Coordinates mouseCoords)
        {
            //TODO;
        }

        public void MouseLeftButtonUp(Coordinates mouseCoords)
        {
            //TODO;
        }

        public void MouseMove(Coordinates mouseCoords)
        {
            //TODO;
        }

        #endregion

        #region override parrent's methods

        public override void PressHandle(InteractiveHandle handle)
        {
            if (!SnappedMode) return;
        }

        public override void ReleaseHandle(InteractiveHandle handle)
        {
            if (!SnappedMode) return;

            //if (handles[handle.Index].IsSnaped)
            //{
            //    base.MoveHandle(handle, handles[handle.Index].Coordinates);
            //}

        }

        public override void MoveHandle(InteractiveHandle handle, Coordinates point)
        {
            base.MoveHandle(handle, point);
            lastMouseCoords = point;

            if (!SnappedMode) return;

        }

        #endregion

    }
}
