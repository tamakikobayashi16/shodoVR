using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    private float minX = float.MaxValue;
    private float maxX = float.MinValue;
    private float minY = float.MaxValue;
    private float maxY = float.MinValue;

    void Update()
    {
        if (Pen.current == null)
            return;

        Vector2 pos = Pen.current.position.ReadValue();

        minX = Mathf.Min(minX, pos.x);
        maxX = Mathf.Max(maxX, pos.x);
        minY = Mathf.Min(minY, pos.y);
        maxY = Mathf.Max(maxY, pos.y);

        Debug.Log(
            $"X:{minX:F0}Å`{maxX:F0}  Y:{minY:F0}Å`{maxY:F0}");

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            
            Debug.Log($"pos = {pos}");
        }
    }
}