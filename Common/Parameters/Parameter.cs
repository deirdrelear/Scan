using System;
using System.ComponentModel;

namespace Common.Parameters
{

    public abstract class Parameter : INotifyPropertyChanged
    {
        public event EventHandler ValidateDelegate;

        protected Parameter(string name) : this()
        {
            Name = name;
        }
        protected Parameter()
        {
            Status = Status.Normal;
            ErrorShortDescr = String.Empty;
            IsVisible = true;
            IsEnabled = true;
        }

        public void Validate()
        {
            EventHandler handler = ValidateDelegate;
            handler?.Invoke(this, new EventArgs());
        }

        public abstract void Update();

        public double ErrorWarningCount { get; set; }

        private int _errorCount;
        public int ErrorCount
        {
            get { return _errorCount; }
            set
            {
                _errorCount = value;
                OnPropertyChanged("ErrorCount");
                OnPropertyChanged("HasErrors");
            }
        }

        public string ErrorShortDescr { get; set; }
        public void ResetValidation()
        {
            ErrorShortDescr = String.Empty;
            ErrorCount = 0;
            ErrorWarningCount = 0;
            ErrorMessage = null;
            Status = Status.Normal;
        }


        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private Status _status;

        public Status Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }


        public bool HasErrors
        {
            get { return (ErrorCount != 0); }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public void SetErrInfo(string errorMessage, Status status, string errorShortDescr = "", bool isAppend = false)
        {
            ErrorShortDescr = errorShortDescr;
            if (isAppend)
                ErrorMessage += "\n\n" + errorMessage;
            else
                ErrorMessage = errorMessage;
            Status = status;
            ErrorWarningCount++;
            if (status == Status.Error)
                ErrorCount++;
        }


        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
    }

}
