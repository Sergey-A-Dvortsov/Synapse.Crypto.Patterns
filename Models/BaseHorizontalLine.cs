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
    /// <summary>
    /// Represents a horizontal line on a plot that supports interactive manipulation, selection, and snapping behavior.
    /// </summary>
    /// <remarks>BaseHorizontalLine provides interactive features for a horizontal line within a plot,
    /// including mouse-based selection, movement, and snapping. It raises the YChanged event when its Y coordinate
    /// changes, allowing consumers to react to updates. The line can be selected or deselected based on mouse
    /// interaction, and its appearance updates accordingly. This class is intended to be used within the context of a
    /// SimulatorViewModel and its associated plot. Thread safety is not guaranteed; all interactions should occur on
    /// the UI thread.</remarks>
    public class BaseHorizontalLine : InteractiveHorizontalLine, IInteractiveLine
    {

        private SimulatorViewModel parent;
        private Plot plot;
        private double hitArea = 0.003;
        private Coordinates lastMouseCoords;
        private float defaultWidth;

        public BaseHorizontalLine(double y) : base()
        {
            parent = SimulatorViewModel.Instance;
            plot = parent.Plot.Plot;
            Y = y;
            defaultWidth = LineStyle.Width;
            LineStyle.Color = ScottPlot.Colors.Blue;
            plot.PlottableList.Add(this);
        }

        public event Action<BaseHorizontalLine> YChanged = delegate { };
        private void OnYChanged()
        {
            YChanged?.Invoke(this);
        }

        public bool SnappedMode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the item is currently selected.
        /// </summary>
        public bool IsSelected { get; private set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes this item from its associated plot.
        /// </summary>
        /// <remarks>After calling this method, the item will no longer be part of the plot and will not
        /// be displayed or updated with it. This operation has no effect if the item is not currently part of the
        /// plot.</remarks>
        public void Remove()
        {
            plot.Remove(this);
        }

        /// <summary>
        /// Sets the Y coordinate to the specified value.
        /// </summary>
        /// <param name="y">The new value for the Y coordinate.</param>
        public void SetNewCoordinate(double y)
        {
            Y = y;
            OnYChanged();
        }

        #region on mouse actions
        public bool IsMouseOver(Coordinates mouseCoords)
        {
            CoordinateRange rngY = new(Y - (Y * hitArea), Y + (Y * hitArea));
            return rngY.Contains(mouseCoords.Y);
        }

        public void MouseLeftButtonDown(Coordinates mouseCoords)
        {
            if (IsMouseOver(mouseCoords))
            {
                IsSelected = true;
                LineStyle.Width = defaultWidth * 2;

            }
            else
            {
                IsSelected = false;
                LineStyle.Width = defaultWidth;
            }

            lastMouseCoords = mouseCoords;
        }

        public void MouseLeftButtonUp(Coordinates mouseCoords)
        {
            //TODO;
        }

        public void MouseMove(Coordinates mouseCoords)
        {
            if (!IsSelected)
            {
                if (IsMouseOver(mouseCoords))
                {
                    LineStyle.Width = defaultWidth * 2;
                }
                else
                {
                    LineStyle.Width = defaultWidth;
                }
            }

            lastMouseCoords = mouseCoords;
        }

        #endregion

        #region override parrent's methods

        public override void PressHandle(InteractiveHandle handle)
        {
            if (!SnappedMode) return;
        }

        public override void ReleaseHandle(InteractiveHandle handle)
        {
            // if (!SnappedMode) return;


            //if (handles[handle.Index].IsSnaped)
            //{
            //    base.MoveHandle(handle, handles[handle.Index].Coordinates);
            //}
            OnYChanged();
        }

        public override void MoveHandle(InteractiveHandle handle, Coordinates point)
        {
            base.MoveHandle(handle, point);
            lastMouseCoords = point;
        }

        #endregion

    }
}
