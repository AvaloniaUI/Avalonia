namespace Perspex.Input
{
    public static class ModifierKeysExtensions
    {
        public static bool IsPressed(this ModifierKeys baseModifier, ModifierKeys toCheck)
        {
            return (baseModifier & toCheck) != 0;
        }
    }
}