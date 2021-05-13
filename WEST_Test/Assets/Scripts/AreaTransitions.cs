using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AreaTransitions : MonoBehaviour
{
    private FollowPlayer cam;
    public Vector2 newMinPos;
    public Vector2 newMaxPos;
	public bool needText;
	public string placeName;
	public GameObject text;
	public Text placeText;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main.GetComponent<FollowPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
			if(needText)
			{
				StartCoroutine(placeNameCo());
			}
        }
    }

	private IEnumerator placeNameCo()
	{
		text.SetActive(true);
		placeText.text = placeName;
		yield return new WaitForSeconds (3f);
		text.SetActive(false);
	}
}
