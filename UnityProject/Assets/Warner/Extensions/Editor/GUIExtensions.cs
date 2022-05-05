using UnityEngine;

namespace Warner
	{
	public static class GUIExtensions
		{
		public static void setRowRects(Rect rect, float space, Rect[] rects)
			{
			int l = rects.Length;
			for (int i = 0; i<l; i++)
				{
				float h = (rect.height-space*(l-1))/l;

				if (h<0f)
					{
					h = 0f;
					}

				Rect r = rects[i];

				r.x = rect.x;
				r.y = rect.y+i*(h+space);
				r.width = rect.width;
				r.height = h;

				rects[i] = r;
				}
			}

		public static Rect getCurrentLinePosition()
			{
			return GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));
			}

		public static Rect getNextLinePosition(float rectHeight)
			{
			return GUILayoutUtility.GetRect(0f, rectHeight, GUILayout.ExpandWidth(true));
			}

		public static Rect[] getRects(int itemCount, float rectHeight, Vector2 padding)
			{
			Rect[] rects = new Rect[itemCount];
			Rect positionRect = GUILayoutUtility.GetRect(0f, rectHeight, GUILayout.ExpandWidth(true));

			int rectsCount = rects.Length;
			float width = (positionRect.width/rectsCount);
			width -= (padding.x/rectsCount);
			width -= (padding.y/rectsCount);

			float xPosition = positionRect.x+padding.x;

			for (int i = 0; i<rectsCount; i++)
				{
				rects[i].width = width;
				rects[i].height = positionRect.height;

				rects[i].x = xPosition+(i*(width));

				rects[i].y = positionRect.y;
				}

			return rects;
			}		
		}
	}