using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows;

namespace DataRecollection.Animations
{
    class UIAnimations
    {
        public static void FadeInAnimation(double duration, FrameworkElement[] elements)
        {
            var sb = new Storyboard();
            var fadeIn = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                From = 0.0,
                To = 1.0
            };

            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            sb.Children.Add(fadeIn);

            foreach (FrameworkElement element in elements)
            {
                sb.Begin(element);
            }
        }

        public static void FadeOutAnimation(double duration, FrameworkElement[] elements)
        {
            var sb = new Storyboard();
            var fadeIn = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                From = 1.0,
                To = 0.0
            };

            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            sb.Children.Add(fadeIn);

            foreach (FrameworkElement element in elements)
            {
                sb.Begin(element);
            }
        }

        public static void GradientAnimation(double duration, string[] from, string[] to, FrameworkElement element)
        {
            var sb = new Storyboard();

            for (int i = 0; i < from.Length; i++)
            {
                var colorAnimation = new ColorAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(duration)),
                    From = (Color)ColorConverter.ConvertFromString(from[i]),
                    To = (Color)ColorConverter.ConvertFromString(to[i])
                };

                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Canvas.Background).(LinearGradientBrush.GradientStops)[" + i + "].(GradientStop.Color)"));
                sb.Children.Add(colorAnimation);
            }

            sb.Begin(element);
        }
    }
}
