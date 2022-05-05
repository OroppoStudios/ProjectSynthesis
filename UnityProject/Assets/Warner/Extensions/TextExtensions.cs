namespace Warner {

public static class TextExtensions
	{
	public static string upFirst(string str, bool lowerCaseRest = true)
		{
		if (string.IsNullOrEmpty(str))
			return "";

		if (lowerCaseRest)
			str = str.ToLower();
							
		return char.ToUpper(str[0]) + str.Substring(1);
		}


	public static string lowerFirst(string str, bool upCaseRest = true)
		{
		if (string.IsNullOrEmpty(str))
			return "";

		if (upCaseRest)
			str = str.ToUpper();

		return char.ToLower(str[0]) + str.Substring(1);
		}
	}

}