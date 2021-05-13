using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchMusicTrigger : MonoBehaviour
{
    public AudioClip newTrack;
    private AudioManager theAM;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            if(newTrack != null)
			{            
				theAM = FindObjectOfType<AudioManager>();
                theAM.ChangeBGM(newTrack);
			}
        }
    }
}
