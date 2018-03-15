using UnityEngine;
using System.Collections;

public class Panel : MonoBehaviour {


    public Animator animator
    {
        get {
            var ret = GetComponent<Animator>();
            return ret;
        }
    }



}
