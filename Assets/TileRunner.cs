using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileRunner : MonoBehaviour {

	public bool isSelected = false;
	public Material normal, outlineBlue, outlineRed, outlineGreen;
	public int x;
	public int z;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void highlight()
	{
		Debug.Log("HIGHLIGHTING A TILE \n\n\n\n\n\n\n\n\n\n\n\n\n\n");
		transform.gameObject.GetComponent<Renderer>().material = outlineGreen;
	}

	public void selected(bool selected)
	{
		this.isSelected = selected;
		if(selected)
		{
			transform.gameObject.GetComponent<Renderer>().material = outlineBlue;
		}
		else
		{
			transform.gameObject.GetComponent<Renderer>().material = normal;
		}
	}
}
