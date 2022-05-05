using UnityEngine;

namespace Warner
	{
	public class BackgroundAsset : MonoBehaviour 
		{
		private SpriteRenderer spriteRenderer;
		private AutoMove autoMove;
		private int assetIndex;
		private BackgroundAssetSpawner.LayerData layerData;

		private void Awake()
			{
			checkComponents();
			}

		private void checkComponents()
			{
			if (spriteRenderer==null)
				spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

			if (autoMove==null)
				autoMove = gameObject.AddComponent<AutoMove>();
			}

		public void init(BackgroundAssetSpawner.LayerData layerData, int spriteIndex, int assetIndex)
			{
			checkComponents();
			gameObject.layer = layerData.transform.gameObject.layer;
			spriteRenderer.sortingLayerName = layerData.sortingLayer.name;
			spriteRenderer.sortingOrder = layerData.sortingOrder;
			spriteRenderer.sprite = layerData.sprites[spriteIndex];
			spriteRenderer.sharedMaterial = layerData.material;
			spriteRenderer.sharedMaterial.SetFloat("_BlurAmount", layerData.blur*0.02f);

			autoMove.movingSide = layerData.movingSide;
			autoMove.speed = layerData.movingSpeed;

			this.assetIndex = assetIndex;
			this.layerData = layerData;
			}


		private void OnDisable()
			{
			BackgroundAssetSpawner.instance.assetGotDeSpawned(layerData, assetIndex);
			}
		}
	}