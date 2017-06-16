using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge.Html5;
using LoopDeLoop;

namespace LoopDeLoopBridge
{
    class CellDisplay
    {

        public CellDisplay(CanvasRenderingContext2D context, List<double> xCoords, List<double> yCoords)
        {
            this.context = context;
            this.xCoords = xCoords;
            this.yCoords = yCoords;
        }

        private CanvasRenderingContext2D context;
        private List<double> xCoords;
        private List<double> yCoords;
        private bool dirty = true;



        public int TargetCount
        {
            get { return targetCount; }
            set
            {
                if (value != targetCount)
                {
                    targetCount = value;
                    dirty = true;
                }
            }
        }

        private int targetCount;

        public void Render()
        {
            if (!dirty) return;
            context.GlobalCompositeOperation = CanvasTypes.CanvasCompositeOperationType.DestinationOut;
            context.StrokeStyle = "black";
            context.BeginPath();
            context.MoveTo(xCoords[xCoords.Count-1], yCoords[yCoords.Count-1]);
            for (int i = 0; i < xCoords.Count; i++)
            {
                context.LineTo(xCoords[i], yCoords[i]);
            }
            context.Fill();

            context.GlobalCompositeOperation = CanvasTypes.CanvasCompositeOperationType.SourceOver;
            if (TargetCount >= 0)
            {
                var metrics = context.MeasureText(TargetCount.ToString());
                double midX = 0;
                double midY = 0;
                for (int i = 0; i < xCoords.Count; i++)
                {
                    midX += xCoords[i];
                    midY += yCoords[i];
                }
                midX /= xCoords.Count;
                midY /= yCoords.Count;
                context.Font = "18pt Tahoma";
                context.TextAlign = CanvasTypes.CanvasTextAlign.Center;
                context.TextBaseline = CanvasTypes.CanvasTextBaselineAlign.Middle;
                context.FillText(TargetCount.ToString(), (int)midX, (int)midY);
            }
        }

    }
}
