using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{ 
    public float speed;
    private Rigidbody2D myRigidbody;
    private Vector3 change;
    private Animator animator;
    public VectorValue startingPosition;
    private bool timeToUpdateAnimationAndMove;
    
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody2D>();
        transform.position = startingPosition.initialValue;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        change = Vector3.zero;
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            change.x = Input.GetAxisRaw("Horizontal");
        }
        else if (Input.GetAxisRaw("Vertical") != 0)
        {
            change.y = Input.GetAxisRaw("Vertical");
        }
        UpdateAnimationAndMove();
    }
    void UpdateAnimationAndMove()
    {
        if (change != Vector3.zero)
        {
            MoveCharacter();
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
        }
        else
        {
            animator.SetBool("moving", false);
        }
    }
    void MoveCharacter()
    {
        change.Normalize();
        myRigidbody.MovePosition(transform.position + change * speed * Time.deltaTime);
    }
}
