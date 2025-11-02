using System.Windows;

namespace Common.WPF
{
    public class ModifyViewModel : BaseViewModel
    {
        protected ModifyViewModel()
        {
            MinimizeCommand = new RelayCommand(OnMinimize);
            MaximizeCommand = new RelayCommand<Window>(OnMaximize);
            CloseCommand = new RelayCommand(OnClose);
        }

        private WindowState _windowState;
        public WindowState WindowState
        {
            get { return _windowState; }
            set
            {
                _windowState = value;
                OnPropertyChanged(() => WindowState);
            }
        }

        public RelayCommand MinimizeCommand { get; }
        public RelayCommand<Window> MaximizeCommand { get; }
        public RelayCommand CloseCommand { get; }

        protected virtual void OnMinimize()
        {
            WindowState = WindowState.Minimized;
        }

        protected virtual void OnMaximize(Window window)
        {
            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Normal
                    ? WindowState.Maximized
                    : WindowState.Normal;
            }
        }

        protected virtual void OnClose()
        {
            Application.Current.Shutdown();
        }
    }
}
