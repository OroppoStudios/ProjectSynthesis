using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Warner
	{
	public static class Misc
		{
        #region CONSOLE

        public static void clearConsole()
            {
            Type logEntries = Type.GetType("UnityEditorInternal.LogEntries, UnityEditor.dll"); 
            MethodInfo clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static 
                | BindingFlags.Public); 

            clearMethod.Invoke(null, null);
            }

		#endregion


		#region LAYERS

        public static bool contains(this LayerMask mask, int layer)
            {
            return (mask == (mask | (1 << layer)));
            }

		public static int toLayer(this LayerMask bitmask)//if the layermask contains a single layer selection, retrieve it
			{
			int result = bitmask>0 ? 0 : 31;

			while( bitmask>1 ) 
				{
				bitmask = bitmask>>1;
				result++;
				}

			return result;
			}

		public static void setLayerRecursively(this GameObject gameObject, int layer)
			{
			gameObject.layer = layer;

			foreach (Transform child in gameObject.transform)
				{
				if (child!=null)
					setLayerRecursively(child.gameObject, layer);
				}
			}

		public static void setLayerRecursively(this GameObject gameObject, LayerMask layer)
			{
			setLayerRecursively(gameObject, layer.toLayer());
			}

		#endregion



		#region VECTORS

		public static Vector2 snap(this Vector2 input, float factor = 1f)
			{
			float x = Mathf.Round(input.x / factor) * factor;
			float y = Mathf.Round(input.y / factor) * factor;

			return new Vector2(x, y);
			}

		public static Vector2 to2(this Vector3 vector)
			{
			return vector;
			}

		public static Vector3 setX(this Vector3 vector, float value)
			{
			vector.x = value;
			return vector;
			}

		public static Vector3 setY(this Vector3 vector, float value)
			{
			vector.y = value;
			return vector;
			}

		public static Vector3 multiplyX(this Vector3 vector, float value)
			{
			vector.x *= value;
			return vector;
			}

		public static Vector3 multiplyY(this Vector3 vector, float value)
			{
			vector.y *= value;
			return vector;
			}

		public static Vector2 setX(this Vector2 vector, float value)
			{
			vector.x = value;
			return vector;
			}

		public static Vector2 setY(this Vector2 vector, float value)
			{
			vector.y = value;
			return vector;
			}

		public static Vector2 multiplyX(this Vector2 vector, float value)
			{
			vector.x *= value;
			return vector;
			}

		public static Vector2 multiplyY(this Vector2 vector, float value)
			{
			vector.y *= value;
			return vector;
			}

		public static float getRandom(this Vector2 vector)
			{
			return UnityEngine.Random.Range(vector.x, vector.y);
			}

		public static float getRandomFromX(this Vector2 vector)
			{
			return UnityEngine.Random.Range(-vector.x, vector.x);
			}

		public static float getRandomFromY(this Vector2 vector)
			{
			return UnityEngine.Random.Range(-vector.y, vector.y);
			}

		#endregion



		#region ENUMS        

		public static T parseEnum<T>(string value, bool ignoreCase = false)
			{
			if (string.IsNullOrEmpty(value))
				return (T) Enum.GetValues(typeof(T)).GetValue(0);					

			return (T) Enum.Parse(typeof(T), value, ignoreCase);
			}

		public static List<string> valuesList<T>(this T theEnum)
			{
			List<string> theList = new List<string>();

			foreach (T value in Enum.GetValues(theEnum.GetType()))
				theList.Add(value.ToString());

			return theList;
			}

		public static T getRandom<T>(this T theEnum)
			{
			Array values = Enum.GetValues(theEnum.GetType());
			int index = UnityEngine.Random.Range(0, values.Length);
			return (T) values.GetValue(index);
			}


		public static bool valueSelected<T>(this T theEnum, object value)
			{
			Array values = Enum.GetValues(theEnum.GetType());

			for (int i = 0; i < values.Length; i++)
				{
				if (!values.GetValue(i).Equals(value))
					continue;

				int layer = 1 << i;
				if (( ((int)(object) theEnum) & layer) != 0)
					{
					return true;
					}
				}

			return false;
			}

		#endregion



		#region RECTS

		public static float getRandomX(this Rect rect)
			{
			return UnityEngine.Random.Range(rect.xMin, rect.xMax);
			}

		public static float getRandomY(this Rect rect)
			{
			return UnityEngine.Random.Range(rect.yMin, rect.yMax);
			}


		#endregion



		#region COLOR

		public static Color setAlpha(this Color color, float value)
			{
			color.a = value;
			return color;
			}

		#endregion



		#region ARRAYS & LISTS

		public static void randomize<T>(this List<T> theList)
			{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			int n = theList.Count;
			while (n > 1)
				{
				byte[] box = new byte[1];
				do provider.GetBytes(box);
				while (!(box[0] < n * (Byte.MaxValue / n)));
				int k = (box[0] % n);
				n--;
				T value = theList[k];
				theList[k] = theList[n];
				theList[n] = value;
				}
			}

		public static int getRandomIndex<T>(this List<T> theList)
			{
			return UnityEngine.Random.Range(0, theList.Count);
			}

		public static T getRandom<T>(this List<T> theList)
			{
			return theList[UnityEngine.Random.Range(0, theList.Count)];
			}

		public static int getRandomIndex<T>(this T[] theList)
			{
			return UnityEngine.Random.Range(0, theList.Length);
			}

		public static T getRandom<T>(this T[] theArray)
			{
			if (theArray.Length>0)
				return theArray[UnityEngine.Random.Range(0, theArray.Length)];
				else
				return default(T);
			}

		public static List<T> toList<T>(this T[] theArray)
			{
			List<T> theList = new List<T>();

			for (int i = 0; i<theArray.Length; i++)
				theList.Add(theArray[i]);

			return theList;
			}

		public static List<Y> transformTo<T, Y>(this List<T> originalList, 
            Func<T, Y> transformDelegate, bool uniqueValues = false)
			{
			List<Y> finalList = new List<Y>();

			for (int i = 0; i<originalList.Count; ++i)
				{
				Y obj = transformDelegate(originalList[i]);

                if (obj!=null)
                    {
                    if (!uniqueValues || (uniqueValues && !finalList.Contains(obj)))
                        finalList.Add(obj);
                    }
				}

			return finalList;
			}


		public static Y[] transformTo<T, Y>(this T[] originalList, 
            Func<T, Y> transformDelegate, bool uniqueValues = false)
            {
            List<Y> finalList = new List<Y>();

            for (int i = 0; i<originalList.Length; ++i)
                {
                Y obj = transformDelegate(originalList[i]);

                if (obj!=null)
                    {
                    if (!uniqueValues || (uniqueValues && !finalList.Contains(obj)))
                        finalList.Add(obj);
                    }
				}

			return finalList.ToArray();
			}


		public static T findMathingOn<T, Y>(this List<T> fromList, 
		                                    Y toFind, Func<T, Y, bool> transformDelegate)
			{
			for (int i = 0; i<fromList.Count; i++)
				{
				if (transformDelegate(fromList[i], toFind))
					return fromList[i];
				}

			return default(T);
			}


		public static T findMathingOn<T, Y>(this T[] fromList, 
		                                    Y toFind, Func<T, Y, bool> transformDelegate)
			{
			for (int i = 0; i<fromList.Length; i++)
				{
				if (transformDelegate(fromList[i], toFind))
					return fromList[i];
				}

			return default(T);
			}


		public static List<T> sortBy<T, TKey>(this List<T> theList, Func<T, TKey> keySelector)
			{
			return theList.OrderBy(keySelector).ToList();
			}


		public static List<T> sortByDescending<T, TKey>(this List<T> theList, Func<T, TKey> keySelector)
			{
			return theList.OrderByDescending(keySelector).ToList();
			}


		public static void sortBy<T>(this IList<T> list)
			{
			if (list is List<T>)
				{
				((List<T>) list).Sort();
				}
			else
				{
				List<T> copy = new List<T>(list);
				copy.Sort();
				Copy(copy, 0, list, 0, list.Count);
				}
			}


		public static void sortBy<T>(this IList<T> list, Comparison<T> comparison)
			{
			if (list is List<T>)
				{
				((List<T>) list).Sort(comparison);
				}
			else
				{
				List<T> copy = new List<T>(list);
				copy.Sort(comparison);
				Copy(copy, 0, list, 0, list.Count);
				}
			}


		public static void sortBy<T>(this IList<T> list, IComparer<T> comparer)
			{
			if (list is List<T>)
				{
				((List<T>) list).Sort(comparer);
				}
			else
				{
				List<T> copy = new List<T>(list);
				copy.Sort(comparer);
				Copy(copy, 0, list, 0, list.Count);
				}
			}

		public static void sortBy<T>(this IList<T> list, int index, int count,
		                             IComparer<T> comparer)
			{
			if (list is List<T>)
				{
				((List<T>) list).Sort(index, count, comparer);
				}
			else
				{
				List<T> range = new List<T>(count);
				for (int i = 0; i<count; i++)
					{
					range.Add(list[index+i]);
					}
				range.Sort(comparer);
				Copy(range, 0, list, index, count);
				}
			}

		private static void Copy<T>(IList<T> sourceList, int sourceIndex,
		                           IList<T> destinationList, int destinationIndex, int count)
			{
			for (int i = 0; i<count; i++)
				{
				destinationList[destinationIndex+i] = sourceList[sourceIndex+i];
				}
			}

		#endregion



		#region FILES

		public static void saveFile(string text, string path, bool append = false, bool newLineAtEnd = false)
			{
			using (FileStream fileStream = new FileStream(path, append ? FileMode.Append : FileMode.Create))
				using (StreamWriter streamWriter = new StreamWriter(fileStream))
					streamWriter.Write(text+((newLineAtEnd) ? "\n" : ""));
			}

		public static TextAsset loadConfigAsset(string fileName)
			{
			return Resources.Load<TextAsset>(fileName);
			}


		public static T loadDataFile<T>(string filePath) where T: new()
			{
			T data;

			if (File.Exists(filePath))
				{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				FileStream stream = File.Open(filePath, FileMode.Open);
				try
					{
					data = (T) binaryFormatter.Deserialize(stream);
					}
				catch
					{
					data = new T();
					}

				stream.Close();
				}
				else
				data = new T();
							
			return data;
			}


		public static T loadConfigFile<T>(TextAsset dataFile) where T: new()
			{
			T config;

			BinaryFormatter binaryFormatter = new BinaryFormatter();
			Stream stream = new MemoryStream(dataFile.bytes);
			try
				{
				config = (T) binaryFormatter.Deserialize(stream);
				}
			catch
				{
				config = new T();
				}

			stream.Close();

			return config;
			}


		public static T loadConfig<T>(string fileName) where T: new()
			{
			TextAsset configAsset = Misc.loadConfigAsset(fileName);
			return loadConfigFile<T>(configAsset);
			}


		public static void saveConfigFile<T>(T config, string filePath)
			{
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			FileStream fileStream = File.Create(filePath);
			binaryFormatter.Serialize(fileStream, config);
			fileStream.Close();
			}

            		#endregion
		}
	}