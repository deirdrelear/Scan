using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Common.WPF
{
	public class ObservableCollectionAdv<T> : ObservableCollection<T>
	{
		public void RemoveRange(int index, int count)
		{
			CheckReentrancy();
			((List<T>)Items).RemoveRange(index, count);
			OnReset();
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			CheckReentrancy();
			((List<T>)Items).InsertRange(index, collection);
			OnReset();
		}

        public void Sort()
        {
            List<T> sorted = ((List<T>)Items).OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count; i++) Move(((List<T>)Items).IndexOf(sorted[i]), i);
        }

        //public ObservableCollectionAdv(IEnumerable<T> collection)
        //{
        //       CheckReentrancy();
        //       ((List<T>)Items).Clear();
        //       ((List<T>)Items).InsertRange(0, collection);
        //       OnReset();
        //   }

        //   public ObservableCollectionAdv()
        //   {}


        void OnReset()
		{
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		void OnPropertyChanged(string propertyName)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}
	}

}
