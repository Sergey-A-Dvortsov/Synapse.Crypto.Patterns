using ScottPlot;
using ScottPlot.Plottables.Interactive;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    public class InteractiveLine
    {

        private Plot plot;

        public InteractiveLine(Plot plot, CoordinateLine line)
        {
            this.plot = plot;
            Segment = plot.Add.InteractiveLineSegment(line);
        }

        public InteractiveLineSegment Segment { get; }

        private double hitArea = 0.002;

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

    }


}
