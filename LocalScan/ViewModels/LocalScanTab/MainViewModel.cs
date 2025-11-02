using Common.Parameters;
using Common.WPF;
using LocalScan.Helpers;
using LocalScan.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using SoundPlayer = System.Media.SoundPlayer;
using Timer = System.Windows.Threading.DispatcherTimer;

namespace LocalScan.ViewModels.LocalScanTab
{
    public class MainViewModel : BaseViewModel, IClosable
    {
        static MainViewModel()
        {
            Alts = new List<Alt>();
            Load();
        }

        public MainViewModel() : base()
        {
            Current = this;

            SoundPlayer = new SoundPlayer();
            Timer = new Timer();
            Timer.Tick += Timer_Tick;

            Characters = new ObservableCollectionAdv<CharacterViewModel>();
            Characters.CollectionChanged += Characters_CollectionChanged;

            StatusText = new ParameterString(nameof(StatusText));
            TimeString = new ParameterString(nameof(TimeString), "20");
            SoundString = new ParameterString(nameof(SoundString));

            ScanProcessCommand = new RelayCommand(OnScanProcess);
            StartScanLocalCommand = new RelayCommand(OnStartScanLocal, () => HasActiveChar && !HasTimerStarted);
            StopScanLocalCommand = new RelayCommand(OnStopScanLocal, () => HasActiveChar && HasTimerStarted);
            SelectSoundCommand = new RelayCommand(OnSelectSound);

            TestCommand = new RelayCommand(OnTest);

            SetError(Message_ProcessNotFound);


            //#if DEBUG
            IsDebug = true;
            OnPropertyChanged(() => IsDebug);
            //#endif
        }

        

        #region Static and Consts

        public static MainViewModel Current { get; private set; }

        public static List<Alt> Alts { get; }

        private const string Message_ProcessNotFound = @"Процессы EVE Online не найдены.";
        private const string Message_NeedCharLogin = @"Необходимо зайти в игру персонажем.";
        private const string Message_SuccessCharLogin = @"Обнаружены активные персонажи.";

        #endregion

        #region Properties

        public ParameterString StatusText { get; }

        public ParameterString TimeString { get; }
        public ParameterString SoundString { get; }

        public ObservableCollectionAdv<CharacterViewModel> Characters { get; }

        public string Header => "Скан локала";

        public bool HasActiveChar { get; private set; }

        private bool _hasTimerStarted;
        public bool HasTimerStarted
        {
            get => _hasTimerStarted;
            set
            {
                _hasTimerStarted = value;
                TimeString.IsEnabled = !value;
                OnPropertyChanged(() => HasTimerStarted);
            }
        }

        public bool IsDebug { get; } = false;

        private Timer Timer { get; }
        private SoundPlayer SoundPlayer { get; }

        #endregion

        #region Commands

        public RelayCommand ScanProcessCommand { get; }
        private void OnScanProcess()
        {
            var procs = System.Diagnostics.Process.GetProcesses();
            var eves = procs.Where(x => x.ProcessName.Contains("exefile") && x.MainWindowTitle.StartsWith("EVE")).OrderBy(x => x.MainWindowTitle).GroupBy(x => x.MainWindowTitle).ToDictionary(x => x.Key, x => x.ToList());

            //  если не найдено процессов
            if (!eves.Any())
            {
                Characters.Clear();
                SetError(Message_ProcessNotFound);
                return;
            }

            //  если среди процессов нет запущенного персонажа
            if (!eves.Any(x => x.Key != "EVE"))
            {
                Characters.Clear();
                SetError(Message_NeedCharLogin);
                return;
            }

            SetSuccess(Message_SuccessCharLogin);

            //  словарь прежних окон
            var dict = Characters.ToDictionary(x => x.MainWindowTitle, x => false);

            //  для каждого запущенного текущего окна
            foreach (var pair in eves.Where(x => x.Key != "EVE"))
            {
                //  если в коллекции уже есть окно с таким чаром, обновить его процесс
                var found = Characters.FirstOrDefault(x => x.MainWindowTitle == pair.Key);
                if (found != null)
                    found.UpdateProcess(pair.Value.First());
                else
                {
                    //  иначе добавляем в коллекцию чара и процесс
                    Characters.Add(new CharacterViewModel(pair.Key.Replace("EVE - ", "").Trim(), pair.Value.First()));
                }

                //  отмечаем в словаре, что такой чар в данный момент работает
                if (dict.ContainsKey(pair.Key))
                    dict[pair.Key] = true;
                else
                    dict.Add(pair.Key, true);
            }

            //  для всех выключенных чаров: надо выкинуть из коллекции
            foreach (var pair in dict.Where(x => !x.Value))
            {
                var found = Characters.FirstOrDefault(x => x.MainWindowTitle == pair.Key);
                if (found != null)
                    Characters.Remove(found);
            }
        }

        public RelayCommand StartScanLocalCommand { get; }
        private void OnStartScanLocal()
        {
            if (!HasTimerStarted)
            {
                HasTimerStarted = true;
                if (!Timer.IsEnabled)
                {
                    int sec = 20;
                    int.TryParse(TimeString.Value, out sec);
                    Timer.Interval = TimeSpan.FromSeconds(sec);
                    Timer.Start();
                }
            }
        }

        public RelayCommand StopScanLocalCommand { get; }
        private void OnStopScanLocal()
        {
            if (HasTimerStarted)
            {
                HasTimerStarted = false;
                if (Timer.IsEnabled)
                    Timer.Stop();
            }
        }

        public RelayCommand TestCommand { get; }
        private void OnTest()
        {
            //if (Characters.Any(x => x.Checked.Value))
            //{
            //    foreach (var _char in Characters.Where(x => x.Checked.Value))
            //    {
            //        BitmapHelper.GetEnemies(_char.MainWindowHandle, _char.WidthArea.Value, _char.HeightArea.Value, cutWidth: _char.IconWidth);
            //    }
            //}

            if (Characters.Any(x => x.Checked.Value))
            {
                var _char = Characters.First(x => x.Checked.Value);
                BitmapHelper.TestPrint(_char.MainWindowHandle, _char.WidthArea.Value, _char.HeightArea.Value, cutWidth: _char.IconWidth);
            }
        }

        public RelayCommand SelectSoundCommand { get; }
        private void OnSelectSound()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".wav"; // Default file extension
            dialog.Filter = "Audio files (.wav)|*.wav"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                SoundString.Value = dialog.FileName;
            }
        }

        #endregion

        #region Methods

        public void OnClose()
        {
            XmlSerialization.Save(Alts, App.CharScanFilePath);
        }

        private void SetError(string message)
        {
            StatusText.Value = message;
            StatusText.Status = Common.Status.Error;
        }

        private void SetSuccess(string message)
        {
            StatusText.Value = message;
            StatusText.Status = Common.Status.OperationSuccess;
        }

        private void Characters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasActiveChar = Characters.Any();
        }

        private static void Load()
        {
            try
            {
                var particles = XmlSerialization.ReadParticles(App.CharScanFilePath);
                if (particles != null && particles.Any())
                {
                    foreach (var particle in particles)
                    {
                        var alts = XmlSerialization.LoadParticles(particle).OfType<Alt>();
                        foreach (var alt in alts)
                            Alts.Add(alt);
                    }
                }
            }
            catch
            {

            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var res = new List<bool>();
            if (Characters.Any(x => x.Checked.Value))
            {
                foreach (var _char in Characters.Where(x => x.Checked.Value))
                {
                    res.Add(BitmapHelper.GetEnemies(_char.MainWindowHandle, _char.WidthArea.Value, _char.HeightArea.Value, cutWidth: _char.IconWidth));
                }
            }

            if (res.Any(x => x))
            {
                var defaultSound = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("LocalScan.alarm.wav");
                // play music
                if (!string.IsNullOrEmpty(SoundString.Value) && System.IO.File.Exists(SoundString.Value))
                    SoundPlayer.SoundLocation = SoundString.Value;
                else
                    SoundPlayer.Stream = defaultSound;
                SoundPlayer.Play();
            }

            Timer.Start();
        }

        #endregion
    }
}
