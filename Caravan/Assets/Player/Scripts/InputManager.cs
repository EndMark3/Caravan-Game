using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager main;

    [System.Serializable]
    public class Button
    {
        public string name;
        public string mainKey;
        public List<string> extraKeys;

        public int sharedWith = -1;

        public bool customBuffer;
        public float customBufferLength;
        public float bufferLength;

        public bool currentlyPressed;
        public List<float> bufferedPresses;
        public List<float> bufferedUnpresses;
    }

    public float defaultBufferLength;
    public bool mainKeyHasToBePressedLast;

    public List<Button> buttons;
    public static Dictionary<string, int> pointers = new Dictionary<string, int>();

    void Start()
    {
        main = this;
        applyHack();
    }

    void Update()
    {
        if (!main || pointers == null)
        { Start(); }

        foreach (Button b in buttons)
        {
            if (b.sharedWith > -1)
            { continue; }

            for (int i = b.bufferedPresses.Count - 1; i > -1; i--)
            {
                b.bufferedPresses[i] += Time.deltaTime;
                if (b.bufferedPresses[i] > b.bufferLength)
                { b.bufferedPresses.Remove(b.bufferedPresses[i]); }
            }
            for (int i = b.bufferedUnpresses.Count - 1; i > -1; i--)
            {
                b.bufferedUnpresses[i] += Time.deltaTime;
                if (b.bufferedUnpresses[i] > b.bufferLength)
                { b.bufferedUnpresses.Remove(b.bufferedUnpresses[i]); }
            }

            bool pressed = Input.GetKeyDown(b.mainKey) || (Input.GetKey(b.mainKey) && (!mainKeyHasToBePressedLast || b.currentlyPressed));
            if (pressed)
            {
                for (int i = 0; i < b.extraKeys.Count; i++)
                { if (!Input.GetKey(b.extraKeys[i])) { pressed = false; break; } }
            } 

            if (pressed && !b.currentlyPressed)
            { b.bufferedPresses.Insert(0, 0); }
            else if (!pressed && b.currentlyPressed)
            { b.bufferedUnpresses.Insert(0, 0); }

            b.currentlyPressed = pressed;
        }
    }

    static bool CheckHack()
    {
        if (pointers == null)
        {
            if (main)
            {
                applyHack();
                return true;
            }
            return false;
        }
        return true;
    }

    static void applyHack()
    {
        pointers = new Dictionary<string, int>();

        for (int i = 0; i < main.buttons.Count; i++)
        {
            int shared = main.buttons[i].sharedWith;
            int toAdd = i;

            while (shared > -1 && shared < main.buttons.Count)
            {
                toAdd = shared;
                shared = main.buttons[shared].sharedWith;
            }

            pointers.Add(main.buttons[i].name, toAdd);
        }
    }

    public static bool GetButtonDown(string button, bool removeIfTrue = true)
    {
        if (!CheckHack())
        { return false; }

        if (pointers.ContainsKey(button))
        {
            Button b = main.buttons[pointers[button]];
            int count = b.bufferedPresses.Count;
            if (count > 0)
            {
                if (removeIfTrue)
                { b.bufferedPresses.Remove(b.bufferedPresses[count - 1]); }
                return true;
            }
        }

        return false;
    }

    public static bool GetButtonUp(string button, bool removeIfTrue = true)
    {
        if (!CheckHack())
        { return false; }

        if (pointers.ContainsKey(button))
        {
            Button b = main.buttons[pointers[button]];
            int count = b.bufferedUnpresses.Count;
            if (count > 0)
            {
                if (removeIfTrue)
                { b.bufferedUnpresses.Remove(b.bufferedUnpresses[count - 1]); }
                return true;
            }
        }

        return false;
    }

    public static bool GetButton(string button)
    {
        if (!CheckHack())
        { return false; }

        if (pointers.ContainsKey(button))
        {
            Button b = main.buttons[pointers[button]];
            return b.currentlyPressed;
        }

        return false;
    }
}
