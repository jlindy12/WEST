using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlawController : MonoBehaviour
{
  [SerializeField] Dialog dialog;
  [SerializeField] GameObject exclamation;
  [SerializeField] GameObject fov;
  Character character;
  private void Awake()
  {
    character = GetComponent<Character>();
  }

  private void Start()
  {
    SetFovRotation(character.Animator.DefaultDirection);
  }
  public IEnumerator TriggerOutlawBattle(PlayerController player)
  {
    // Show Exclamation
    exclamation.SetActive(true);
    yield return new WaitForSeconds(0.5f);
    exclamation.SetActive(false);

    // Walk Towards Player
    var diff = player.transform.position - transform.position;
    var moveVec = diff - diff.normalized;
    moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));
    
    yield return character.Move(moveVec);
    
    // Show Dialog
    StartCoroutine (DialogManager.Instance.ShowDialog(dialog, () =>
    {
      Debug.Log("Starting Outlaw Battle");
    }));
  }

  public void SetFovRotation(FacingDirection dir)
  {
    float angle = 0f;
    if (dir == FacingDirection.Right)
      angle = 90f;
    else if (dir == FacingDirection.Up)
      angle = 180f;
    else if (dir == FacingDirection.Left)
      angle = 270f;

    fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
  }
}
