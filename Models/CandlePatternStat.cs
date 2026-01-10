using MathNet.Numerics.Statistics;
using Synapse.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Crypto.Patterns
{
    // Copyright(c) [2026], [Sergey Dvortsov]
    /// <summary>
    /// Статистические параметры, получаемые из наборов размеченных свечей. Используются для распознавания одиночных свечных формаций.
    /// </summary>
    public class CandlePatternStat
    {

        public CandlePatternStat(DescriptiveStatistics body, DescriptiveStatistics shadowRatio, DescriptiveStatistics bodyShadowRatio) 
        { 
            Body = body;
            ShadowRatio = shadowRatio;
            BodyShadowRatio = bodyShadowRatio;
            SetRange();
        }

        /// <summary>
        /// Описательная статистика относительных размеров тела свечи.
        /// </summary>
        public DescriptiveStatistics Body {  get; private set; }

        /// <summary>
        /// Описательная статистика соотношения размеров теней свечи.
        /// </summary>
        public DescriptiveStatistics ShadowRatio { get; private set; }

        /// <summary>
        /// Описательная статистика соотношения размеров тела и наибольшей тени свечи.
        /// </summary>
        public DescriptiveStatistics BodyShadowRatio { get; private set; }

        /// <summary>
        /// Диапазон относительных размеров тела для данной модели свечи
        /// </summary>
        public Range<double> BodyRange { get; private set; }

        /// <summary>
        /// Диапазон значений отношения большей и меньшей тени для данной модели свечи
        /// </summary>
        public Range<double> ShadowRatioRange { get; private set; }

        /// <summary>
        /// Диапазон значений соотношения тела и наибольшей тени для данной модели свечи
        /// </summary>
        public Range<double> BodyShadowRatioRange { get; private set; }

        private void SetRange()
        {
            BodyRange = new Range<double>() 
            { 
                Max = Body.Mean + Body.StandardDeviation * 3, 
                Min = Body.Mean - Body.StandardDeviation * 3
            };

            ShadowRatioRange = new Range<double>()
            {
                Max = ShadowRatio.Mean + ShadowRatio.StandardDeviation * 3,
                Min = ShadowRatio.Mean - ShadowRatio.StandardDeviation * 3
            };

            BodyShadowRatioRange = new Range<double>()
            {
                Max = BodyShadowRatio.Mean + BodyShadowRatio.StandardDeviation * 3,
                Min = BodyShadowRatio.Mean - BodyShadowRatio.StandardDeviation * 3
            };

        }

    }
}
