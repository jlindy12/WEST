using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle, Dialog, Cutscene }

public class GameController : MonoBehaviour
{
  	[SerializeField] PlayerController playerController;
	[SerializeField] BattleSystem battleSystem;
	[SerializeField] Camera worldCamera;

	GameState state;
	
	public static GameController Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		ConditionsDB.Init();
	}

	private void Start()
	{
		playerController.OnEncountered += StartBattle;
		battleSystem.OnBattleOver += EndBattle;

		playerController.OnEnterTrainersView += (Collider2D outlawCollider) =>
		{
			var outlaw = outlawCollider.GetComponentInParent<OutlawController>();
			if (outlaw != null)
			{
				state = GameState.Cutscene;
				StartCoroutine(outlaw.TriggerOutlawBattle(playerController));
			}
		};

			DialogManager.Instance.OnShowDialog += () =>
		{
			state = GameState.Dialog;
		};

		DialogManager.Instance.OnCloseDialog += () =>
		{
			if (state == GameState.Dialog)
				state = GameState.FreeRoam;
		};
	}
	
	void StartBattle()
	{
		state = GameState.Battle;
		battleSystem.gameObject.SetActive(true);
		worldCamera.gameObject.SetActive(false);

		var playerParty = playerController.GetComponent<AnimalParty>();
		var wildAnimal = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildAnimal();

		var wildAnimalCopy = new Animal(wildAnimal.Base, wildAnimal.Level);
		
		battleSystem.StartBattle(playerParty, wildAnimalCopy);
	}

	private OutlawController outlaw;
	
	public void StartOutlawBattle(OutlawController outlaw)
	{
		state = GameState.Battle;
		battleSystem.gameObject.SetActive(true);
		worldCamera.gameObject.SetActive(false);

		this.outlaw = outlaw;
		var playerParty = playerController.GetComponent<AnimalParty>();		
		var outlawParty = outlaw.GetComponent<AnimalParty>();	
		
		battleSystem.StartOutlawBattle(playerParty, outlawParty);
	}

	void EndBattle(bool won)
	{
		if (outlaw != null && won == true)
		{
			outlaw.BattleLost();
			outlaw = null;
		}
		
		state = GameState.FreeRoam;
		battleSystem.gameObject.SetActive(false);
		worldCamera.gameObject.SetActive(true);
	}

	private void Update()
	{
		if (state == GameState.FreeRoam)
		{
			playerController.HandleUpdate();
		}
		else if (state == GameState.Battle)
		{
			battleSystem.HandleUpdate();
		}
		else if (state == GameState.Dialog)
		{
			DialogManager.Instance.HandleUpdate();
		}
	}
}