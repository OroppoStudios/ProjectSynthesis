using UnityEngine;
using DG.Tweening;

namespace Warner {

public class TextIndicator: MonoBehaviour
	{
	#region MEMBER FIELDS

	public Color color;

	private TextRenderer textRenderer;

	#endregion



	#region INIT STUFF

	private void Awake()
		{
		textRenderer = GetComponent<TextRenderer>();
		}


	public void show(string text, float targetScale = 1.7f, float duration = 0.35f)
		{
		textRenderer.color = color;

		textRenderer.renderText(text);

		transform.localScale = Vector3.one*0.6f;

		float xMovement = UnityEngine.Random.Range(0.1f, 0.35f);
		float yMovement = UnityEngine.Random.Range(0f, 0.3f);

		transform.DOMove(new Vector3(transform.position.x+xMovement, transform.position.y+yMovement, transform.position.z), duration);
		transform.DOScale(targetScale+UnityEngine.Random.Range(-0.1f, 0.1f), duration).OnComplete(() =>
			{
			PoolManager.Destroy(gameObject);
			});
		}

	#endregion
	}

}