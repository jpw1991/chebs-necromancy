using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.PvP;
using Jotunn.Entities;

namespace ChebsNecromancy.Commands.Appearance
{
    public class SetBoneColor : ConsoleCommand
    {
        public override string Name => "chebgonaz_setbonecolor";

        public override string Help => "Sets the color of your skeletons.\n" +
                                       $"Usage: {Name} [{ColorsAsString()}]\n" +
                                       $"eg. {Name} Red";
        
        private static string ColorsAsString()
        {
            var values = Enum.GetValues(typeof(SkeletonMinion.BoneColor)).Cast<SkeletonMinion.BoneColor>();
            return string.Join("|", values);
        }
        
        private static Dictionary<string, SkeletonMinion.BoneColor> _boneColorLookup;

        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }
            
            if (_boneColorLookup == null)
            {
                _boneColorLookup = new Dictionary<string, SkeletonMinion.BoneColor>();
                foreach (SkeletonMinion.BoneColor boneColor in Enum.GetValues(typeof(SkeletonMinion.BoneColor)))
                {
                    var key = boneColor.ToString().ToLower();
                    _boneColorLookup.Add(key, boneColor);
                }
            }

            if (!_boneColorLookup.TryGetValue(args[0].ToLower(), out var chosenColor))
            {
                Console.instance.Print($"Invalid type: {args[0]}. Valid options: {ColorsAsString()}");
                return;
            }
            
            SkeletonMinion.SetBoneColor(chosenColor);
        }

        public override List<string> CommandOptionList()
        {
            var options = Enum.GetValues(typeof(SkeletonMinion.BoneColor))
                .Cast<SkeletonMinion.BoneColor>()
                .Select(o =>$"{o}")
                .ToList();
            return options;
        }
    }
}
