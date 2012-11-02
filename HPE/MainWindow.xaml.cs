using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace HPE
{
    public partial class MainWindow : Window
    {
        #region variables

        KinectSensor kinect;
        int conteoDePersonasEscena = 0;
        int conteoDePersonasRastreadas = 0;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Si no es posible inicializar el kinect mandamos un mensaje de error
            try
            {
                kinect = KinectSensor.KinectSensors[0];

                //Habilitamos la cámara de color(IR), y el rastreo de esqueletos
                kinect.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
                kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                kinect.SkeletonStream.Enable();

                // Creamos el manejador de eventos
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);

                inclinacion.PreviewMouseUp += new MouseButtonEventHandler(inclinacion_PreviewMouseUp);
                inclinacion.ValueChanged += new RoutedPropertyChangedEventHandler<double>(inclinacion_ValueChanged);

                // Iniciamos el Kinect y sus cámaras
                kinect.Start();
                kinect.ElevationAngle = 0;
            }
            catch
            {
                MessageBox.Show(
                "No se puede iniciar el programa. Esto puede suceder por varios motivos. " +
                "Verifique que tiene por lo menos un Kinect conectado y que ninguna otra aplicación " +
                "esta haciendo uso de el. Tambien es posible que no tenga suficiciente memoria para ejecutar el programa", "Error");
                Application.Current.Shutdown();
            }
        }



        Skeleton[] skeletons = null;

        byte[] imagenColorProfundidad = null;

        short[] datosProfundidad = null;

        WriteableBitmap imagenProfundidadMapaDeBits = null;



        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);
                }
            }

            if (skeletons == null) return;

            #region ConteoDePersonasEnLaEscena

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    conteoDePersonasEscena++;
                }
            }
            LblpersonasEnEscena.Content = conteoDePersonasEscena;
            conteoDePersonasEscena = 0;

            #endregion

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    conteoDePersonasRastreadas++;
                    Joint headJoint = skeleton.Joints[JointType.Head];
                    SkeletonPoint headPosition = headJoint.Position;

                    double angulo = ObtenerAnguloEspalda(skeleton);

                    anguloCabezaImagen1.RenderTransform = new RotateTransform(angulo, anguloCabezaImagen1.Width / 2, anguloCabezaImagen1.Height / 2);

                }
            }
            LblpersonasRastreadas.Content = conteoDePersonasRastreadas;
            conteoDePersonasRastreadas = 0;
        }

        void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame imagenProfundidad = e.OpenDepthImageFrame())
            {

                if (imagenProfundidad == null) return;

                if (datosProfundidad == null)
                {
                    datosProfundidad = new short[imagenProfundidad.PixelDataLength];
                }

                if (imagenColorProfundidad == null)
                {
                    imagenColorProfundidad = new byte[imagenProfundidad.PixelDataLength * 4];
                }

                imagenProfundidad.CopyPixelDataTo(datosProfundidad);

                int depthColorImagePos = 0;

                for (int depthPos = 0; depthPos < imagenProfundidad.PixelDataLength; depthPos++)
                {
                    int depthValue = datosProfundidad[depthPos] >> 3;
                    // Check for the invalid values 
                    if (depthValue == kinect.DepthStream.UnknownDepth)
                    {
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Blue 
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Green 
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Red 
                    }
                    else if (depthValue == kinect.DepthStream.TooFarDepth)
                    {
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Blue 
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Green 
                        imagenColorProfundidad[depthColorImagePos++] = 0; // Red 
                    }
                    else if (depthValue == kinect.DepthStream.TooNearDepth)
                    {
                        imagenColorProfundidad[depthColorImagePos++] = 255; // Blue 
                        imagenColorProfundidad[depthColorImagePos++] = 255; // Green 
                        imagenColorProfundidad[depthColorImagePos++] = 255; // Red 
                    }
                    else
                    {
                        byte depthByte = (byte)(255 - (depthValue >> 4));
                        imagenColorProfundidad[depthColorImagePos++] = depthByte; // Blue 
                        imagenColorProfundidad[depthColorImagePos++] = depthByte; // Green 
                        imagenColorProfundidad[depthColorImagePos++] = depthByte; // Red 
                    }
                    // transparency 
                    depthColorImagePos++;
                }

                // we now have a new array of color data 

                if (imagenProfundidadMapaDeBits == null)
                {
                    imagenProfundidadMapaDeBits = new WriteableBitmap(
                        imagenProfundidad.Width,
                        imagenProfundidad.Height,
                        96,  // DpiX 
                        96,  // DpiY 
                        PixelFormats.Bgr32,
                        null);
                    profundidadGUI.Source = imagenProfundidadMapaDeBits;
                }

                imagenProfundidadMapaDeBits.WritePixels(new Int32Rect(0, 0, imagenProfundidad.Width, imagenProfundidad.Height), imagenColorProfundidad, imagenProfundidad.Width * 4, 0);

            }
        }

        void inclinacion_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            angulo.Content = (int)inclinacion.Value;
        }

        void inclinacion_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            kinect.ElevationAngle = (int)inclinacion.Value;
        }

        double ObtenerAnguloEspalda(Skeleton esqueleto)
        {
            Joint hombroIzquierdo = esqueleto.Joints[JointType.ShoulderLeft];
            Joint hombroDerecho = esqueleto.Joints[JointType.ShoulderRight];

            if (hombroDerecho.TrackingState == JointTrackingState.NotTracked ||
                hombroDerecho.TrackingState == JointTrackingState.Inferred)
            {
                return 90;
            }

            else if (hombroIzquierdo.TrackingState == JointTrackingState.NotTracked ||
                hombroIzquierdo.TrackingState == JointTrackingState.Inferred)
            {
                return -90;
            }
            else
            {
                return Math.Atan2(
                    hombroDerecho.Position.Z - hombroIzquierdo.Position.Z,
                    hombroDerecho.Position.X - hombroIzquierdo.Position.X) * 180.0 / Math.PI;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            kinect.ColorStream.Disable();
            kinect.DepthStream.Disable();
            kinect.SkeletonStream.Disable();
            kinect.Stop();
        }

    }
}



