using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace camera
{
    class Program
    {
        static void Main(string[] args)
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            


            var camera = new Camera(4);
            camera.Record(3);



        }


    }

    class Camera
    {


        private VideoCaptureDevice _camera;
        private int _frames;
        private bool _recording;

        public Camera(int frames)
        {
            _camera = new VideoCaptureDevice(new FilterInfoCollection(FilterCategory.VideoInputDevice)[2].MonikerString);
            _camera.NewFrame += new NewFrameEventHandler(video_NewFrame);
            _frames = frames;

        }

        public void Record(int frames)
        {
            _recording = true;
            _camera.Start();
            while (_recording)
            {
                
            }
            _camera.SignalToStop();
        }
        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // get new frame
            var bitmap = eventArgs.Frame;
            bitmap.Save("file"+_frames+".png");
            _frames--;
            if (_frames == 0)
                _recording = false;

            // process the frame
        }
    }
}
