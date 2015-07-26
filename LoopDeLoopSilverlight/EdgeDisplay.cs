using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LoopDeLoop
{
    public class EdgeDisplay : Canvas
    {

        public EdgeDisplay()
        {
            edgeLine.Stroke = new SolidColorBrush(Colors.LightGray);
            this.Children.Add(edgeLine);
            xLine1.Stroke = new SolidColorBrush(Colors.Black);
            xLine2.Stroke = new SolidColorBrush(Colors.Black);
        }

        Line edgeLine = new Line();
        Line xLine1 = new Line();
        Line xLine2 = new Line();



        public bool Marked
        {
            get { return (bool)GetValue(MarkedProperty); }
            set { SetValue(MarkedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Marked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkedProperty =
            DependencyProperty.Register("Marked", typeof(bool), typeof(EdgeDisplay), new PropertyMetadata(new PropertyChangedCallback(OnMarkedChanged)));


        public static void OnMarkedChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            EdgeDisplay display = (EdgeDisplay)source;
            display.edgeLine.Stroke = display.EdgeState == EdgeState.Filled ? (display.Marked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black)) : new SolidColorBrush(Colors.LightGray);
            display.xLine1.Stroke = display.Marked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black);
            display.xLine2.Stroke = display.Marked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black);
        }



        public bool NegGradient
        {
            get { return (bool)GetValue(NegGradientProperty); }
            set { SetValue(NegGradientProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NegGradient.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NegGradientProperty =
            DependencyProperty.Register("NegGradient", typeof(bool), typeof(EdgeDisplay), new PropertyMetadata(false, new PropertyChangedCallback(OnNegGradientChanged)));

        public static void OnNegGradientChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            EdgeDisplay display = (EdgeDisplay)source;
            UpdateMainLinePos(display);


        }

        private static void UpdateMainLinePos(EdgeDisplay display)
        {
            if (display.NegGradient)
            {
                display.edgeLine.X1 = display.XDiff;
                display.edgeLine.X2 = 0.0;
                display.edgeLine.Y1 = 0.0;
                display.edgeLine.Y2 = display.YDiff;
            }
            else
            {
                display.edgeLine.X1 = 0.0;
                display.edgeLine.X2 = display.XDiff;
                display.edgeLine.Y1 = 0.0;
                display.edgeLine.Y2 = display.YDiff;
            }
        }

        public double XDiff
        {
            get { return (double)GetValue(XDiffProperty); }
            set { SetValue(XDiffProperty, value); }
        }

        public static readonly DependencyProperty XDiffProperty =
            DependencyProperty.Register("XDiff", typeof(double), typeof(EdgeDisplay), new PropertyMetadata(new PropertyChangedCallback(OnXChanged)));

        public static void OnXChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            EdgeDisplay display = (EdgeDisplay)source;
            UpdateMainLinePos(display);

            double length = Math.Pow(display.XDiff, 2.0) + Math.Pow(display.YDiff, 2.0);
            length = Math.Sqrt(length) / 3.0;
            double partDiff = length / Math.Sqrt(2.0);
            display.xLine1.X1 = display.XDiff / 2.0 - partDiff / 2.0;
            display.xLine1.X2 = display.XDiff / 2.0 + partDiff / 2.0;
            display.xLine1.Y1 = display.YDiff / 2.0 - partDiff / 2.0;
            display.xLine1.Y2 = display.YDiff / 2.0 + partDiff / 2.0;
            display.xLine2.X1 = display.XDiff / 2.0 + partDiff / 2.0;
            display.xLine2.X2 = display.XDiff / 2.0 - partDiff / 2.0;
            display.xLine2.Y1 = display.YDiff / 2.0 - partDiff / 2.0;
            display.xLine2.Y2 = display.YDiff / 2.0 + partDiff / 2.0;
        }

        public double YDiff
        {
            get { return (double)GetValue(YDiffProperty); }
            set { SetValue(YDiffProperty, value); }
        }

        public static readonly DependencyProperty YDiffProperty =
            DependencyProperty.Register("YDiff", typeof(double), typeof(EdgeDisplay), new PropertyMetadata(new PropertyChangedCallback(OnYChanged)));


        public static void OnYChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            EdgeDisplay display = (EdgeDisplay)source;
            UpdateMainLinePos(display);

            double length = Math.Pow(display.XDiff, 2.0) + Math.Pow(display.YDiff, 2.0);
            length = Math.Sqrt(length) / 3.0;
            double partDiff = length / Math.Sqrt(2.0);
            display.xLine1.X1 = display.XDiff / 2.0 - partDiff / 2.0;
            display.xLine1.X2 = display.XDiff / 2.0 + partDiff / 2.0;
            display.xLine1.Y1 = display.YDiff / 2.0 - partDiff / 2.0;
            display.xLine1.Y2 = display.YDiff / 2.0 + partDiff / 2.0;
            display.xLine2.X1 = display.XDiff / 2.0 + partDiff / 2.0;
            display.xLine2.X2 = display.XDiff / 2.0 - partDiff / 2.0;
            display.xLine2.Y1 = display.YDiff / 2.0 - partDiff / 2.0;
            display.xLine2.Y2 = display.YDiff / 2.0 + partDiff / 2.0;
        }

        public EdgeState EdgeState
        {
            get { return (EdgeState)GetValue(EdgeStateProperty); }
            set { SetValue(EdgeStateProperty, value); }
        }

        public static readonly DependencyProperty EdgeStateProperty =
            DependencyProperty.Register("EdgeState", typeof(EdgeState), typeof(EdgeDisplay), new PropertyMetadata(new PropertyChangedCallback(OnEdgeStateChanged)));


        public static void OnEdgeStateChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            EdgeDisplay display = (EdgeDisplay)source;
            display.edgeLine.Stroke = display.EdgeState == EdgeState.Filled ? (display.Marked ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black)) : new SolidColorBrush(Colors.LightGray);
            if (display.EdgeState == EdgeState.Excluded)
            {
                if (display.Children.Count == 1)
                {
                    display.Children.Add(display.xLine1);
                    display.Children.Add(display.xLine2);
                }
            }
            else
            {
                if (display.Children.Count > 1)
                {
                    display.Children.Remove(display.xLine1);
                    display.Children.Remove(display.xLine2);
                }
            }
        }


        public int EdgeColor
        {
            get { return (int)GetValue(EdgeColorProperty); }
            set { SetValue(EdgeColorProperty, value); }
        }

        public static readonly DependencyProperty EdgeColorProperty =
            DependencyProperty.Register("EdgeColor", typeof(int), typeof(EdgeDisplay), new PropertyMetadata(0));
    }
}
