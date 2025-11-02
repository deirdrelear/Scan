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

namespace LocalScan.ViewModels
{
    public class MainWindowViewModel : ModifyViewModel
    {
        public MainWindowViewModel() : base()
        {
            Current = this;

            Tabs = new ObservableCollectionAdv<IClosable>()
            {
                new LocalScanTab.MainViewModel(),
            };
        }

        #region Static and Consts

        public static MainWindowViewModel Current { get; private set; }

        #endregion

        #region Properties

        public ObservableCollectionAdv<IClosable> Tabs { get; }

        public bool HaveUnsavedChangesChars { set; get; }
        public bool HaveUnsavedChangesOres { set; get; }

        #endregion

        #region Commands



        #endregion

        #region Methods

        protected override void OnClose()
        {
            foreach (var tab in Tabs)
                tab.OnClose();

            Application.Current.Shutdown();
        }

        #endregion
    }
}
