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
    }

    public void Hide() {
        if(isVisible)
        {
            isVisible = false;
            DoHide();
        }
    }

    protected virtual void DoShow() { gameObject.SetActive(true); }
    protected virtual void DoHide() { gameObject.SetActive(false); }

}

