using UnityEngine;

public class ControlsDisplayWidget : MonoBehaviour
{
	[Header("UI Elements")]
	[SerializeField] private GameObject _toggleDrawer;


	public void ToggleDrawer()
	{
		_toggleDrawer.SetActive(!_toggleDrawer.activeSelf);
	}
}
