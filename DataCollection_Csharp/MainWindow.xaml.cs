//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace DataColletion
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using DataColletion.Source;
    using Microsoft.Kinect.Wpf.Controls;
    using DataColletion.Animations;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    public partial class MainWindow : Window
    {
        private KinectManager kinect = null;
        private DataCapture capture = null;

        private Storyboard flashAnimation;
        private Storyboard timerAnimation;

        private Random rnd;

        private long nextCapture = 0;
        private int captureCooldown = 3000;

        bool fullScreen = false;
        bool waitingToReturn = false;
        int currentCapture = -1;

        private enum CaptureStage
        {
            Idle,
            Tracking,
            Agreement,
            Alignment,
            Demo,
            ImageCaptures,
            AudioCapture,
            End
        }

        CaptureStage stage = CaptureStage.Idle;

        public MainWindow()
        {
            kinect = new KinectManager();
            capture = new DataCapture();

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
                TimerAnimation();
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
                stage = CaptureStage.Agreement;
                ButtonLabel.Text = "Aceptar";
                MainLabel.Text = "Vamos a tomar una serie de fotos que ayudaran a entrenar el algoritmo";
                SmallLabel.Text = "Debes aceptar darnos permiso de usar las imágenes capturadas en el entrenamiento del algoritmo. \n * Estas imágenes no serán publicadas ni mostradas en ninguna parte.";
            }
            else if (stage == CaptureStage.Agreement)
            {
                stage = CaptureStage.Alignment;
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
            else if (stage == CaptureStage.Alignment)
            {
                stage = CaptureStage.Demo;
                capture.NewSubject();
                CaptureData(0);
                currentCapture = 1;
                TemplateImage.Visibility = Visibility.Collapsed;
                BottomPanelText.Text = "Imagenes de guia apareceran en la pantalla, por cada una intenta imitar la orientación de la cabeza mostrada en la imagen. Presiona continuar para empezar las capturas.";
                ImgReference.Visibility = Visibility.Visible;
                ImgReferenceBG.Visibility = Visibility.Visible;
                DemoReel();
            }
            else if (stage == CaptureStage.Demo)
            {
                stage = CaptureStage.ImageCaptures;
                StartButton.Visibility = Visibility.Collapsed;
                ButtonLabel.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Collapsed;
                BottomPanelText.Visibility = Visibility.Collapsed;
                ImgReference.Source = (ImageSource)FindResource("Ref_1");

                Task.Delay(3000).ContinueWith(t => NextCapture());
            }
        }

        private void NextCapture()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (currentCapture < 9) CaptureData(currentCapture);

                currentCapture += 1;
                if (currentCapture < 9 && stage == CaptureStage.ImageCaptures)
                {
                    ImgReference.Source = (ImageSource)FindResource("Ref_" + currentCapture);
                    Task.Delay(3000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 9 && stage == CaptureStage.ImageCaptures)
                {
                    stage = CaptureStage.AudioCapture;
                    capture.StartAudioRecording("Name");
                    VideoCapture.Visibility = Visibility.Collapsed;
                    ImgReference.Visibility = Visibility.Collapsed;
                    ImgReferenceBG.Visibility = Visibility.Collapsed;
                    MainLabel.Visibility = Visibility.Visible;
                    MainLabel.Text = "Por favor di tu nombre.";
                    MainLabel.VerticalAlignment = VerticalAlignment.Center;
                    TimerAnimation();
                    Task.Delay(8000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 10 && stage == CaptureStage.AudioCapture)
                {
                    capture.EndAudioRecording();
                    capture.StartAudioRecording("Email");
                    MainLabel.Text = "Di tu correo electrónico si deseas ser contactado con información y avances del proyecto.";
                    TimerAnimation();
                    Task.Delay(8000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 11 && stage == CaptureStage.AudioCapture)
                {
                    stage = CaptureStage.End;
                    capture.EndAudioRecording();
                    ButtonLabel.Text = "Empezar";
                    SmallLabel.Text = "Levanta tu mano y presiona el boton para empezar.";
                    MainLabel.Text = "Muchas gracias por tu participación!";
                    Timer.Visibility = Visibility.Collapsed;
                    TimerBG.Visibility = Visibility.Collapsed;
                }
            });
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
            ImgReference.Visibility = Visibility.Collapsed;
            ImgReferenceBG.Visibility = Visibility.Collapsed;
            Timer.Visibility = Visibility.Collapsed;
            TimerBG.Visibility = Visibility.Collapsed;

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

        void TimerAnimation()
        {
            if (timerAnimation == null)
            {
                timerAnimation = FindResource("TimerAnimation") as Storyboard;
            }

            Timer.Visibility = Visibility.Visible;
            TimerBG.Visibility = Visibility.Visible;
            timerAnimation.Begin();
        }

        void DemoReel()
        {
            if (stage == CaptureStage.Demo)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ImgReference.Source = (ImageSource)FindResource("Ref_" + rnd.Next(1, 9));
                    Task.Delay(1000).ContinueWith(t => DemoReel());
                });
            }
        }

        void WaitToReturn()
        {
            if (waitingToReturn)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (stage == CaptureStage.ImageCaptures || stage == CaptureStage.AudioCapture || stage == CaptureStage.Demo)
                    {
                        capture.Incomplete(stage.ToString());
                    }
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
    }
}
