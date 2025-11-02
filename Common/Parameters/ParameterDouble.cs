using System;

namespace Common.Parameters
{
    [Serializable]
    public class ParameterDouble : Parameter
    {       
        public ParameterDouble(string name, double value)
            :base(name)
        {
            Value = value; 
        }

        public ParameterDouble(string name)
            : base(name)
        {
        }

        private double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Validate();               
                OnPropertyChanged("Value");
            }
        }

        public bool CheckMinCondition(double minValue, Status errType, string errorShortDescr = "")
        {
            bool isOk = true;
            if (Value < minValue)
            {
                SetErrInfo("Значение " + Name + " должно быть не менее " + minValue, errType, errorShortDescr);
                isOk = false;
            }
            return isOk;
        }

        public override void Update()
        {
            Value = _value;
        }


        public void CheckRangeCondition(double beginValue, double endValue, bool isInsideRange, Status errType, string errorMessage, string errorShortDescr = "", bool isAppend = false)
        {
            if (isInsideRange) //Если число должно лежать внутри диапазона
            { 
                // Но не лежит
                if (beginValue > Value || Value > endValue)
                {
                    SetErrInfo(errorMessage, errType, errorShortDescr, isAppend);
                }
            }
            else            
            {
                if (beginValue <= Value && Value <= endValue)
                {
                    SetErrInfo(errorMessage, errType, errorShortDescr, isAppend);
                }
            }           
        }       
    }
}



