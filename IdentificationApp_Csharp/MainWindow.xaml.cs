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
        private IdentifyCallback callback;

        private Storyboard flashAnimation;

        private Random rnd;

        private long nextCapture = 0;
        int currentCapture = -1;
        int subjectToProcess;

        bool fullScreen = false;
        bool waitingToReturn = false;

        private enum CaptureStage
        {
            Idle,
            Tracking,
            Capture,
            Identify,
            Matched,
            FirstTime,
            Register,
            Demo,
            RegisterCaptures,
            End,
            BadMatch,
            Training
        }

        CaptureStage stage = CaptureStage.Idle;

        public MainWindow()
        {
            kinect = new KinectManager();
            capture = new DataCapture();
            faceId = new FaceID();
            callback = new IdentifyCallback(this);

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
                stage = CaptureStage.Capture;
                SmallLabel.Visibility = Visibility.Collapsed;
                Button1.SetValue(Grid.RowProperty, 2);
                Button1Label.SetValue(Grid.RowProperty, 2);
                Button1Label.Text = "Continuar";
                BottomPanel.Visibility = Visibility.Visible;
                BottomPanelText.Visibility = Visibility.Visible;
                VideoCapture.Visibility = Visibility.Visible;
                TemplateImage.Visibility = Visibility.Visible;
                MainLabel.Visibility = Visibility.Collapsed;
            }
            else if (stage == CaptureStage.Capture)
            {
                stage = CaptureStage.Identify;
                MainLabel.Visibility = Visibility.Visible;
                MainLabel.Text = "Identificando...";
                int sbj = capture.NewSubjectIdentify();
                CaptureData(0, true);
                faceId.Identify(sbj, callback);
                TemplateImage.Visibility = Visibility.Collapsed;
                VideoCapture.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Collapsed;
                BottomPanelText.Visibility = Visibility.Collapsed;
                Button1.Visibility = Visibility.Collapsed;
                Button1Label.Visibility = Visibility.Collapsed;
            }
            else if (stage == CaptureStage.Matched)
            {
                stage = CaptureStage.End;
                IdentityImage.Visibility = Visibility.Collapsed;
                MainLabel.Visibility = Visibility.Visible;
                MainLabel.Text = "Genial!";
                Button1Label.Visibility = Visibility.Collapsed;
                Button1.Visibility = Visibility.Collapsed;
                Button2Label.Visibility = Visibility.Collapsed;
                Button2.Visibility = Visibility.Collapsed;
                BottomPanelText.Text = "Me alegra volver a verte. Muchas gracias por ayudarme a aprender.";
            }
            else if (stage == CaptureStage.BadMatch)
            {
                AskToRegister();
            }
            else if (stage == CaptureStage.FirstTime)
            {
                AskToRegister();
            }
            else if (stage == CaptureStage.Register)
            {
                stage = CaptureStage.Demo;
                MainLabel.Visibility = Visibility.Collapsed;
                TemplateImage.Visibility = Visibility.Collapsed;
                BottomPanelText.Text = "Imagenes de guia apareceran en la pantalla, por cada una intenta imitar la orientación de la cabeza mostrada en la imagen. Presiona continuar para empezar las capturas.";
                ImgReference.Visibility = Visibility.Visible;
                ImgReferenceBG.Visibility = Visibility.Visible;
                Button2.Visibility = Visibility.Collapsed;
                Button2Label.Visibility = Visibility.Collapsed;
                BottomPanel.SetValue(Grid.RowProperty, 3);
                BottomPanelText.SetValue(Grid.RowProperty, 3);
                Button1Label.SetValue(Grid.RowProperty, 2);
                Button1.SetValue(Grid.RowProperty, 2);
                Button2Label.SetValue(Grid.RowProperty, 2);
                Button2.SetValue(Grid.RowProperty, 2);
                VideoCapture.Visibility = Visibility.Visible;
                TemplateImage.Visibility = Visibility.Visible;
                DemoReel();
            }
            else if (stage == CaptureStage.Demo)
            {
                stage = CaptureStage.RegisterCaptures;
                subjectToProcess = capture.NewSubjectRegistry();
                CaptureData(0, false);
                currentCapture = 1;
                Button1.Visibility = Visibility.Collapsed;
                Button1Label.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Collapsed;
                BottomPanelText.Visibility = Visibility.Collapsed;
                TemplateImage.Visibility = Visibility.Collapsed;
                ImgReference.Source = (ImageSource)FindResource("Ref_1");
                Task.Delay(3000).ContinueWith(t => NextCapture());
            }
        }

        private void OnNoButton(object sender, RoutedEventArgs e)
        {
            if (stage == CaptureStage.Matched)
            {
                stage = CaptureStage.BadMatch;
                IdentityImage.Visibility = Visibility.Collapsed;
                MainLabel.Visibility = Visibility.Visible;
                MainLabel.Text = "Perdon por confundirte. ¿Es la primera vez que te veo?";
                BottomPanel.Visibility = Visibility.Collapsed;
                BottomPanelText.Visibility = Visibility.Collapsed;
                Button1.SetValue(Grid.RowProperty, 3);
                Button1Label.SetValue(Grid.RowProperty, 3);
                Button2.SetValue(Grid.RowProperty, 3);
                Button2Label.SetValue(Grid.RowProperty, 3);
            }
            else if (stage == CaptureStage.Register)
            {
                stage = CaptureStage.End;
                MainLabel.Visibility = Visibility.Visible;
                MainLabel.Text = "Muchas gracias por visitarme!";
                Button1Label.Visibility = Visibility.Collapsed;
                Button1.Visibility = Visibility.Collapsed;
                Button2Label.Visibility = Visibility.Collapsed;
                Button2.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Collapsed;
                BottomPanelText.Visibility = Visibility.Collapsed;
            }
            else if (stage == CaptureStage.FirstTime || stage == CaptureStage.BadMatch)
            {
                stage = CaptureStage.End;
                MainLabel.Visibility = Visibility.Visible;
                MainLabel.Text = "Lo siento :(";
                Button1Label.Visibility = Visibility.Collapsed;
                Button1.Visibility = Visibility.Collapsed;
                Button2Label.Visibility = Visibility.Collapsed;
                Button2.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Visible;
                BottomPanelText.Visibility = Visibility.Visible;
                BottomPanelText.Text = "Entrenare de nuevo para mejorar. Vuelve pronto!";
                BottomPanel.SetValue(Grid.RowProperty, 3);
                BottomPanelText.SetValue(Grid.RowProperty, 3);
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
            Button1.Visibility = Visibility.Visible;
            Button1Label.Visibility = Visibility.Visible;
            string[] fromGradient = { "#ff1f719b", "#ff238aad", "#ff33a3bc", "#ff4cbcc9", "#ff6bd5d3" };
            string[] toGradient = { "#ffd52941", "#ffe45f42", "#ffee894c", "#fff6b061", "#fffcd581" };
            UIAnimations.GradientAnimation(1.0, fromGradient, toGradient, BGCanvas);
            UIAnimations.FadeInAnimation(1.0, new FrameworkElement[] { Button1, Button1Label, SmallLabel });
        }

        private void PersonLeave()
        {
            if (stage == CaptureStage.End || stage == CaptureStage.Tracking)
            {
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
            stage = CaptureStage.Idle;
            Button1Label.Text = "Empezar";
            Button1Label.Visibility = Visibility.Collapsed;
            Button1.Visibility = Visibility.Collapsed;
            Button2Label.Text = "No";
            Button2Label.Visibility = Visibility.Collapsed;
            Button2.Visibility = Visibility.Collapsed;
            BottomPanel.Visibility = Visibility.Collapsed;
            BottomPanelText.Visibility = Visibility.Collapsed;
            BottomPanelText.Text = "Ubícate para que tu cabeza se alinee con la imagen. Presiona continuar cuando hayas terminado.";
            SmallLabel.Text = "Levanta tu mano y presiona el boton para empezar.";
            SmallLabel.Visibility = Visibility.Collapsed;
            MainLabel.Text = "¿Quieres ayudar a crear un algoritmo de identificación facial?";
            MainLabel.Visibility = Visibility.Visible;
            VideoCapture.Visibility = Visibility.Collapsed;
            IdentityImage.Visibility = Visibility.Collapsed;
            ImgReference.Visibility = Visibility.Collapsed;
            ImgReferenceBG.Visibility = Visibility.Collapsed;
            BottomPanel.SetValue(Grid.RowProperty, 3);
            BottomPanelText.SetValue(Grid.RowProperty, 3);
            Button1Label.SetValue(Grid.RowProperty, 3);
            Button1.SetValue(Grid.RowProperty, 3);
            Button2Label.SetValue(Grid.RowProperty, 3);
            Button2.SetValue(Grid.RowProperty, 3);

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
                    waitingToReturn = false;
                    PersonLeaveAnimation();
                });
            }
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

        private void NextCapture()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (currentCapture < 9) CaptureData(currentCapture, false);

                currentCapture += 1;
                if (currentCapture < 9 && stage == CaptureStage.RegisterCaptures)
                {
                    ImgReference.Source = (ImageSource)FindResource("Ref_" + currentCapture);
                    Task.Delay(3000).ContinueWith(t => NextCapture());
                }
                else if (currentCapture == 9 && stage == CaptureStage.RegisterCaptures)
                {
                    stage = CaptureStage.End;
                    faceId.Process(subjectToProcess);
                    VideoCapture.Visibility = Visibility.Collapsed;
                    ImgReference.Visibility = Visibility.Collapsed;
                    ImgReferenceBG.Visibility = Visibility.Collapsed;
                    MainLabel.Visibility = Visibility.Visible;
                    MainLabel.Text = "Muchas Gracias!";
                    BottomPanel.Visibility = Visibility.Visible;
                    BottomPanelText.Visibility = Visibility.Visible;
                    BottomPanelText.Text = "Regresa otro dia y te reconocere.";
                }
            });
        }

        private void CaptureData(int captureNumber, bool identify)
        {
            Rect irRect = kinect.GetInfraredRect();

            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Color), "color", captureNumber, identify);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Depth), "depth", captureNumber, identify);
            capture.CaptureData(kinect.GetData(KinectManager.BitmapType.Depth), "depth", captureNumber, (int)irRect.Width, (int)irRect.Height, identify);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.Infrared), "infrared", captureNumber, identify);
            capture.CaptureImage(kinect.GetBitmap(KinectManager.BitmapType.BodyIndex), "index", captureNumber, identify);
            FlashAnimation();
        }

        public void AskToRegister()
        {
            stage = CaptureStage.Register;
            IdentityImage.Visibility = Visibility.Collapsed;
            MainLabel.Visibility = Visibility.Visible;
            MainLabel.Text = "Es un gusto conocerte";
            BottomPanelText.Text = "¿Me permitirias guardar fotos de ti para poder recordarte?";
            BottomPanel.Visibility = Visibility.Visible;
            BottomPanelText.Visibility = Visibility.Visible;
        }

        public void Matched(int subject)
        {
            if (stage == CaptureStage.Identify)
            {
                this.Dispatcher.Invoke(() =>
                {
                    stage = CaptureStage.Matched;
                    IdentityImage.Source = capture.GetIdentityBitmap(subject, 0);
                    IdentityImage.Visibility = Visibility.Visible;
                    MainLabel.Visibility = Visibility.Collapsed;
                    Button1Label.Text = "Si";
                    Button1Label.Visibility = Visibility.Visible;
                    Button1.Visibility = Visibility.Visible;
                    Button2Label.Text = "No";
                    Button2Label.Visibility = Visibility.Visible;
                    Button2.Visibility = Visibility.Visible;
                    BottomPanel.Visibility = Visibility.Visible;
                    BottomPanelText.Visibility = Visibility.Visible;
                    BottomPanelText.Text = "Pienso que ya te conozco. ¿Eres tu la persona de esta foto?";
                    BottomPanel.SetValue(Grid.RowProperty, 2);
                    BottomPanelText.SetValue(Grid.RowProperty, 2);
                    Button1Label.SetValue(Grid.RowProperty, 3);
                    Button1.SetValue(Grid.RowProperty, 3);
                    Button2Label.SetValue(Grid.RowProperty, 3);
                    Button2.SetValue(Grid.RowProperty, 3);
                });
            }
        }

        public void FirstTime()
        {
            if (stage == CaptureStage.Identify)
            {
                this.Dispatcher.Invoke(() =>
                {
                    stage = CaptureStage.FirstTime;
                    MainLabel.Text = "Eres nuevo";
                    Button1Label.Text = "Si";
                    Button1Label.Visibility = Visibility.Visible;
                    Button1.Visibility = Visibility.Visible;
                    Button2Label.Text = "No";
                    Button2Label.Visibility = Visibility.Visible;
                    Button2.Visibility = Visibility.Visible;
                    BottomPanel.Visibility = Visibility.Visible;
                    BottomPanelText.Visibility = Visibility.Visible;
                    BottomPanelText.Text = "Pienso que no te conozco. ¿Es la primera vez que te veo?";
                    BottomPanel.SetValue(Grid.RowProperty, 2);
                    BottomPanelText.SetValue(Grid.RowProperty, 2);
                    Button1Label.SetValue(Grid.RowProperty, 3);
                    Button1.SetValue(Grid.RowProperty, 3);
                    Button2Label.SetValue(Grid.RowProperty, 3);
                    Button2.SetValue(Grid.RowProperty, 3);
                });
            }
        }

        public class IdentifyCallback : IFaceIDCallback
        {
            public MainWindow main;

            public IdentifyCallback(MainWindow from)
            {
                main = from;
            }

            public void Match(int subject)
            {
                main.Matched(subject);
            }

            public void FirstTime()
            {
                main.FirstTime();
            }
        }
    }
}
