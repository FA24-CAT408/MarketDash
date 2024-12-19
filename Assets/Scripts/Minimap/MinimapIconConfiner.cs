using Unity.VisualScripting;
using UnityEngine;

public class MinimapIconConfiner : MonoBehaviour
{
	Transform MinimapCam;
	float MinimapSize;
	[SerializeField] Vector3 TempV3;
	float originalYPosition;

	void Start()
	{
		MinimapCam = GameObject.Find("Minimap Cam").transform;
		MinimapSize = MinimapCam.GetComponent<Camera>().orthographicSize;
	}

	void Update()
	{
		TempV3 = transform.parent.transform.position;
		TempV3.y = transform.position.y-5;
		transform.position = TempV3;
	}


	void LateUpdate()
	{
		// Center of Minimap
		Vector3 centerPosition = MinimapCam.transform.localPosition;


		// Just to keep a distance between Minimap camera and this Object (So that camera don't clip it out)
		centerPosition.y -= 0.5f;


		// Distance from the gameObject to Minimap
		float Distance = Vector3.Distance(transform.position, centerPosition);


		// If the Distance is less than MinimapSize, it is within the Minimap view and we don't need to do anything
		// But if the Distance is greater than the MinimapSize, then do this
		if (Distance > MinimapSize)
		{
			// Gameobject - Minimap
			Vector3 fromOriginToObject = transform.position - centerPosition;


			// Multiply by MinimapSize and Divide by Distance
			fromOriginToObject *= MinimapSize / (Distance + 7);


			// Minimap + above calculation
			transform.position = centerPosition + fromOriginToObject;
		}
	}

}
