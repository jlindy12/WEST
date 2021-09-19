using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition
{ 
    public ConditionID Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string StartMessage { get; set; }
    
    public Action<Animal> OnStart { get; set; }
    
    public Action<Animal> OnAfterTurn { get; set; }
    public Func<Animal, bool> OnBeforeMove { get; set; }
}
