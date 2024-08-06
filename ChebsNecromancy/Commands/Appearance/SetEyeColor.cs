using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.PvP;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands.Appearance
{
    public class SetEyeColor : ConsoleCommand
    {
        public override string Name => "chebgonaz_seteyecolor";

        public override string Help => "Sets the eye color of your undead.\n" +
                                       $"Usage: {Name} [{ColorsAsString()}]\n" +
                                       $"eg. {Name} Red";
        
        private static string ColorsAsString()
        {
            var values = Enum.GetValues(typeof(UndeadMinion.EyeColor)).Cast<UndeadMinion.EyeColor>();
            return string.Join("|", values);
        }
        
        private static Dictionary<string, UndeadMinion.EyeColor> _eyeColorLookup;

        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }
            
            if (_eyeColorLookup == null)
            {
                _eyeColorLookup = new Dictionary<string, UndeadMinion.EyeColor>();
                foreach (UndeadMinion.EyeColor eyeColor in Enum.GetValues(typeof(UndeadMinion.EyeColor)))
                {
                    var key = eyeColor.ToString().ToLower();
                    _eyeColorLookup.Add(key, eyeColor);
                }
            }

            if (!_eyeColorLookup.TryGetValue(args[0].ToLower(), out var chosenColor))
            {
                Console.instance.Print($"Invalid type: {args[0]}. Valid options: {ColorsAsString()}");
                return;
            }

            UndeadMinion.SetEyeColor(chosenColor);
        }

        public override List<string> CommandOptionList()
        {
            var options = Enum.GetValues(typeof(UndeadMinion.EyeColor))
                .Cast<UndeadMinion.EyeColor>()
                .Select(o =>$"{o}")
                .ToList();
            return options;
        }
    }
}
