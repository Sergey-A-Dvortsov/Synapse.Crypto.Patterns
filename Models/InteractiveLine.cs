using NUnit.Framework.Internal.Execution;
using ScottPlot;
using ScottPlot.Plottables.Interactive;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Synapse.Crypto.Patterns
{
    public class InteractiveLine : IDisposable  
    {

        private SimulatorViewModel parent;
        private Plot plot;
  
        public InteractiveLine(CoordinateLine line)
        {
            parent = SimulatorViewModel.Instance;
            plot = parent.Plot.Plot;

            plot?.HandleHoverChanged += OnHandleHoverChanged;
            plot?.HandleMoved += OnHandleMoved;
            plot?.HandlePressed += OnHandlePressed;
            plot?.HandleReleased += OnHandleReleased;

            Segment = plot.Add.InteractiveLineSegment(line);

        }


        public void Dispose()
        {
            plot.Remove(Segment);
            plot?.HandleHoverChanged -= OnHandleHoverChanged;
            plot?.HandleMoved -= OnHandleMoved;
            plot?.HandlePressed -= OnHandlePressed;
            plot?.HandleReleased -= OnHandleReleased;

        }

        public InteractiveLineSegment Segment { get; }

        private double hitArea = 0.002;

        /// <summary>
        /// Enable/disable the sticking to candlestick extremes mode
        /// </summary>
        public bool SnappedMode { get; set; } = true;

        public bool IsSelected { get; private set; }

        public void MouseMove(Coordinates mouseCoords)
        {
            if (IsMouseOver(mouseCoords))
            {
                // делаем подсветку
                if (!IsSelected)
                {
                    Segment.StartMarkerStyle.IsVisible = true;
                    Segment.EndMarkerStyle.IsVisible = true;
                }
            }
            else
            {
                if (!IsSelected)
                {
                    // убираем подсветку
                    Segment.StartMarkerStyle.IsVisible = false;
                    Segment.EndMarkerStyle.IsVisible = false;
                }
            }
        }

        public void MouseLeftButtonDown(Coordinates mouseCoords)
        {
            if (IsMouseOver(mouseCoords))
            {
                IsSelected = true;
                Segment.StartMarkerStyle.IsVisible = true;
                Segment.EndMarkerStyle.IsVisible = true;
            }
            else
            {
                IsSelected = false;
                Segment.StartMarkerStyle.IsVisible = false;
                Segment.EndMarkerStyle.IsVisible = false;
            }
        }

        public void MouseLeftButtonUp(Coordinates mouseCoords)
        {
            //TODO
        }

        /// <summary>
        /// Determines whether the mouse cursor is over a line.
        /// </summary>
        /// <param name="mouseCoords"></param>
        /// <param name="hitArea"></param>
        /// <returns></returns>
        public bool IsMouseOver(Coordinates mouseCoords)
        {
            var box = Segment.Line.BoundingBox(); // rectangle in which the line is inscribed

            bool insidebox = box.Contains(mouseCoords); // whether the cursor coordinates are inside the rectangle

            if (!insidebox) return false;

            var lineY = Segment.Line.Y(mouseCoords.X); // line's Y to X current cursor position

            CoordinateRange rngY = new(lineY - (lineY * hitArea), hitArea + (lineY * hitArea));

            if (rngY.Contains(mouseCoords.Y)) return true;


            return IsMouseOverStartHandle(mouseCoords) || IsMouseOverEndHandle(mouseCoords);

        }

        /// <summary>
        /// Determines whether the mouse cursor is over a start handle.
        /// </summary>
        /// <param name="mouseCoords"></param>
        /// <returns></returns>
        public bool IsMouseOverStartHandle(Coordinates mouseCoords)
        {
            var pxl = plot.GetPixel(Segment.Line.Start);
            var rect = plot.GetCoordinateRect(pxl, Segment.StartMarkerStyle.Size);
            return rect.Contains(mouseCoords);
        }

        /// <summary>
        /// Determines whether the mouse cursor is over a end handle.
        /// </summary>
        /// <param name="mouseCoords"></param>
        /// <returns></returns>
        public bool IsMouseOverEndHandle(Coordinates mouseCoords)
        {
            var pxl = plot.GetPixel(Segment.Line.End);
            var rect = plot.GetCoordinateRect(pxl, Segment.EndMarkerStyle.Size);
            return rect.Contains(mouseCoords);
        }

        #region InteractiveHandle handlers

        private void OnHandleHoverChanged(object? sender, InteractiveHandle? e)
        {
            if(e  == null || e.Parent != Segment ) return;
        }

        private void OnHandleMoved(object? sender, InteractiveHandle? e)
        {
            if (e == null || e.Parent != Segment) return;
        }

        private void OnHandlePressed(object? sender, InteractiveHandle? e)
        {
            if (e == null || e.Parent != Segment) return;
        }

        private void OnHandleReleased(object? sender, InteractiveHandle? e)
        {
            if (e == null || e.Parent != Segment) return;
        }

        #endregion

    }

}

///var handles = line.GetHandles(); // метод из IHasInteractiveHandles