using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
	[SerializeField] Text nameText;
	[SerializeField] Text levelText;
	[SerializeField] Text statusText;
	[SerializeField] HPBar hpBar;

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
		levelText.text = "Lvl " + animal.Level;
		hpBar.SetHP((float) animal.HP / animal.MaxHP);

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

	public IEnumerator UpdateHP()
	{
		if (_animal.HpChanged)
		{
			yield return hpBar.SetHPSmooth((float)_animal.HP / _animal.MaxHP);
			_animal.HpChanged = false;
		}
	}
}
