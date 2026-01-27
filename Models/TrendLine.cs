using FluentAssertions.Equivalency;
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
    public interface IInteractiveLine : IDisposable
    {
        public bool SnappedMode { get; set; }
        public bool IsSelected { get; }
        public void MouseMove(Coordinates mouseCoords);
        public void MouseLeftButtonDown(Coordinates mouseCoords);
        public void MouseLeftButtonUp(Coordinates mouseCoords);
        public bool IsMouseOver(Coordinates mouseCoords);
        public void Remove();
    }

    public class Interactiveine : IDisposable
    {

        private SimulatorViewModel parent;
        private Plot plot;

        public Interactiveine(CoordinateLine line)
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
            if (e == null || e.Parent != Segment) return;
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

    public class TrendLine : InteractiveLineSegment, IInteractiveLine
    {

        private class Handle
        {
            public bool IsSnaped { get; private set; } = false;
            public Coordinates Coordinates { get; private set; }
            public MarkerStyle MarkerStyle { get; set; }

            public void Snapped(Coordinates coordinates) 
            {
                IsSnaped = true;
                Coordinates = coordinates;
                MarkerStyle.FillColor = ScottPlot.Colors.Red;
            }

            public void Release(ScottPlot.Color color)
            {
                IsSnaped = false;
                Coordinates = Coordinates.Zero;
                MarkerStyle.FillColor = color;
            }
        }

        private SimulatorViewModel parent;
        private Plot plot;
        private double hitArea = 0.003;
        private Dictionary<int, Handle> handles;
        private Coordinates lastMouseCoords;

        public TrendLine(CoordinateLine line) : base()
        {
            parent = SimulatorViewModel.Instance;
            plot = parent.Plot.Plot;
            Line = line;
            Color = ScottPlot.Colors.Blue;
            plot.PlottableList.Add(this);
            handles = new() { 
                { 0, new Handle() { MarkerStyle = StartMarkerStyle } }, 
                { 1, new Handle() { MarkerStyle = EndMarkerStyle } } };
        }

        public void Dispose()
        {
           // throw new NotImplementedException();
        }

        /// <summary>
        /// Enable/disable the sticking to candlestick extremes mode
        /// </summary>
        public bool SnappedMode { get; set; } = true;

        public bool IsSelected { get; private set; }

        public bool ExtendInBoth { get; set; } = false;

        public void Remove()
        {
            plot.Remove(this);
        }

        #region on mouse actions

        public void MouseMove(Coordinates mouseCoords)
        {
            if (IsMouseOver(mouseCoords))
            {
                // делаем подсветку
                if (!IsSelected)
                {
                    StartMarkerStyle.IsVisible = true;
                    EndMarkerStyle.IsVisible = true;
                }
            }
            else
            {
                if (!IsSelected)
                {
                    // убираем подсветку
                    StartMarkerStyle.IsVisible = false;
                    EndMarkerStyle.IsVisible = false;
                }
            }

            lastMouseCoords = mouseCoords;

        }

        public void MouseLeftButtonDown(Coordinates mouseCoords)
        {
            if (IsMouseOver(mouseCoords))
            {
                IsSelected = true;
                StartMarkerStyle.IsVisible = true;
                EndMarkerStyle.IsVisible = true;
            }
            else
            {
                IsSelected = false;
                StartMarkerStyle.IsVisible = false;
                EndMarkerStyle.IsVisible = false;
            }

            lastMouseCoords = mouseCoords;

        }

        public void MouseLeftButtonUp(Coordinates mouseCoords)
        {
            lastMouseCoords = mouseCoords;
        }

        /// <summary>
        /// Determines whether the mouse cursor is over a line.
        /// </summary>
        /// <param name="mouseCoords"></param>
        /// <param name="hitArea"></param>
        /// <returns></returns>
        public bool IsMouseOver(Coordinates mouseCoords)
        {
            var box = Line.BoundingBox(); // rectangle in which the line is inscribed

            bool insidebox = box.Contains(mouseCoords); // whether the cursor coordinates are inside the rectangle

            if (!insidebox) return false;

            var lineY = Line.Y(mouseCoords.X); // line's Y to X current cursor position

            CoordinateRange rngY = new(lineY - (lineY * hitArea), lineY + (lineY * hitArea));

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
            var pxl = plot.GetPixel(Line.Start);
            var rect = plot.GetCoordinateRect(pxl, StartMarkerStyle.Size);
            return rect.Contains(mouseCoords);
        }

        /// <summary>
        /// Determines whether the mouse cursor is over a end handle.
        /// </summary>
        /// <param name="mouseCoords"></param>
        /// <returns></returns>
        public bool IsMouseOverEndHandle(Coordinates mouseCoords)
        {
            var pxl = plot.GetPixel(Line.End);
            var rect = plot.GetCoordinateRect(pxl, EndMarkerStyle.Size);
            return rect.Contains(mouseCoords);
        }

        private bool IsMouseOverSnappedHandle(Coordinates mouseCoords, InteractiveHandle handle)
        {
            if (!handles[handle.Index].IsSnaped) return false;
            var pxl = plot.GetPixel(mouseCoords);
            var rect = plot.GetCoordinateRect(pxl, EndMarkerStyle.Size);
            return rect.Contains(mouseCoords);
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

            if (handles[handle.Index].IsSnaped)
            {
                base.MoveHandle(handle, handles[handle.Index].Coordinates);
            }

        }

        public override void MoveHandle(InteractiveHandle handle, Coordinates point)
        {
            base.MoveHandle(handle, point);
            lastMouseCoords = point;

            if (!SnappedMode) return;

            var snappedPoint = parent.GetSnappedPoint(point);

            if (snappedPoint == null)
            {
                if (handles[handle.Index].IsSnaped)
                {
                    handles[handle.Index].Release(LineStyle.Color);
                }
            }
            else 
            {
                if (!handles[handle.Index].IsSnaped || !handles[handle.Index].Coordinates.Equals(snappedPoint))
                {
                    base.MoveHandle(handle, snappedPoint.Value);
                    lastMouseCoords = snappedPoint.Value;
                }

                handles[handle.Index].Snapped(snappedPoint.Value);
            }
            
        }

        public override void Render(RenderPack rp)
        {
            if (IsVisible == false) return;

            PixelLine pxLine = Axes.GetPixelLine(Line);

            // Calculate the slope and extend the line to the edges of the data area
            PixelLine extendedLine = ExtendLineToEdges(pxLine, rp.DataRect);

            LineStyle.Render(rp.Canvas, extendedLine, rp.Paint);
            StartMarkerStyle.Render(rp.Canvas, pxLine.Pixel1, rp.Paint);
            EndMarkerStyle.Render(rp.Canvas, pxLine.Pixel2, rp.Paint);
        }

        private PixelLine ExtendLineToEdges(PixelLine line, PixelRect dataRect)
        {
            // Handle vertical line case
            if (Math.Abs(line.DeltaX) < 0.001)
            {
                if (ExtendInBoth)
                    return new PixelLine(line.X1, dataRect.Top, line.X1, dataRect.Bottom);
                else
                    return new PixelLine(line.X1, line.Y1, line.X1, dataRect.Bottom);
            }

            // Handle horizontal line case
            if (Math.Abs(line.DeltaY) < 0.001)
            {
                if (ExtendInBoth)
                    return new PixelLine(dataRect.Left, line.Y1, dataRect.Right, line.Y1);
                else
                    return new PixelLine(line.X1, line.Y1, dataRect.Right, line.Y1);
            }

            // Calculate intersections with the data rect edges
            float leftY = line.Slope * dataRect.Left + line.YIntercept;
            float rightY = line.Slope * dataRect.Right + line.YIntercept;
            float topX = (dataRect.Top - line.YIntercept) / line.Slope;
            float bottomX = (dataRect.Bottom - line.YIntercept) / line.Slope;

            // Find which two edges the line intersects
            List<Pixel> intersections = [];

            if (ExtendInBoth)
            {

                if (leftY >= dataRect.Top && leftY <= dataRect.Bottom)
                    intersections.Add(new Pixel(dataRect.Left, leftY));

                if (rightY >= dataRect.Top && rightY <= dataRect.Bottom)
                    intersections.Add(new Pixel(dataRect.Right, rightY));

                if (topX >= dataRect.Left && topX <= dataRect.Right)
                    intersections.Add(new Pixel(topX, dataRect.Top));

                if (bottomX >= dataRect.Left && bottomX <= dataRect.Right)
                    intersections.Add(new Pixel(bottomX, dataRect.Bottom));

            }
            else
            {
                intersections.Add(new Pixel(line.X1, line.Y1));

                if (rightY >= dataRect.Top && rightY <= dataRect.Bottom)
                    intersections.Add(new Pixel(dataRect.Right, rightY));

                if (topX >= dataRect.Left && topX <= dataRect.Right)
                    intersections.Add(new Pixel(topX, dataRect.Top));

                if (bottomX >= dataRect.Left && bottomX <= dataRect.Right)
                    intersections.Add(new Pixel(bottomX, dataRect.Bottom));

            }

            // Return line between first two intersections
            if (intersections.Count >= 2)
                return new PixelLine(intersections[0], intersections[1]);

            // Fallback to original line if something went wrong
            return line;
        }

      

        #endregion

    }

}




