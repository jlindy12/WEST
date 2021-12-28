using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AnimalParty : MonoBehaviour
{
   [SerializeField] List<Animal> animals;

   public List<Animal> Animals
   {
      get { return animals; }
   }

   private void Start()
   {
      foreach (var animal in animals)
      {
         animal.Init();
      }
   }

   public Animal GetHealthyAnimal()
   {
      return animals.Where(x => x.HP > 0).FirstOrDefault();
   }

   public void AddAnimal(Animal newAnimal)
   {
      if (animals.Count < 6)
      {
         animals.Add(newAnimal);
      }
      else
      {
         // TODO: Add to the PC
      }
   }
}
