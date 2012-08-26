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
        KinectSensor kinect;
        int conteoDePersonasEscena = 0;
        int conteoDePersonasRastreadas = 0;
      
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

                //Habilitamos la cámara de color, y el rastreo de esqueletos
                kinect.ColorStream.Enable();
                kinect.SkeletonStream.Enable();

                // Creamos los manejadores de eventos de color y esqueleto
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                // Iniciamos el Kinect y sus cámaras
                kinect.Start();
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
                }
            }
            LblpersonasRastreadas.Content = conteoDePersonasRastreadas;
            conteoDePersonasRastreadas = 0;
        }

        byte[] datosColor = null;

        WriteableBitmap imagenMapaDeBits = null;

        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imagenColor = e.OpenColorImageFrame())
            {
                // Nos aseguramos que la imagen entregada por el kinect no sea invalida y genere error 
                if (imagenColor == null) return;

                // Creamos un contenedor para la imagen de color
                if (datosColor == null)
                {
                    datosColor = new byte[imagenColor.PixelDataLength];
                }

                // Copiamos la imagen al contenedor
                imagenColor.CopyPixelDataTo(datosColor);

                // La primera vez se crea la imagen
                if (imagenMapaDeBits == null)
                {
                    imagenMapaDeBits = new WriteableBitmap(imagenColor.Width, imagenColor.Height, 96, 96, PixelFormats.Bgr32, null);
                }

                // Luego se reescribe cada vez que el kinect envia una imagen al manejador de eventos
                imagenMapaDeBits.WritePixels(
                    new Int32Rect(0, 0, imagenColor.Width, imagenColor.Height),
                    datosColor,
                    imagenColor.Width * imagenColor.BytesPerPixel,
                    0);

                // Mostramos la imagen en el GUI
                ColorGUI.Source = imagenMapaDeBits;
            }
        }

    }
}
