//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace DataRecollection
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using DataRecollection.Source;
    using Microsoft.Kinect.Wpf.Controls;
    using DataRecollection.Animations;

    public partial class MainWindow : Window
    {
        private KinectManager kinect = null;
        private DataCapture capture = null;

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
            ImageCaptures,
            AudioCapture,
            End
        }

        CaptureStage stage = CaptureStage.Idle;

        public MainWindow()
        {
            kinect = new KinectManager();
            capture = new DataCapture();

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
                stage = CaptureStage.ImageCaptures;
                capture.NewSubject();
                currentCapture = 0;
                StartButton.Visibility = Visibility.Collapsed;
                ButtonLabel.Visibility = Visibility.Collapsed;
                SmallLabel.Visibility = Visibility.Collapsed;
                VideoCapture.Visibility = Visibility.Visible;
                Circle.Visibility = Visibility.Visible;
                Circle.HorizontalAlignment = HorizontalAlignment.Left;
                MainLabel.Text = "Gira tu cabeza hacia la dirección del punto";
                MainLabel.VerticalAlignment = VerticalAlignment.Top;
                MainLabel.FontSize = 82;
                Task.Delay(4000).ContinueWith(t => NextCapture());
            }
        }

        private void NextCapture()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (currentCapture < 3) CaptureData(currentCapture);

                currentCapture += 1;
                if (currentCapture == 1 && stage == CaptureStage.ImageCaptures)
                {
                    Circle.HorizontalAlignment = HorizontalAlignment.Center;
                    Task.Delay(2000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 2 && stage == CaptureStage.ImageCaptures)
                {
                    Circle.HorizontalAlignment = HorizontalAlignment.Right;
                    Task.Delay(2000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 3 && stage == CaptureStage.ImageCaptures)
                {
                    stage = CaptureStage.AudioCapture;
                    capture.StartAudioRecording("Name");
                    Circle.Visibility = Visibility.Collapsed;
                    MainLabel.Text = "Por favor di tu nombre.";
                    MainLabel.VerticalAlignment = VerticalAlignment.Center;
                    Task.Delay(5000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 4 && stage == CaptureStage.AudioCapture)
                {
                    capture.EndAudioRecording();
                    capture.StartAudioRecording("Email");
                    Circle.Visibility = Visibility.Collapsed;
                    MainLabel.Text = "Di tu correo electrónico si deseas ser contactado con información y avances del proyecto.";
                    Task.Delay(7000).ContinueWith(t => NextCapture());
                }
                else if (stage == CaptureStage.AudioCapture)
                {
                    stage = CaptureStage.End;
                    capture.EndAudioRecording();
                    ButtonLabel.Text = "Empezar";
                    SmallLabel.Text = "Levanta tu mano y presiona el boton para empezar.";
                    MainLabel.Text = "Muchas gracias por tu participación!";
                    MainLabel.VerticalAlignment = VerticalAlignment.Center;
                    MainLabel.FontSize = 142;
                    VideoCapture.Visibility = Visibility.Collapsed;
                    SmallLabel.Visibility = Visibility.Collapsed;
                    StartButton.Visibility = Visibility.Collapsed;
                    ButtonLabel.Visibility = Visibility.Collapsed;
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
            if (stage != CaptureStage.Idle)
            {
                waitingToReturn = true;
                Task.Delay(1000).ContinueWith(t => WaitToReturn());
            }
        }

        private void PersonLeaveAnimation()
        {
            ButtonLabel.Text = "Empezar";
            SmallLabel.Text = "Levanta tu mano y presiona el boton para empezar.";
            MainLabel.Text = "¿Quieres ayudar a crear un algoritmo de identificación facial?";
            MainLabel.VerticalAlignment = VerticalAlignment.Center;
            MainLabel.FontSize = 92;
            VideoCapture.Visibility = Visibility.Collapsed;
            SmallLabel.Visibility = Visibility.Collapsed;
            StartButton.Visibility = Visibility.Collapsed;
            ButtonLabel.Visibility = Visibility.Collapsed;
            Circle.Visibility = Visibility.Collapsed;

            string[] fromGradient = { "#ffd52941", "#ffe45f42", "#ffee894c", "#fff6b061", "#fffcd581" };
            string[] toGradient = { "#ff1f719b", "#ff238aad", "#ff33a3bc", "#ff4cbcc9", "#ff6bd5d3" };
            UIAnimations.GradientAnimation(1.0, fromGradient, toGradient, BGCanvas);
        }

        void WaitToReturn()
        {
            if (waitingToReturn)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (stage == CaptureStage.ImageCaptures || stage == CaptureStage.AudioCapture)
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
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Color), "color", captureNumber);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Depth), "depth", captureNumber);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Infrared), "infrared", captureNumber);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.BodyIndex), "index", captureNumber);

            var mesh = kinect.GetMesh();
            var meshIndices = kinect.GetMeshIndices();
            var faceResults = kinect.GetTrackedFaceData();

            if (mesh != null && mesh.Count > 0)
            {
                capture.CaptureFaceMesh(mesh, meshIndices, captureNumber);
            }

            if (faceResults != null)
            {
                capture.WriteCaptureInfo(faceResults, captureNumber);
            }
        }
    }
}
