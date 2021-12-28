using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver }
public enum BattleAction { Move, SwitchAnimal, UseItem, Run }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
	[SerializeField] PartyScreen partyScreen;
	[SerializeField] Image playerImage;
	[SerializeField] Image outlawImage;
	[SerializeField] GameObject trappersNetSprite;
	[SerializeField] MoveSelectionUI moveSelectionUI;

	BattleState state;
	BattleState? prevState;
	int currentAction;
	int currentMove;
	int currentMember;
	bool aboutToUseChoice = true;

	AnimalParty playerParty;
	AnimalParty outlawParty;
	Animal wildAnimal;

	bool isOutlawBattle = false;
	PlayerController player;
	OutlawController outlaw;

	int escapeAttempts;

    public event Action<bool> OnBattleOver;
    
    // Start is called before the first frame update

	public void StartBattle(AnimalParty playerParty, Animal wildAnimal)
	{
		this.playerParty = playerParty;
		this.wildAnimal = wildAnimal;
		player = playerParty.GetComponent<PlayerController>();
		isOutlawBattle = false;
		
	    StartCoroutine(SetupBattle());
    }    
	
	public void StartOutlawBattle(AnimalParty playerParty, AnimalParty outlawParty)
	{
		this.playerParty = playerParty;
		this.outlawParty = outlawParty;

		isOutlawBattle = true;
		player = playerParty.GetComponent<PlayerController>();
		outlaw = outlawParty.GetComponent<OutlawController>();
		StartCoroutine(SetupBattle());
	}  

    public IEnumerator SetupBattle()
    {
	    playerUnit.Clear();
	    enemyUnit.Clear();
	    
	    if (!isOutlawBattle)
	    {
		    //Wild Animal Battle
		    playerUnit.Setup(playerParty.GetHealthyAnimal());
		    enemyUnit.Setup(wildAnimal);
		    
		    dialogBox.SetMoveNames(playerUnit.Animal.Moves);

		    yield return dialogBox.TypeDialog($"A wild {enemyUnit.Animal.Base.Name} attacked!");
	    }
	    else
	    {
		    //Outlaw Battle
		    
		    //Show Trainer and Outlaw Sprites
		    playerUnit.gameObject.SetActive(false);
		    enemyUnit.gameObject.SetActive(false);
		    
		    playerImage.gameObject.SetActive(true);
		    outlawImage.gameObject.SetActive(true);
		    playerImage.sprite = player.Sprite;
		    outlawImage.sprite = outlaw.Sprite;

		    yield return dialogBox.TypeDialog($"{outlaw.Name} demands a duel!");
		    
		    // Send out first animal of the outlaw
		    outlawImage.gameObject.SetActive(false);
		    enemyUnit.gameObject.SetActive(true);
		    var enemyAnimal = outlawParty.GetHealthyAnimal();
		    enemyUnit.Setup(enemyAnimal);

		    yield return dialogBox.TypeDialog($"{outlaw.Name} sent out {enemyAnimal.Base.Name}!");
		    
		    // Send out first animal of the player
		    playerImage.gameObject.SetActive(false);
		    playerUnit.gameObject.SetActive(true);
		    var playerAnimal = playerParty.GetHealthyAnimal();
		    playerUnit.Setup(playerAnimal);

		    yield return dialogBox.TypeDialog($"Get 'em {playerAnimal.Base.Name}!");
		    dialogBox.SetMoveNames(playerUnit.Animal.Moves);
	    }

	    escapeAttempts = 0;
        partyScreen.Init();
        ActionSelection();
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

	IEnumerator AboutToUse(Animal newAnimal)
	{
		state = BattleState.Busy;
		yield return dialogBox.TypeDialog($"{outlaw.Name} is about to use {newAnimal.Base.Name}. Do you want to switch animals?");

		state = BattleState.AboutToUse;
		dialogBox.EnableChoiceBox(true);
	}

	IEnumerator ChooseMoveToForget(Animal animal, MoveBase newMove)
	{
		state = BattleState.Busy;
		yield return dialogBox.TypeDialog($"Choose a move to forget.");
		moveSelectionUI.gameObject.SetActive(true);
		//Set Move Names
		moveSelectionUI.SetMoveData(animal.Moves.Select(x => x.Base).ToList(), newMove);

		state = BattleState.MoveToForget;
	}

	IEnumerator RunTurns(BattleAction playerAction)
	{
		state = BattleState.RunningTurn;

		if (playerAction == BattleAction.Move)
		{
			playerUnit.Animal.CurrentMove = playerUnit.Animal.Moves[currentMove];
			enemyUnit.Animal.CurrentMove = enemyUnit.Animal.GetRandomMove();

			int playerMovePriority = playerUnit.Animal.CurrentMove.Base.Priority;
			int enemyMovePriority = enemyUnit.Animal.CurrentMove.Base.Priority;
			
			// Check Who Goes First
			bool playerGoesFirst = true;
			if (enemyMovePriority > playerMovePriority)
				playerGoesFirst = false;
			else if (enemyMovePriority == playerMovePriority)
				playerGoesFirst = playerUnit.Animal.Speed >= enemyUnit.Animal.Speed;
			
			var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
			var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

			var secondAnimal = secondUnit.Animal;
			
			 
			// First Turn
			yield return RunMove(firstUnit, secondUnit, firstUnit.Animal.CurrentMove);
			yield return RunAfterTurn(firstUnit);
			if (state == BattleState.BattleOver) yield break;

			if (secondAnimal.HP > 0)
			{
				// Second Turn
				yield return RunMove(secondUnit, firstUnit, secondUnit.Animal.CurrentMove);
				yield return RunAfterTurn(secondUnit);
				if (state == BattleState.BattleOver) yield break;
			}
		}
		else
		{
			if (playerAction == BattleAction.SwitchAnimal)
			{
				var selectedAnimal = playerParty.Animals[currentMember];
				state = BattleState.Busy;
				yield return SwitchAnimal(selectedAnimal);
			}
			else if (playerAction == BattleAction.UseItem)
			{
				dialogBox.EnableActionSelector(false);
				yield return ThrowTrappersNet();
			}
			else if (playerAction == BattleAction.Run)
			{
				yield return TryToEscape();
			}
			
			// Enemy Turn
			var enemyMove = enemyUnit.Animal.GetRandomMove();
			yield return RunMove(enemyUnit, playerUnit, enemyMove);
			yield return RunAfterTurn(enemyUnit);
			if (state == BattleState.BattleOver) yield break;
		}

		if (state != BattleState.BattleOver)
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
				yield return HandleAnimalFainted(targetUnit);
			}
		}
		else
		{
			yield return dialogBox.TypeDialog($"{sourceUnit.Animal.Base.Name}'s attack missed!");
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

	IEnumerator RunAfterTurn(BattleUnit sourceUnit)
	{
		if (state == BattleState.BattleOver) yield break;
		yield return new WaitUntil(() => state == BattleState.RunningTurn);
		
		//Apply any after turn effects (i.e. Poison Damage)
		sourceUnit.Animal.OnAfterTurn();
		yield return ShowStatusChanges(sourceUnit.Animal);
		yield return sourceUnit.Hud.UpdateHP();
		if(sourceUnit.Animal.HP <= 0)
		{
			yield return HandleAnimalFainted(sourceUnit);
			yield return new WaitUntil(() => state == BattleState.RunningTurn);
		}
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

	IEnumerator HandleAnimalFainted(BattleUnit faintedUnit)
	{
		yield return dialogBox.TypeDialog($"{faintedUnit.Animal.Base.Name} can no longer fight!");
		faintedUnit.PlayFaintAnimation();
		yield return new WaitForSeconds(2f);

		if (!faintedUnit.IsPlayerUnit)
		{
			//EXP GAIN
			int expYield = faintedUnit.Animal.Base.ExpYield;
			int enemyLevel = faintedUnit.Animal.Level;
			float outlawBonus = (isOutlawBattle)? 1.5f : 1f;

			int expGain = Mathf.FloorToInt((expYield * enemyLevel * outlawBonus) / 7);
			playerUnit.Animal.Exp += expGain;
			yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} gained {expGain} experience!");
			yield return playerUnit.Hud.SetExpSmooth();

			//Check Level Up
			while (playerUnit.Animal.CheckForLevelUp())
			{
				playerUnit.Hud.SetLevel();
				yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} reached level {playerUnit.Animal.Level}!");
				
				// Try to Learn New Move
				var newMove = playerUnit.Animal.GetLearnableMoveAtCurrLevel();
				if (newMove != null)
				{
					if (playerUnit.Animal.Moves.Count < AnimalBase.MaxNumOfMoves)
					{
						playerUnit.Animal.LearnMove(newMove);
						yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} learned {newMove.Base.Name}");
						dialogBox.SetMoveNames(playerUnit.Animal.Moves);
					}
					else
					{
						yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} wants to learn {newMove.Base.Name},");
						yield return dialogBox.TypeDialog($"but it can only learn {AnimalBase.MaxNumOfMoves} moves.");
						yield return ChooseMoveToForget(playerUnit.Animal, newMove.Base);
						yield return new WaitUntil(() => state != BattleState.MoveToForget);
					}
				}
				
				yield return playerUnit.Hud.SetExpSmooth(true);
			}

			yield return new WaitForSeconds(1f);
		}

		CheckForBattleOver(faintedUnit);
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
		{
			if (!isOutlawBattle)
			{
				BattleOver(true);
			}
			else
			{
				var nextAnimal = outlawParty.GetHealthyAnimal();
				if (nextAnimal != null) 
					StartCoroutine(AboutToUse(nextAnimal));	
				else
					BattleOver(true);
			}
		}
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
		else if (state == BattleState.AboutToUse)
		{
			HandleAboutToUse();
		}
		else if (state == BattleState.MoveToForget)
		{
			moveSelectionUI.HandleMoveSelection();
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
					StartCoroutine(RunTurns(BattleAction.UseItem));
				}
			else if (currentAction == 2)
				{
					//Animals
					prevState = state;
					OpenPartyScreen();
				}
			else if (currentAction == 3)
				{
					//Run
					StartCoroutine(RunTurns(BattleAction.Run));
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
			var move = playerUnit.Animal.Moves[currentMove];
			if (move.PP == 0) return;

			dialogBox.EnableMoveSelector(false);
			dialogBox.EnableDialogText(true);
			StartCoroutine(RunTurns(BattleAction.Move));
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

			if (prevState == BattleState.ActionSelection)
			{
				prevState = null;
				StartCoroutine(RunTurns(BattleAction.SwitchAnimal));
			}
			else
			{
				state = BattleState.Busy;
				StartCoroutine(SwitchAnimal(selectedMember));
			}
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			if (playerUnit.Animal.HP <= 0)
			{
				partyScreen.SetMessageText("You must select an animal to continue the duel!");
				return;
			}
			
			partyScreen.gameObject.SetActive(false);

			if (prevState == BattleState.AboutToUse)
			{
				prevState = null;
				StartCoroutine(SendNextOutlawAnimal());
			}
			else
				ActionSelection();
		}
	}

	void HandleAboutToUse()
	{
		if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
			aboutToUseChoice = !aboutToUseChoice;

		dialogBox.UpdateChoiceBox(aboutToUseChoice);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			dialogBox.EnableChoiceBox(false);
			if (aboutToUseChoice == true)
			{
				// Yes Option
				prevState = BattleState.AboutToUse;
				OpenPartyScreen();
			}
			else
			{
				// No Option
				StartCoroutine(SendNextOutlawAnimal());
			}
		}
		else if (Input.GetKeyDown(KeyCode.X))
		{
			dialogBox.EnableChoiceBox(false);
			StartCoroutine(SendNextOutlawAnimal());
		}
	}

	IEnumerator SwitchAnimal(Animal newAnimal)
	{
		if (playerUnit.Animal.HP >0)
		{
			yield return dialogBox.TypeDialog($"{playerUnit.Animal.Base.Name} come back!");
			playerUnit.PlayFaintAnimation();
			yield return new WaitForSeconds(2f);
		}

		playerUnit.Setup(newAnimal);

		dialogBox.SetMoveNames(newAnimal.Moves);
		yield return dialogBox.TypeDialog($"Go {newAnimal.Base.Name}!");

		if (prevState == null)
		{
			state = BattleState.RunningTurn;
		}
		else if (prevState == BattleState.AboutToUse)
		{
			prevState = null;
			StartCoroutine(SendNextOutlawAnimal());
		}
		
	}

	IEnumerator SendNextOutlawAnimal()
	{
		state = BattleState.Busy;

		var nextAnimal = outlawParty.GetHealthyAnimal();
		enemyUnit.Setup(nextAnimal);
		yield return dialogBox.TypeDialog($"{outlaw.Name} sent out {nextAnimal.Base.Name}!");

		state = BattleState.RunningTurn;
	}

	IEnumerator ThrowTrappersNet()
	{
		state = BattleState.Busy;

		if (isOutlawBattle)
		{
			yield return dialogBox.TypeDialog($"Outlaw blocked the net, you can't steal someone else's posse members!");
			state = BattleState.RunningTurn;
			yield break;
		}

		yield return dialogBox.TypeDialog($"{player.Name} threw a TRAPPER'S NET!");
		
		var trappersNetObj = Instantiate(trappersNetSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
		var trappersNet = trappersNetObj.GetComponent<SpriteRenderer>();
		
		// Throw Animation
		yield return trappersNet.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 1f, 1, 1f).WaitForCompletion();
		
		// Capture Animation
		yield return enemyUnit.PlayCaptureAnimation();
		
		// Net Falling to Ground Animation
		yield return trappersNet.transform.DOMoveY(enemyUnit.transform.position.y - 2.5f, 0.5f).WaitForCompletion();

		int shakeCount = TryToCatchAnimal(enemyUnit.Animal);
		
		// Shake Animation
		for (int i=0; i< Mathf.Min(shakeCount, 3); ++i)
		{
			yield return new WaitForSeconds(0.5f);
			yield return trappersNet.transform.DOPunchRotation(new Vector3(0, 0, 10f), 1f).WaitForCompletion();
		}

		if (shakeCount == 4)
		{
			// Animal is Caught
			yield return dialogBox.TypeDialog($"{enemyUnit.Animal.Base.Name} was caught!");
			yield return trappersNet.DOFade(0, 1.5f).WaitForCompletion();

			playerParty.AddAnimal(enemyUnit.Animal);
			yield return dialogBox.TypeDialog($"{enemyUnit.Animal.Base.Name} was added to your posse!");

			Destroy(trappersNet);
			BattleOver(true);
		}
		else
		{
			// Animal Broke Out
			yield return new WaitForSeconds(1f);
			trappersNet.DOFade(0, 0.2f);
			yield return enemyUnit.PlayBreakOutAnimation();
			
			if (shakeCount < 2)
				yield return dialogBox.TypeDialog($"{enemyUnit.Animal.Base.Name} easily ripped through the net!");
			else
				yield return dialogBox.TypeDialog($"{enemyUnit.Animal.Base.Name} barely got out!");

			Destroy(trappersNet);
			state = BattleState.RunningTurn;
		}
	}

	int TryToCatchAnimal(Animal animal)
	{
		float a = (3 * animal.MaxHP - 2 * animal.HP) * animal.Base.CatchRate * ConditionsDB.GetStatusBonus(animal.Status) / (3 * animal.MaxHP);

		if (a >= 255)
			return 4;

		float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

		int shakeCount = 0;
		while (shakeCount < 4)
		{
			if (UnityEngine.Random.Range(0, 65535) >= b)
				break;
			
			++shakeCount;
		}

		return shakeCount;
	}

	IEnumerator TryToEscape()
	{
		state = BattleState.Busy;

		if (isOutlawBattle)
		{
			yield return dialogBox.TypeDialog($"You can't run from a duel!");
			state = BattleState.RunningTurn;
			yield break;
		}

		++escapeAttempts;

		int playerSpeed = playerUnit.Animal.Speed;
		int enemySpeed = enemyUnit.Animal.Speed;

		if (enemySpeed < playerSpeed)
		{
			yield return dialogBox.TypeDialog($"Got away safely!");
			BattleOver(true);
		}
		else
		{
			float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
			f = f % 256;

			if (UnityEngine.Random.Range(0, 256) < f)
			{
				yield return dialogBox.TypeDialog($"Got away safely!");
				BattleOver(true);
			}
			else
			{
				yield return dialogBox.TypeDialog($"Unable to get away!");
				state = BattleState.RunningTurn;
			}
		}
	}
}
