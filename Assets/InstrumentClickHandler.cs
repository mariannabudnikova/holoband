using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class InstrumentClickHandler : MonoBehaviour, IInputClickHandler {

    public GameObject bandInfoBillboard;

    /// <summary>
    /// Show information about the band when musical instrument has been clicked
    /// </summary>
    /// <param name="eventData"></param>
    public void OnInputClicked(InputEventData eventData)
    {
        bandInfoBillboard.SetActive(true);
    }
}
