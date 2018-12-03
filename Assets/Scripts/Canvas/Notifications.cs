using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Notifications : MonoBehaviour {
    public int numLines = 8;
    Queue<string> lines;
    TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start() {
        lines = new Queue<string>();
        for (int i = 0; i < numLines - 1; i++)
            lines.Enqueue("");
        lines.Enqueue("Welcome to Cataclysm City!");

        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Post(string message) {
        lines.Dequeue();
        lines.Enqueue(message);
    }

    // Update is called once per frame
    void Update() {
        text.text = "";

        string[] lines = this.lines.ToArray();
        for (int i = 0; i < numLines; i++) {
            text.text += string.Format("<alpha=#{0:X}>", (int)(255 * Mathf.Lerp(0, 1, (i + 1) / (float)(numLines + 1)))) + lines[i] + "\n";
        }
    }
}
