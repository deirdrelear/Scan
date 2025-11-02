using System;

namespace Common.Parameters
{
    [Serializable]
    public class ParameterBool : Parameter
    {
        public ParameterBool(string name, bool value)
            : base(name)
        {
            Value = value;
        }


        private bool _value;
        public bool Value
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
