using System;

namespace Common.Parameters
{
    [Serializable]
    public class ParameterInt: Parameter
    {
         public ParameterInt(string name, int value)
            : base(name)
        {
            Value = value; 
        }


         private int _value;
         public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Validate();               
                OnPropertyChanged("Value");
            }
        }

         public bool CheckMinCondition(int minValue, Status errType, string errorShortDescr = "")
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
        
        //public void CallSelectionChange()
        //{
        //    Value = _value + 1;
        //    Value = _value - 1;
        //}


        //public void CheckRangeCondition(double beginValue, double endValue, bool isInsideRange, StateType errType)
        //{
        //    if (isInsideRange) //Если число должно лежать внутри диапазона
        //    { 
        //        // Но не лежит
        //        if (beginValue > Value || Value > endValue)
        //        {
        //            SetErrInfo("Значение " + Name + " должно лежать в диапазоне [" + beginValue + ";" + endValue + "]", errType);
        //        }
        //    }
        //    else            
        //    {
        //        if (beginValue <= Value && Value <= endValue)
        //        {
        //            SetErrInfo("Значение " + Name + " должно лежать вне диапазона [" + beginValue + ";" + endValue +"]", errType);
        //        }
        //    }           
        //}       
    }
}
