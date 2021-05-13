using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothing = 0.1f;
    [SerializeField] public Vector2 minPosition;
    [SerializeField] public Vector2 maxPosition;

    // Start is called before the first frame update
    private void Start()
    {
         transform.position = GetClampedCameraPosition();
    }

    private void FixedUpdate()
	{
		if(transform.position != player.position)
		{
			transform.position = Vector3.Lerp(transform.position, GetClampedCameraPosition(), smoothing);
		}
	}

	private Vector3 GetClampedCameraPosition()
	{
        Vector3 playerPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
		
		playerPosition.x = Mathf.Clamp(playerPosition.x, minPosition.x, maxPosition.x);
		playerPosition.y = Mathf.Clamp(playerPosition.y, minPosition.y, maxPosition.y);
			
  		return playerPosition;
    }
}
