using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Library.Wpf.Mvvm;

namespace Microscope
{

    public class MicroscopeViewModel : ObservableBase, IClosing
    {

        private VideoCaptureDevice _camera;


        private FilterInfo _cameraInfo;
        public FilterInfo CameraInfo
        {
            get => _cameraInfo;
            set => SetField(ref _cameraInfo, value);
        }

        private BitmapSource _currentFrame;
        public BitmapSource CurrentFrame
        {
            get => _currentFrame;
            set => SetField(ref _currentFrame, value);
        }

        private ObservableCollection<FilterInfo> _deviceList;
        public ObservableCollection<FilterInfo> DeviceList
        {
            get => _deviceList;
            set => SetField(ref _deviceList, value);
        }

        private bool _playEnabled;
        public bool PlayEnabled
        {
            get => _playEnabled;
            set => SetField(ref _playEnabled, value);
        }

        public MicroscopeViewModel()
        {

            _deviceList = new ObservableCollection<FilterInfo>();
            DeviceList = new ObservableCollection<FilterInfo>();

            //set CameraInfo so SelectedItem gets populated in ComboBox
            DeviceList.CollectionChanged += (sender, e) =>
            {
                if (!DeviceList.Contains(CameraInfo))
                {
                    CameraInfo = DeviceList.FirstOrDefault();
                }
            };

            foreach (FilterInfo vcd in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                if (!string.IsNullOrEmpty(vcd.Name))
                    DeviceList.Add(vcd);
            }

            PlayEnabled = true;

        }


        private RelayCommand _startCommand;
        public ICommand StartCommand
        {
            get { return _startCommand ?? (_startCommand = new RelayCommand(param => this.Start())); }
        }

        public void Start()
        {
            _camera = new VideoCaptureDevice(CameraInfo.MonikerString);
            _camera.NewFrame += GetCameraFrame;
            _camera.Start();
            PlayEnabled = false;


        }

        private RelayCommand _stopCommand;
        public ICommand StopCommand
        {
            get { return _stopCommand ?? (_stopCommand = new RelayCommand(param => this.Stop())); }
        }

        public void Stop()
        {
            _camera.SignalToStop();
            PlayEnabled = true;
            CurrentFrame = null;
            _camera.NewFrame -= GetCameraFrame;
            _camera = null;

        }



        private void GetCameraFrame(object sender, NewFrameEventArgs eventArgs)
        {

            using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
            using (var stream = new MemoryStream())
            {
                var image = new BitmapImage();

                image.BeginInit();
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                bitmap.Save(stream, ImageFormat.Bmp);
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                CurrentFrame = image;
            }

        }

        public bool OnClosing()
        {
            if (_camera == null) return true;

            _camera.SignalToStop();
            PlayEnabled = true;
            CurrentFrame = null;
            _camera.NewFrame -= GetCameraFrame;
            _camera = null;
            return true;
        }

    }


}
