using Common.Parameters;
using Common.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LocalScan.ViewModels.LocalScanTab
{
    public class CharacterViewModel : BaseViewModel
    {
        public CharacterViewModel(string charName, Process process) : base()
        {
            Name = charName;
            UpdateProcess(process);

            Alt = MainViewModel.Alts.FirstOrDefault(x => x.Name == Name);
            if (Alt == null)
            {
                Alt = new Models.Alt(Name) { WidthArea = 150, HeightArea = 640, IconWidth = 20 };
                MainViewModel.Alts.Add(Alt);
            }

            Checked = new ParameterBool(nameof(Checked), false);

            WidthArea = new ParameterInt(nameof(WidthArea), Alt.WidthArea);
            HeightArea = new ParameterInt(nameof(HeightArea), Alt.HeightArea);
            IconWidth = Alt.IconWidth;

            WidthArea.ValidateDelegate += WidthArea_ValidateDelegate;
            HeightArea.ValidateDelegate += WidthArea_ValidateDelegate;
        }

        private void WidthArea_ValidateDelegate(object sender, EventArgs e)
        {
            Alt.WidthArea = WidthArea.Value;
            Alt.HeightArea = HeightArea.Value;
            Alt.IconWidth = IconWidth;
        }

        #region Static and Consts



        #endregion

        #region Properties

        private Models.Alt Alt { get; }

        public Process Process { get; private set; }

        public string MainWindowTitle => Process != null ? Process.MainWindowTitle : null;
        public IntPtr MainWindowHandle => Process != null ? Process.MainWindowHandle : IntPtr.Zero;

        public string Name { get; }

        public ParameterBool Checked { get; }
        public ParameterInt WidthArea { get; }
        public ParameterInt HeightArea { get; }
        public int IconWidth { get; set; }

        #endregion

        #region Commands



        #endregion

        #region Methods

        public void UpdateProcess(Process process)
        {
            Process = process;
        }

        #endregion
    }
}
