using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, ActionSelection, MoveSelection, PerformMove, Busy, PartyScreen, BattleOver }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
	[SerializeField] PartyScreen partyScreen;

	BattleState state;
	int currentAction;
	int currentMove;
	int currentMember;

	AnimalParty playerParty; 
	Animal wildAnimal;

    public event Action<bool> OnBattleOver;
    
    // Start is called before the first frame update

	public void StartBattle(AnimalParty playerParty, Animal wildAnimal)
	{
		this.playerParty = playerParty;
		this.wildAnimal = wildAnimal;
	    StartCoroutine(SetupBattle());
    }    

    public IEnumerator SetupBattle()
    {
        playerUnit.Setup(playerParty.GetHealthyAnimal());
        enemyUnit.Setup(wildAnimal);

        partyScreen.Init();

		dialogBox.SetMoveNames(playerUnit.Animal.Moves);

		yield return dialogBox.TypeDialog($"A wild {enemyUnit.Animal.Base.Name} attacked!");

		ChooseFirstTurn();
    }

    void ChooseFirstTurn()
	{
		if(playerUnit.Animal.Speed >= enemyUnit.Animal.Speed)
			ActionSelection();
		else
			StartCoroutine(EnemyMove());
	}

	void BattleOver(bool won)
    {
	    state = BattleState.BattleOver;
		playerParty.Animals.ForEach(p => p.OnBattleOver());
	    OnBattleOver(won);
    }

	void ActionSelection()
	{
		state = BattleState.ActionSelection;
		dialogBox.SetDialog ("Choose an action.");
		dialogBox.EnableActionSelector(true);
	}

	void OpenPartyScreen()
	{
		state = BattleState.PartyScreen;
		partyScreen.SetPartyData(playerParty.Animals);
		partyScreen.gameObject.SetActive(true);
	}

	void MoveSelection()
	{
		state = BattleState.MoveSelection;
		dialogBox.EnableActionSelector(false);
		dialogBox.EnableDialogText(false);
		dialogBox.EnableMoveSelector(true);
	}

	IEnumerator PlayerMove()
	{
		state = BattleState.PerformMove;

		var move = playerUnit.Animal.Moves[currentMove];
		yield return RunMove(playerUnit, enemyUnit, move);
		
		//If the battle state was not changed by RunMove, then go to next step
		if (state == BattleState.PerformMove)
			StartCoroutine(EnemyMove());
	}

	IEnumerator EnemyMove()
	{
		state = BattleState.PerformMove;

		var move = enemyUnit.Animal.GetRandomMove();
		yield return RunMove(enemyUnit, playerUnit, move);
		
		//If the battle state was not changed by RunMove, then go to next step
		if (state == BattleState.PerformMove)
			ActionSelection();
	}

	IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
	{
		bool canRunMove = sourceUnit.Animal.OnBeforeMove();
		if (!canRunMove)
		{
			yield return ShowStatusChanges(sourceUnit.Animal);
			yield return sourceUnit.Hud.UpdateHP();
			yield break;
		}
		yield return ShowStatusChanges(sourceUnit.Animal);
		
		move.PP--;
		yield return dialogBox.TypeDialog($"{sourceUnit.Animal.Base.Name} used {move.Base.Name}!");

		if (CheckIfMoveHits(move, sourceUnit.Animal, targetUnit.Animal))
		{
			sourceUnit.PlayAttackAnimation();
			yield return new WaitForSeconds(1f);
			targetUnit.PlayHitAnimation();

			if (move.Base.Category == MoveCategory.Status)
			{
				yield return RunMoveEffects(move.Base.Effects, sourceUnit.Animal, targetUnit.Animal, move.Base.Target);
			}
			else
			{
				var damageDetails = targetUnit.Animal.TakeDamage(move, sourceUnit.Animal);
				yield return targetUnit.Hud.UpdateHP();
				yield return ShowDamageDetails(damageDetails);
			}

			if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Animal.HP > 0)
			{
				foreach (var secondary in move.Base.Secondaries)
				{
					var rnd = UnityEngine.Random.Range(1, 101);
					if (rnd <= secondary.Chance)
						yield return RunMoveEffects(secondary, sourceUnit.Animal, targetUnit.Animal, secondary.Target);
				}
			}
			
			if(targetUnit.Animal.HP <= 0)
			{
				yield return dialogBox.TypeDialog($"{targetUnit.Animal.Base.Name} can no longer fight!");
				targetUnit.PlayFaintAnimation();
				yield return new WaitForSeconds(2f);

				CheckForBattleOver(targetUnit);
			}
		}
		else
		{
			yield return dialogBox.TypeDialog($"{sourceUnit.Animal.Base.Name}'s attack missed!");
		}

		//Apply any after turn effects (i.e. Poison Damage)
		sourceUnit.Animal.OnAfterTurn();
		yield return ShowStatusChanges(sourceUnit.Animal);
		yield return sourceUnit.Hud.UpdateHP();
		if(sourceUnit.Animal.HP <= 0)
		{
			yield return dialogBox.TypeDialog($"{sourceUnit.Animal.Base.Name} can no longer fight!");
			sourceUnit.PlayFaintAnimation();
			yield return new WaitForSeconds(2f);

			CheckForBattleOver(sourceUnit);
		}
	}

	IEnumerator RunMoveEffects(MoveEffects effects, Animal source, Animal target, MoveTarget moveTarget)
	{
		// Stat Boosting
		if(effects.Boosts != null)
		{
			if(moveTarget == MoveTarget.Self)
				source.ApplyBoosts(effects.Boosts);
			else
				target.ApplyBoosts(effects.Boosts);
		}

		//Status Conditions
		if (effects.Status != ConditionID.none)
		{
			target.SetStatus(effects.Status);
		}

		//VolatileStatus Conditions
		if (effects.VolatileStatus != ConditionID.none)
		{
			target.SetVolatileStatus(effects.VolatileStatus);
		}
		
		yield return ShowStatusChanges(source);
		yield return ShowStatusChanges(target);
	}

	//Incorporating the Accuracy Stat
	bool CheckIfMoveHits(Move move, Animal source, Animal target)
	{
		if (move.Base.AlwaysHits)
			return true;
		
		float moveAccuracy = move.Base.Accuracy;

		int accuracy = source.StatBoosts[Stat.Accuracy];
		int evasion = target.StatBoosts[Stat.Evasion];

		var boostValues = new float[] {1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f};

		if (accuracy > 0)
			moveAccuracy *= boostValues[accuracy];
		else
			moveAccuracy /= boostValues[-accuracy];

		if (evasion > 0)
			moveAccuracy /= boostValues[evasion];
		else
			moveAccuracy *= boostValues[-evasion];
		
		return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
	}

	IEnumerator ShowStatusChanges(Animal animal)
	{
		while (animal.StatusChanges.Count > 0)
		{
			var message = animal.StatusChanges.Dequeue();
			yield return dialogBox.TypeDialog(message);
		}
	}

	void CheckForBattleOver(BattleUnit faintedUnit)
	{
		if (faintedUnit.IsPlayerUnit)
		{
			var nextAnimal = playerParty.GetHealthyAnimal();
			if (nextAnimal != null)
				OpenPartyScreen();
			else
				BattleOver(false);
		}
		else
			BattleOver(true);
	}

	//Show Damage Details in the Dialog Box
	IEnumerator ShowDamageDetails(DamageDetails damageDetails)
	{
		if (damageDetails.Critical > 1f)
			yield return dialogBox.TypeDialog("A critical hit!");
		
		if (damageDetails.TypeEffectiveness > 1f)
			yield return dialogBox.TypeDialog("It's super effective!");
		else if (damageDetails.TypeEffectiveness < 1f)
			yield return dialogBox.TypeDialog("It doesn't have much effect...");
	}

	public void HandleUpdate()
	{
		if(state == BattleState.ActionSelection)
		{
			HandleActionSelection();
		}
		else if (state == BattleState.MoveSelection)
		{
			HandleMoveSelection();
		}
		else if (state == BattleState.PartyScreen)
		{
			HandlePartySelection();
		}
	}

	//Using Arrow Keys to Choose an Action
	void HandleActionSelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow))
			++currentAction;
		else if (Input.GetKeyDown(KeyCode.LeftArrow))
			--currentAction;
		else if (Input.GetKeyDown(KeyCode.DownArrow))
			currentAction += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow))
			currentAction -= 2;

		currentAction = Mathf.Clamp(currentAction, 0, 3);

		dialogBox.UpdateActionSelection(currentAction);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (currentAction == 0)
				{
					//Fight
					MoveSelection();
				}
			else if (currentAction == 1)
				{
					//Bag
				}
			else if (currentAction == 2)
				{
					//Animals
					OpenPartyScreen();
				}
			else if (currentAction == 3)
				{
					//Run
				}
		}
	}
	
	//Using Arrow Keys to Choose a Move
	void HandleMoveSelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow))
			++currentMove;
		else if (Input.GetKeyDown(KeyCode.LeftArrow))
			--currentMove;
		else if (Input.GetKeyDown(KeyCode.DownArrow))
			currentMove += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow))
			currentMove -= 2;

		currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Animal.Moves.Count -1);

		dialogBox.UpdateMoveSelection(currentMove, playerUnit.Animal.Moves[currentMove]);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			dialogBox.EnableMoveSelector(false);
			dialogBox.EnableDialogText(true);
			StartCoroutine(PlayerMove());
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			dialogBox.EnableMoveSelector(false);
			dialogBox.EnableDialogText(true);
			ActionSelection();
		}
	}

	void HandlePartySelection()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow))
			++currentMember;
		else if (Input.GetKeyDown(KeyCode.LeftArrow))
			--currentMember;
		else if (Input.GetKeyDown(KeyCode.DownArrow))
			currentMember += 2;
		else if (Input.GetKeyDown(KeyCode.UpArrow))
			currentMember -= 2;

		currentMember = Mathf.Clamp(currentMember, 0, playerParty.Animals.Count -1);

		partyScreen.UpdateMemberSelection(currentMember);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			var selectedMember = playerParty.Animals[currentMember];
			if (selectedMember.HP <=0)
			{
				partyScreen.SetMessageText($"{selectedMember.Base.Name} is unable to fight!");
				return;
			}
			if (selectedMember == playerUnit.Animal)
			{
				partyScreen.SetMessageText($"{playerUnit.Animal.Base.Name} is already in the fight!");
				return;
			}

			partyScreen.gameObject.SetActive(false);
			state = BattleState.Busy;
			StartCoroutine(SwitchAnimal(selectedMember));
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			partyScreen.gameObject.SetActive(false);
			ActionSelection();
		}
	}

	IEnumerator SwitchAnimal(Animal newAnimal)
	{
		bool currentAnimalFainted = true;
		if (playerUnit.Animal.HP >0)
		{
			currentAnimalFainted = false;
			yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} come back!");
			playerUnit.PlayFaintAnimation();
			yield return new WaitForSeconds(2f);
		}

		playerUnit.Setup(newAnimal);

		dialogBox.SetMoveNames(newAnimal.Moves);
		yield return dialogBox.TypeDialog($"Go {newAnimal.Base.Name}!");

		if (currentAnimalFainted)
			ChooseFirstTurn();
		else
			StartCoroutine(EnemyMove());
	}
}
