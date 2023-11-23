using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Configuration;

namespace Bio
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        private readonly MainWindowBL _mbl;

        public PlotModel DrawModel { get; private set; }

        private LineSeries _series;

        private List<DataPoint> _savedSource;
        private List<DataPoint> SavedSource
        { 
            get { return _savedSource; }
            set { _savedSource = value; OnPropertyChanged(nameof(SavedDataNotNull)); }
        }
        public bool SavedDataNotNull => (_savedSource != null) & (_isScaning == false);

        private bool _isScaning;
        public bool IsScaning
        {
            get { return _isScaning & _isConnected; }
            set { _isScaning = value; OnPropertyChanged(nameof(IsScaning)); OnPropertyChanged(nameof(IsWaiting)); OnPropertyChanged(nameof(SavedDataNotNull)); }
        }
        public bool IsWaiting { get { return !_isScaning & _isConnected; } }

        public string Title { get { return (_startFrq/10).ToString() + "-" + (_endFrq/10).ToString() + "-" + (_frqStep / 10).ToString(); } }

        private uint _startFrq = 10000;
        public uint StartFrq
        {
            get { return _startFrq / 10; }
            set { _startFrq = value * 10; OnPropertyChanged(nameof(StartFrq)); OnPropertyChanged(nameof(Title)); }
        }

        private uint _endFrq   = 120000;
        public uint EndFrq
        {
            get { return _endFrq / 10; }
            set { _endFrq = value * 10; OnPropertyChanged(nameof(EndFrq)); OnPropertyChanged(nameof(Title)); }
        }

        private uint _frqStep  = 100;
        public uint FrqStep
        {
            get { return _frqStep / 10; }
            set { _frqStep = value * 10; OnPropertyChanged(nameof(FrqStep)); OnPropertyChanged(nameof(Title)); }
        }

        private uint _voltageNum = 0;
        public uint VoltageNum
        {
            get { return _voltageNum; }
            set { _voltageNum = value; OnPropertyChanged(nameof(VoltageNum)); OnPropertyChanged(nameof(VoltageText)); }
        }
        private string[] _voltageText = new string[] { "200mV", "400mV", "1 V", "2 V" };
        public string VoltageText { get { return _voltageText[_voltageNum]; } }
// x5
        private byte[] _voltage = new byte[] { 0x02, 0x04, 0x06, 0x00};
// x1        private byte[] _voltage = new byte[] { 0x03, 0x05, 0x07, 0x01 };

        private uint _typeOfScan;
        public uint TypeOfScan
        {
            get { return _typeOfScan; }
            set { _typeOfScan = value; OnPropertyChanged(nameof(TypeOfScan)); }
        }
        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged(nameof(Progress)); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); OnPropertyChanged(nameof(IsDisconnected)); OnPropertyChanged(nameof(IsWaiting)); OnPropertyChanged(nameof(IsScaning)); }
        }
        public bool IsDisconnected { get { return !IsConnected; } }

        private bool _smooth = true;
        public bool Smooth
        {
            get { return _smooth; }
            set { _smooth = value; OnPropertyChanged(nameof(Smooth)); }
        }

        public bool[] ModeArray { get; } = new bool[] { false, false, false, false, false };

        private int SelectedMode => Array.IndexOf(ModeArray, true);

        public MainWindowVM()
        {
            _mbl = new MainWindowBL();
            _mbl.NewDataBlockReady += new EventHandler<DataEvent>(DataBlockReceiveHandler);
            _mbl.ProcessEnd += new EventHandler(ProcessEndHandler);
            _mbl.ProgressNow += new EventHandler<DataEvent>(ProgressNow);
            DrawModel = new PlotModel { };

        }

        public void RestoreData()
        {
            uint i;
            bool k;
            StartFrq = uint.TryParse(ConfigurationManager.AppSettings["StartFrq"], out i) ? i : 1000;
            EndFrq = uint.TryParse(ConfigurationManager.AppSettings["EndFrq"], out i) ? i : 100000;
            FrqStep = uint.TryParse(ConfigurationManager.AppSettings["FrqStep"], out i) ? i : 100;
            VoltageNum = uint.TryParse(ConfigurationManager.AppSettings["VoltageNum"], out i) ? i : 0;
            TypeOfScan = uint.TryParse(ConfigurationManager.AppSettings["TypeOfScan"], out i) ? i : 0;
            ModeArray[ uint.TryParse(ConfigurationManager.AppSettings["SelectedMode"], out i) ? i : 0] = true;
            Smooth = bool.TryParse(ConfigurationManager.AppSettings["Smooth"], out k) && k;
            DrawModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = _startFrq / 10,
                Maximum = _endFrq / 10,
                Angle = -90,

            });

            RestorePlotData();
        }

        public void SaveData()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(0);
            config.AppSettings.Settings.Remove("StartFrq");
            config.AppSettings.Settings.Add("StartFrq", StartFrq.ToString());
            config.AppSettings.Settings.Remove("EndFrq");
            config.AppSettings.Settings.Add("EndFrq", EndFrq.ToString());
            config.AppSettings.Settings.Remove("FrqStep");
            config.AppSettings.Settings.Add("FrqStep", FrqStep.ToString());
            config.AppSettings.Settings.Remove("VoltageNum");
            config.AppSettings.Settings.Add("VoltageNum", VoltageNum.ToString());
            config.AppSettings.Settings.Remove("TypeOfScan");
            config.AppSettings.Settings.Add("TypeOfScan", TypeOfScan.ToString());
            config.AppSettings.Settings.Remove("SelectedMode");
            config.AppSettings.Settings.Add("SelectedMode", SelectedMode.ToString());
            config.AppSettings.Settings.Remove("Smooth");
            config.AppSettings.Settings.Add("Smooth", Smooth.ToString());
            config.Save(ConfigurationSaveMode.Minimal);
            SavePlotData();
        }

        private void SavePlotData()
        {
            foreach (string sFile in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "*.plotdata"))
            {
                System.IO.File.Delete(sFile);
            }
            
            for (int i = 0; i < DrawModel.Series.Count; i++)
            {
                if (((LineSeries)DrawModel.Series[i]).StrokeThickness == 1)
                {
                    SavedSource = ((LineSeries)DrawModel.Series[i]).Points;
                    foreach (DataPoint dp in SavedSource)
                    {
                        System.IO.File.AppendAllText(SelectColor(((LineSeries)DrawModel.Series[i]).Color) +".plotdata", string.Format("{0};{1}{2}", dp.X, dp.Y, Environment.NewLine));
                    }
                }
            }
        }

        private void RestorePlotData()
        {
            foreach (string sFile in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "*.plotdata"))
            {
                try
                {
                    int i = Int32.Parse(System.IO.Path.GetFileNameWithoutExtension(sFile));
                    var allLinesText = new List<string>(System.IO.File.ReadAllLines(i.ToString() + ".plotdata"));
                    OxyColor _colorLine = SelectColor(i);
                    _series = new LineSeries { MarkerType = MarkerType.None, Color = _colorLine, StrokeThickness = 1 };
                    List<DataPoint> collectedData = new();

                    if (_smooth)
                    {
                        _series.InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline;
                    }

                    foreach (string line in allLinesText)
                    {
                        var dd = Array.ConvertAll(line.Split(';'), Double.Parse);
                        collectedData.Add(new DataPoint(dd[0], dd[1]));
                    }
                    _series.Points.AddRange(collectedData);
                    DrawModel.Series.Add(_series);
                }
                catch { }
            }

            DrawModel.InvalidatePlot(true);
        }


        private RelayCommand _startScanCommand;
        public RelayCommand StartScanCommand => _startScanCommand ??= new RelayCommand(obj =>
            {
                IsScaning = true;

                OxyColor _colorLine = SelectColor(SelectedMode);

                _series = new LineSeries { MarkerType = MarkerType.None, Color = _colorLine, StrokeThickness = 1 };

                if (_smooth)
                {
                    _series.InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline;
                }

                for (int i = 0; i < DrawModel.Series.Count; i++)
                {
                    if (((LineSeries)DrawModel.Series[i]).Color == _colorLine & ((LineSeries)DrawModel.Series[i]).StrokeThickness == 1)
                    {
                        SavedSource = ((LineSeries)DrawModel.Series[i]).Points;
                        DrawModel.Series.RemoveAt(i);
                    }
                }
                
                DrawModel.Series.Add(_series);

                DrawModel.Axes.Clear();
                DrawModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Minimum = _startFrq / 10,
                    Maximum = _endFrq / 10,
                    Angle = -90,

                });
                DrawModel.InvalidatePlot(true);
                _mbl.Start(_startFrq, _endFrq, _frqStep, _voltage[_voltageNum], _typeOfScan);
            });

        private RelayCommand _simplifyCommand;
        public RelayCommand SimplifyCommand => _simplifyCommand ??= new RelayCommand(obj =>
        {
            int _sourceIndex = -1;
            OxyColor _colorLine = SelectColor(SelectedMode);

            _series = new LineSeries { MarkerType = MarkerType.None, Color = _colorLine, StrokeThickness = 2 };
            if (_smooth)
            {
                _series.InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline;
            }
            
            for (int i = 0; i < DrawModel.Series.Count; i++)
            {
                if (((LineSeries)DrawModel.Series[i]).Color == _colorLine & ((LineSeries)DrawModel.Series[i]).StrokeThickness == 2)
                {
                    DrawModel.Series.RemoveAt(i);
                }
            }

            for (int i = 0; i < DrawModel.Series.Count; i++)
            {
                if (((LineSeries)DrawModel.Series[i]).Color == _colorLine & ((LineSeries)DrawModel.Series[i]).StrokeThickness == 1)
                {
                    _sourceIndex = i;
                }
            }

            if (_sourceIndex >= 0)
            {
                List<DataPoint> collectedData = new();
                List<DataPoint> source = ((LineSeries)DrawModel.Series[_sourceIndex]).Points;

                DrawModel.Series.Add(_series);
                IFilter _filter = new Average();
                collectedData.Add(new DataPoint(source[0].X, source[0].Y));
                source.ForEach(delegate (DataPoint item) {
                    (bool, double) filtred = _filter.Calculate(item.Y);
                    if (filtred.Item1)
                    {
                        collectedData.Add(new DataPoint(item.X, filtred.Item2));
                    }
                });

                _series.Points.AddRange(collectedData);

                DrawModel.InvalidatePlot(true);
            }
        });

        private RelayCommand _mixCommand;
        public RelayCommand MixCommand => _mixCommand ??= new RelayCommand(obj =>
        {
            int _sourceIndex = -1;
            OxyColor _colorLine = SelectColor(SelectedMode);

            if (SavedSource != null)
            {
                _series = new LineSeries { MarkerType = MarkerType.None, Color = _colorLine, StrokeThickness = 1 };
                if (_smooth)
                {
                    _series.InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline;
                }

                for (int i = 0; i < DrawModel.Series.Count; i++)
                {
                    if (((LineSeries)DrawModel.Series[i]).Color == _colorLine & ((LineSeries)DrawModel.Series[i]).StrokeThickness == 1)
                    {
                        _sourceIndex = i;
                    }
                }

                if (_sourceIndex >= 0)
                {
                    List<DataPoint> collectedData = new();
                    List<DataPoint> source = ((LineSeries)DrawModel.Series[_sourceIndex]).Points;
                    if (source.Count == SavedSource.Count)
                    {
                        DrawModel.Series.RemoveAt(_sourceIndex);

                        DrawModel.Series.Add(_series);

                        for (var i = 0; i < source.Count; i++)
                        {
                            collectedData.Add(new DataPoint(source[i].X, (source[i].Y + SavedSource[i].Y) / 2));
                        }

                        _series.Points.AddRange(collectedData);

                        DrawModel.InvalidatePlot(true);
                    }
                }
                SavedSource = null;
            }
        });

        private RelayCommand _clearCommand;
        public RelayCommand ClearCommand => _clearCommand ??= new RelayCommand(obj =>
        {
            while (DrawModel.Series.Count > 0) {
                DrawModel.Series.RemoveAt(0);
            }
            DrawModel.InvalidatePlot(true);
            SavedSource = null;
        });

        private RelayCommand _connectCommand;
        public RelayCommand ConnectCommand => _connectCommand ??= new RelayCommand(obj =>
        {
            IsConnected = _mbl.InitI2C();
        });

        private RelayCommand _disconnectCommand;
        public RelayCommand DisconnectCommand => _disconnectCommand ??= new RelayCommand(obj =>
        {
            IsConnected = false;
            if (_isScaning == true)
            {
                _mbl.Stop();
                IsScaning = false;
                SavedSource = null;
            }
        });

        private RelayCommand _stopScanCommand;
        public RelayCommand StopScanCommand => _stopScanCommand ??= new RelayCommand(obj =>
            {
                _mbl.Stop();
                IsScaning = false;
            });

        private RelayCommand _fitCommand;
        public RelayCommand FitCommand => _fitCommand ??= new RelayCommand(obj =>
            {
                DrawModel.ResetAllAxes();
                DrawModel.InvalidatePlot(true);
            });

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void DataBlockReceiveHandler(object sender, DataEvent e)
        {
            _series.Points.AddRange(e.data);
            Progress = e.Progress;
        }

        private void ProcessEndHandler(object sender, EventArgs e)
        {
            DrawModel.InvalidatePlot(true);
            Progress = 100;
            IsScaning = false;
        }

        private void ProgressNow(object sender, DataEvent e)
        {
            Progress = e.Progress;
        }

        private OxyColor SelectColor(int num)
        {
            OxyColor _colorLine = OxyColors.Black;
            switch (num)
            {
                case 0:
                    _colorLine = OxyColors.Cyan; break;
                case 1:
                    _colorLine = OxyColors.Red; break;
                case 2:
                    _colorLine = OxyColors.Green; break;
                case 3:
                    _colorLine = OxyColors.Purple; break;
                case 4:
                    _colorLine = OxyColors.Black; break;
            }
            return _colorLine;
        }

        private int SelectColor(OxyColor color)
        {
            int _colorNum = 0;
            if (color.Equals(OxyColors.Cyan)) _colorNum = 0;
            if (color.Equals(OxyColors.Red)) _colorNum = 1;
            if (color.Equals(OxyColors.Green)) _colorNum = 2;
            if (color.Equals(OxyColors.Purple)) _colorNum = 3;
            if (color.Equals(OxyColors.Black)) _colorNum = 4;
            return _colorNum;
        }

    }
}
