using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleHUD : MonoBehaviour
{
	[SerializeField] Text nameText;
	[SerializeField] Text levelText;
	[SerializeField] Text statusText;
	[SerializeField] HPBar hpBar;
	[SerializeField] GameObject expBar;

	[SerializeField] Color psnColor;
	[SerializeField] Color parColor;
	[SerializeField] Color frzColor;
	[SerializeField] Color slpColor;
	[SerializeField] Color brnColor;

	Animal _animal;
	Dictionary<ConditionID, Color> statusColors;

	public void SetData(Animal animal)
 	{
		_animal = animal;

		nameText.text = animal.Base.Name;
		SetLevel();
		hpBar.SetHP((float) animal.HP / animal.MaxHP);
		SetExp();

		statusColors = new Dictionary<ConditionID, Color>()
		{
			{ConditionID.psn, psnColor },
			{ConditionID.brn, brnColor },
			{ConditionID.slp, slpColor },
			{ConditionID.frz, frzColor },
			{ConditionID.par, parColor },
		};

		SetStatusText();
		_animal.OnStatusChanged += SetStatusText;
 	}

	void SetStatusText()
	{
		if(_animal.Status == null)
		{
			statusText.text = "";
		}
		else
		{
			statusText.text = _animal.Status.Id.ToString().ToUpper();
			statusText.color = statusColors[_animal.Status.Id];
		}
	}

	public void SetLevel()
	{
		levelText.text = "Lvl " + _animal.Level;
	}

	public void SetExp()
	{
		if (expBar == null) return;

		float normalizedExp = GetNormalizedExp();
		expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
	}

	public IEnumerator SetExpSmooth(bool reset=false)
	{
		if (expBar == null) yield break;

		if (reset)
			expBar.transform.localScale = new Vector3(0, 1, 1);

		float normalizedExp = GetNormalizedExp();
		yield return expBar.transform.DOScaleX(normalizedExp, 1f).WaitForCompletion();
	}

	float GetNormalizedExp()
	{
		int currLevelExp = _animal.Base.GetExpForLevel(_animal.Level);
		int nextLevelExp = _animal.Base.GetExpForLevel(_animal.Level + 1);

		float normalizedExp = (float)(_animal.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
		return Mathf.Clamp01(normalizedExp);
	}

	public IEnumerator UpdateHP()
	{
		if (_animal.HpChanged)
		{
			yield return hpBar.SetHPSmooth((float)_animal.HP / _animal.MaxHP);
			_animal.HpChanged = false;
		}
	}
}
