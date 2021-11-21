using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Library.Wpf.Mvvm;

namespace Microscope
{

    public class MicroscopeViewModel : ObservableBase, IClosing
    {

        private VideoCaptureDevice _camera;

        public struct pinlocation
        {
            public string pinnum;
            public double x;
            public double y;
        }

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

        private ImageSource _currentPinFrame;
        public ImageSource CurrentPinFrame
        {
            get => _currentPinFrame;
            set => SetField(ref _currentPinFrame, value);
        }

        private ObservableCollection<FilterInfo> _deviceList;
        public ObservableCollection<FilterInfo> DeviceList
        {
            get => _deviceList;
            set => SetField(ref _deviceList, value);
        }

        private string _profileName;
        public string ProfileName
        {
            get => _profileName;
            set => SetField(ref _profileName, value);
        }
        //original profile read from the csv
        private Dictionary<string, pinlocation> _originalProfile;
        public Dictionary<string, pinlocation> OriginalProfile
        {
            get => _originalProfile;
            set => SetField(ref _originalProfile, value);
        }
        //translated to the GUI image xy location
        private Dictionary<string, pinlocation> _imageProfile;
        public Dictionary<string, pinlocation> ImageProfile
        {
            get => _imageProfile;
            set => SetField(ref _imageProfile, value);
        }

        private ObservableCollection<string> _profileList;
        public ObservableCollection<string> ProfileList
        {
            get => _profileList;
            set => SetField(ref _profileList, value);
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

            //load profile
            _profileList = new ObservableCollection<string>();
            ProfileList = new ObservableCollection<string>();
            DirectoryInfo theFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profile"));
            FileInfo[] fileInfo = theFolder.GetFiles();
            foreach (FileInfo NextFile in fileInfo)  //遍历文件
                ProfileList.Add(NextFile.Name);

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


            ////pin
            //Bitmap bitmapPin = new Bitmap(500, 200);
            //Graphics gPin = Graphics.FromImage(bitmapPin);
            //Font fontPin = new System.Drawing.Font("宋体", 20, System.Drawing.FontStyle.Regular);
            //System.Drawing.Brush brushPin = new SolidBrush(System.Drawing.Color.White);
            //int MarginPin = 0;
            //gPin.DrawString("内容", fontPin, brushPin, 0 + MarginPin, 50 + MarginPin);
            //gPin.DrawString("内容", fontPin, brushPin, 0 + MarginPin, 77 + MarginPin);

            //IntPtr hBitmap = bitmapPin.GetHbitmap();
            //System.Windows.Media.ImageSource WpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());


            //CurrentPinFrame = WpfBitmap;
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
            CurrentPinFrame = null;
            _camera.NewFrame -= GetCameraFrame;
            _camera = null;

        }

        private RelayCommand _reloadprofileCommand;
        public ICommand ReloadProfileCommand
        {
            get { return _reloadprofileCommand ?? (_reloadprofileCommand = new RelayCommand(param => this.ReloadProfile())); }
        }


        //minx y for image shrink
        private double minx = 9999999999999;
        private double miny = 9999999999999;
        public void ReloadProfile()
        {
            OriginalProfile = new Dictionary<string, pinlocation>();
            
            using (StreamReader sr = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profile",ProfileName)))
            {
                string line;
                int ipointer = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    string[] arr = line.Split(',');
                    if (arr.Length==14 && ipointer>1)//do not parse first 2 line(header and begin)
                    {
                        pinlocation p;
                        p.pinnum = arr[1];
                        double x = 0;
                        double y = 0;
                        Double.TryParse(arr[3], out x);
                        Double.TryParse(arr[4], out y);
                        p.x = x;
                        p.y = y;
                        OriginalProfile.Add(p.pinnum, p);

                        //assign minx and miny
                        if (ipointer == 2)//the first value is minx y and then compare
                        {
                            minx = x;
                            miny = 0 - y;
                        }
                        else 
                        {
                            minx = (x < minx) ? x : minx;
                            miny = (y < miny) ? y : miny;
                        }

                    }
                    ipointer += 1;
                }
            }

            //transform to the image cordinate
            TranslateProfile();
        }

        private void TranslateProfile()
        {
            int shrinkratio = 10;
            ImageProfile = new Dictionary<string, pinlocation>();

            //move the imageprofile to 0,0
            foreach (KeyValuePair<string, pinlocation> kv in OriginalProfile)
            {
                pinlocation p;
                p.pinnum = kv.Key;
                p.x = (kv.Value.x - minx) / shrinkratio;
                p.y = (kv.Value.y - miny) / shrinkratio;
                ImageProfile.Add(p.pinnum, p);
            }
        }

        private RelayCommand _captureCommand;
        public ICommand CaptureCommand
        {
            get { return _captureCommand ?? (_captureCommand = new RelayCommand(param => this.Capture())); }
        }

        public void Capture()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "PNG图片(*.png)|*.png|JPEG图片(*.jpg)|*.jpg|所有文件(*.*)|*.*";
            var rst = dlg.ShowDialog();
            if (rst == true)
            {
                var fileName = dlg.FileName;

                SaveImageToFile(CurrentFrame, fileName);
                System.Windows.MessageBox.Show("保存成功");
            }

        }
        /// <summary>
        /// 保存图片到文件
        /// </summary>
        /// <param name="image">图片数据</param>
        /// <param name="filePath">保存路径</param>
        private void SaveImageToFile(BitmapSource image, string filePath)
        {
            BitmapEncoder encoder = GetBitmapEncoder(filePath);
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
        /// <summary>
        /// 根据文件扩展名获取图片编码器
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>图片编码器</returns>
        private BitmapEncoder GetBitmapEncoder(string filePath)
        {
            var extName = Path.GetExtension(filePath).ToLower();
            if (extName.Equals(".png"))
            {
                return new PngBitmapEncoder();
            }
            else
            {
                return new JpegBitmapEncoder();
            }
        }


        private void GetCameraFrame(object sender, NewFrameEventArgs eventArgs)
        {

            using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
            using (var stream = new MemoryStream())
            {
                var image = new BitmapImage();

                image.BeginInit();

                //draw
                Graphics g = Graphics.FromImage(bitmap);
                Font font = new Font("Calibri", 8, FontStyle.Regular);
                System.Drawing.Brush brush = new SolidBrush(System.Drawing.Color.White);
                int Margin = 0;
                g.DrawString("string test", font, brush, 0 + Margin, 50 + Margin);
                g.DrawString("string test", font, brush, 0 + Margin, 77 + Margin);

                //int x = 0;
                //for (int i = 0; i < 100; i++)
                //{

                //    for (int j = 0; j < 100; j++)
                //    {

                //        g.DrawString("□", font, brush, i*10, j * 10);
                //        x += 1;
                //    }

                //}

                //load imageprofile
                Font fontpin = new Font("Calibri", 8, FontStyle.Regular);
                Font fontpinnum = new Font("Calibri", 5, FontStyle.Regular);
                if (ImageProfile != null)
                {
                    foreach (KeyValuePair<string, pinlocation> kv in ImageProfile)
                    {
                        g.DrawString("□", fontpin, brush, (float)kv.Value.x, (float)kv.Value.y);
                        //g.DrawString(kv.Key, fontpinnum, brush, (float)kv.Value.x, (float)kv.Value.y+4);
                    }
                }
              

                //bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
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
