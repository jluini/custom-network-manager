using UnityEngine;
using System.Collections;

public class LobbyPanel : VisibilityToggling
{

    protected override void DoShow()
    {
        //Debug.Log("SHOWING lobby panel");
        gameObject.SetActive(true);
    }

    protected override void DoHide()
    {
        //Debug.Log("HIDING  lobby panel");
        gameObject.SetActive(false);
    }

}

