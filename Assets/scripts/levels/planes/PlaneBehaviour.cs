﻿using UnityEngine;
using System.Collections.Generic;

public class PlaneBehaviour : MonoBehaviour 
{
	public Dictionary<GameObject, PlaneProperties> layers; 
	public GameObject LayerPrefab;
	void Start () 
	{
		
	}
	
	public void Setup(List<PlaneProperties> layerProps)
	{
		layers = new Dictionary<GameObject, PlaneProperties>();
		foreach (var currentLayerProps in layerProps)
		{
			var newObject = Instantiate<GameObject>(LayerPrefab);
			newObject.GetComponent<MeshRenderer>().material.mainTexture = currentLayerProps.MainTexture;
			newObject.transform.SetParent(gameObject.transform, false);
			layers.Add(newObject, currentLayerProps);
			
			// TODO: Сделать привязку скриптов
		}
	}
	
	public bool HitTestPoint(Vector3 point)
	{
		foreach (var currentLayerPair in layers)
		{
			var layerGameObject = currentLayerPair.Key;
			var layerProperties = currentLayerPair.Value;
			
			var texture = layerProperties.MainTexture as Texture2D;
			
			var x = point.x - layerGameObject.transform.position.x;
			var y = point.z - layerGameObject.transform.position.z;
			
			var r = Utils.RotateVector2(new Vector2(x, y), layerGameObject.transform.eulerAngles.y);
			
			x = Mathf.Floor((r.x + layerGameObject.transform.localScale.x / 2) / layerGameObject.transform.localScale.x * texture.width);
			y = Mathf.Floor((r.y + layerGameObject.transform.localScale.y / 2) / layerGameObject.transform.localScale.y * texture.height);
			
			if (texture.GetPixel((int)x, (int)y).a > 0)
				return true;
		}
		return false;	
	}
	
	void Update () 
	{
		if (layers == null)
			return;
		foreach (var currentLayer in layers)
		{
			var layerGameObject = currentLayer.Key;
			var layerProperties = currentLayer.Value;
			
			layerGameObject.transform.Rotate(0, 0, layerProperties.RotationSpeed * Time.deltaTime);
		}
	}
}
