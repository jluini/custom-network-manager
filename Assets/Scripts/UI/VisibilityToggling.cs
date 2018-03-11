using System.Collections;

using UnityEngine;

public class VisibilityToggling : MonoBehaviour
{
    public bool isVisible;

    public void Show()
    {
        if(!isVisible)
        {
            isVisible = true;
            DoShow();
        }
        else
        {
            Debug.LogWarning("Already shown");
        }
    }

    public void Hide() {
        if(isVisible)
        {
            isVisible = false;
            DoHide();
        }
        else
        {
            Debug.LogWarning("Already hidden");
        }
    }

    protected virtual void DoShow() { gameObject.SetActive(true); }
    protected virtual void DoHide() { gameObject.SetActive(false); }

}

