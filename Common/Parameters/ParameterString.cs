using System;

namespace Common.Parameters
{
    [Serializable]
    public class ParameterString : Parameter
    {

        public ParameterString(string name, string value)
            : base(name)
        {
            Value = value;
        }

        public ParameterString(string name)
             : base(name)
        {
        }


        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Validate();
                OnPropertyChanged("Value");
            }
        }

        public override void Update()
        {
            Value = _value;
        }
    }
}
