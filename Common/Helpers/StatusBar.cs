using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Common.Helpers
{
    public class StatusBar : INotifyPropertyChanged
    {

        public StatusBar()
        {
            Reset();
        }

        public StatusBar(Status status, string message = "", bool isVisible = true)
        {
            IsVisible = isVisible;
            Status = status;
            Message = message;
        }

        public void Reset()
        {
           IsVisible = false;
           Status = Status.Default;
           Message = String.Empty;
        }

        private Status _status;
        public Status Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _message;
            
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
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
