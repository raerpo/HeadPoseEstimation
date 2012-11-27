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


//============================================================================================================================
// Lista de tareas por hacer:

// *Cambiar el nombre de los objetos del GUI de tal forma que sea evidente si es un label, boton, canvas, etc


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
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
            
            try
            {
                inicializar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Para que este programa funcione debe contar con Kinect conectado al equipo");
            }

        }

//====================================================================================================================
        // Metodo que inicaliza el sensor kinect
        // habilita los streams de datos del kinect y crea los manejadores de eventos de esqueleto y profundidad, ademas 
        // de los manejadores que controlan el slider "sldInclinacion" 
        void inicializar()
        {
            kinect = KinectSensor.KinectSensors[0];

            kinect.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
            kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            kinect.SkeletonStream.Enable();

            // Creamos los manejadores de eventos
            kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
            inclinacion.PreviewMouseUp += new MouseButtonEventHandler(inclinacion_PreviewMouseUp);
            inclinacion.ValueChanged += new RoutedPropertyChangedEventHandler<double>(inclinacion_ValueChanged);

            // Iniciamos el Kinect y sus cámaras
            kinect.Start();
            kinect.ElevationAngle = 0;
            LblkinectConectado.Content = "Si";
        }

        //=====================================================================================================================
        // Variables globales

        Skeleton[] skeletons = null;

        ColorImagePoint cabezaColor;

        DepthImagePoint cabezaProfundidad;


        byte[] imagenColorProfundidad = null;

        short[] datosProfundidad = null;

        WriteableBitmap imagenProfundidadMapaDeBits = null;


//=====================================================================================================================
        // Metodo que controla los cambios de estado del kinect
        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Status == KinectStatus.Connected)
            {
                inicializar();
                LblkinectConectado.Content = "Si";
            }
            if (e.Status == KinectStatus.Disconnected)
            {
                MessageBox.Show("El kinect ha sido desconectado, conectelo nuevamente para que el programa funcione");
                LblkinectConectado.Content = "No";
            }
        }


//=====================================================================================================================
        // Metodo que se dispara cuando el kinect tiene el stream de datos de color listo
        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null) return;
                byte[] colorData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(colorData);

                BitmapSource imagenColor = BitmapSource.Create(
                    frame.Width, frame.Height,
                    96,
                    96,
                    PixelFormats.Gray16, // Imagen proveniente de la camara infraroja
                    null,
                    colorData,
                    frame.Width * frame.BytesPerPixel);

                Int32Rect coordenadasRegion;

                if (cabezaColor.X < 50 && cabezaColor.Y < 50)
                {
                    coordenadasRegion = new Int32Rect(cabezaColor.X, cabezaColor.Y, 100, 100);
                }
                else
                {
                    coordenadasRegion = new Int32Rect(cabezaColor.X - 50, cabezaColor.Y - 50, 100, 100);
                }
                    byte[] regionData = new byte[100 * 100 * frame.BytesPerPixel];
                    imagenColor.CopyPixels(coordenadasRegion, regionData, 100 * frame.BytesPerPixel, 0);
                    BitmapSource regionImage = BitmapSource.Create(
                    100, 100, 96, 96, PixelFormats.Gray16, null, regionData, 100 * frame.BytesPerPixel);
                    imgColorCabeza1.Source = regionImage;     
            }
        }

//=====================================================================================================================
        // Metodo que se dispara cuando el kinect tiene el stream de datos de los esqueletos listo
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

            //Devuelve la variable "conteoPersonasEscena"
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


            #region OperacionesConEsqueletos
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    conteoDePersonasRastreadas++;

                    if (conteoDePersonasRastreadas == 1)
                    {

                        // Obtener las coordenadas de la cabeza de la primera persona tanto en color como
                        // en profundidad
                        SkeletonPoint cabeza = skeleton.Joints[JointType.Head].Position;
                        cabezaColor = kinect.MapSkeletonPointToColor(cabeza, ColorImageFormat.RgbResolution640x480Fps30);
                        cabezaProfundidad = kinect.MapSkeletonPointToDepth(cabeza, DepthImageFormat.Resolution640x480Fps30);

                        lblPrueba1.Content = cabezaColor.X;
                        lblPrueba2.Content = cabezaColor.Y;

                        double angulo1 = ObtenerAnguloEspalda(skeleton);

                        // Rotamos la imagen que representa el angulo de la primera cabeza
                        anguloCabezaImagen1.RenderTransform = new RotateTransform(
                            angulo1, anguloCabezaImagen1.Width / 2, anguloCabezaImagen1.Height / 2);
                    }

                    if (conteoDePersonasRastreadas == 2)
                    {

                        double angulo2 = ObtenerAnguloEspalda(skeleton);

                        // Rotamos la imagen que representa el angulo de la primera cabeza
                        anguloCabezaImagen2.RenderTransform = new RotateTransform(
                            angulo2, anguloCabezaImagen2.Width / 2, anguloCabezaImagen2.Height / 2);
                    }
                }
            }
            LblpersonasRastreadas.Content = conteoDePersonasRastreadas;
            conteoDePersonasRastreadas = 0;
            #endregion


        }


//=====================================================================================================================
        // Metodo que se dispara cuando el kinect tiene un frame de profundidad listo
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

//======================================================================================================================
        // angulo es el label que muestra en el GUI el angulo de inclinacion del kinect
        void inclinacion_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            angulo.Content = (int)inclinacion.Value;
        }

//======================================================================================================================
        // inclinacion es el nombre del slider que controla el angulo de elevacion del kinect
        // El evento PreviewMouseUP se dispara cuando el mouse suelta un clic encima del objeto en el GUI
        void inclinacion_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            kinect.ElevationAngle = (int)inclinacion.Value;
        }

//======================================================================================================================
        // El metodo obtenerAnguloEspalda recibe como argumento una variable de tipo "Skeleton" y entrega
        // el angulo en grados (double) del cuerpo con respecto al kinect. 
        
        // *Por el momento solo funciona si las personas
        // rastreadas estan de frente al Kinect ya que si la persona esta de espalda el angulo es erroneo*
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

//======================================================================================================================
        // Evento que se dispara cuando la ventana esta siendo cerrada.
        private void Window_Closed(object sender, EventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.SkeletonStream.Disable();
                kinect.Stop();
            }
        }

    }
}



