using ChebsNecromancy.Options;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands;

public class ShowOptions : ConsoleCommand
{
    public override string Name => "chebgonaz_options";

    public override string Help => "Shows mod options.";

    public override void Run(string[] args)
    {
        OptionsGUI.TogglePanel();
    }
}