using UnityEngine;

namespace Warner
	{
	[ExecuteInEditMode,RequireComponent(typeof(SpriteRenderer))]
	public class GodRays: MonoBehaviour
		{
		public Color color1 = Color.white;
		public Color color2 = Color.white;
		[Range(0f, 5f)] public float speed = 0.5f;
		[Range(1f, 30f)] public float size = 15f;
		[Range(0f, 1f)] public float width = 1f;
		[Range(-1f, 1f)] public float skew = 0.5f;
		[Range(0f, 5f)] public float shear = 1f;
		[Range(0f, 1f)] public float fade = 1f;
		[Range(0f, 50f)] public float contrast = 1f;

		private SpriteRenderer spriteRenderer;
		private Material material;
		private Color _color1;
		private Color _color2;
		private float _speed;
		private float _size;
		private float _skew;
		private float _shear;
		private float _fade;
		private float _contrast;
		private float _width;

		private void Awake()
			{
			material = new Material(Shader.Find("Warner/Sprites/GodRays"));
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.material = material;
			}

		void Update()
			{
			if (AnythingChanged())
				{
				material.SetColor("_Color1", _color1);
				material.SetColor("_Color2", _color2);
				material.SetFloat("_Speed", _speed);
				material.SetFloat("_Size", _size);
				material.SetFloat("_Skew", _skew);
				material.SetFloat("_Shear", _shear);
				material.SetFloat("_Fade", _fade);
				material.SetFloat("_Contrast", _contrast);
				material.SetFloat("_Width", _width);
				}
			}

		bool AnythingChanged()
			{
			if (_color1!=color1)
				{
				_color1 = color1;
				return true;
				}
			if (_color2!=color2)
				{
				_color2 = color2;
				return true;
				}
			if (_speed!=speed)
				{
				_speed = speed;
				return true;
				}
			if (_size!=size)
				{
				_size = size;
				return true;
				}
			if (_skew!=skew)
				{
				_skew = skew;
				return true;
				}
			if (_shear!=shear)
				{
				_shear = shear;
				return true;
				}
			if (_fade!=fade)
				{
				_fade = fade;
				return true;
				}
			if (_contrast!=contrast)
				{
				_contrast = contrast;
				return true;
				}

			if (_width!=width)
				{
				_width = width;
				return true;
				}

			return false;
			}

		}
	}