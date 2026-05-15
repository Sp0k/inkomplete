using System;
using TMPro;
using UnityEngine;

public class BlotCounterWidget : MonoBehaviour
{
	private const string k_CounterFormat = "{0} / {1}";

	[SerializeField]
	private TMP_Text m_CounterText;

	private int m_CachedTotal;


	public void InitCounterText(int current, int total)
	{
		m_CachedTotal = total;
		UpdateCurrentCount(current);
	}


	public void UpdateCurrentCount(int current)
	{
		SetCounterText(current, m_CachedTotal);
	}


	private void SetCounterText(int current, int total)
	{
		string txt = string.Format(k_CounterFormat, current, total);
		m_CounterText.text = txt;
	}
}
