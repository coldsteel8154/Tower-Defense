using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Path : MonoBehaviour
{
    public Transform[] point;
    public static Path path {  get; private set; }
    private void Awake()
    {
        path = this;
    }
}
