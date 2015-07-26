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
    public class CellDisplay : Canvas
    {

        private TextBlock text = new TextBlock();


        public PointCollection Points
        {
            get { return (PointCollection)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Points.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(PointCollection), typeof(CellDisplay), new PropertyMetadata(new PropertyChangedCallback(OnPointsChanged)));

        private static void OnPointsChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            CellDisplay display = (CellDisplay)source;
            UpdatePosition(display);
            UpdateDisplayed(display);
        }

        private static void UpdateDisplayed(CellDisplay display)
        {
            if (display.Points.Count > 0 && display.TargetCount >= 0 && display.FontFamily != null && display.FontSize > 0)
            {
                if (display.Children.Count == 0)
                    display.Children.Add(display.text);
            }
            else
            {
                if (display.Children.Count != 0)
                    display.Children.Clear();
            }
        }

        private static void UpdatePosition(CellDisplay display)
        {
            if (display.Points.Count > 0)
            {
                double mx = 0.0;
                double my = 0.0;
                foreach (Point p in display.Points)
                {
                    mx += p.X;
                    my += p.Y;
                }
                mx /= display.Points.Count;
                my /= display.Points.Count;
                display.text.SetValue(Canvas.TopProperty, my - display.text.FontSize / 2.0);
                display.text.SetValue(Canvas.LeftProperty, mx - display.text.ActualWidth / 2.0);
            }
        }



        public int CellColor
        {
            get { return (int)GetValue(CellColorProperty); }
            set { SetValue(CellColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CellColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CellColorProperty =
            DependencyProperty.Register("CellColor", typeof(int), typeof(CellDisplay), new PropertyMetadata(0));



        public int TargetCount
        {
            get { return (int)GetValue(TargetCountProperty); }
            set { SetValue(TargetCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetCountProperty =
            DependencyProperty.Register("TargetCount", typeof(int), typeof(CellDisplay), new PropertyMetadata(-1, new PropertyChangedCallback(OnTargetChanged)));

        private static void OnTargetChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            CellDisplay display = (CellDisplay)source;
            display.text.Text = display.TargetCount.ToString();
            UpdatePosition(display);
            UpdateDisplayed(display);
        }


        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FontFamily.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(CellDisplay), new PropertyMetadata(new PropertyChangedCallback(OnFontFamilyChanged)));


        private static void OnFontFamilyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            CellDisplay display = (CellDisplay)source;
            display.text.FontFamily = display.FontFamily;
            UpdatePosition(display);
            UpdateDisplayed(display);
        }


        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(CellDisplay), new PropertyMetadata(new PropertyChangedCallback(OnFontSizeChanged)));


        private static void OnFontSizeChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            CellDisplay display = (CellDisplay)source;
            display.text.FontSize = display.FontSize;
            UpdatePosition(display);
            UpdateDisplayed(display);
        }


    }
}
