using IdentificationApp.Animations;
using IdentificationApp.Source;
using Microsoft.Kinect.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IdentificationApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectManager kinect = null;
        private DataCapture capture = null;
        private FaceID faceId = null;

        private Storyboard flashAnimation;

        private Random rnd;

        private long nextCapture = 0;
        private int captureCooldown = 3000;

        bool fullScreen = false;
        bool waitingToReturn = false;

        private enum CaptureStage
        {
            Idle,
            Tracking,
            Capture,
            Identification,
            Matched,
            FirstTime,
            End
        }

        CaptureStage stage = CaptureStage.Idle;

        public MainWindow()
        {
            kinect = new KinectManager();
            capture = new DataCapture();
            faceId = new FaceID();

            rnd = new Random();

            kinect.OnStartTracking += PersonEnter;
            kinect.OnStopTracking += PersonLeave;

            KinectRegion.SetKinectRegion(this, kinectRegion);

            this.DataContext = this;
            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
            this.InitializeComponent();

            Mouse.OverrideCursor = Cursors.None;
        }

        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && kinect.IsTracking() && DateTime.Now.Ticks > nextCapture)
            {
                capture.NewSubject();

                CaptureData(0);

                nextCapture = DateTime.Now.Ticks + captureCooldown;
            }
            if (e.Key == Key.F11)
            {
                if (fullScreen)
                {
                    WindowStyle = WindowStyle.ToolWindow;
                    WindowState = WindowState.Normal;
                    ResizeMode = ResizeMode.NoResize;
                    fullScreen = false;
                }
                else
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    ResizeMode = ResizeMode.NoResize;
                    fullScreen = true;
                }
            }
            if (e.Key == Key.F)
            {
                FlashAnimation();
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return kinect.GetBitmap(KinectManager.BitmapType.Color);
            }
        }

        public ImageSource IndexImageSource
        {
            get
            {
                return kinect.GetBitmap(KinectManager.BitmapType.BodyIndex);
            }
        }

        private void OnStartButton(object sender, RoutedEventArgs e)
        {
            if (stage == CaptureStage.Tracking)
            {
                stage = CaptureStage.Capture;
                faceId.Identify(0, new IdentifyCallback());
                SmallLabel.Visibility = Visibility.Collapsed;
                StartButton.SetValue(Grid.RowProperty, 2);
                ButtonLabel.SetValue(Grid.RowProperty, 2);
                ButtonLabel.Text = "Continuar";
                BottomPanel.Visibility = Visibility.Visible;
                BottomPanelText.Visibility = Visibility.Visible;
                VideoCapture.Visibility = Visibility.Visible;
                TemplateImage.Visibility = Visibility.Visible;
                MainLabel.Visibility = Visibility.Collapsed;
            }
            else if (stage == CaptureStage.Capture)
            {
                stage = CaptureStage.Identification;
                capture.NewSubject();
                CaptureData(0);
                TemplateImage.Visibility = Visibility.Collapsed;
                BottomPanelText.Text = "Imagenes de guia apareceran en la pantalla, por cada una intenta imitar la orientación de la cabeza mostrada en la imagen. Presiona continuar para empezar las capturas.";
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            kinect.CloseReaders();
        }

        private void PersonEnter()
        {
            if (!waitingToReturn && stage == CaptureStage.Idle)
            {
                stage = CaptureStage.Tracking;
                PersonEnterAnimation();
            }
            waitingToReturn = false;
        }

        private void PersonEnterAnimation()
        {
            SmallLabel.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Visible;
            ButtonLabel.Visibility = Visibility.Visible;
            string[] fromGradient = { "#ff1f719b", "#ff238aad", "#ff33a3bc", "#ff4cbcc9", "#ff6bd5d3" };
            string[] toGradient = { "#ffd52941", "#ffe45f42", "#ffee894c", "#fff6b061", "#fffcd581" };
            UIAnimations.GradientAnimation(1.0, fromGradient, toGradient, BGCanvas);
            UIAnimations.FadeInAnimation(1.0, new FrameworkElement[] { StartButton, ButtonLabel, SmallLabel });
        }

        private void PersonLeave()
        {
            if (stage == CaptureStage.End || stage == CaptureStage.Tracking)
            {
                stage = CaptureStage.Idle;
                waitingToReturn = false;
                PersonLeaveAnimation();
            }
            else if (stage != CaptureStage.Idle)
            {
                waitingToReturn = true;
                Task.Delay(3500).ContinueWith(t => WaitToReturn());
            }
        }

        private void PersonLeaveAnimation()
        {
            ButtonLabel.Text = "Empezar";
            ButtonLabel.SetValue(Grid.RowProperty, 3);
            ButtonLabel.Visibility = Visibility.Collapsed;
            StartButton.SetValue(Grid.RowProperty, 3);
            StartButton.Visibility = Visibility.Collapsed;
            BottomPanel.Visibility = Visibility.Collapsed;
            BottomPanelText.Visibility = Visibility.Collapsed;
            BottomPanelText.Text = "Ubícate para que tu cabeza se alinee con la imagen. Presiona continuar cuando hayas terminado.";
            SmallLabel.Text = "Levanta tu mano y presiona el boton para empezar.";
            SmallLabel.Visibility = Visibility.Collapsed;
            MainLabel.Text = "¿Quieres ayudar a crear un algoritmo de identificación facial?";
            MainLabel.Visibility = Visibility.Visible;
            VideoCapture.Visibility = Visibility.Collapsed;

            string[] fromGradient = { "#ffd52941", "#ffe45f42", "#ffee894c", "#fff6b061", "#fffcd581" };
            string[] toGradient = { "#ff1f719b", "#ff238aad", "#ff33a3bc", "#ff4cbcc9", "#ff6bd5d3" };
            UIAnimations.GradientAnimation(1.0, fromGradient, toGradient, BGCanvas);
        }

        void FlashAnimation()
        {
            if (flashAnimation == null)
            {
                flashAnimation = FindResource("FlashAnimation") as Storyboard;
            }

            flashAnimation.Begin();
        }

        void WaitToReturn()
        {
            if (waitingToReturn)
            {
                this.Dispatcher.Invoke(() =>
                {
                    stage = CaptureStage.Idle;
                    waitingToReturn = false;
                    PersonLeaveAnimation();
                });
            }
        }

        private void CaptureData(int captureNumber)
        {
            Rect irRect = kinect.GetInfraredRect();

            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Color), "color", captureNumber);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Depth), "depth", captureNumber);
            capture.CaptureData(kinect.GetData(KinectManager.BitmapType.Depth), "depth", captureNumber, (int)irRect.Width, (int)irRect.Height);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Infrared), "infrared", captureNumber);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.BodyIndex), "index", captureNumber);
            FlashAnimation();
        }

        public class IdentifyCallback : IFaceIDCallback
        {
            public void Match(string subject)
            {
                Console.WriteLine(subject);
            }

            public void FirstTime()
            {
                Console.WriteLine("First Time");
            }

            public void Error(string error)
            {
                Console.WriteLine(error);
            }
        }
    }
}
