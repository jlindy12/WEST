using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
   [SerializeField] List<Animal> wildAnimals;

   public Animal GetRandomWildAnimal()
   {
      var wildAnimal = wildAnimals[Random.Range(0, wildAnimals.Count)];
      wildAnimal.Init();
      return wildAnimal;
   }
}
