using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    public event Action<bool> OnBattleOver;
    
    // Start is called before the first frame update
    public void StartBattle()
    {
        
    }

    // Update is called once per frame
    public void HandleUpdate()
    {
        OnBattleOver(true);
    }
}
