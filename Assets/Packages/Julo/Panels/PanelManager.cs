using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;



public class PanelManager : MonoBehaviour {

    public Panel initiallyOpen;

    protected Panel current;
    private int openParameterId;

    private GameObject previouslySelected;

    private const string openTransitionName = "Open";
    private const string closeTransitionName = "Close";

    public void OnEnable()
    {
        //We cache the Hash to the "Open" Parameter, so we can feed to Animator.SetBool.
        openParameterId = Animator.StringToHash (openTransitionName);

        if(initiallyOpen != null)
        {
            OpenPanel(initiallyOpen);
        }
    }

    public void OpenPanel(Panel panelToOpen)
    {
        if(panelToOpen == current)
        {
            return;
        }

        // activate this panel
        panelToOpen.gameObject.SetActive(true);

        // Save the currently selected button that was used to open this Screen. (CloseCurrent will modify it)
        GameObject newPreviouslySelected = EventSystem.current.currentSelectedGameObject;

        // move the panel to front
        panelToOpen.transform.SetAsLastSibling();

        CloseCurrent();

        previouslySelected = newPreviouslySelected;

        current = panelToOpen;

        if(current.animator)
        {
            current.animator.SetBool(openParameterId, true);
        }

        GameObject newSelected = FindFirstEnabledSelectable(panelToOpen.gameObject);

        SetSelected(newSelected);
    }

    private GameObject FindFirstEnabledSelectable(GameObject container)
    {
        GameObject ret = null;

        var selectables = container.GetComponentsInChildren<Selectable>(true);

        foreach(var selec in selectables)
        {
            if(selec.IsActive() && selec.IsInteractable())
            {
                ret = selec.gameObject;
                break;
            }
        }

        return ret;
    }

    public void CloseCurrent()
    {
        if(current == null)
        {
            return;
        }

        //start the close animation...
        if(current.animator)
        {
            current.animator.SetBool(openParameterId, false);
        }

        SetSelected(previouslySelected);

        Panel panelToClose = current;

        //StartCoroutine(DisablePanelDeleyed(panelToClose));

        current = null;

        //... 

        panelToClose.gameObject.SetActive(false);
    }

    private void SetSelected(GameObject newSelected)
    {
        EventSystem.current.SetSelectedGameObject(newSelected);

        var inputModule = EventSystem.current.currentInputModule as StandaloneInputModule;

        if(inputModule == null)
        { // is a pointer device
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
