using System.ComponentModel;

namespace Common.Helpers
{
    public class ProgressBar : INotifyPropertyChanged
    {

        public ProgressBar(int max, bool isVisible = true)
        {
            Value = 0;
            Maximum = max;
            IsVisible = isVisible;
        }

        public void Incr()
        {
            Value++;
        }

        public ProgressBar()
        {
            Reset();
        }

        public void Reset()
        {
            Value = 0;
            Maximum = 0;
            IsVisible = false;
        }

        private int _value;

        public int Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        private int _maximum;
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                OnPropertyChanged(nameof(Maximum));
            }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        // for Resharper
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
