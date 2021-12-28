using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
	{
		foreach (var kvp in Conditions)
		{
			var conditionId = kvp.Key;
			var condition = kvp.Value;

			condition.Id = conditionId;
		}
	}

	public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name = "Posion",
                StartMessage = "has been poisoned!",
                OnAfterTurn = (Animal animal) =>
                {
                    animal.UpdateHP(animal.MaxHP / 8);
                    animal.StatusChanges.Enqueue($"{animal.Base.Name} is hurt by the poison!");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Name = "Burn",
                StartMessage = "has been burned!",
                OnAfterTurn = (Animal animal) =>
                {
                    animal.UpdateHP(animal.MaxHP / 16);
                    animal.StatusChanges.Enqueue($"{animal.Base.Name} is hurt by its burn!");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Name = "Paralyzed",
                StartMessage = "has been paralyzed!",
                OnBeforeMove = (Animal animal) =>
                {
                    // 25% Chance of Being Unable to Move due to Paralysis
                    if (Random.Range(1, 5) == 1)
                    {
                        animal.StatusChanges.Enqueue($"{animal.Base.Name} is paralyzed and is unable to move!");
                        return false;
                    }
                    
                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name = "Freeze",
                StartMessage = "has been frozen!",
                OnBeforeMove = (Animal animal) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        animal.CureStatus();
                        animal.StatusChanges.Enqueue($"{animal.Base.Name} is no longer frozen!");
                        return true;
                    }
                    
                    return false;
                }
            }
        },
		{
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep!",
                OnStart = (Animal animal) =>
                {
                    //Sleep for 1-3 turns
					animal.StatusTime = Random.Range(1, 4);
					Debug.Log($"Will be asleep for {animal.StatusTime} moves");
                },
				OnBeforeMove = (Animal animal) =>
                {
                    if (animal.StatusTime <=0)
					{
						animal.CureStatus();
						animal.StatusChanges.Enqueue($"{animal.Base.Name} woke up!");
						return true;
					}

					animal.StatusTime--;
					animal.StatusChanges.Enqueue($"{animal.Base.Name} is still asleep!");
                    return false;
                }
            }
        },

		//Volatile Status Conditions
		{
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                StartMessage = "has become confused!",
                OnStart = (Animal animal) =>
                {
                    //Confused for 1-4 turns
					animal.VolatileStatusTime = Random.Range(1, 5);
					Debug.Log($"Will be confused for {animal.StatusTime} moves");
                },
				OnBeforeMove = (Animal animal) =>
                {
                    if (animal.VolatileStatusTime <=0)
					{
						animal.CureVolatileStatus();
						animal.StatusChanges.Enqueue($"{animal.Base.Name} snapped out of confusion!");
						return true;
					}

					animal.VolatileStatusTime--;
					animal.StatusChanges.Enqueue($"{animal.Base.Name} is confused...");

					//50% Chance to Perform Move
					if (Random.Range(1, 3) == 1)
						return true;

					//Hurt by Confusion
                    animal.UpdateHP(animal.MaxHP / 8);
					animal.StatusChanges.Enqueue($"{animal.Base.Name} hurt itself in confusion!");
					return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
        else if (condition.Id == ConditionID.slp || condition.Id == ConditionID.frz)
            return 2f;
        else if (condition.Id == ConditionID.par || condition.Id == ConditionID.psn || condition.Id == ConditionID.brn)
            return 1.5f;

        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, slp, par, frz,
	confusion
}