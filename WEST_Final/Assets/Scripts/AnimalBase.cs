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

