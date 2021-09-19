using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector2 input;
	private Character character;
	public event Action OnEncountered;
	public event Action<Collider2D> OnEnterTrainersView;
    private bool isTurning;
    private float stopMovingTime;
    private float time;
    
    private void Awake()
    {
		character = GetComponent<Character>();
    }

    public void HandleUpdate()
    {
        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            //remove diagonal movement
            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                if (((character.Animator.MoveX != input.x || character.Animator.MoveY != input.y) && !isTurning) &&
                    Time.time - stopMovingTime > 0.1f)
                {
                    time = Time.time;
                    StartCoroutine(Turn(new Vector2(input.x, input.y)));
                }
                if (!isTurning)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        character.moveSpeed = 8f;
                    }
                    else character.moveSpeed = 5f;
                    StartCoroutine(character.Move(input, OnMoveOver)); 
                }
            }
        }

		character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Space))
            Interact();
    }

    IEnumerator Turn(Vector2 turnPos)
    {
        isTurning = true;
        while (Time.time - time < 0.1f)
        {
            character.Animator.MoveX = turnPos.x;
            character.Animator.MoveY = turnPos.y;
            yield return null;
        }
        isTurning = false;
    }
    void Interact()
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer);
        if (collider != null)
        {
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

	private void OnMoveOver()
	{
		CheckForEncounters();
		CheckIfInTrainersView();
	}

    private void CheckForEncounters()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.GrassLayer) != null)
        {
            if (UnityEngine.Random.Range(1, 101) <= 10)
            {
                character.Animator.IsMoving = false;
				OnEncountered();
            }
        }
    }

	private void CheckIfInTrainersView()
	{
		var collider = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.FovLayer);
		if (collider != null)
		{
			character.Animator.IsMoving = false;
			OnEnterTrainersView?.Invoke(collider);
		}
	}
}

