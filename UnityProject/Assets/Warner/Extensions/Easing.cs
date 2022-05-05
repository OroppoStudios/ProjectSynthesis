namespace Warner {

public static class Easing
	{
	public static float linear(float t)// accelerating from zero velocity
		{
		return t;
		}  


	public static float easeInQuad(float t)// decelerating to zero velocity
		{
		return t*t;
		}


	public static float easeOutQuad(float t)// acceleration until halfway, then deceleration
		{
		return t*(2-t);
		}


	public static float easeInOutQuad(float t)
		{
		return t<.5f ? 2*t*t : -1+(4-2*t)*t;
		}


	public static float easeInCubic(float t)
		{
		return t*t*t;
		}


	public static float easeOutCubic(float t)
		{
		return (--t)*t*t+1;
		}


	public static float easeInOutCubic(float t)
		{
		return t<.5f ? 4*t*t*t : (t-1)*(2*t-2)*(2*t-2)+1;
		}


	public static float easeInQuart(float t)
		{
		return t*t*t*t;
		}


	public static float easeOutQuart(float t)
		{
		return 1-(--t)*t*t*t;
		}


	public static float easeInOutQuart(float t)
		{
		return t<.5f ? 8*t*t*t*t : 1-8*(--t)*t*t*t;
		}


	public static float easeInQuint(float t) 
		{
		return t*t*t*t*t;
		}


	public static float easeOutQuint(float t)
		{
		return 1+(--t)*t*t*t*t;
		}


	public static float easeInOutQuint(float t)
		{
		return t<.5f ? 16*t*t*t*t*t : 1+16*(--t)*t*t*t*t;
		}
	}

}