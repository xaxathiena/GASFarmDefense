using UnityEditor;
using UnityEngine;

public class TestMenu
{
    [MenuItem("AAA_TEST/Say Hello")]
    public static void SayHello()
    {
        Debug.Log("Hello from Test Menu!");
    }
}
