using UnityEngine;
using System.Collections;

public class PlayButton : VisibilityToggling
{
    protected override void DoShow()
    {
        gameObject.SetActive(true);
    }

    protected override void DoHide()
    {
        gameObject.SetActive(false);
    }

}

