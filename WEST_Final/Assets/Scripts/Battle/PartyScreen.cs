using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText;
    PartyMemberUI[] memberSlots;
    List<Animal> animals;

    //Function Preventing the need for Manual Changes to Party Members/Composition in the Inspector
    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
    }

    //Will only show the number of slots equal to the number of members in the party. Disables others.
    public void SetPartyData(List<Animal> animals)
    {
        this.animals = animals;
        
        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < animals.Count) 
			{ 
                memberSlots[i].gameObject.SetActive(true);
				memberSlots[i].SetData(animals[i]);
			}
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        messageText.text = "Choose an Animal.";
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        for (int i = 0; i < animals.Count; i++)
        {
            if (i == selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }
    }

	public void SetMessageText(string message)
	{
		messageText.text = message;
	}
}
