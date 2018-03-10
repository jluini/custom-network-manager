using UnityEngine;
using System.Collections;

public class MainMenuPanel : VisibilityToggling
{
    
    protected override void DoShow()
    {
        //Debug.Log("SHOWING main menu");
        gameObject.SetActive(true);
    }

    protected override void DoHide()
    {
        //Debug.Log("HIDING  main menu");
        gameObject.SetActive(false);
    }

}

