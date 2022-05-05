using System;
using System.Text;
using System.Reflection;

namespace Warner {

public class NoGarbageString
	{
	public int maxSize
		{
		get;
		private set;
		}

	public int spaceLeft
		{
		get
			{
			return maxSize-length;
			}
		}

	public int length
		{
		get
			{
			return builder.Length;
			}
		}

	public string str
		{
		get; 
		private set;
		}

	public StringBuilder builder {get;private set;}


	public NoGarbageString(int size = 32)
		{
		builder = new StringBuilder(size,size);
		maxSize = size;
        
		try
			{
			//Mono (Unity3D)
			FieldInfo typeInfo = builder.GetType().GetField("_str", BindingFlags.NonPublic | BindingFlags.Instance);
			if (typeInfo != null)
				{
				str = (string)typeInfo.GetValue(builder);
				}
			}
		catch
			{                
			try
				{
				//.NET platform
				FieldInfo typeInfo = builder.GetType().GetField("_cached_str", BindingFlags.NonPublic | BindingFlags.Instance);
				if (typeInfo != null)
					{
					str = (string)typeInfo.GetValue(builder);
					}
				}
			catch
				{
				throw new Exception("No access to StringBuilders internal string.");
				/* 
                test for the correct reference name
                Type t = StringBuilder.GetType();
                foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    UnityEngine.Debug.Log(f.Name);
                */
				}
            
			}
		}


	public void clear()
		{
		builder.Remove(0, builder.Length);
		}

	public void create(string text, bool fillOverflow = true)
		{
		create(ref text, fillOverflow);
		}

	public void create(ref string text, bool fillOverflow = true)
		{
		clear();
        
		int max = spaceLeft;

		if (text.Length>=spaceLeft)
			{
			builder.Append(text, 0, max);
			}
			else
			{
			builder.Append(text);
			if (fillOverflow)
				doFillOverflow();
			}
		}

	public void append(ref string text)
		{
		var max = spaceLeft;

		if (text.Length>=spaceLeft)
			builder.Append(text, 0, max);
			else
			builder.Append(text);
		}

	public string append(string text)
		{
		append(ref text);
		return str;
		}


	public void doFillOverflow(char character = ' ')
		{
		var overflow = spaceLeft;
		if (overflow > 0)
			builder.Append(character, overflow);
		}
	}

}