using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Main
{
    public static Main()
    {
        string path = "dialogue_ex.txt";
        List<string> lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).Select(line => line.Trim()).ToList();
        Node root = (new Parser(lines)).ParseFull();
        DialogueEngine engine = new DialogueEngine(root);

        EngineNode node = engine.GetNext();
        while (node != null) // or (node.Type != DialogueType.end)
        {
            Console.WriteLine($"type: {node.Type} | name: {node.Name} | content: {node.Content} | choices: {String.Join(" ; ", node.Choices.ToArray())}");
            if (node.Type == DialogueType.option)
            {
                engine.SelectOption(0); // select first option
            }
            node = engine.GetNext();
        }
    }
}
