using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class Parsing
{
    public static Parsing(string path)
    {
        List<string> lines = File.ReadAllLines(path).Select(line => line.Trim()).ToList();
        Parser parser = new Parser(lines);
        Node root = parser.ParseFull();
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            node = engine.GetNext();
            Debug.Log($"type: {node.Type} | name: {node.Name} | content: {node.Content} | choices: {String.Join(" ; ", node.Choices.ToArray())}");
        }
        if (node != null && node.Type == DialogueType.option)
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                engine.SelectOption(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                engine.SelectOption(2);
            }
        }
    }
}

public class DialogueEngine
{
    public Node currentNode;

    private bool inOption;
    private string currentOption;
    private Dictionary<string, int> options = new Dictionary<string, int>();

    private Dictionary<string, int> GetAllOptionNames(Node root)
    {
        Dictionary<string, int> optionNames = new Dictionary<string, int>();

        void Traverse(Node node)
        {
            if (node == null) return;

            if (node.Type == "option" && !optionNames.ContainsKey(node.Name))
            {
                optionNames.Add(node.Name, -1);
            }

            foreach (var nextNode in node.Next)
            {
                Traverse(nextNode);
            }
        }

        Traverse(root);
        return optionNames;
    }

    public DialogueEngine(Node root)
    {
        currentNode = root;
        options = GetAllOptionNames(root);
        
    }

    public EngineNode GetNext()
    {
        if (inOption) throw new Exception("Must SelectOption() before getting next node");
        if (currentNode.Next.Count == 0) return null;
      
        currentNode = currentNode.Next[0];
        EngineNode engineNode = CraftNode(ref currentNode);

        return engineNode;
    }

    EngineNode CraftNode(ref Node currentNode)
    {
        EngineNode returnNode = new EngineNode();

        switch (currentNode.Type)
        {
            case "scene":
                returnNode.Type = DialogueType.scene;
                returnNode.Name = currentNode.Content;
                break;
            case "end":
                returnNode.Type = DialogueType.end;
                break;
            case "choice":
                List<Tuple<string, int>> choices = currentNode.Content.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split(','))
                                                        .Select(subParts => Tuple.Create(subParts[0].Trim(), int.Parse(subParts[1].Trim()))).ToList();
                Node through = currentNode.Next.Last();
                foreach (Tuple<string, int> choice in choices)
                {
                    if (options[choice.Item1] == choice.Item2)
                    {
                        through = currentNode.Next[choice.Item2-1];
                        break;
                    }
                }
                currentNode = through;
                return CraftNode(ref currentNode);
            case "option":
                returnNode.Type = DialogueType.option;
                returnNode.Choices = currentNode.Content.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(op => op.Trim()).ToList();
                currentOption = currentNode.Name;
                inOption = true;
                break;
            case "person_say":
                returnNode.Type = DialogueType.person_say;
                returnNode.Name = currentNode.Name;
                returnNode.Content = currentNode.Content;
                break;
            case "person_think":
                returnNode.Type = DialogueType.person_think;
                returnNode.Name = currentNode.Name;
                returnNode.Content = currentNode.Content;
                break;
        }
        return returnNode;
    }

    public void SelectOption(int pos)
    {
        if (!inOption) return;

        options[currentOption] = pos;
        inOption = false;
    }
}

public enum DialogueType
{
    scene,
    option,
    end,
    choice,
    person_say,
    person_think
}

public class EngineNode
{
    public DialogueType Type;
    public string Name = "";
    public string Content = "";
    public List<string> Choices = new List<string>();
}

public class Node
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public List<Node> Next { get; set; }

    public Node()
    {
        Type = string.Empty;
        Name = string.Empty;
        Content = string.Empty;
        Next = new List<Node>();
    }

    public override string ToString()
    {
        return $"{Type} | {Name} | {Content} | {Next.Count}";
    }
}

class Parser
{
    private List<string> lines;
    private List<string> optionNames;
    private int currentLine;
    private List<Node> looseNodes;

    public Parser(List<string> lines)
    {
        this.lines = lines;
        this.optionNames = new List<string>();
        this.currentLine = 0;
        this.looseNodes = new List<Node>();
    }

    private List<string> GetIds(string line)
    {
        var ids = Regex.Split(line, @"<|>").Where((_, index) => index % 2 != 0).ToList();
        return ids;
    }

    private string GetContent(string line)
    {
        return line.Substring(line.LastIndexOf('>') + 1).Trim();
    }

    public Node ParseFull()
    {
        Node root = Parse();

        Node FixNodes(Node node)
        {
            if (node == null) return null;

            int i = 0;
            while (i < node.Next.Count)
            {
                Node nextNode = node.Next[i];
                if (string.IsNullOrEmpty(nextNode.Type) || nextNode.Type == "dead")
                {
                    node.Next.RemoveAt(i);
                    node.Next.InsertRange(i, nextNode.Next);
                }
                else
                {
                    FixNodes(nextNode);
                    i++;
                }
            }

            return node;
        }

        return FixNodes(root);
    }

    private Node Parse()
    {
        Node currentNode = new Node();
        Node root = currentNode;
        int looseTarget = -1;

        while (currentLine < lines.Count)
        {
            string line = lines[currentLine];
            List<string> ids = GetIds(line);

            if (ids.Count == 0) break;

            if (ids.Count == 1)
            {
                switch (ids[0])
                {
                    case "scene":
                        currentNode.Type = "scene";
                        currentNode.Content = GetContent(line);
                        break;
                    case "option":
                        currentNode.Type = "option";
                        currentNode.Name = GetContent(line);
                        currentNode.Content = ParseOption();
                        optionNames.Add(currentNode.Name);
                        break;
                    case "END":
                        currentNode.Type = "end";
                        return root;
                    case "choices":
                        currentLine++;
                        currentNode.Type = "choice";
                        (int looseTargetDec, List<List<string>> options) = GetChoiceEnd();
                        looseTarget = looseTargetDec;
                        currentNode.Content = string.Join("; ", options.Select(opt => string.Join(",", opt)));
                        foreach (var _ in options)
                        {
                            Node x = Parse();
                            currentNode.Next.Add(x);
                        }
                        break;
                    case "//":
                        currentLine++;
                        currentNode.Type = "dead";
                        looseNodes.Add(currentNode);
                        return root;
                    case "/":
                        break;
                    default:
                        currentNode.Type = "person_say";
                        currentNode.Name = ids[0];
                        currentNode.Content = GetContent(line);
                        break;
                }
            }
            else
            {
                if (optionNames.Contains(ids[0]))
                {
                    currentLine++;
                    continue;
                }
                currentNode.Type = "person_think";
                currentNode.Name = ids[0];
                currentNode.Content = GetContent(line);
            }

            if (currentLine == looseTarget)
            {
                foreach (var end in looseNodes)
                {
                    end.Next.Add(currentNode);
                }
            }

            Node nextNode = new Node();
            currentNode.Next.Add(nextNode);

            currentNode = nextNode;
            currentLine++;
        }

        return root;
    }

    private string ParseOption()
    {
        currentLine++;

        string content = "";
        while (GetIds(lines[currentLine])[0] != "/")
        {
            content += GetContent(lines[currentLine]) + "; ";
            currentLine++;
        }
        return content;
    }

    private (int, List<List<string>>) GetChoiceEnd()
    {
        int tracker = currentLine;
        List<List<string>> options = new List<List<string>>();
        options.Add(GetIds(lines[tracker]));
        int inOption = 1;
        int blockLevel = 1;

        while (blockLevel != 0)
        {
            tracker++;
            string lineId = GetIds(lines[tracker]).First();

            if (lineId == "choices" || lineId == "option")
                blockLevel++;
            else if (lineId == "/")
                blockLevel--;

            if (optionNames.Contains(lineId))
            {
                inOption++;
                if (inOption == 1)
                    options.Add(GetIds(lines[tracker]));
            }
                
            if (lineId == "//")
                inOption--;
        }

        return (tracker + 1, options);
    }
}
