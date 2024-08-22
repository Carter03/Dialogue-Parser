# C# Dialogue Parser

Easy to use API for reading dialogue from text files in custom dialogue format.\
Support for:
* Unlimited different speaker names
* Scene names/IDs
* Options for dialogue thinking vs. speaking
* Abrupt (mid-file) endings
* Player choices
* Storage of past choices for automatic future path-splits.

# Dialogue Language (File Structure)

Dialogue Parser reads from .txt files written in a specific format.\
Text files should be written in the following structure:

	<keyword><argument> dialogue text
	
The only permitted arguments are \<\~\> and \<1\> (or \<2\>, \<3\>, ...)\
<~> is the *thinking* indicator.\
<[int]> is the option selection argument.\
All other lines should omit the argument.

Dialogue Parser has ~**six keywords**:
|keyword|meaning|ex|
|--|--|--|
|scene| event identifier | \<scene\> foobarSing_01
|END| end dialogue sequence | \<END\>
|option| player choice | \<option\> OP1<br/>&emsp;\<1\> this is option one<br/>&emsp;<2> this is option two<br/></>
|choice| automatic branching due<br/>to prior option selection| \<choice\><br/>&emsp;\<OP1\>\<1\><br/>&emsp;&emsp;...<br/>&emsp;<//><br/>&emsp;<OP1\>\<2\><br/>&emsp;&emsp;...<br/>&emsp;<//><br/></> |
|/| option / choice end | (see above)
|//| path branch end | (see above)

Anything in <> that is not a keyword *and* is not a previously defined option name will be interpreted as a player name.\
Ex: `<foobario> Hi! I'm Foobario.`

## Example Dialogue

This example dialogue is annotated with "//" comments.\
**NOTE:** comments are *not* supported in the parser. These are for illustrative purposes only. Adding them with produce errors or unexpected behavior.

    <scene>						// first line is ignored (still must follow syntax)
    <scene> openingScene				// stores scene name as string
    <mainCharacter><~> Who is this guy?			// thinking
    <foo> Hey there! How are you?			// foo is a person
    <option> OP1					// option stored as OP1 (could be anything except intended person name)
        <1> Not good :(					// player option 1
        <2> Great!					// player option 2
    </>							// option must be closed with </>
    <choices>						// path splits here
        <OP1><1>					// if player chose option 1 in OP1:
            <foo> Oh no!				// path continues here
	        <foo> Anyway...
        <//>						// path split must be closed with <//>
        <OP1><2>					// if player chose option 2:
            <foo> Good to hear.
            <END>					// abruptly end (will not continue)
        <//>
    </>							// choices block must be closed with </>
    <mainCharacter> Hey! Not nice!			// all paths resume here (unless ENDed)
    <END>

# Additional Information
## Choice Operators / Composition

To require multiple options to have been chosen, you must redeclare \<choices\>.\
(Note the \</\> and \<//\> placements)

    <choices>
	    <OP1><1>
		    <choices>
			    <OP2><2>
				    <foo> The player chose option 1 of OP1 AND option 2 of OP2.
				<//>
			</>
		<//>
	</>

To require for one option OR another option to have been chosen, there is not a clean method.\
One way is to duplicate the \<choices\> block:

    <choices>
	    <OP1><1>
			<foo> The player chose option 1 of OP1.
		<//>
	</>
	<choices>
	    <OP2><2>
			<foo> The player chose option 2 of OP2.
		<//>
	</>

## Notes

* Whitespace is not permitted. You must remove excess newlines and trim the lines before parsing.
* If something does not work as expected, check the file syntax. Likely, you used \</\> instead of \<//\> or omitted one entirely.
* \<scene\> has no functionality besides to function as time event name retrieval. You will have to handle the different scene names you declare yourself, should you choose to use them.

<br/>

    <choices>
        <OP1><2>
    	    <foo> you chose option 1 of OP1
    	<//>
    	<OP2><1>
    		<foo> you chose option 2 of OP2
    	<//>
    </>
* Within a \<choices\> block, non-selected option specifiers are ignored. For example, if the player chose option 1 of OP1 and option 2 of OP2, the above \<choices\> block would be skipped entirely.

<br/>

    <choices>
        <OP1><1>
    	    <foo> you chose option 1 of OP1
    	<//>
    	<OP2><2>
    		<foo> you chose option 2 of OP2
    	<//>
    </>
* If the player chose option 1 of OP1 **and** option 2 of OP2, only \<OP1\>\<1\> will be executed (since it is first in the list). To ensure both are executed, use two \<choices\> blocks:

       <choices>
	       <OP1><1>
	           <foo> you chose option 1 of OP1
        	 <//>
       </>
       <choices>
        	 <OP2><2>
        		  <foo> you chose option 2 of OP2
        	 <//>
       </>


# Dialogue Parser API (C#)

## Creating a DialogueEngine
To interact with your dialogue, you must create a DialogueEngine from the data. It is fairly simple:


First, create a List\<string\> of the lines in your dialogue .txt file, ensuring whitespace is removed . For example,

    List<string> lines  =  File.ReadAllLines("dialogue.txt").Where(l  =>  !string.IsNullOrWhiteSpace(l)).Select(line  =>  line.Trim()).ToList();

Then, retrieve the root node of the parsed lines:

    Node  root  =  (new Parser(lines)).ParseFull();

Finally, create the DialogueEngine from the root node:

    engine  =  new  DialogueEngine(root);

You can combine these steps into one line, if you'd like :)

## Using the Engine

There are only two DialogueEngine methods:
* GetNext()
* SelectOption(optionNum)


### GetNext()
	
    EngineNode node = engine.GetNext()
	
Whenever you want the next line of dialogue, call GetNext.\
This returns an EngineNode (not to be confused with Node, which was only used when creating the engine).

EngineNode simply stores the following public fields:
* DialogueType *Type*
* string *Name*
* string *Content*
* List\<string\> *Choices*

Type is defined for every EngineNode, and it is the keyword used in the dialogue.txt file.

What is contained in *Name*, *Content*, and *Choices* depends on Type:
|Stored Line| node .Type | node .Name | node .Content | node .Choices
|--|--|--|--|--|
| \<scene\> sceneName | DialogueType.scene | "sceneName" | "" | {} |
| \<foobario\> I'm Foobario | DialogueType.person_say | "foobario" | "I'm Foobario" | {} |
| \<foobario\>\<~\> I'm Foobario | DialogueType.person_think | "foobario" | "I'm Foobario" | {} |
| \<option\> OP1<br/>&emsp;<1> option one<br/>&emsp;<2> option two<br/></> | DialogueType.option | "" | "" | {"option one", "option two"} |
| \<END\> | DialogueType.end | "" | "" | {} |
| \<choice\><br/>&emsp;\<OP1\>\<1\><br/>&emsp;&emsp;...<br/>&emsp;<//><br/>&emsp;<OP1\>\<2\><br/>&emsp;&emsp;...<br/>&emsp;<//><br/></>  | [choice never appears as an EngineNode. It is replaced with the first node of the appropriate conditional path] | <---- | <---- | <---- |


### SelectOption()

If the retrieved EngineNode from GetNext() has type DialogueType.option, you must call SelectOption() before calling GetNext() again.

For example:

    node = engine.GetNext()
    if (node.Type == DialogueType.option)
    {
	    engine.SelectOption(optionNum);
	}

In this case, option number optionNum is selected and stored.

**Notes:**
* optionNum should be zero-based. That is, to select option \<1\>, call SelectOption(0)
* Calling SelectOption() when the current node is not an \<option\> will do nothing
* Calling GetNext() without selecting an option will raise an error.
* Calling SelectOption() with an argument greater than Choices.Count - 1 will produce an index error.

If the given EngineNode is of type DialogueType.option, its Choices field will be populated with the options. These are strings that may be displayed however you'd like.

Once you call SelectOption(), that option value will be stored internally. Whenever a \<choices\> block is reached thereafter, GetNext() will automatically return the node of the branch corresponding to the given options selected.

Example:

    <scene>
    <scene> openingScene
    <mainCharacter><~> Who is this guy?
    <foo> Hey there! How are you?	
    <option> OP1						
        <1> Not good :(					
        <2> Great!							
    </>							
    <choices>							
        <OP1><1>					
            <foo> Oh no!				
         <foo> Anyway...
        <//>						
        <OP1><2>							
            <foo> Good to hear.
            <END>							
        <//>
    </>									
    <mainCharacter> Hey! Not nice!
    <END>

Here is an example script that always chooses the first option:

    node = engine.GetNext();
    while (node != null)
    {
		Console.WriteLine($"type: {node.Type} | name: {node.Name} | content: {node.Content}");
		if (node.Type == DialogueType.option)
		{
			engine.SelectOption(0); // will always choose first option
			Console.WriteLine($"\tchoices: {String.Join(" ; ", node.Choices.ToArray())}");
		}
		node = engine.GetNext();
    }
Output:

    type: scene | name: openingScene | content: 
    type: person_think | name: mainCharacter | content: Who is this guy?
    type: person_say | name: foo | content: Hey there! How are you?
    type: option | name:  | content: 
        choices: Note good :(; Great!;
    type: person_say | name: foo | content: Good to hear.
    type: end | name:  | content: 
