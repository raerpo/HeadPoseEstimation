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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Obtiene el primer Kinect conectado
            kinect = KinectSensor.KinectSensors[0];
            
            //Habilitamos la cámara de color, la de profundidad, y el rastreo de esqueletos
            kinect.ColorStream.Enable();
            kinect.DepthStream.Enable();
            kinect.SkeletonStream.Enable();
            
            // Creamos los manejadores de eventos de color y esqueleto
            kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

            // Iniciamos el Kinect y sus cámaras
            kinect.Start();
        }

        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imagenColor = e.OpenColorImageFrame())
            {
                // Nos aseguramos que la imagen entregada por el kinect no sea invalida y genere error 
                if (imagenColor == null) return;

                // Creamos un contenedor para la imagen de color
                byte[] datosColor = new byte[imagenColor.PixelDataLength];                  
                // Copiamos la imagen al contenedor
                imagenColor.CopyPixelDataTo(datosColor);
                // Mostramos la imagen en el GUI
                ColorGUI.Source = BitmapSource.Create(
                    imagenColor.Width,
                    imagenColor.Height,
                    96, 96,
                    PixelFormats.Bgr32,
                    null,
                    datosColor,
                    imagenColor.Width * imagenColor.BytesPerPixel);
            }
        }
    }
}
