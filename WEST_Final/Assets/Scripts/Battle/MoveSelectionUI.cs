using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveSelectionUI : MonoBehaviour
{
  	[SerializeField] List<Text> moveTexts;

	[SerializeField] Color highlightedColor;

	int currentSelection = 0;

	public void SetMoveData(List<MoveBase> currentMoves, MoveBase newMove)
	{
		for (int i=0; i<currentMoves.Count; ++i)
		{
			moveTexts[i].text = currentMoves[i].Name;
		}

		moveTexts[currentMoves.Count].text = newMove.Name;
	}

	public void HandleMoveSelection()
	{
		if (Input.GetKeyDown(KeyCode.DownArrow))
			++currentSelection;
		else if (Input.GetKeyDown(KeyCode.UpArrow))
			--currentSelection;

		currentSelection = Mathf.Clamp(currentSelection, 0, AnimalBase.MaxNumOfMoves);

		UpdateMoveSelection(currentSelection);
	}

	public void UpdateMoveSelection(int selection)
	{
		for (int i = 0; i < AnimalBase.MaxNumOfMoves+1; i++)
		{
			if (i == selection)
				moveTexts[i].color = highlightedColor;
			else
				moveTexts[i].color =Color.black;
		}
	}
}
