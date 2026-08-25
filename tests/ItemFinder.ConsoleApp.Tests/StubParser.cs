using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.ConsoleApp.Tests;

/// <summary>Parser stub returning a fixed result regardless of input.</summary>
public sealed class StubParser(ParseResult result) : IDataFileParser
{
    public ParseResult ParseFile(string path) => result;

    public ParseResult ParseText(string text) => result;

    public static StubParser WithSampleForest()
    {
        var walk = new DirectionNode("Walk to the end of the hall.");

        var turnLeft = new DirectionNode("Turn left.");
        var firstDoor = new DirectionNode("Go through the first door on the right.");
        var cabinetLeft = new DirectionNode("Open the cabinet on the left.");
        cabinetLeft.AddChild(new ItemNode("Cookies"));
        var cabinetSink = new DirectionNode("Open the cabinet above the sink.");
        cabinetSink.AddChild(new ItemNode("Coffee Mug"));
        var fridge = new DirectionNode("Open the refridgerator.");
        fridge.AddChild(new ItemNode("Milk"));
        firstDoor.AddChild(cabinetLeft);
        firstDoor.AddChild(cabinetSink);
        firstDoor.AddChild(fridge);
        turnLeft.AddChild(firstDoor);

        var turnRight = new DirectionNode("Turn right.");
        var endDoor = new DirectionNode("Go through the door at the end of the hall.");
        var desk = new DirectionNode("Look on top of the desk.");
        desk.AddChild(new ItemNode("Mobile Phone"));
        var drawer = new DirectionNode("Open the desk drawer.");
        drawer.AddChild(new ItemNode("Pencils"));
        endDoor.AddChild(desk);
        endDoor.AddChild(drawer);
        turnRight.AddChild(endDoor);

        walk.AddChild(turnLeft);
        walk.AddChild(turnRight);
        return new StubParser(ParseResult.Ok(new DirectionForest([walk])));
    }
}