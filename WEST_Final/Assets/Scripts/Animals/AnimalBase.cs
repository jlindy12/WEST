using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Animal", menuName = "Animal/Create New Animal")]

public class AnimalBase : ScriptableObject
{
   [SerializeField] string name;

	[TextArea]
	[SerializeField] string description;

	[SerializeField] Sprite frontSprite;
	[SerializeField] Sprite backSprite;

	[SerializeField] AnimalType type1;
	[SerializeField] AnimalType type2;

	// Base Stats
	[SerializeField] int maxHp;
	[SerializeField] int attack;
	[SerializeField] int defense;
	[SerializeField] int spAttack;
	[SerializeField] int spDefense;
	[SerializeField] int speed;

	[SerializeField] List<LearnableMove> learnableMoves;
	
	public string Name
	{
		get { return name; }
	}
	
	public string Description
	{
		get { return description; }
	}

	public Sprite FrontSprite
	{
		get { return frontSprite; }
	}
	
	public Sprite BackSprite
	{
		get { return backSprite; }
	}

	public AnimalType Type1
	{
		get { return type1; }
	}
	
	public AnimalType Type2
	{
		get { return type2; }
	}

	public int MaxHP
	{
		get { return maxHp; }
	}
	
	public int Attack
	{
		get { return attack; }
	}

	public int Defense
	{
		get { return defense; }
	}
	
	public int SpAttack
	{
		get { return spAttack; }
	}

	public int SpDefense
	{
		get { return spDefense; }
	}
	
	public int Speed
	{
		get { return speed; }
	}

	public List<LearnableMove> LearnableMoves
	{
		get { return learnableMoves; }
	}
}

[System.Serializable]

public class LearnableMove
{
	[SerializeField] MoveBase moveBase;
	[SerializeField] int level;

	public MoveBase Base
	{
		get { return moveBase; }
	}
	
	public int Level
	{
		get { return level; }
	}
}

public enum AnimalType
{
	None,
	Normal,
	Fire,
	Water,
	Electric,
	Grass,
	Ice,
	Fighting,
	Poison,
	Ground,
	Flying,
	Psychic,
	Bug,
	Rock,
	Ghost,
	Dragon
}

public enum Stat
{
	Attack,
	Defense,
	SpAttack,
	SpDefense,
	Speed,
	
	//Not Actual Stats, Used to Boost moveAccuracy
	Accuracy,
	Evasion
}

//Storing Type Advantages - Must be Same Order as enum Above
public class TypeChart
{
	static float[][] chart =
	{
		//                   NOR FIR WAT ELE GRA POI
		/*NOR*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f},
		/*FIR*/new float[] { 1f,0.5f,0.5f,1f,2f, 1f},
		/*WAT*/new float[] { 1f, 2f, 0.5f,2f,0.5f,1f},
		/*ELE*/new float[] { 1f, 1f, 2f, 0.5f,0.5f,1f},
		/*GRS*/new float[] { 1f,0.5f,2f, 2f, 0.5f,0.5f},
		/*POI*/new float[] { 1f, 1f, 1f, 1f, 2f, 1f},
	};

	public static float GetEffectiveness(AnimalType attackType, AnimalType defenseType)
	{
		if (attackType == AnimalType.None || defenseType == AnimalType.None)
			return 1;

		int row = (int)attackType - 1;
		int col = (int)defenseType - 1;
		
		return chart[row][col];
	}
}

