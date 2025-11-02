using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Parameters
{
    public class ParameterRange : Parameter
    {



        public ParameterRange(string name, string value)
            : base(name)
        {
            Value = value;
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

        public bool IsFormatRange(out bool isNotInRange, int min, int max)
        {
            isNotInRange = false;
            var numbers = Value.Replace(" ", "").Split(new []{ ",","-"}, StringSplitOptions.None);
            //регулярное выражение для числа от 1-250
            foreach (var numStr in numbers)
            {
                int num;
                if (int.TryParse(numStr, out num))
                {
                    if (num < min || num > max)
                    {
                        isNotInRange = true;
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
