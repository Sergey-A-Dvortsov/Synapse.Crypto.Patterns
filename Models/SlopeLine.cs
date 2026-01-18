using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Plottables.Interactive;
using Synapse.Crypto.Bybit;
using Synapse.Crypto.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    /// <summary>
    /// Наклонная линия. Строится по двум координатам. Эти базовые координаты определяются
    /// при помощи свечей FirstCandle и SecondCandle, которые выбирает пользователь. 
    /// Линия продлевается до уровней недельного максимума/минимума или максимума/минимума загруженных данных, если
    /// загружено меньше недели.
    /// </summary>
    public class SlopeLine
    {

        public SlopeLine(Candle first, Candle second, TimeFrames timeFrame = TimeFrames.Min15)
        {
            FirstCandle = first.OpenTime < second.OpenTime ? first : second;
            SecondCandle = second.OpenTime > first.OpenTime ? second : first;
            TimeFrame = timeFrame;
        }


        #region properties

        /// <summary>
        /// Первая свеча. Выбирается пользователем
        /// </summary>
        public Candle? FirstCandle { get; set; }

        /// <summary>
        /// Вторая свеча. Выбирается пользователем
        /// </summary>
        private Candle? _secondCandle;
        public Candle? SecondCandle
        {
            get => _secondCandle;
            set
            {
                _secondCandle = value;
            }
        }

        /// <summary>
        /// Направление линии. 0 - нисходящее, 1 - восхоящее, -1 - error
        /// </summary>
        public int Direct
        {
            get  
             {
                if (FirstCandle?.OpenTime == DateTime.MinValue || SecondCandle?.OpenTime == DateTime.MinValue) return -1;
                var dirct = FirstCandle?.High > SecondCandle?.High ? 0 : FirstCandle?.Low < SecondCandle?.Low ? 1 : -1;
                return dirct;
              }
        }

        /// <summary>
        /// Наклон линии. Нисходящий < 0, восходящий > 0.
        /// </summary>
        public double Slope
        {
            get
            {
                return GetSlope();
            }
        }

        /// <summary>
        /// Интервал графика (свечи)
        /// </summary>
        public TimeFrames TimeFrame { get; set; }

        /// <summary>
        /// Максимум последней недели или максимум загруженных данных, если загружено меньше недели
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Минимум последней недели или минимум загруженных данных, если загружено меньше недели
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Координата первой точки
        /// </summary>
        public Coordinates? FirstCoordinates
        {
            get
            {
                if (Direct == -1) return null;
                
                if(FirstCandle != null && SecondCandle != null)
                {
                    double Y = Direct == 0 ? FirstCandle.Value.High : FirstCandle.Value.Low;
                    return new Coordinates(FirstCandle.Value.OpenTime.ToOADate(), Y);
                    //if (Direct == 0)
                    //    return new Coordinates(FirstCandle.Value.OpenTime.ToOADate(), FirstCandle.Value.High);
                    //else
                    //    return new Coordinates(FirstCandle.Value.OpenTime.ToOADate(), FirstCandle.Value.Low);
                }

                return null;
            }
        }

        /// <summary>
        /// Координата второй точки
        /// </summary>
        public Coordinates? SecondCoordinates
        {
            get
            {
                if (Direct == -1) return null;
                return GetSecondCoordinate();
            }
        }

        /// <summary>
        /// График наклонной
        /// </summary>
        public LinePlot? Line { set; get; }

        #endregion

        public void SetMinMax(List<Candle> candles)
        {
            int bars = (60 / (int)TimeFrame) * 24 * 7;  
            var temp = candles.TakeLast(bars);
            Min = temp.Min(c => c.Low);
            Max = temp.Max(c => c.High);
        }

        /// <summary>
        /// Возвращает объект линии, который используется в графическом компоненте
        /// </summary>
        public CoordinateLine? GetCoordinateLine()
        {
            if (Direct == -1) return null;
            return new CoordinateLine(FirstCoordinates.GetValueOrDefault(), SecondCoordinates.GetValueOrDefault());
        }


        // Возвращает координату второй точки
        private Coordinates? GetSecondCoordinate()
        {
            if (SecondCandle == null) return null;

            double Y = 0;
            DateTime X = DateTime.MinValue;
            int barsToMin = 0; // число баров от второй свечи до локального минимума

            if (Direct == 0)
            {
                barsToMin = Math.Abs((int)Math.Round((SecondCandle.Value.High - Min) / Slope, 0));
                Y = SecondCandle.Value.High + Slope * barsToMin;
            }
            else if (Direct == 1)
            {
                barsToMin = Math.Abs((int)Math.Round((Max - SecondCandle.Value.Low) / Slope, 0));
                Y = SecondCandle.Value.Low + Slope * barsToMin;
            }


            X = SecondCandle.Value.OpenTime + TimeSpan.FromMinutes(barsToMin * (int)TimeFrame);

            return new Coordinates(X.ToOADate(), Y);


        }

        // Возвращает уровень наклона линии - абсолютное изменение цены на единицу интервала
        private double GetSlope()
        {
            if (Direct == -1) return double.NaN;


            if (FirstCandle != null && SecondCandle != null)
            {
                int bars = (int)((SecondCandle?.OpenTime - FirstCandle?.OpenTime) / TimeSpan.FromMinutes((int)TimeFrame));

                if (Direct == 0)
                    return (SecondCandle.Value.High - FirstCandle.Value.High) / bars;
                else
                    return (SecondCandle.Value.Low - FirstCandle.Value.Low) / bars;
            }

            return double.NaN;

        }

    }

    /// <summary>
    /// Наклонная линия. Строится по двум координатам. Эти базовые координаты определяются
    /// при помощи свечей FirstCandle и SecondCandle, которые выбирает пользователь. 
    /// Линия продлевается до уровней недельного максимума/минимума или максимума/минимума загруженных данных, если
    /// загружено меньше недели.
    /// </summary>
    public class InteractiveSlopeLine
    {

        public InteractiveSlopeLine()
        {
        }


        #region properties

        /// <summary>
        /// Направление линии. 0 - нисходящее, 1 - восхоящее, -1 - error
        /// </summary>
        public int Direct
        {
            get
            {
                if (FirstCoordinates == null || SecondCoordinates == null) return -1;
                var dirct = FirstCoordinates.Value.Y > SecondCoordinates.Value.Y ? 0 : 1;
                return dirct;
            }
        }

        /// <summary>
        /// Наклон линии. Нисходящий < 0, восходящий > 0.
        /// </summary>
        public double Slope
        {
            get { return GetSlope(); }
        }

        /// <summary>
        /// Интервал графика (свечи)
        /// </summary>
        public TimeFrames TimeFrame { get; set; }

        /// <summary>
        /// Координата первой точки
        /// </summary>
        public Coordinates? FirstCoordinates { set; get; }

        /// <summary>
        /// Координата второй точки
        /// </summary>
        public Coordinates? SecondCoordinates { set; get; }

        /// <summary>
        /// График наклонной
        /// </summary>
        public InteractiveLineSegment Segment { set; get; }

        #endregion

        /// <summary>
        /// Создает экземпляр CoordinateLine
        /// </summary>
        public CoordinateLine? CreateCoordinateLine()
        {
            if (FirstCoordinates == null || SecondCoordinates == null) return null;
            return new CoordinateLine(FirstCoordinates.GetValueOrDefault(), SecondCoordinates.GetValueOrDefault());
        }

        // Возвращает уровень наклона линии - абсолютное изменение цены на единицу интервала
        private double GetSlope()
        {
            if (Direct == -1) return double.NaN;

            //if (FirstCandle != null && SecondCandle != null)
            //{
            //    int bars = (int)((SecondCandle?.OpenTime - FirstCandle?.OpenTime) / TimeSpan.FromMinutes((int)TimeFrame));

            //    if (Direct == 0)
            //        return (SecondCandle.Value.High - FirstCandle.Value.High) / bars;
            //    else
            //        return (SecondCandle.Value.Low - FirstCandle.Value.Low) / bars;
            //}

            return double.NaN;

        }

    }

}
