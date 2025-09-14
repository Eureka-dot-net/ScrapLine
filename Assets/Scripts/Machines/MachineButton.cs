using UnityEngine;
using UnityEngine.UI;
using System;

public class MachineButton : MonoBehaviour
{
    public event Action<MachineDef, GameObject> OnButtonClicked;

    private MachineDef machineDef;
    private Button button;

    public void Init(MachineDef def)
    {
        machineDef = def;
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        OnButtonClicked?.Invoke(machineDef, this.gameObject);
    }
}