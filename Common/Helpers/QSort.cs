using System.Collections.Generic;

namespace Common.Helpers
{
	public delegate int ItemComparer<in T>(T a, T b);
	public delegate void Swap<T>(IList<T> elements, int index1, int index2);
	public static class QSort
	{

		public static void Sort<T>(IList<T> elements, ItemComparer<T> comparer, bool reverse = false)
		{
			Sort(elements, CollectionSwap, comparer, reverse);
		}
		public static void Sort<T>(IList<T> elements, Swap<T> swap, ItemComparer<T> comparer, bool reverse = false)
		{
			Sort(elements, 0, elements.Count - 1, swap, comparer);
			if (reverse)
				for (int i = 0; i < elements.Count / 2; i++)
					swap(elements, i, elements.Count - 1 - i);
		}

		static void Sort<T>(IList<T> elements, int left, int right, Swap<T> swap, ItemComparer<T> comparer)
		{
			if (elements.Count == 0)
				return;
			int i = left;
			int j = right;
			T pivot = elements[(left + right) / 2];
			while (i <= j)
			{
				while (comparer(elements[i], pivot) < 0)
					i++;
				while (comparer(elements[j], pivot) > 0)
					j--;
				if (i <= j)
				{
					swap(elements, i, j);
					i++;
					j--;
				}
			}
			if (left < j)
				Sort(elements, left, j, swap, comparer);
			if (i < right)
				Sort(elements, i, right, swap, comparer);
		}
		static void CollectionSwap<T>(IList<T> elements, int index1, int index2)
		{
			T tmp = elements[index1];
			elements[index1] = elements[index2];
			elements[index2] = tmp;
		}
	}
}
